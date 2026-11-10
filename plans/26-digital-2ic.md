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

- Phase 3 of the build-out. Memory (Hindsight + local llama.cpp embeddings on
  :8090) live; tmux-bridge worker loop verified end-to-end; Telegram gateway
  live. Workboard seeded with buckets + plans (issue #34).
- Next: worker-management skill authoring; ongoing bucket categorisation.

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
