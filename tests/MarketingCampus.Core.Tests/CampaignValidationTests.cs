using MarketingCampus.Core.Campaigns;

namespace MarketingCampus.Core.Tests;

public sealed class CampaignValidationTests
{
    private static CampaignDraft Draft() => new("portfolio-launch", "Portfolio launch", "blackcircuit", "/portfolio");

    [Fact]
    public void IncompleteDraftCanBeSavedButCannotBePublished()
    {
        Assert.Empty(CampaignValidation.ValidateDraft(Draft()));
        var errors = CampaignValidation.ValidatePublication(Draft());
        Assert.Contains("Audience is required.", errors);
        Assert.Contains("Headline is required.", errors);
        Assert.Contains("SEO title is required.", errors);
        Assert.Contains("Call to action is required.", errors);
    }

    [Fact]
    public void CompleteContentCanBePublished()
    {
        var draft = Draft() with
        {
            Audience = "Creative teams", Objective = "Start a project inquiry",
            Content = new CampaignContent("Ideas given form", "Explore our work",
                CallToAction: new("Start a conversation", "/contact"),
                HeroImage: new("/images/portfolio.webp", "Selected portfolio work"),
                OfferingIds: ["creative-consulting"],
                Seo: new("Portfolio | Black Circuit", "Selected creative work", StructuredDataJson: "{\"@type\":\"CollectionPage\"}"))
        };
        Assert.Empty(CampaignValidation.ValidatePublication(draft));
    }

    [Theory]
    [InlineData("https://example.com/portfolio")]
    [InlineData("//example.com/portfolio")]
    [InlineData("/portfolio?campaign=launch")]
    [InlineData("/portfolio#contact")]
    [InlineData("/../admin")]
    [InlineData("/a/./portfolio")]
    [InlineData("/%2e%2e/admin")]
    [InlineData("/a\\portfolio")]
    [InlineData("/portfolio\n")]
    [InlineData("portfolio")]
    [InlineData("/a//portfolio")]
    public void InvalidIntakePathsAreRejected(string path)
    {
        Assert.Contains(CampaignValidation.ValidateDraft(Draft() with { IntakePath = path }), e => e.StartsWith("IntakePath"));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,test")]
    [InlineData("http://example.com")]
    [InlineData("https://user:password@example.com")]
    [InlineData("//example.com")]
    public void UnsafeActionUrlsAreRejected(string url)
    {
        var draft = Draft() with { Content = new(CallToAction: new("Contact", url)) };
        Assert.Contains(CampaignValidation.ValidateDraft(draft), e => e.StartsWith("Call-to-action URL"));
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("42")]
    [InlineData("null")]
    public void InvalidStructuredDataIsRejected(string json)
    {
        var draft = Draft() with { Content = new(Seo: new(StructuredDataJson: json)) };
        Assert.Contains(CampaignValidation.ValidateDraft(draft), e => e.StartsWith("Structured data"));
    }

    [Fact]
    public void OfferingReferencesAndImageAccessibilityAreValidated()
    {
        var draft = Draft() with { Content = new(HeroImage: new("/hero.webp", ""), OfferingIds: ["bad_slug", "same", "same"]) };
        var errors = CampaignValidation.ValidateDraft(draft);
        Assert.Contains("Offering IDs must be kebab-case slugs.", errors);
        Assert.Contains("Offering IDs must not contain duplicates.", errors);
        Assert.Contains("Hero image alternative text is required.", errors);
    }
}
