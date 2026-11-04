# Search & Discovery (Embedding Search + Location API)

Map: [#3](https://github.com/bmazzarol-bunnings/bmazzarol-bunnings/issues/3) · Jira: INTPLAT-1 (embedding search epic; children 5, 9, 10, 11), INTPLAT-51 (Location API EDDH epic; children API-33173…33181, mostly team-assigned), TPP-2053 (search uplift initiative)

## Vision

Embedding-based semantic search live in the Item API — provider-agnostic
(Vertex/Coveo) with quality monitoring, developer docs, and a rollout plan to
other domains — plus the Location API supporting suburb & postcode search via
the EDDH → Cosmos pipeline.

Prefer a provider abstraction that is future-safe: if Coveo is pulled,
traffic falls back to Vertex.

## Scope

In:
- Item API semantic search: new GET + `$search` OData route (provider
  differentiator); `POST /search/{countryCode}` stays Coveo during a versioned
  migration (OAS deprecation + sunset, optional serving-config bridge, then
  POST retired).
- `Item.Bkg.Embedding` hosted job (incremental on item-change events; full
  rebuild via PubSub seeding) writing the `EmbeddedItem` container.
- Hybrid search = Cosmos full-text + vector, RRF fusion, in-process
  nomic-embed-text ("vertex" means the POC-style hybrid, not a managed Google
  service).
- Monitoring & tuning: v1 operational + confidence-proxy signals; phase-2
  recall via golden query set (recall@k/MRR); reembed loop with versioned docs.
- Rollout: build production-ready in item first (reference impl); abstract
  shared OData+Cosmos into a dotnet.common UseCase when location lands as the
  second adopter. Four GA gates: corpus text modelled, golden-set baseline,
  latency SLO, serving config + monitoring.
- Location API: suburb & postcode search via EDDH → Cosmos. Ben = epic
  owner/coordinator; implementation team-assigned. AU-first (Geoscape G-NAF
  settled); NZ source dataset still researching.
- **Item as the semantic-capability test bed**: most item work bundles
  around the aim of providing semantic capability to our domains. The item
  Cosmos migration (GH item #116-119) sits in this scope of work.

Out:
- Coveo/Vertex vendor contracts or procurement.
- Item API rework beyond adding search.
- INTPLAT-51 children assigned to other assignees (coordination only).

## Current state

- Foundation complete: INTPLAT-12/13/14, 597/682 (PubSub seeding).
- Designs settled (#9 item search, #11 monitoring, #12 rollout); research
  done (#10). INTPLAT-51 coordination done (#13) — API-33173 assigned to Ben
  for the API integration piece.
- Open: none at the frontier — all tickets resolved; execution is the next
  step.

## Evolution log

- 2026-11-04 — Plan doc created from map #3 (bucket standardisation, issue #34).
- 2026-11-04 — Walkthrough: item confirmed as the semantic-capability test bed; item Cosmos migration (GH #116-119) folded into this bucket's scope.
