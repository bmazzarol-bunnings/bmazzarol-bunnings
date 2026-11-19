# Retro — #73 CodeQL quick-fix batch (Q1-002..005)

Date: 2026-11-15 · Outcome: SHIPPED (PR dotnet.common#1796, merged 13:31 UTC)

## Timeline
- 14:45 Nov 14: worker-3 dispatched
- ~15:03: 3 commits (Q1-002/003/005), PR #1796 raised during the bench
- 13:31 Nov 15: Ben merged (after undrafting)

## What worked
- One PR / four commits, exactly as carded
- **Q1-004 correctly deferred with a design argument**: ResourcesImpl is pooled and reused across runs — disposing the lazy SemaphoreSlim destroys it for the next run; needs a pool-eviction hook that doesn't exist. Escape hatch used properly
- Pre-existing test failure (TelemetryExample Case2) proven pre-existing via A/B at base commit

## What didn't
- Handback protocol skipped (no release comment, no inflight, pane left open) — verified manually
- Pool entry Q1-005 had a wrong path (Foundation/Reflection → actually Json/Converters); caught in pre-dispatch verification, card fixed

## Improvements
1. DONE — HANDBACK block in task-body template
2. OPEN (rivet) — pool entries need a path-verification pass before carding (scout-sourced paths can be wrong)

## Follow-ups
- Q1-004 → needs a lifetime-design decision (pool-eviction hook); candidate for a future design card
- Pool refresh after wave-1 merges will drop 002/003/005
