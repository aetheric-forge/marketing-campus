namespace MarketingCampus.Core.Campaigns;

/// <summary>Private working content. Public hosts must read publication snapshots instead.</summary>
public sealed record CampaignDraft(
    string Id,
    string Name,
    string WebsiteId,
    string IntakePath,
    string? Audience = null,
    string? Objective = null,
    CampaignContent? Content = null,
    string? Notes = null,
    long Revision = 0);

public sealed record CampaignContent(
    string? Headline = null,
    string? Summary = null,
    string? Body = null,
    CampaignAction? CallToAction = null,
    CampaignImage? HeroImage = null,
    IReadOnlyList<string>? OfferingIds = null,
    CampaignSeo? Seo = null);

public sealed record CampaignAction(string Label, string Href);
public sealed record CampaignImage(string Url, string AlternativeText);
public sealed record CampaignSeo(
    string? Title = null,
    string? Description = null,
    string? SocialTitle = null,
    string? SocialDescription = null,
    CampaignImage? SocialImage = null,
    string? StructuredDataJson = null);
