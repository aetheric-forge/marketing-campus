using AethericForge.Runtime.Abstractions.Interfaces.Institutions;
using AethericForge.Runtime.Institutions.Plugins;
using AethericForge.Runtime.Models.Institutions;
using MarketingCampus.Institutions.Marketing;
using MarketingCampus.Plugin;

namespace MarketingCampus.Plugin.Tests;

public sealed class MarketingPluginTests
{
    [Fact]
    public async Task FactoryMountsMarketingIntoItsParentScope()
    {
        var services = new EmptyServices();
        var parent = new HostInstitution(new InstitutionContext(MarketingDefinition.CreateTemplate(), services));
        var factory = Assert.Single(new MarketingPluginPackage().GetFactories());

        Assert.Equal(typeof(IMarketing), factory.ContractType);
        var marketing = Assert.IsAssignableFrom<IMarketing>(factory.Create(parent, services));
        Assert.Same(parent, marketing.Context.Parent);
        Assert.Same(services, marketing.Context.Services);
        parent.Register<IMarketing>(marketing);
        Assert.Same(marketing, parent.Resolve<IMarketing>());
        await marketing.InitializeAsync();
        await marketing.StartAsync();
        await marketing.StopAsync();
    }

    [Fact]
    public void RuntimeLoaderDiscoversPackageAndCreatesCompatibleInstitution()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"marketing-plugin-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var pluginPath = typeof(MarketingPluginPackage).Assembly.Location;
            File.Copy(pluginPath, Path.Combine(directory, Path.GetFileName(pluginPath)));
            var institutionPath = typeof(IMarketing).Assembly.Location;
            File.Copy(institutionPath, Path.Combine(directory, Path.GetFileName(institutionPath)));
            File.Copy(Path.Combine(AppContext.BaseDirectory, "MarketingCampus.Plugin.deps.json"),
                Path.Combine(directory, "MarketingCampus.Plugin.deps.json"));
            var loaded = Assert.Single(new InstitutionPluginLoader().LoadDirectory(directory));
            var factory = Assert.Single(loaded.Factories);
            Assert.Empty(loaded.OrganizationFactories);
            Assert.Equal("Marketing", factory.Manifest.Name);
            Assert.Equal(new Version(0, 1, 0), factory.Manifest.Version);

            var services = new EmptyServices();
            var parent = new HostInstitution(new InstitutionContext(MarketingDefinition.CreateTemplate(), services));
            var child = factory.Create(parent, services);
            Assert.True(factory.ContractType.IsInstanceOfType(child));
            Assert.Same(parent, child.Context.Parent);
            // Package-specific types belong to the isolated plugin context. A dynamic host
            // uses the advertised contract type rather than loading a second static copy.
            typeof(IInstitution).GetMethod(nameof(IInstitution.Register))!
                .MakeGenericMethod(factory.ContractType).Invoke(parent, [child]);
            var resolved = typeof(IInstitution).GetMethod(nameof(IInstitution.Resolve))!
                .MakeGenericMethod(factory.ContractType).Invoke(parent, null);
            Assert.Same(child, resolved);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class EmptyServices : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class HostInstitution(IInstitutionContext context) : InstitutionBase(context);
}
