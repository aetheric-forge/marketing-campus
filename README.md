# Marketing Campus

Standalone Marketing institution for authoring campaigns and publishing website content and SEO metadata.
Initial target hosts are Black Circuit and Aetheric Forge, with separate public and administrative applications.

## Status

Milestone 1 foundation: a runtime institution, discoverable plugin package, pinned runtime dependency,
and scope/integration documentation. Campaign persistence, management UI, public readers, and host wiring
are subsequent work. Loading the foundation does not publish content or expose campaign operations.

The runtime is pinned to `d70a8bdc87769c1e8ea2c8184a541216b03033e4` through the `runtime` Git submodule.

## Build and test

Requires the .NET 10 SDK. From a fresh clone:

```sh
git submodule update --init --recursive
dotnet build MarketingCampus.slnx
dotnet test MarketingCampus.slnx
```

Produce the runtime plugin output with:

```sh
dotnet publish src/MarketingCampus.Plugin -c Release -o artifacts/marketing-plugin
```

Keep the complete output together for dependency resolution. A host using the runtime plugin loader
can discover `MarketingCampus.Plugin.dll` and obtain its Marketing factory. The factory creates a child
institution; registration and lifecycle remain host responsibilities. This package does not introduce
a shared web/admin interface or automatically mount routes in either application.

## Layout

- `src/MarketingCampus.Institutions.Marketing`: Marketing runtime identity and institution.
- `src/MarketingCampus.Plugin`: definition and runtime plugin entry point.
- `tests/MarketingCampus.Plugin.Tests`: runtime discovery and mounting checks.
- `runtime`: pinned Aetheric Forge Runtime.
- [v0.1 plan](docs/v0.1-plan.md): scope, milestones, and release acceptance.
- [Integration convention](docs/integration.md): public/admin responsibilities and initial host touchpoints.
