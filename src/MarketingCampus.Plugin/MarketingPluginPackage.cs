using AethericForge.Runtime.Abstractions.Interfaces.Institutions.Plugins;

[assembly: InstitutionPluginPackage(typeof(MarketingCampus.Plugin.MarketingPluginPackage))]

namespace MarketingCampus.Plugin;

public sealed class MarketingPluginPackage : IInstitutionPluginPackage
{
    public IReadOnlyCollection<IInstitutionFactory> GetFactories() => [new MarketingInstitutionFactory()];
}
