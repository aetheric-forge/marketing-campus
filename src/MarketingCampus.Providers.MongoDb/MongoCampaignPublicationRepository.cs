using System.Text.Json;
using MarketingCampus.Core.Campaigns;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;

namespace MarketingCampus.Providers.MongoDb;

public sealed class MongoCampaignPublicationRepository : ICampaignPublicationRepository
{
    public const string CollectionName = "marketingPublications";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IMongoCollection<BsonDocument> collection;
    private readonly TimeProvider clock;

    public MongoCampaignPublicationRepository(IMongoDatabase database, TimeProvider? clock = null)
    {
        ArgumentNullException.ThrowIfNull(database);
        collection = database.GetCollection<BsonDocument>(CollectionName);
        this.clock = clock ?? TimeProvider.System;
    }

    public async Task<CampaignPublication?> GetActiveAsync(string websiteId, string intakePath, CancellationToken cancellationToken = default)
    {
        var document = await collection.Find(TargetFilter(websiteId, intakePath) & ActiveFilter())
            .FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : Read(document);
    }

    public async Task<CampaignPublication?> GetActiveForCampaignAsync(string campaignId, CancellationToken cancellationToken = default)
    {
        var document = await collection.Find(Builders<BsonDocument>.Filter.Eq("campaignId", campaignId) & ActiveFilter())
            .FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : Read(document);
    }

    public async Task<CampaignPublication> PublishAsync(CampaignDraft draft, string? expectedVersion, CancellationToken cancellationToken = default)
    {
        var errors = CampaignValidation.ValidatePublication(draft).ToList();
        if (draft.Revision <= 0) errors.Add("Save the campaign before publishing.");
        if (!CampaignTarget.IsSupported(draft.WebsiteId, draft.IntakePath)) errors.Add("Unsupported campaign target page.");
        if (errors.Count > 0) throw new CampaignValidationException(errors);
        // One active publication per campaign, in addition to the target's unique _id. This prevents
        // moving an active campaign to another page without ending its previous publication.
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("campaignId"),
            new CreateIndexOptions<BsonDocument>
            {
                Name = "one_active_target_per_campaign", Unique = true,
                PartialFilterExpression = Builders<BsonDocument>.Filter.Eq("isActive", true)
            }), cancellationToken: cancellationToken);
        var publication = new CampaignPublication(draft.Id, draft.WebsiteId, CampaignTarget.CanonicalPath(draft.IntakePath),
            draft.Revision, Guid.NewGuid().ToString("N"), clock.GetUtcNow(), true, draft.Content!);
        var filter = TargetFilter(publication.WebsiteId, publication.IntakePath) &
            Builders<BsonDocument>.Filter.Eq("schemaVersion", 1);
        filter &= expectedVersion is null
            ? Builders<BsonDocument>.Filter.Eq("isActive", false)
            : ActiveFilter() & Builders<BsonDocument>.Filter.Eq("campaignId", draft.Id) &
                Builders<BsonDocument>.Filter.Eq("version", expectedVersion);
        try
        {
            var result = await collection.ReplaceOneAsync(filter, Write(publication),
                new ReplaceOptions { IsUpsert = expectedVersion is null }, cancellationToken);
            if (result.MatchedCount == 0 && result.UpsertedId is null) throw new CampaignPublicationConflictException();
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        { throw new CampaignPublicationConflictException(); }
        return publication;
    }

    public async Task EndAsync(string campaignId, string websiteId, string intakePath, string expectedVersion, CancellationToken cancellationToken = default)
    {
        var filter = TargetFilter(websiteId, intakePath) & ActiveFilter() &
            Builders<BsonDocument>.Filter.Eq("campaignId", campaignId) & Builders<BsonDocument>.Filter.Eq("version", expectedVersion);
        var update = Builders<BsonDocument>.Update.Set("isActive", false)
            .Set("endedAt", clock.GetUtcNow().ToString("O")).Set("version", Guid.NewGuid().ToString("N"));
        var result = await collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        if (result.MatchedCount == 0) throw new CampaignPublicationConflictException();
    }

    private static FilterDefinition<BsonDocument> ActiveFilter() => Builders<BsonDocument>.Filter.Eq("isActive", true) &
        Builders<BsonDocument>.Filter.Eq("schemaVersion", 1);
    private static FilterDefinition<BsonDocument> TargetFilter(string websiteId, string path) =>
        Builders<BsonDocument>.Filter.Eq("_id", $"{websiteId}:{CampaignTarget.CanonicalPath(path)}");

    private static BsonDocument Write(CampaignPublication publication)
    {
        var document = BsonDocument.Parse(JsonSerializer.Serialize(publication, JsonOptions));
        document["_id"] = $"{publication.WebsiteId}:{publication.IntakePath}";
        document["schemaVersion"] = 1;
        return document;
    }

    private static CampaignPublication Read(BsonDocument stored)
    {
        var document = (BsonDocument)stored.DeepClone();
        document.Remove("_id");
        document.Remove("schemaVersion");
        return JsonSerializer.Deserialize<CampaignPublication>(document.ToJson(new JsonWriterSettings
        {
            OutputMode = JsonOutputMode.RelaxedExtendedJson
        }), JsonOptions) ?? throw new InvalidDataException("Campaign publication could not be read.");
    }
}
