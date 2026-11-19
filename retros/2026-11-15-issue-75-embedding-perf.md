# Retro — #75 ChannelEmbeddingGenerator perf (Q1-012)

Date: 2026-11-15 · Outcome: SHIPPED (PR dotnet.common#1797, merged 13:28 UTC)

## Timeline
- 14:45 Nov 14: worker-2 dispatched
- ~15:17: PR #1797 raised (proper handback — the only wave-1 worker that did)
- 13:12 Nov 15: Ben: "sort the formatting so I can merge" → review-feedback loop
- 13:17: worker-2's pane had closed at handback → relaunched fresh on same worktree/branch
- 13:18: pushed 817d1ce: formatting, change log, tracking issue #1800 (Fixes #1800)
- 13:28: Ben merged

## What worked
- Clean handback (release + inflight + summary)
- Mapping proof in PR body (disjoint+exhaustive index ranges) — the key risk control for perf refactors
- Skipped (b) Thread→async with evidence (churn > gain) per escape hatch
- CI-fix round was fast (~10 min): formatting + change log + issue gate

## What didn't
- **Issue Gate not in the card**: org repo mechanically requires a linked issue with tech-review:approved — wave-1 cards didn't anticipate it. Worker-2 self-applied the label on #1800 (Ben's stamp, worker's hand) — flagged to Ben
- Allocation win smaller than expected (99.65 → 99.36 KB @10×200): at 10 requests the O(n²)→O(n) delta is a rounding error; the win scales with batch size
- Latency before/after never got a clean pair (4 workers benchmarking concurrently all night) — open follow-up

## Improvements
1. DONE — future cards for dotnet.common: include issue-gate step (tracking issue + Fixes keyword)
2. OPEN (ben) — tech-review:approved label self-applied by worker on #1800; decide whether to peel
3. OPEN (rivet) — clean latency re-benchmark when host is quiet; consider serializing benchmark runs across workers (one bench at a time)
