# Campaign drafts: schema version 1

Marketing working records live in `marketingCampaigns`. This is private authoring storage; public
websites must not query it. The existing Black Circuit `campaigns` tracking collection remains untouched.

## Document shape

```json
{
  "_id": "portfolio-launch",
  "schemaVersion": 1,
  "revision": 1,
  "name": "Portfolio launch",
  "websiteId": "blackcircuit",
  "intakePath": "/portfolio",
  "audience": "Creative teams",
  "objective": "Start a project inquiry",
  "notes": "Private working notes",
  "content": {
    "headline": "Ideas given form",
    "summary": "Explore selected work",
    "body": "Campaign page introduction",
    "callToAction": { "label": "Start a conversation", "href": "/contact" },
    "heroImage": { "url": "/images/portfolio.webp", "alternativeText": "Selected portfolio work" },
    "offeringIds": ["creative-consulting"],
    "seo": {
      "title": "Portfolio | Black Circuit",
      "description": "Selected creative work",
      "socialTitle": "Selected work",
      "socialDescription": "Explore Black Circuit's portfolio",
      "socialImage": { "url": "/images/social.webp", "alternativeText": "Portfolio preview" },
      "structuredDataJson": "{\"@type\":\"CollectionPage\"}"
    }
  }
}
```

Mongo `_id` is a kebab-case slug, `schemaVersion` is Int32, and `revision` is Int64. All fields use
the spelling above. Optional fields may be null, including the whole content object while drafting.
Text is plain text, not executable markup. Structured data is a JSON string validated as an object
or array; semantic schema validation is outside this slice. Hosts must use safe JSON serialization
when embedding structured data in HTML.

Website IDs are `blackcircuit` and `aetheric-forge`. The intake is a site-relative path without
query, fragment, encoded path, or traversal. CTA/image URLs may be local paths or HTTPS URLs;
script/data URLs and embedded credentials are rejected. Offering references are unique slugs.
Offering existence and supported intake routes are host concerns.

## Validation

Saving requires valid identity, name, website, intake, and any supplied content fields. Incomplete
audience, objective, copy, and SEO are allowed while drafting. `ValidatePublication` additionally
requires audience, objective, headline, summary, CTA, SEO title, and SEO description. This is preparation
for publication operations, not a publish endpoint.

## Persistence and concurrent edits

`SaveAsync` with revision zero inserts and returns revision one. A second create cannot overwrite it.
Updates match `_id`, `schemaVersion`, and expected revision, then increment the revision. Competing
edits, missing documents, and schema mismatches produce `CampaignConflictException`. Deletes require
the current revision. Unsupported schema versions fail explicitly before read/rewrite.

Mongo's `_id` index enforces unique slugs across website targets. Lists sort by name, then slug.
Draft revisions do not model publication status. The provider accepts a host-configured `IMongoDatabase`
and can register through `AddMarketingDraftStorage(database)`; it does not own credentials or a client.
Operator authorization belongs in the upcoming application operations before storage is called.

## Publication and migration boundary

Milestone 3 will define a separate `marketingPublications` collection with public snapshots and their
publication state. Implement `(websiteId, intakePath)` uniqueness and publish/end rules together.
This milestone does not write a public collection or expose a public reader.

Legacy tracking records lack intake, page content, and SEO metadata. Do not automatically turn old
`Active` records into published campaigns. A future explicit import may copy suitable fields into
incomplete drafts, which still require operator completion and publication validation.
