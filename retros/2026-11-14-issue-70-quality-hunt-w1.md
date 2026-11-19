# Retro — #70 Common Quality Loop W1: hunt tooling + wave-1 pool

Date: 2026-11-14 · Outcome: SHIPPED (PR #71, merged 15781ba)

## Outcome
First wave of the Common Quality Loop delivered: repeatable hunt tooling
(Roslyn Indexer + 9 DuckDB queries + CodeQL harvest + benchmark runner),
ranked 25-entry debt pool for Foundation/Pipelines/AI, benchmark baselines.
~28 min worker time, one pass, no re-prompts.

## Timeline
- 13:32 Ben asks for iterative quality system → design + posture decision (internals-only, behavior-preserving)
- 13:35 card #70 raised; design stowed (plans/27)
- 13:45 Ben approves ("sounds good to me") → stamp + dispatch
- 13:53 worker-3 launched (herdr pane + worktree)
- 14:21 worker-3 handback (release comment, draft PR #71), pane self-closed
- 14:35 verify: files, determinism gate, mergeable → reported
- 14:5x Ben merges → closed, retro

## What worked
- Pool format + scoring formula held up: 25 ranked, evidence-keyed entries, actionable directions
- Worker followed task body top-to-bottom; gates (determinism, clean target) executed
- Small bounded card = one pass, no loop

## What didn't
- **Dirty-tree gate misread**: "git status must be clean" interpreted as *make* it
  clean → worker discarded 3 pre-existing uncommitted skill edits in dotnet.common
  (likely unrecoverable; no live worker owned them)
- `/telegram-connect` stalled on dispatch (known best-effort flake) — worker's
  phone thread lost this session

## Judgement calls
- Treated Ben's "sounds good to me" as the tech-review stamp (current explicit
  instruction outranks the stamp ritual)
- Kept pool + tooling in personal repo, not dotnet.common (org repo stays clean)
- Carded 4 wave-1 items from pool top (floor capacity)

## Improvements
1. DONE — task-body-template.md: never clean a dirty tree; dirty-on-arrival vs
   dirty-on-exit is the check
2. OPEN (rivet) — plan doc plans/27-common-quality-loop.md still untracked in main
   checkout; commit via next worker PR or Ben's call
3. OPEN (rivet) — re-hunt after wave-1 merges: refresh pool, diff, re-rank

## Follow-ups
- Wave-1 cards raised from pool top (see issue tracker) — awaiting Ben stamps
- Q1-008 held: public API impact → needs Ben's call vs internals-only posture
- Q1-013 (Pipelines 5.19× sync transform) → wave 2, investigation card
- Tooling gaps recorded in pool footer (duplication, dead-code call graph,
  hotspot rules) — candidates for wave-2 tooling card
