# 27 — Common Quality Loop

Status: ACTIVE (wave 1: 3 of 4 merged; #74 in review-feedback loop)
Shipped: #72 → PR 1799 · #73 → PR 1796 · #75 → PR 1797 (all merged 2026-11-15)
In flight: #74 → PR 1798 (worker-4: Ben's pool comment + formatting + CI green)
Held: Q1-008 (public API impact — Ben call) · Q1-009 (orphaned Options project) · Q1-013 (wave-2 investigation: 5.19× sync transform) · Q1-004 (needs pool-eviction design)
Learnings: see retros/2026-11-15-issue-{72,73,75}*.md — template now has WAIT/NO-USER/HANDBACK rules; dotnet.common cards need issue-gate step
Owner: rivet (orchestration), ben (tech-review stamps + PR merges)

## Problem

Common (dotnet.common) accumulates quality debt faster than manual refactoring
can keep up, because AI-generated code volume is high. We need an iterative,
independent quality system — not tied to incoming PRs — that hunts down code
and improves it for simplicity, performance, and readability.

## Posture (Ben-approved 2026-11-14)

- **Internals-only, behavior-preserving.** No public API changes.
- Public API cleanups (deprecations, obsolete attrs, signature tweaks) tracked
  separately, not part of the loop.

## System

```
HUNT → POOL → CARD → [Ben stamps] → WORKER → DRAFT PR → [Ben reviews] → MERGE → RE-HUNT
```

1. **Hunt** — repeatable scan per wave (subagent/worker task, no org-repo changes):
   - Roslyn Indexer → DuckDB queries: hot paths, long/complex methods, god
     classes, dead code, unused public surface, duplication.
   - CodeQL (already in CI): complexity hotspots, dead stores.
   - Benchmark harness: per-component baselines (`Common.Benchmark.props`
     exists; Pipelines + AI have benchmark projects) so perf claims are
     measured.
   - AI scout pass over top-N offenders: readability/simplicity findings,
     structured, evidence-attached.
2. **Pool** — ranked debt pool in this repo (`quality/pool-<date>.md` or
   `quality/` dir). One entry per finding: file, metric, evidence, direction,
   class (simplicity/perf/readability). Never written into dotnet.common.
3. **Card** — 2IC converts top-N findings into board cards (delivery:task)
   with hard acceptance criteria: behavior preserved (tests green), public API
   unchanged (API baseline diff), metric or benchmark improves.
4. **Gate** — Ben stamps tech-review per card (or per wave — his call).
5. **Worker** — crewmate, same delivery loop, one class/file family per PR.
6. **Verify + re-hunt** — on handback: verify API diff, benchmarks, metrics.
   After each wave, re-scan refreshes the pool. Wave size = 4 concurrent
   (floor capacity). Standing loop, cadence: next wave starts as prior handbacks
   process.

## Scope

- Wave 1: Components/Foundation, Components/Pipelines, Components/AI
  (benchmarks exist for Pipelines/AI; Foundation is the hot path for everything).
- Later waves: remaining components, expanded by impact (call frequency,
  consumer count).

## Guardrails

- dotnet.common is read-only for hunt tasks; only workers raise PRs against it.
- Every refactor PR: tests green, no public API diff, before/after metric or
  benchmark numbers in the PR description.
- Small PRs: one class/file family at a time. Verifiable, revertible.
- Ben merges everything (hard gate, no change).

## Open decisions

- Batch tech-review stamps per wave vs per card (load question — ask Ben at
  wave 1 review time).
- Where perf baselines live: this repo `quality/benchmarks/` (proposed).
