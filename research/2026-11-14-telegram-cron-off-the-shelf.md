# Off-the-shelf components for the Telegram + cron pieces

Date: 2026-11-14 (web research, Rivet)
Question: is there an existing well-defined offering that covers the Telegram
integration (full feature set: voice, image, ...) and/or the cron hub, so we stop
writing that complexity in `tooling/TelegramGateway` + `tooling/WakeTool`?

## TL;DR

- **Telegram:** yes — **OpenClaw** (openclaw/openclaw, MIT, 501(c)(3) foundation,
  very active). Self-hosted gateway on the laptop, Telegram via grammY with the
  full Bot API surface: inbound voice-note transcription (multi-provider),
  TTS replies incl. `[[audio_as_voice]]` voice notes, photos/video/docs in and
  out, stickers, inline keyboards, allowFrom/pairing security, long-poll or
  webhook. Runs local-first (state/memory/creds on our machine) — fits the
  "work laptop only" constraint.
- **Cron:** no good off-the-shelf match. WakeTool's distinctive features
  (versioned markdown task files + change-gate fingerprinting + herdr inject)
  are not what any standard scheduler offers. Hangfire/Quartz.NET/NCronJob/
  Coravel are heavier and none do markdown tasks or change gating. Keep WakeTool.

## OpenClaw — the main candidate

- Repo: https://github.com/openclaw/openclaw (formerly Clawdbot / Moltbot; by
  Peter Steinberger; stewarded by OpenClaw Foundation, 501(c)(3); donors incl.
  OpenAI, Anthropic-adjacent, NVIDIA. Not owned by any company.)
- Node.js/TypeScript, `npm i -g openclaw`, one gateway process (WS+HTTP on
  :18789, loopback by default), Control UI, CLI, TUI, companion apps.
- 36+ channels: Telegram, WhatsApp, Discord, Slack, Signal, iMessage, Teams,
  Matrix, IRC, WebChat... Telegram is a core channel (grammY).
- **Media/voice (the part we lack today):**
  - Inbound: voice-note transcripts (provider or CLI STT, e.g. whisper),
    image/audio/video understanding, `maxBytes` guards.
  - Outbound: TTS (ElevenLabs et al.), auto-TTS modes, voice-note delivery
    (`[[audio_as_voice]]`), image/video/music generation tools.
  - "Talk" modes: realtime, stt-tts, transcript.
- **Built-in cron** (automation: cron + webhooks + hooks) — a superset of our
  WakeTool scheduling, but *not* the same model: OpenClaw cron jobs are
  configured in its config/agent world, not versioned markdown files with
  fingerprint change-gating.
- **Agent runtimes:** ships its own embedded Pi-based agent runtime (sessions,
  memory, skills, tools, workspace). Also runs external coding harnesses via
  ACP (claude, codex, copilot, gemini, cursor, droid, ... and pi) — but those
  are *new sessions owned by OpenClaw*, not an attachment to an existing
  session. There is an ACP *bridge* mode where an external ACP client talks to
  OpenClaw as ACP server — the opposite direction from what we need.
- **No documented path to drive our pinned herdr pi session** (`herdr agent
  prompt rivet ...`). OpenClaw owns its sessions; herdr owns ours. Bridging
  would be a small custom adapter (plugin SDK exists; a "herdr" channel/agent
  plugin that shell-outs to `herdr agent prompt`).
- Security model: inbound = untrusted; DM pairing/approval, allowFrom,
  sandboxing docs, exposure runbook. Treat like any agent-given-shell tool.
- Forks in the same family (narrower, Pi-focused, smaller): crocbot
  (moshehbenavraham/crocbot), OpenHoof (OpenHoof-AI/OpenHoof) — "Any OS +
  WhatsApp/Telegram/Discord/iMessage gateway for AI agents (Pi)". Smaller
  surface than OpenClaw but same core; crocbot explicitly bridges
  Telegram→Pi via grammY with streaming.

## Pi-native Telegram bridges (added after OpenClaw rejected on corp policy)

Our 2IC is a pi session, so a pi extension is the cleanest slot: no new
runtime, no separate gateway process, loads into the pinned session.

### @llblab/pi-telegram — top candidate

- https://github.com/llblab/pi-telegram · `pi install npm:@llblab/pi-telegram`
- Actively maintained standalone fork of badlogic/pi-telegram (Mario Zechner,
  pi contributor). Pi 1.0+ peer dep.
