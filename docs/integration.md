# Public and admin integration by convention

Follow [Black Circuit's integration convention](https://github.com/blackcircuit-brian/blackcircuit-admin/blob/6921abdfdfdf32ffc0533fa7d390793781d709a7/docs/institution-contract.md).
Each host keeps its own types, UI, navigation, and registration. Match collection and wire-field names;
do not introduce a shared generic catalog/repository or a shared web/admin plugin contract.

The runtime package in this repository is a separate concern: it establishes Marketing's institution
identity for runtime-aware hosts. Public content readers do not need to load it to read campaign documents.

## Admin contribution

- Author and validate campaign data through a repository and list/edit pages.
- Register services in the admin composition root and add navigation.
- Enforce operator authorization at the operation boundary; UI visibility alone is insufficient.
- Publish an explicit snapshot. Saving draft edits must not mutate the public version.
- Define storage ownership and migration before replacing the current Campaign CRUD.

Existing Black Circuit touchpoints: `Domain/CampaignModels.cs`, `Data/CampaignRepository.cs`,
`Data/MongoContext.cs` (`campaigns` collection), `Components/Pages/Campaigns`, `Program.cs`,
and `Components/Layout/MainLayout.razor`.

## Public contribution

- Use an immutable read model and entity-specific catalog for published campaign content.
- Follow the existing Mongo refreshing-snapshot and empty-fallback pattern.
- Read only published fields; never render private notes or draft content.
- Resolve the applicable campaign by configured website identity and supported intake path.
- Keep website layout and baseline content in the host. Apply published fields to supported page slots.
- When no campaign applies, render baseline content and metadata. Document the refresh delay.

Existing Black Circuit touchpoints: `Core/Abstractions/Abstractions.cs`,
`Infrastructure/DependencyInjection.cs`, `Infrastructure/Offerings/MongoOfferingCatalog.cs`,
`Infrastructure/Offerings/EmptyOfferingCatalog.cs`, and `Web/Program.cs`.
`Web/Components/Pages/Portfolio.razor` provides the first page's headline, copy, `PageTitle`,
and `HeadContent` integration points. The homepage's Offering catalog demonstrates existing consumption.

Marketing has public-facing content for the agreed v0.1. The upstream convention's statement that
Marketing is admin-only describes the current tracking implementation, not this repository's scope.
Agree on a versioned document shape and publication rules before implementing readers in either host.

## Foundation acceptance

Tests prove runtime plugin discovery, factory creation under a parent institution, registration, and
lifecycle invocation. They do not prove wiring or UI in any real host. Those changes require follow-up
work in the corresponding application repositories and end-to-end acceptance.
