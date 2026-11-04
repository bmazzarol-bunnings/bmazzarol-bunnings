# Data Engineering Agentic Craft

Map: [#25](https://github.com/bmazzarol-bunnings/bmazzarol-bunnings/issues/25) · Jira: INTPLAT-1758 (epic), children 1759 (DBT), 1760 (EDP), 1761 (Airflow), under TPP-2077 (Support the data engineering uplift)

## Vision

Improve the working model for data engineering — review each codebase (DBT,
EDP, Airflow), document how it works, identify where agentic engineering can
improve the process, propose changes, and work through getting them done.

Approach: review → document → propose change → implement. The specific
changes are not yet known — they fall out of the reviews.

## Scope

In:
- DBT codebase review (INTPLAT-1759)
- EDP codebase review (INTPLAT-1760)
- Airflow codebase review (INTPLAT-1761)
- Agentic-craft improvements that emerge from the reviews

Out:
- TPP-2077 children owned by other assignees.
- Non-data-engineering agentic work (covered by map #2).

## Current state

- **DBT review completed 2026-10-29** — full report posted to Jira. Strong
  foundations (layered architecture, conventions, 3 Copilot skills); main gap
  is agent onboarding (no subject-area index, no model graph, no authoring
  skill). Top recommendations: agent onboarding layer, model authoring skill,
  slim model graph.
- Open: what the EDP and Airflow reviews will surface; sequencing of the
  remaining reviews; whether changes are scoped per-codebase or cross-cutting.

## Evolution log

- 2026-10-29 — Map created; destination set by user. Three review tasks mapped as frontier.
- 2026-11-04 — Plan doc created from map #25 (bucket standardisation, issue #34).
