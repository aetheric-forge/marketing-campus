using MarketingCampus.Application;
using MarketingCampus.Core.Campaigns;

namespace MarketingCampus.Core.Tests;

public sealed class CampaignManagementAuthorizationTests
{
    [Fact]
    public async Task EveryAdministrativeOperationRejectsUnauthorizedCallsBeforeStorage()
    {
        var repositories = new NeverAccessStorage();
        var service = new CampaignManagementService(repositories, repositories, new DenyOperator());
        var draft = new CampaignDraft("launch", "Launch", "blackcircuit", "/portfolio");
        var publication = new CampaignPublication("launch", "blackcircuit", "/portfolio", 1, "version", DateTimeOffset.UtcNow, true, new());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetAllAsync());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetAsync("launch"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SaveAsync(draft));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetActivePublicationAsync("launch"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetPublicationAsync("launch"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.PublishAsync("launch", 1, null));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.EndAsync(publication));
    }

    private sealed class DenyOperator : ICampaignOperatorAuthorizer
    {
        public Task EnsureCanManageAsync(CancellationToken cancellationToken = default) => throw new UnauthorizedAccessException();
    }

    private sealed class NeverAccessStorage : ICampaignDraftRepository, ICampaignPublicationRepository
    {
        public Task<CampaignDraft?> GetAsync(string id, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Storage reached");
        public Task<IReadOnlyList<CampaignDraft>> GetAllAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException("Storage reached");
        public Task<CampaignDraft> SaveAsync(CampaignDraft draft, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Storage reached");
        public Task DeleteAsync(string id, long expectedRevision, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Storage reached");
        public Task<CampaignPublication?> GetActiveAsync(string websiteId, string path, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Storage reached");
        public Task<CampaignPublication?> GetActiveForCampaignAsync(string id, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Storage reached");
        public Task<CampaignPublication?> GetForCampaignAsync(string id, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Storage reached");
        public Task<CampaignPublication> PublishAsync(CampaignDraft draft, string? expectedVersion, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Storage reached");
        public Task EndAsync(string id, string website, string path, string version, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Storage reached");
    }
}
