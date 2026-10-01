using System.Text.Json;
using System.Text.RegularExpressions;

namespace MarketingCampus.Core.Campaigns;

public static partial class CampaignValidation
{
    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();

    public static IReadOnlyList<string> ValidateDraft(CampaignDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(draft.Id) || !SlugPattern().IsMatch(draft.Id))
            errors.Add("Id must be a kebab-case slug.");
        Required(draft.Name, "Name", errors);
        if (draft.WebsiteId is not ("blackcircuit" or "aetheric-forge"))
            errors.Add("WebsiteId must be blackcircuit or aetheric-forge.");
        if (!IsLocalPath(draft.IntakePath))
            errors.Add("IntakePath must be a site-relative path without query, fragment, encoding, or traversal.");
        if (draft.Revision < 0 || draft.Revision == long.MaxValue)
            errors.Add("Revision is outside the supported range.");

        if (draft.Content is { } content)
        {
            if (content.CallToAction is { } action)
            {
                Required(action.Label, "Call-to-action label", errors);
                if (!IsContentUrl(action.Href)) errors.Add("Call-to-action URL must be a local path or HTTPS URL.");
            }
            ValidateImage(content.HeroImage, "Hero image", errors);
            ValidateImage(content.Seo?.SocialImage, "Social image", errors);
            if (content.OfferingIds is { } ids)
            {
                if (ids.Any(id => string.IsNullOrWhiteSpace(id) || !SlugPattern().IsMatch(id)))
                    errors.Add("Offering IDs must be kebab-case slugs.");
                if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Count)
                    errors.Add("Offering IDs must not contain duplicates.");
            }
            if (content.Seo?.StructuredDataJson is { } json)
            {
                try
                {
                    using var document = JsonDocument.Parse(json);
                    if (document.RootElement.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
                        errors.Add("Structured data must be a JSON object or array.");
                }
                catch (JsonException) { errors.Add("Structured data must be valid JSON."); }
            }
        }
        return errors;
    }

    /// <summary>Drafts may be incomplete. Publication requires content suitable for the page and its metadata.</summary>
    public static IReadOnlyList<string> ValidatePublication(CampaignDraft draft)
    {
        var errors = ValidateDraft(draft).ToList();
        Required(draft.Audience, "Audience", errors);
        Required(draft.Objective, "Objective", errors);
        Required(draft.Content?.Headline, "Headline", errors);
        Required(draft.Content?.Summary, "Summary", errors);
        if (draft.Content?.CallToAction is null) errors.Add("Call to action is required.");
        Required(draft.Content?.Seo?.Title, "SEO title", errors);
        Required(draft.Content?.Seo?.Description, "SEO description", errors);
        return errors;
    }

    public static bool IsLocalPath(string? path) =>
        !string.IsNullOrWhiteSpace(path) && path.StartsWith('/') && !path.StartsWith("//") &&
        !path.Any(c => char.IsWhiteSpace(c) || char.IsControl(c) || c is '\\' or '?' or '#' or '%') &&
        !path.Split('/').Any(segment => segment is "." or "..") &&
        !path.Contains("//", StringComparison.Ordinal);

    private static bool IsContentUrl(string? value)
    {
        if (IsLocalPath(value)) return true;
        return !string.IsNullOrWhiteSpace(value) &&
            !value.Any(c => char.IsWhiteSpace(c) || char.IsControl(c) || c == '\\') &&
            Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
            !string.IsNullOrWhiteSpace(uri.Host) && string.IsNullOrEmpty(uri.UserInfo);
    }

    private static void Required(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value)) errors.Add($"{name} is required.");
    }

    private static void ValidateImage(CampaignImage? image, string name, List<string> errors)
    {
        if (image is null) return;
        if (!IsContentUrl(image.Url)) errors.Add($"{name} URL must be a local path or HTTPS URL.");
        Required(image.AlternativeText, $"{name} alternative text", errors);
    }
}
