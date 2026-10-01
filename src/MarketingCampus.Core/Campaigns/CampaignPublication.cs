namespace MarketingCampus.Core.Campaigns;

/// <summary>Public snapshot. Deliberately excludes working notes, audience, and objective.</summary>
public sealed record CampaignPublication(
    string CampaignId,
    string WebsiteId,
    string IntakePath,
    long DraftRevision,
    string Version,
    DateTimeOffset PublishedAt,
    bool IsActive,
    CampaignContent Content,
    DateTimeOffset? EndedAt = null);

public interface ICampaignPublicationRepository
{
    Task<CampaignPublication?> GetActiveAsync(string websiteId, string intakePath, CancellationToken cancellationToken = default);
    Task<CampaignPublication?> GetActiveForCampaignAsync(string campaignId, CancellationToken cancellationToken = default);
    Task<CampaignPublication> PublishAsync(CampaignDraft draft, string? expectedVersion, CancellationToken cancellationToken = default);
    Task EndAsync(string campaignId, string websiteId, string intakePath, string expectedVersion, CancellationToken cancellationToken = default);
}

public sealed class CampaignPublicationConflictException()
    : Exception("The page already has an active campaign, or the publication changed. Reload before publishing or ending.");

public static class CampaignTarget
{
    // Blazor routes are case-insensitive and tolerate a trailing slash. Use one storage key for aliases.
    public static string CanonicalPath(string path) => path == "/" ? "/" : path.TrimEnd('/').ToLowerInvariant();

    public static bool IsSupported(string websiteId, string path) => (websiteId, CanonicalPath(path)) is
        ("blackcircuit", "/portfolio") or ("aetheric-forge", "/projects");
}
