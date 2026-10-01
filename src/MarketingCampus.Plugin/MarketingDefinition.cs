using AethericForge.Runtime.Institutions.Abstractions.Builders;
using AethericForge.Runtime.Institutions.Abstractions.Models;
using AethericForge.Runtime.Institutions.Abstractions.Primitives;

namespace MarketingCampus.Plugin;

public static class MarketingDefinition
{
    public const string Id = "marketing";

    public static InstitutionDescriptor Descriptor { get; } = new(
        "Marketing",
        new Version(0, 1, 0),
        "Campaign authoring and publication of website content and SEO metadata.");

    // This foundation declares identity only. Campaign operations are added with their
    // implementations in later milestones, rather than advertising unavailable capabilities.
    public static IInstitutionTemplate CreateTemplate() => new InstitutionBuilder()
        .SetDescriptor(Descriptor)
        .AddDomain(new DomainDefinition("Campaigns", "Campaign content and publication lifecycle."))
        .Build();
}
