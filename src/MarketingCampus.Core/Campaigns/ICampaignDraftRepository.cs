namespace MarketingCampus.Core.Campaigns;

public interface ICampaignDraftRepository
{
    Task<CampaignDraft?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CampaignDraft>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Revision zero creates; a saved revision updates only that version. Returns the new revision.</summary>
    Task<CampaignDraft> SaveAsync(CampaignDraft draft, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, long expectedRevision, CancellationToken cancellationToken = default);
}

public sealed class CampaignConflictException(string id)
    : Exception($"Campaign '{id}' changed or no longer exists. Reload before saving or deleting.");

public sealed class CampaignValidationException(IReadOnlyList<string> errors)
    : Exception(string.Join(" ", errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}
