using System.Text.Json;
using MarketingCampus.Core.Campaigns;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;

namespace MarketingCampus.Providers.MongoDb;

public sealed class MongoCampaignDraftRepository : ICampaignDraftRepository
{
    public const string CollectionName = "marketingCampaigns";
    public const int SchemaVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IMongoCollection<BsonDocument> collection;

    public MongoCampaignDraftRepository(IMongoDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        collection = database.GetCollection<BsonDocument>(CollectionName);
    }

    public async Task<CampaignDraft?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var document = await collection.Find(IdFilter(id)).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : Read(document);
    }

    public async Task<IReadOnlyList<CampaignDraft>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var documents = await collection.Find(FilterDefinition<BsonDocument>.Empty)
            .Sort(Builders<BsonDocument>.Sort.Ascending("name").Ascending("_id"))
            .ToListAsync(cancellationToken);
        return documents.Select(Read).ToArray();
    }

    public async Task<CampaignDraft> SaveAsync(CampaignDraft draft, CancellationToken cancellationToken = default)
    {
        var errors = CampaignValidation.ValidateDraft(draft);
        if (errors.Count > 0) throw new CampaignValidationException(errors);
        var saved = draft with { Revision = draft.Revision + 1 };
        var document = Write(saved);
        if (draft.Revision == 0)
        {
            try { await collection.InsertOneAsync(document, cancellationToken: cancellationToken); }
            catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
            { throw new CampaignConflictException(draft.Id); }
        }
        else
        {
            var filter = IdFilter(draft.Id) & Builders<BsonDocument>.Filter.Eq("revision", draft.Revision) &
                Builders<BsonDocument>.Filter.Eq("schemaVersion", SchemaVersion);
            var result = await collection.ReplaceOneAsync(filter, document, cancellationToken: cancellationToken);
            if (result.MatchedCount == 0) throw new CampaignConflictException(draft.Id);
        }
        return saved;
    }

    public async Task DeleteAsync(string id, long expectedRevision, CancellationToken cancellationToken = default)
    {
        if (expectedRevision <= 0) throw new ArgumentOutOfRangeException(nameof(expectedRevision));
        var filter = IdFilter(id) & Builders<BsonDocument>.Filter.Eq("revision", expectedRevision) &
            Builders<BsonDocument>.Filter.Eq("schemaVersion", SchemaVersion);
        var result = await collection.DeleteOneAsync(filter, cancellationToken);
        if (result.DeletedCount == 0) throw new CampaignConflictException(id);
    }

    private static FilterDefinition<BsonDocument> IdFilter(string id) => Builders<BsonDocument>.Filter.Eq("_id", id);

    private static BsonDocument Write(CampaignDraft draft)
    {
        var document = BsonDocument.Parse(JsonSerializer.Serialize(draft, JsonOptions));
        document.Remove("id");
        document["_id"] = draft.Id;
        document["revision"] = new BsonInt64(draft.Revision);
        document["schemaVersion"] = SchemaVersion;
        return document;
    }

    private static CampaignDraft Read(BsonDocument stored)
    {
        if (!stored.TryGetValue("schemaVersion", out var version) || !version.IsInt32 || version.AsInt32 != SchemaVersion)
            throw new InvalidDataException($"Unsupported Marketing campaign schema for '{stored.GetValue("_id", "unknown")}'.");
        var document = (BsonDocument)stored.DeepClone();
        document["id"] = document["_id"];
        document.Remove("_id");
        document.Remove("schemaVersion");
        return JsonSerializer.Deserialize<CampaignDraft>(document.ToJson(new JsonWriterSettings
        {
            OutputMode = JsonOutputMode.RelaxedExtendedJson
        }), JsonOptions) ?? throw new InvalidDataException("Campaign document could not be read.");
    }
}
