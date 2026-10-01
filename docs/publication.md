# Publication and management

Management operations are implemented by `CampaignManagementService`. Every administrative read and
write calls the host-supplied `ICampaignOperatorAuthorizer` before storage. The storage provider is an
internal persistence boundary; exposing it as an unauthenticated endpoint would bypass that authority.
Public hosts use their own read-only types and never reference the management package.

## Lifecycle

- A draft has no publication snapshot. It can be incomplete and saved at any time.
- Publish loads the saved draft at the operator's expected revision, validates required content and
  supported target, and writes a public snapshot. Later draft edits leave that snapshot unchanged.
- Republish requires the current publication version, replacing the live snapshot deliberately.
- End requires the current publication version and marks it inactive. Its snapshot is retained so
  the admin can report Ended. Publishing again creates a new version; it does not reactivate old tokens.

Supported initial pages are Black Circuit `/portfolio` and Aetheric Forge `/projects`. Case and trailing
slash aliases canonicalize to one page key. End before moving an active campaign to another target.
Publication exclusivity is enforced by Mongo, including concurrent competing publishers. It does not
require a replica set or cross-collection transaction. A draft saved concurrently after the reviewed
revision was loaded does not alter the snapshot being published.

## Public documents

`marketingPublications` schema version 1 contains `_id` (campaign slug), `schemaVersion`, `campaignId`,
`websiteId`, canonical `intakePath`, `draftRevision`, opaque `version`, ISO `publishedAt`, `isActive`,
`content` (the draft content shape), and optional ISO `endedAt`.
It contains no private notes, audience, objective, or working draft. One latest snapshot is retained
per campaign; prior republish versions are not a history/audit log.

A unique partial index on `(websiteId, intakePath)` applies only when `isActive` is true. `_id` uniqueness
allows only one current snapshot per campaign. Initial publish can replace an inactive snapshot but
cannot replace another campaign's active content. Republish/end compare the opaque version token.
A stale operator action therefore cannot end or overwrite a newer publication.

Public readers query schema version 1, their configured website ID, and `isActive: true`, projecting
only the intake path and content. The 30-second cache refreshes on subsequent page requests. A browser
already displaying a page must reload to see changes. Unknown/deleted offering references are skipped.

## Deployment and operation

Each host integration has `/marketing/campaigns` and `/marketing/campaigns/{id}` admin routes, separate
from Black Circuit's legacy `/campaigns` tracking routes. The admin preserves its existing policy:
operator sign-in is required outside Development. No role or authority is granted by the UI itself.

Configure an admin and its corresponding public website to read the same Marketing database. To
manage both destinations from either admin, point both admin applications and public readers at the
same Marketing database; otherwise use each host for the target whose database it shares. Configure
Mongo authorization so public readers can read publication snapshots (and offerings) but cannot write
or read private drafts. Admin accounts need write access and permission to create the publication index.

Black Circuit accepts `Marketing:MongoDb` and otherwise uses its existing `MongoDb` configuration.
Aetheric Forge enables Marketing only when `Marketing:MongoDb` is present, resolving platform endpoint
settings through the existing institution configuration resolver while requiring Marketing-owned
username/password. It never inherits another institution's credentials. With Marketing unconfigured,
public hosts use an empty reader and keep rendering baseline content without Mongo.

Choose a campaign slug, target, and intake page. Save drafts while authoring. Use Save and publish
when content/SEO is ready, Save and republish to update live content, and End campaign plus confirmation
to restore the host's normal content. Draft/Active/Ended are shown from publication state.

Aetheric Forge's featured offering reader uses the structurally matching `offerings` collection in
its Marketing database. Black Circuit resolves offering references through its existing offering catalog.
Ensure the offering content database is shared when promoting those records across websites.

## Remaining acceptance

Black Circuit was exercised locally against isolated Mongo through create, publish, draft edit,
republish, and end, including SEO/social updates, JSON script escaping, private-note isolation, and
baseline restoration. Production sign-in and deployment remain to be verified in a provisioned environment.

Aetheric admin builds and its regression suite runs with isolated Redis. The Aetheric public Marketing
reader and actual Razor components have an independent test target. Its full host currently references
Governance projects absent from its pinned runtime and runtime main, so full-host acceptance remains
blocked on that existing dependency mismatch. Do not replace those missing institutions with stubs.
