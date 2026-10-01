# Marketing v0.1 handoff

## Coordinated PRs and merge order

1. [Marketing publication operations #2](https://github.com/aetheric-forge/marketing-campus/pull/2).
2. [Black Circuit admin #2](https://github.com/blackcircuit-brian/blackcircuit-admin/pull/2) and
   [Black Circuit web #7](https://github.com/blackcircuit-brian/blackcircuit-web/pull/7).
3. [Aetheric admin #17](https://github.com/aetheric-forge/aetheric-admin/pull/17).
4. [Aetheric web #44, draft](https://github.com/aetheric-forge/aetheric-web/pull/44), after repairing
   its existing Governance runtime dependency mismatch.

The admin integrations pin the institution's tested code at `f340cdcf0d38fc09d0682ed20426829ba0bd13ab`.
Later commits in the institution PR add acceptance tooling and this handoff, not different operations.
Public readers keep their own types and do not reference the institution package or runtime.
No deployment or PR merge was performed as part of this work.

## Verification

| Component | Result |
| --- | --- |
| Marketing institution | 36 tests pass against isolated Mongo; CI passes |
| Black Circuit admin | Build and 29 tests pass |
| Black Circuit public site | Build and 65 tests pass |
| Aetheric admin | Build and 56 tests pass with isolated Redis; CI passes |
| Aetheric public Marketing target | 7 tests pass, linking actual source/Razor; CI passes |
| Black Circuit browser lifecycle | Create, save/publish, draft edit, republish, and end pass |

The browser walkthrough verifies public copy, SEO/social metadata, JSON script escaping, private-note
isolation, explicit republish appearing publicly, and complete baseline content/metadata restoration.
It uses disposable Mongo and Development hosts on loopback. Screenshots were captured locally under
`/tmp/marketing-acceptance`. The institution's `tools/acceptance-blackcircuit.py` preserves the walkthrough.
It expects local admin at port 5192 and public site at port 5193 against the same isolated test database;
run with Python Playwright and Chromium installed. It creates an isolated slug and ends the campaign,
but intentionally retains its draft/ended snapshot for inspection. Use a disposable database.

## Resume here

- Review/merge the institution PR first, then matching host integrations.
- Configure public/admin Mongo access to the same Marketing database. Keep public users read-only on
  `marketingPublications` and `offerings`, excluding private `marketingCampaigns`.
- Repair Aetheric web's existing Governance references before marking its PR ready. The current pinned
  runtime and runtime main lack the projects already referenced by the host; the new Marketing test
  target does not conceal or replace this dependency.
- Verify provisioned operator sign-in, direct-route protection, and lifecycle behavior in Production.
- Perform a full Aetheric admin/public browser walkthrough after that host can start, then deploy and
  verify both websites. This is the remaining release acceptance, not an implemented scheduling feature.

The supported initial pages are Black Circuit `/portfolio` and Aetheric Forge `/projects`. Page-target
aliases canonicalize to one key. Draft/Active/Ended state, explicit snapshots, stale-action protection,
and one active campaign per page are implemented. Scheduling, analytics, attribution, A/B testing,
and external marketing channels remain outside v0.1.
