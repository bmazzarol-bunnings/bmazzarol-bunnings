# RETRO 2026-11-03 — #31 Docsite font scaling (dotnet.common)

**Outcome:** Merged 2026-11-04 (cf389acea). Worker-built, verified.
**Lane:** laptop, agent-managed; worker pane did the coding.

## Timeline (phases)

Intake → design decision (1 question: zensical exact values) → tech review
published + approved → implementation v1 (**agent did it itself — role
violation**) → stopped at review gate per Ben → branch reset, re-dispatched
to worker pane → RESULT in mailbox → verified (commit/diff/build/render) →
force-push → PR updated.

## What worked

- Scoping with `gh`/local inspection BEFORE asking; one design question
  settled the only real ambiguity (step-up-only vs exact values).
- Tech review as a published issue artifact — auditable, and it became the
  worker's task spec almost verbatim.
- Task-file + one-line dispatch (multi-line `tmux_type` payloads break JSON
  encoding).
- Verify-before-belief held: commit hash, diff content, built `_site`, and a
  browser width-matrix (1366/1600/1920/2560, light+dark) with parity against
  the live KB site — measured, not eyeballed.
- Marker contract (RESULT:/BLOCKED: to the 2ic mailbox) made completion a
  cheap parse, not a conversation.

## What didn't

- Agent coded v1 itself. 2IC doesn't do; the work had to be redone through
  the worker. Cost: one build cycle + a force-push.
- `terminal(background=true)` is broken here (non-TTY shells exec into
  Nushell → REPL error). Fell back to a nohup hack; tmux panes are the
  correct long-running lane.
- Manual-approval prompts: 5 gates (file edit, dotnet ×2, git, 2 MCP tools)
  ate most of the wall-clock. Fixed: `--allow-all-tools` in the floor.
- Waited on CI for a docs-only PR — the pipeline can't pass for docs changes
  (no tests, no changelog). Local build is the gate for this class.
- Added #31 to the company Agentic Workboard — that board is a HANDOFF board;
  agent-driven work stays on the personal board.

## Judgement calls (who decided what)

- **Ben:** zensical exact values (whole-site ~25% bigger); stop-at-review
  gate; yolo on worker sessions; handoff-board semantics.
- **Agent:** branch naming, rem conversions, px-outlier selector list,
  force-push after verification, PR description shape.

## Improvements

1. Role boundary (2IC never codes) → skill patch, DONE (worker-management).
2. Long-running process lane = tmux pane via tmux-bridge → skill patch, DONE.
3. `--allow-all-tools` on worker launch → skill + tsm `work.yaml`, DONE.
4. Docs-only PRs: don't gate on CI → memory, DONE.
5. Handoff-board semantics → intake-work skill + memory, DONE.
6. OPEN: one-shot ETA wake per dispatch — 30m heartbeat is too coarse for
   short tasks; schedule a single check-in at expected completion.
7. OPEN: multi-worker attention model (below) — validate with 2-3 real
   parallel dispatches before trusting it.

## Follow-ups

- DONE 2026-11-04: merged (cf389acea), #31 closed, board Done, worktree +
  branch removed, retro committed.
- Wire one-shot ETA wakes (improvement 6).
- Volume test: parallel dispatches (improvement 7).
