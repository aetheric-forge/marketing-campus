using MarketingCampus.Application;
using MarketingCampus.Core.Campaigns;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MarketingCampus.Providers.MongoDb.Tests;

public sealed class CampaignPublicationTests : IAsyncLifetime
{
    private readonly IMongoDatabase database = new MongoClient(
        Environment.GetEnvironmentVariable("MONGO_TEST_CONNECTION") ?? "mongodb://localhost:27217")
        .GetDatabase($"marketing-publication-tests-{Guid.NewGuid():N}");

    private MongoCampaignDraftRepository Drafts => new(database);
    private MongoCampaignPublicationRepository Publications => new(database);
    private CampaignManagementService Management => new(Drafts, Publications, new AllowOperator());

    private static CampaignDraft Draft(string id = "launch", string website = "blackcircuit", string path = "/portfolio") =>
        new(id, "Launch", website, path, "Creative teams", "Start an inquiry",
            new("Published headline", "Published summary", CallToAction: new("Contact", "/contact"),
                Seo: new("Portfolio", "Selected work")), "PRIVATE NOTES");

    public async Task InitializeAsync() => await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
    public Task DisposeAsync() => database.Client.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName);

    [Fact]
    public async Task PublicationIsSeparateFromDraftAndRepublishIsExplicit()
    {
        var saved = await Drafts.SaveAsync(Draft());
        var first = await Management.PublishAsync(saved.Id, saved.Revision, null);
        var edited = await Drafts.SaveAsync(saved with { Content = saved.Content! with { Headline = "New draft headline" } });
        Assert.Equal("Published headline", (await Publications.GetActiveAsync("blackcircuit", "/portfolio"))!.Content.Headline);
        var stored = await database.GetCollection<BsonDocument>(MongoCampaignPublicationRepository.CollectionName)
            .Find(new BsonDocument()).SingleAsync();
        Assert.False(stored.Contains("notes"));
        Assert.False(stored.Contains("audience"));
        Assert.False(stored.Contains("objective"));
        Assert.DoesNotContain("PRIVATE NOTES", stored.ToJson());
        var second = await Management.PublishAsync(edited.Id, edited.Revision, first.Version);
        Assert.Equal("New draft headline", second.Content.Headline);
        Assert.NotEqual(first.Version, second.Version);
        await Assert.ThrowsAsync<CampaignPublicationConflictException>(() => Management.EndAsync(first));
        await Management.EndAsync(second);
        Assert.Null(await Publications.GetActiveAsync("blackcircuit", "/portfolio"));
        Assert.NotNull(await Drafts.GetAsync(saved.Id));
    }

    [Fact]
    public async Task CompetingCampaignsOnAliasedRouteAllowOnlyOneWinner()
    {
        var a = await Drafts.SaveAsync(Draft("first", path: "/PORTFOLIO/"));
        var b = await Drafts.SaveAsync(Draft("second"));
        async Task<bool> Attempt(CampaignDraft draft)
        {
            try { await Publications.PublishAsync(draft, null); return true; }
            catch (CampaignPublicationConflictException) { return false; }
        }
        var results = await Task.WhenAll(Attempt(a), Attempt(b));
        Assert.Single(results, result => result);
        Assert.NotNull(await Publications.GetActiveAsync("blackcircuit", "/portfolio"));
        Assert.Equal(1, await database.GetCollection<BsonDocument>(MongoCampaignPublicationRepository.CollectionName)
            .CountDocumentsAsync(new BsonDocument("isActive", true)));
    }

    [Fact]
    public async Task EndingReleasesPageAndOldOperatorActionCannotEndNewCampaign()
    {
        var a = await Drafts.SaveAsync(Draft("first"));
        var first = await Publications.PublishAsync(a, null);
        await Management.EndAsync(first);
        var b = await Drafts.SaveAsync(Draft("second"));
        var second = await Publications.PublishAsync(b, null);
        Assert.False((await Publications.GetForCampaignAsync(first.CampaignId))!.IsActive);
        Assert.NotNull((await Publications.GetForCampaignAsync(first.CampaignId))!.EndedAt);
        await Assert.ThrowsAsync<CampaignPublicationConflictException>(() => Management.EndAsync(first));
        Assert.Equal(second.Version, (await Publications.GetActiveAsync("blackcircuit", "/portfolio"))!.Version);
    }

    [Fact]
    public async Task ActiveCampaignCannotMoveToAnotherWebsiteUntilEnded()
    {
        var saved = await Drafts.SaveAsync(Draft());
        var publication = await Publications.PublishAsync(saved, null);
        var moved = await Drafts.SaveAsync(saved with { WebsiteId = "aetheric-forge", IntakePath = "/projects" });
        await Assert.ThrowsAsync<CampaignPublicationConflictException>(() => Publications.PublishAsync(moved, null));
        await Management.EndAsync(publication);
        Assert.NotNull(await Publications.PublishAsync(moved, null));
    }

    [Fact]
    public async Task UnsupportedPageIncompleteContentAndStaleDraftCannotPublish()
    {
        var unsupported = await Drafts.SaveAsync(Draft("unsupported", path: "/admin"));
        await Assert.ThrowsAsync<CampaignValidationException>(() => Management.PublishAsync(unsupported.Id, unsupported.Revision, null));
        var incomplete = await Drafts.SaveAsync(Draft("incomplete") with { Content = null });
        await Assert.ThrowsAsync<CampaignValidationException>(() => Management.PublishAsync(incomplete.Id, incomplete.Revision, null));
        var saved = await Drafts.SaveAsync(Draft());
        await Drafts.SaveAsync(saved with { Name = "Edited" });
        await Assert.ThrowsAsync<CampaignConflictException>(() => Management.PublishAsync(saved.Id, saved.Revision, null));
        Assert.Null(await Publications.GetActiveAsync("blackcircuit", "/portfolio"));
    }

    private sealed class AllowOperator : ICampaignOperatorAuthorizer
    {
        public Task EnsureCanManageAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