- **Attaches to the live active pi session of the connected pi instance**
  (`/telegram-connect` "connects this Pi instance and its active session").
  Runtime adapter, not a remote terminal: queueing, previews, final replies,
  artifacts all scoped to the pi instance that accepted the work. No hidden
  processes, no PTY tricks.
- Full feature set we want:
  - Inbound: text, replies, edits, images, files, albums, voice notes (STT
    providers pluggable), forwards.
  - Outbound: rich markdown, draft streaming previews, files/artifacts via
    `telegram_attach`, voice replies (manual/mirror/always + `telegram_voice`),
    buttons, callback routing.
  - Queue runtime: prompts while busy become queued turns; reaction
    shortcuts (👍 priority, 🔥 steer, 👎 skip); control lane.
  - Operator menu (`/start`), prompt templates as Telegram commands
    (replaces our task-alias expansion), model/thinking switching,
    `/compact`, diagnostics.
  - **Threaded Mode**: one bot hosts one leader + visible follower pi
    instances, each with its own Telegram thread — workers could get threads.
  - Extension platform: companion extensions add commands, sections, voice
    providers without owning a second bot loop.
  - Security: first-`/start` owner pairing or preconfigured allowedUserId
    (we already have TELEGRAM_ALLOWED_USERS).
- Companion: johnlam1968/pi-voice-telegram (STT transcript echo, voice UX
  fill-ins) on top of llblab's voice pipeline.

### Other pi-Telegram options (shortlist)

- **earendil-works/pi-chat** (first-party, MIT, 435 stars): Telegram/Discord
  → sandboxed pi session in a Gondolin QEMU micro-VM per channel, persistent
  workspace/memory/skills, file attachments, tmux worker orchestration.
  Credibility max; but VM per channel and its own sessions — diverges from
  the herdr floor.
- **kariemSeiam/pi-telegram** (`pi-telegram` on npm, `pitg` + systemd unit):
  standalone service, spawns a pi subprocess per chat; **built-in cron**
  (one-shot/interval/cron expr, /cron menu, AI `<tg-cron>` tag); images,
  documents, session fork/undo, multi-bot. Cron here covers simple
  scheduling, but sessions are its own — not our pinned herdr session; voice
  not a headline feature.
- **pi-telebridge** (acarerdinc, small): two-way relay between the *active*
  pi session and Telegram. Minimal.
- **pi-telegram-connect** (artyom-ivanov): single-owner DM bridge for one
  local pi session, streaming, typing indicators.
- **pi-telegram-plus** (jalyfeng): full control, commands, menus, model mgmt;
  ~1.6k downloads/mo.
- **gamalan/pi-gateway** (fork of 0xKobold/pi-gateway): multi-platform
  (Telegram/Discord/Slack/WhatsApp), streaming, per-chat sessions, RBAC.
- **Qusic/pi-telegram**: fork of badlogic/pi-telegram with streaming +
  thinking markers.

### Claude/Codex-oriented (lower fit, noted)

- **smart-telegram-gateway** (tvpustovoy, MIT): Telegram↔Claude Code/Codex;
  receipts, pinned status line, voice pipeline, 14 MCP tools, episode
  bookkeeping. Best-in-class reliability design; wrong agent substrate.
- claudegram, TeleClaude, telecode, Telex, terranc/claude-telegram-bot-bridge,
  Jeffrey0117/ClaudeBot, Swigler/harness-telegram-bridge, carlosvianney/
  claude-telegram-bridge — all Claude Code/Codex bridges; voice+images common
  in the class.

## Revised recommendation (OpenClaw excluded by corp policy)

**Adopt `@llblab/pi-telegram` as a pi extension on the pinned 2IC session.**
It slots directly into the existing architecture: same process (the pi
session in the herdr pane), no new runtime, full voice/media feature set from
upstream, queueing + operator menu + Threaded Mode for workers. It replaces
TelegramGateway entirely (task-alias expansion → prompt templates; file
outbox → `telegram_attach`/direct delivery). WakeTool stays (cron verdict
unchanged).

Costs/risks:
- STT/TTS provider config: what's acceptable on corp laptop (local whisper
  vs API key)?
