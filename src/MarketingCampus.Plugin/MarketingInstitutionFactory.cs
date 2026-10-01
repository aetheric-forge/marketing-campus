using AethericForge.Runtime.Abstractions.Interfaces.Institutions;
using AethericForge.Runtime.Abstractions.Interfaces.Institutions.Plugins;
using AethericForge.Runtime.Institutions.Abstractions.Primitives;
using AethericForge.Runtime.Models.Institutions;
using MarketingCampus.Institutions.Marketing;

namespace MarketingCampus.Plugin;

public sealed class MarketingInstitutionFactory : IInstitutionFactory
{
    public Type ContractType => typeof(IMarketing);
    public IInstitutionManifest Manifest => MarketingDefinition.Descriptor;
    public IInstitutionTemplate Template { get; } = MarketingDefinition.CreateTemplate();

    public IInstitution Create(IInstitution parent, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(services);
        return new Marketing(new InstitutionContext(Template, services, parent));
    }
}
