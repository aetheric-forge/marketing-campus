# Marketing Campus

Standalone Marketing institution for authoring campaigns and publishing website content and SEO metadata.
Initial target hosts are Black Circuit and Aetheric Forge, with separate public and administrative applications.

## Status

The runtime institution foundation and milestone 2 campaign model/storage are implemented. Drafts
contain website targets, intake paths, audience/objective, page content, offerings, and SEO metadata.
Mongo persistence validates writes and uses optimistic revisions to reject conflicting edits.
Management UI, publication operations, public readers, and actual host wiring remain subsequent work.
Loading the runtime foundation alone does not register storage or publish content.

The runtime is pinned to `d70a8bdc87769c1e8ea2c8184a541216b03033e4` through the `runtime` Git submodule.

## Build and test

Requires the .NET 10 SDK. From a fresh clone:

```sh
git submodule update --init --recursive
dotnet build MarketingCampus.slnx
docker run --rm -d --name marketing-test-mongo -p 127.0.0.1:27217:27017 mongo:8.0
dotnet test MarketingCampus.slnx
docker stop marketing-test-mongo
```

Storage tests create and remove disposable databases. Use `MONGO_TEST_CONNECTION` to override the
default `mongodb://localhost:27217` test connection. CI supplies its own Mongo service.

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
- `src/MarketingCampus.Core`: draft content, validation, and the campaign-specific repository boundary.
- `src/MarketingCampus.Providers.MongoDb`: durable drafts and optional host service registration.
- `src/MarketingCampus.Plugin`: definition and runtime plugin entry point.
- `tests/MarketingCampus.Plugin.Tests`: runtime discovery and mounting checks.
- `tests/MarketingCampus.Core.Tests`: draft/publication validation.
- `tests/MarketingCampus.Providers.MongoDb.Tests`: real-Mongo persistence, conflict, and compatibility checks.
- `runtime`: pinned Aetheric Forge Runtime.
- [v0.1 plan](docs/v0.1-plan.md): scope, milestones, and release acceptance.
- [Integration convention](docs/integration.md): public/admin responsibilities and initial host touchpoints.
- [Campaign storage schema](docs/campaign-schema.md): field names, ownership, and legacy migration boundary.
