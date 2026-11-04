# AsyncAPI / Event Documentation Pipeline

Map: [#4](https://github.com/bmazzarol-bunnings/bmazzarol-bunnings/issues/4) · Jira: INTPLAT-764 (C# library), 761 (spec validation), 763 (build pipeline integration), 765 (GitHub workflows for event validation), 762 (publish to DevHub), 760 (usage guidelines & templates)

## Vision

The full event-documentation lifecycle running end-to-end — C# AsyncAPI
generation library, spec validation, CI/CD + GitHub workflow integration,
auto-publish to the DevHub catalogue, and developer guidelines/templates —
adopted by at least one pilot workload.

These stories are not epic-linked in Jira; this map is their tracking parent.
Pipeline order: 764 → 761 → 763/765 → 762 → 760.

Prefer annotations/attributes over hand-written docs; minimal manual
intervention in CI.

## Scope

In:
- BKG async-operation manifest: per-domain `{domain}.Bkg.Index` code-first
  manifest (IOP-mirrored, asymmetric). Code is source of truth; index
  generates compatible `subscriptions.json`. AsyncAPI 3.0 via a hand-rolled
  zero-dependency emitter. Schemas are PROVIDED, not generated.
- Exporter tool in the `Bunnings.UseCase.AspNetCore.Tools*` family emitting
  subscriptions.json + asyncapi.yaml.
- Validation: official `@asyncapi/cli` via npx on Node 20. Four blocking PR
  checks: (1) drift vs regenerated manifest, (2) 3.0 conformance
  (warnings-as-errors), (3) subscriptions.json against manifest JSON Schema,
  (4) provided schema refs exist and parse. Per-domain PR workflow first,
  then shared reusable workflow in GHA.API.Templates.
- Publish to DevHub (Spotify Backstage): file-based ingestion of `kind: API`
  YAML or per-repo catalog-info.yaml; AsyncAPI is a first-class spec.type.
- Pilot workloads: **item** (first), **loyalty** (second).

Out:
- Changes to event contracts themselves (schemas stay as-is; docs generated
  from them).
- OpenAPI (sync) documentation — events/AsyncAPI only.
- Soundcheck/DevHub governance (post-publish, 762).

## Current state

- Research done (#15 DevHub/Backstage), manifest design settled (#14),
  validation strategy settled (#16), pilot chosen (#17: item, then loyalty).
- Phases: 0 spike on item (byte-compat gate), 1 core usecase + EMP builder
  integration, 2 exporter tool, 3 pilot adoption feeding 763/765/762.

## Evolution log

- 2026-11-04 — Plan doc created from map #4 (bucket standardisation, issue #34).