- Trust: third-party npm extension loaded into the 2IC with bot token; audit
  before cutover (it's a fork of a well-regarded original, heavily tested).
- Restart semantics: extension loads with the pi session; `/telegram-connect`
  re-run or auto-restore after rotation (Threaded Mode restores thread on
  session resume).
- Threaded Mode follower threads for workers = possible future simplification
  of the herdr-inject path; don't conflate in v1.

Spike card scope: install on 2IC, pair, verify voice in/out with a chosen
STT/TTS provider, verify queue + prompt-template behavior against our
heartbeat task, then cutover and decommission TelegramGateway.

## Other Telegram options (for completeness)

- **grammY** (TS), **aiogram** (Python), **python-telegram-bot** (Python):
  full Bot API frameworks. Libraries, not turnkey — voice transcription, TTS,
  media handling are still our code. python-telegram-bot has JobQueue (cron
  built in) but is Python and still not a "full feature set" offering.
- **smart-telegram-gateway** (tvpustovoy, MIT): Telegram↔Claude-Code/Codex
  session bridge; does voice transcription, receipts, pinned status line,
  undo windows. Smaller, Claude/Codex-oriented, not pi/herdr.
- **soulgate** (M4MEET): multi-channel personal AI gateway, 13 connectors,
  voice, tools. Broader than we need, heavier.
- **iva-agent** (smixs): self-hosted Telegram AI assistant, voice notes/photos
  into a markdown vault; systemd-based. Different product (memory vault), not
  a gateway for an existing agent.
- **praktor** (mtzanidakis): Telegram I/O for Claude Code agents in Docker.
  Different substrate.

## Cron side (why nothing fits)

- .NET options: Hangfire (queue + state machine + dashboard, needs storage
  provider — SQL/Redis), Quartz.NET (full scheduler, persistence, clustering —
  heavy), NCronJob (in-memory, lightweight), Coravel (ASP.NET-hosted),
  Cronos (expression parsing only, not a scheduler).
- None provide: versioned markdown task definitions under git,
  fingerprint-based change gating (zero agent runs when nothing changed),
  or herdr `agent prompt` injection. That combination *is* WakeTool.
- Verdict: keep WakeTool. It is small, does one job, and replacing it buys us
  a scheduler plus a database we don't need.

## Fit analysis for our architecture

Current pieces:
- TelegramGateway (.NET, 435 LOC, text-only, getUpdates long-poll, outbox dir)
- WakeTool (.NET, cron + change-gate + herdr inject)
- Pinned 2IC pi session in herdr pane; workers = herdr pi sessions

Option A — **OpenClaw as Telegram channel only**: keep pinned 2IC + herdr
floor + WakeTool; replace TelegramGateway with OpenClaw's Telegram channel +
a small herdr-bridge plugin (message → `herdr agent prompt`, reply → outbox/
Telegram). Gains: full voice/media for free, active upstream, security
model. Costs: Node process on the laptop, a custom plugin we maintain
(upstream-able), two agent worlds coexisting (OpenClaw's runtime would be
disabled/unused).

Option B — **Full OpenClaw adoption**: 2IC becomes an OpenClaw agent; its
cron replaces WakeTool; its sessions replace herdr panes. Gains: one
framework, everything built in. Costs: re-home the herdr worker floor,
AGENTS.md contracts, state/ conventions, all seven skills; biggest
architectural shift of the three; contradicts "compose, don't monolith".

Option C — **Extend TelegramGateway ourselves**: add voice STT, TTS, media in
.NET. The "complex code on our side" Ben wants to avoid; every feature is
re-implemented against upstream OpenClaw/grammY anyway.

## Open questions before deciding

1. Can OpenClaw's agent runtime be disabled and the gateway used purely as a
   channel bridge (config surface for a passthrough/external-agent channel)?
2. Does the plugin SDK support an outbound "custom agent" channel cleanly, or
   is the herdr bridge a hack on internals?
3. STT/TTS providers on a corp laptop: what's acceptable (local whisper vs
   provider API keys)?
4. Is a Node.js + npm-global process acceptable on the work laptop (pi is
   already Node; probably yes)?

## Recommendation (pending Ben)

Option A is the strong default: adopt OpenClaw for the Telegram edge only,
keep WakeTool, keep the herdr floor. Smallest bridge code, maximal feature
set, strongest upstream. Needs a spike to confirm Q1/Q2 before committing.
