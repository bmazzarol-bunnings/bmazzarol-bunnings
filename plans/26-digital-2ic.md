# Digital 2IC (Hermes + Hindsight + tmux)

Map: [#26](https://github.com/bmazzarol-bunnings/bmazzarol-bunnings/issues/26) · Jira: — (internal infra, tracked on GitHub only)

## Vision

Build a "digital 2IC" — a central Hermes session living in this repo that Ben
talks to directly, which manages status and dispatches work to other agent
sessions (Claude / Codex / Copilot) doing the actual work. Composed from tools
that each do one job well:

1. **Hermes** (NousResearch/hermes-agent) — the 2IC brain: memory, cron,
   webhooks, skills; the thing Ben talks to.
2. **Hindsight** (vectorize-io, `local_embedded` mode) — scalable long-term
   memory (knowledge graph, entity resolution, cross-memory synthesis) without
   a heavy standalone Postgres on the laptop.
3. **tmux-bridge-mcp** (howardpen9) — live read/message control of worker
   sessions running in tmux panes.

## Constraints

- **Work laptop only** — no VPS, no external server. Everything runs locally.
- **Compose, don't monolith** — each component does one job; no new
  heavyweight framework.
- **Memory must scale beyond MEMORY.md** — Hindsight `local_embedded`
  (embedded Postgres, no separate daemon) is the chosen path.
- **This repo is the 2IC's home** — skills, config, and state anchored here
  and versioned in git.

## Scope

In:
- Hermes as orchestrator (already trusted; built-in cron/webhooks/skills).
- Hindsight local_embedded for durable memory.
- tmux-bridge-mcp for worker control (zero infra beyond tmux + Node 18).
- The delivery/workboard machinery: buckets, plans, triage, intake, shipping.

Out:
- Self-hosted Honcho (needs Postgres + Redis + workers — too heavy).
- Managed-cloud memory (work data leaves the laptop — unacceptable).
- Mem0 (extra moving part Hindsight already covers).

## Current state

- Phase: herdr + pi migration (2026-11-13 decision). 2IC = one pinned pi
  session (`state/2ic.json`) in a herdr pane in this repo, aigateway/qwen-27b.
  Workers = pi sessions in herdr panes on git worktrees (herdr pi integration
  v9 gives lifecycle state). WakeTool (.NET) is the single cron/events hub:
  versioned task definitions (`tooling/WakeTool/tasks/*.md`) + `herdr agent
  prompt` inject mode. Phone access = Telegram only (corp network, no inbound).
- Migration card on the board: AGENTS.md floor rewrite, skill set, WakeTool
  Herdr mode, cutover runbook below.
- Hermes and Hindsight retired to reference-only at cutover.

## Evolution log

- 2026-11-04 — Plan doc created from map #26 (bucket standardisation, issue #34).
- 2026-11-10 — **Pivot to Copilot-CLI-native firstmate.** The Hermes/Hindsight/tmux-bridge
  composition above is superseded: the 2IC becomes one all-day Copilot CLI session in this
  repo (the "first mate"), composed only from approved Copilot primitives. Hybrid worker
  model (subagents + tmux copilot crewmates), three ported cron jobs driven by a .NET wake
  tool running as a process under this repo, built-in memory only, seven repo-level skills
  (dispatch, supervise, bearings, decisions, afk, laptop-watch, stow), Rivet identity kept.
  Telegram gateway tracked as in-repo .NET work (#49); personality port tracked as #50.
  Decision log on map #26. The vision (one captain, one interface; scripts own mechanics,
  agents own judgment; restart is a non-event) carries over unchanged — only the substrate
  changes.
- 2026-11-13 — **Pivot to herdr + pi.** The copilot-offline/tmux floor is superseded.
  Decisions (settled with Ben):
  1. Workers: pi everywhere (crewmates and subagents), aigateway/qwen-27b default,
     `.github/skills` honored in-repo; no offline-copilot shield needed — pi +
     aigateway already outside org policy.
  2. 2IC: the pinned pi session in a herdr pane (workspace in this repo);
     boot = `pi --continue` in that pane; restart is a non-event.
  3. Phone: Telegram only (#49 re-scoped, injection via `herdr agent prompt`);
     `herdr --remote` dropped — corp network blocks phone SSH.
  4. Memory: built-in pi (session store, compaction) + stow; Hindsight retired,
     installed as reference only.
  5. Board: My Workboard project unchanged.
  6. Recurring work / events: WakeTool stays .NET (Ben's home language) and is
     the single hub — jobs = schedule + monitor fingerprint + versioned task
     definitions; no inbound webhooks possible, so polling + Telegram
     long-poll are the only doors and both are covered.
  Cutover runbook:
  1. Ben: `herdr` — attach default session; workspace auto-created in this repo.
  2. Ben: split a pane, run `pi --continue` there; verify AGENTS.md loaded.
  3. 2IC (in-pane): verify herdr skill, `herdr agent list` works from the pane.
  4. 2IC: stop Hermes gateway + cron (reference-only from now).
  5. 2IC: start WakeTool with Herdr inject mode (after migration card ships).
  6. Retire the pre-herdr pi session.
  The vision (one captain, one interface; scripts own mechanics, agents own
  judgment; restart is a non-event) carries over unchanged — only the substrate
  changes.
