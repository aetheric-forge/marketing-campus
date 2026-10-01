using MarketingCampus.Core.Campaigns;

namespace MarketingCampus.Application;

/// <summary>Hosts decide who is an operator; every administrative operation demands that authority.</summary>
public interface ICampaignOperatorAuthorizer
{
    Task EnsureCanManageAsync(CancellationToken cancellationToken = default);
}

public sealed class CampaignManagementService(
    ICampaignDraftRepository drafts,
    ICampaignPublicationRepository publications,
    ICampaignOperatorAuthorizer authorizer)
{
    public async Task<IReadOnlyList<CampaignDraft>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await authorizer.EnsureCanManageAsync(cancellationToken);
        return await drafts.GetAllAsync(cancellationToken);
    }

    public async Task<CampaignDraft?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        await authorizer.EnsureCanManageAsync(cancellationToken);
        return await drafts.GetAsync(id, cancellationToken);
    }

    public async Task<CampaignDraft> SaveAsync(CampaignDraft draft, CancellationToken cancellationToken = default)
    {
        await authorizer.EnsureCanManageAsync(cancellationToken);
        return await drafts.SaveAsync(draft, cancellationToken);
    }

    public async Task<CampaignPublication?> GetActivePublicationAsync(string id, CancellationToken cancellationToken = default)
    {
        await authorizer.EnsureCanManageAsync(cancellationToken);
        return await publications.GetActiveForCampaignAsync(id, cancellationToken);
    }

    public async Task<CampaignPublication> PublishAsync(
        string id, long expectedDraftRevision, string? expectedVersion, CancellationToken cancellationToken = default)
    {
        await authorizer.EnsureCanManageAsync(cancellationToken);
        var draft = await drafts.GetAsync(id, cancellationToken) ?? throw new CampaignConflictException(id);
        if (draft.Revision != expectedDraftRevision) throw new CampaignConflictException(id);
        var errors = CampaignValidation.ValidatePublication(draft).ToList();
        if (!CampaignTarget.IsSupported(draft.WebsiteId, draft.IntakePath))
            errors.Add("This page is not supported yet. Use Black Circuit /portfolio or Aetheric Forge /projects.");
        if (errors.Count > 0) throw new CampaignValidationException(errors);
        // Snapshot the version reviewed by the operator. Later draft edits do not alter this content.
        return await publications.PublishAsync(draft, expectedVersion, cancellationToken);
    }

    public async Task EndAsync(CampaignPublication publication, CancellationToken cancellationToken = default)
    {
        await authorizer.EnsureCanManageAsync(cancellationToken);
        await publications.EndAsync(publication.CampaignId, publication.WebsiteId, publication.IntakePath,
            publication.Version, cancellationToken);
    }
}
