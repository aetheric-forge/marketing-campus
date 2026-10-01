using MarketingCampus.Core.Campaigns;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MarketingCampus.Providers.MongoDb.Tests;

public sealed class MongoCampaignDraftRepositoryTests : IAsyncLifetime
{
    private readonly IMongoDatabase database = new MongoClient(
        Environment.GetEnvironmentVariable("MONGO_TEST_CONNECTION") ?? "mongodb://localhost:27217")
        .GetDatabase($"marketing-tests-{Guid.NewGuid():N}");

    private MongoCampaignDraftRepository Repository => new(database);
    private static CampaignDraft Draft(string id = "portfolio-launch") =>
        new(id, "Portfolio launch", "blackcircuit", "/portfolio", Notes: "Private working notes");

    public async Task InitializeAsync() => await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
    public Task DisposeAsync() => database.Client.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName);

    [Fact]
    public async Task CreateEditListAndDeleteRoundTripNestedContent()
    {
        var draft = Draft() with
        {
            Audience = "Creative teams", Objective = "Start a conversation",
            Content = new("Ideas given form", "Selected work", "Portfolio introduction",
                new("Contact", "/contact"), new("/hero.webp", "Selected work"), ["creative-consulting"],
                new("Portfolio", "Selected work", "Social title", "Social description", new("/social.webp", "Preview"), "{\"@type\":\"CollectionPage\"}"))
        };
        var saved = await Repository.SaveAsync(draft);
        Assert.Equal(1, saved.Revision);
        var loaded = Assert.IsType<CampaignDraft>(await Repository.GetAsync(saved.Id));
        Assert.Equal(saved.Notes, loaded.Notes);
        Assert.Equal(saved.Content!.Headline, loaded.Content!.Headline);
        Assert.Equal(saved.Content.Seo, loaded.Content.Seo);
        Assert.Equal(saved.Content.OfferingIds, loaded.Content.OfferingIds);

        var edited = await Repository.SaveAsync(loaded with { Name = "Revised name" });
        Assert.Equal(2, edited.Revision);
        Assert.Equal("Revised name", Assert.Single(await Repository.GetAllAsync()).Name);
        await Repository.DeleteAsync(edited.Id, edited.Revision);
        Assert.Null(await Repository.GetAsync(edited.Id));
    }

    [Fact]
    public async Task ConcurrentEditsAllowOnlyOneWinner()
    {
        var saved = await Repository.SaveAsync(Draft());
        async Task<bool> Attempt(string name)
        {
            try { await Repository.SaveAsync(saved with { Name = name }); return true; }
            catch (CampaignConflictException) { return false; }
        }
        var results = await Task.WhenAll(Attempt("First editor"), Attempt("Second editor"));
        Assert.Single(results, result => result);
        Assert.Equal(2, (await Repository.GetAsync(saved.Id))!.Revision);
    }

    [Fact]
    public async Task DuplicateCreateAndStaleDeleteCannotOverwriteOrRemoveCampaign()
    {
        var saved = await Repository.SaveAsync(Draft());
        await Assert.ThrowsAsync<CampaignConflictException>(() => Repository.SaveAsync(Draft() with { Name = "Overwrite" }));
        var edited = await Repository.SaveAsync(saved with { Name = "Current" });
        await Assert.ThrowsAsync<CampaignConflictException>(() => Repository.DeleteAsync(saved.Id, saved.Revision));
        Assert.Equal("Current", (await Repository.GetAsync(edited.Id))!.Name);
    }

    [Fact]
    public async Task InvalidDraftDoesNotReachStorage()
    {
        await Assert.ThrowsAsync<CampaignValidationException>(() => Repository.SaveAsync(Draft() with { IntakePath = "//example.com" }));
        Assert.Empty(await Repository.GetAllAsync());
    }

    [Fact]
    public async Task LegacyCollectionIsUntouchedAndWireShapeIsExplicit()
    {
        var legacy = database.GetCollection<BsonDocument>("campaigns");
        await legacy.InsertOneAsync(new BsonDocument { ["_id"] = "portfolio-launch", ["status"] = "Paused", ["budget"] = 500 });
        await Repository.SaveAsync(Draft());
        var stored = await database.GetCollection<BsonDocument>(MongoCampaignDraftRepository.CollectionName)
            .Find(new BsonDocument("_id", "portfolio-launch")).SingleAsync();
        Assert.Equal(1, stored["schemaVersion"].AsInt32);
        Assert.Equal(1, stored["revision"].AsInt64);
        Assert.Equal("blackcircuit", stored["websiteId"].AsString);
        Assert.Equal("/portfolio", stored["intakePath"].AsString);
        Assert.False(stored.Contains("id"));
        Assert.Equal("Paused", (await legacy.Find(new BsonDocument()).SingleAsync())["status"].AsString);
    }

    [Fact]
    public async Task FutureSchemaIsRejectedWithoutOverwriting()
    {
        var collection = database.GetCollection<BsonDocument>(MongoCampaignDraftRepository.CollectionName);
        await collection.InsertOneAsync(new BsonDocument { ["_id"] = "portfolio-launch", ["schemaVersion"] = 2, ["revision"] = 1 });
        await Assert.ThrowsAsync<InvalidDataException>(() => Repository.GetAsync("portfolio-launch"));
        await Assert.ThrowsAsync<CampaignConflictException>(() => Repository.SaveAsync(Draft() with { Revision = 1 }));
        Assert.Equal(2, (await collection.Find(new BsonDocument()).SingleAsync())["schemaVersion"].AsInt32);
    }
}
