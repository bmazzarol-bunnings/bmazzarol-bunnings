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

Progress (2026-11-13): infra PR #52 (AGENTS.md, skills, WakeTool baseline) and
WakeTool PR #53 (Herdr inject mode + task definitions) drafted for review.
**Finding:** `PI_REASONING_LEVEL=low` aborts aigateway/qwen-27b requests ("Request
was aborted") — worker default reasoning is **medium** (the unbiased level for
qwen); low is not usable on this provider/model pair.

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

## Telegram cutover runbook (pi-telegram, #64)

Retire the .NET TelegramGateway (text-only) for the `@llblab/pi-telegram` pi
extension at project scope, Threaded Mode: General = 2IC (leader), one thread
per worker session (followers). Voice runs on-device: faster-whisper STT
(offline-forced) + edge-tts TTS (keyless) via committed wrappers.

Preconditions (all checked before starting):

- #64 merged: `.pi/` project install of `@llblab/pi-telegram@0.59.1`,
  `.pi/prompts/` symlinks, `tooling/voice/` wrappers committed.
- Voice round-trip verified locally: `tts.sh "hello world test"` → OGG/Opus,
  `stt.sh` on it → "hello world".
- BotFather: private-chat Threaded Mode already enabled on the Rivet bot.
- `~/.config/rivet/telegram-bot-token` present, chmod 600 (existing gateway
  token — reused, not rotated).

### 1. Config — `~/.pi/agent/telegram.json` (user scope only, chmod 600, never in repo)

```json
{
  "profiles": {
    "default": {
      "botToken": "$TELEGRAM_BOT_TOKEN",
      "allowedUserId": 123456789
    }
  },
  "inboundHandlers": [
    {
      "mime": "audio/*",
      "template": ["/home/bmazzarol/work/bmazzarol-bunnings/tooling/voice/stt.sh", "--file={file}"]
    }
  ],
  "outboundHandlers": [
    {
      "type": "voice",
      "template": ["/home/bmazzarol/work/bmazzarol-bunnings/tooling/voice/tts.sh", "-", "{ogg}"],
      "output": "ogg"
    }
  ],
  "voice": { "replyMode": "mirror" }
}
```

Notes on the fields:

- `botToken`: prefer the `$TELEGRAM_BOT_TOKEN` env reference — `/telegram-setup`
  prefills it and persists the reference, not the secret. Fallback: if the
  herdr pane env has no `TELEGRAM_BOT_TOKEN`, store the existing value from
  `~/.config/rivet/telegram-bot-token` (chmod 600) directly in the config.
  The token never goes into this repo, in any file or commit.
- `allowedUserId`: Ben's numeric Telegram id (reuse the value already in the
  gateway's `TELEGRAM_ALLOWED_USERS`). Owner pairing still applies on first
  `/start`.
- `inboundHandlers` `audio/*` → local STT: transcript text becomes the prompt.
  Audio never leaves the laptop (HF_HUB_OFFLINE=1).
- `outboundHandlers` voice: text arrives on stdin, `{ogg}` is the output path
  (OGG/Opus is the sendVoice requirement). edge-tts sends reply text to
  Microsoft's endpoint — accepted trade for keyless TTS; swap to a local TTS
  later via the same handler template if needed.
- `voice.replyMode: mirror`: voice in → voice out; text turns stay text.
- Paths point at the main checkout (not a worktree) so the config survives
  worktree churn.

### 2. Connect + pair (in the pinned 2IC session)

1. 2IC: `/telegram-setup` — paste the bot token (or confirm the pre-filled
   `$TELEGRAM_BOT_TOKEN` env reference). Verifies the token, saves config.
2. 2IC: `/telegram-connect` — this pi instance becomes the leader and owns
   bot polling.
3. Ben's phone: DM the Rivet bot and send `/start` — first successful `/start`
   pairs the owner.
4. 2IC: `/telegram-status` — leader, polling healthy, owner paired.

Workers: the dispatch envelope now runs `herdr agent prompt worker-N
/telegram-connect` (best-effort) after session start — the worker becomes a
follower with its own thread. Subagent one-shots (`pi --print`) never connect
and never create threads.

### 3. Validation passes (worker drives, Ben's phone verifies)

1. **Text round-trip (General):** phone DM → 2IC; streamed reply lands back.
2. **Prompt template:** send a task name (e.g. `/heartbeat`) → the task file
   content is injected (parity with the old alias expansion).
3. **Voice:** voice note in → transcript reaches 2IC; 2IC voice reply delivers
   as a native OGG/Opus voice note.
4. **Media:** inbound image inspectable by 2IC; 2IC artifact sent as a file.
5. **Worker thread:** dispatch a real worker → its thread appears; a phone
   message in that thread reaches the worker; handback posts to the thread;
   thread persists after handback (cleanup only on session delete).
6. **Restart pass:** restart the 2IC session → leadership/ownership preserved,
   reconnect works (`/telegram-connect` again if needed); resume a worker
   session → its thread restored.

### 4. Decommission (only after every validation pass is green)

1. Stop the TelegramGateway process.
2. Delete `tooling/TelegramGateway/` and `state/telegram/` (outbox dirs) —
   one PR, after Ben's word.
3. Update this plan's Current state + AGENTS.md references (gateway →
   pi-telegram; task-alias expansion → prompt templates).
4. Close #62 (duplicate inbound) as moot — no duplicate inbound paths remain.

Rollback (any point before step 4): `/telegram-disconnect` in 2IC; restart the
TelegramGateway process. The gateway code stays in git history until its
deletion PR lands.
