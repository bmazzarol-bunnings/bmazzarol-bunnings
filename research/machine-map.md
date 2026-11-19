# Machine Map — bmazzarol's work laptop (WSL2)

High-level durable reference. The contents of `work/` change as projects come and go — don't treat any repo list as canonical, look at the directory.

## The 2IC repo (control plane)

`/home/bmazzarol/work/bmazzarol-bunnings` — the 2IC (Rivet) lives here. Stable layout:

- `AGENTS.md` — 2IC contract (role, gates, loop)
- `.github/skills/` — 2IC skills (dispatch, supervise, bearings, decisions, afk, laptop-watch, stow, herdr)
- `plans/` · `research/` · `retros/` — durable docs (plans, findings, retros)
- `state/` — live records: `2ic.json` (pinned session), `floor/` (crewmate dispatches), `inflight/` (worker liveness), `cron.json`, `standup-state.json`
- `tooling/WakeTool/` — cron/events hub (log: `/tmp/waketoool.log`); `tooling/voice/` — Telegram STT/TTS
- `.pi/` — project-level pi config (pi-telegram)

## Development work

- `/home/bmazzarol/work/` — all development repos live here; the set of repos changes over time. Domain repos (item, dotnet.common, supplier, checkout, ...) are company repos under the `Bunnings-Technology-Delivery` GitHub org.
- Worktree convention: `<repo>-<issue-or-slug>` next to the main checkout, or `<repo>-worktrees/<slug>` for multi-worktree repos (item uses `item-worktrees/`). Workers get a fresh worktree per dispatch; merged worktrees get removed.
- Company repos: draft PRs from workers, Ben merges (hard gate). 2IC workboard = issues on `bmazzarol-bunnings/bmazzarol-bunnings` (labels: `delivery:task`, `tech-review:approved`, `delivery:hitl`).

## Well-known config folders (stable)

- `~/.config/` — tool + project config; many important per-tool projects live here (e.g. `rivet/` — pi-telegram bot token, `octopus/`, `sonar/`, `bagc/`, `cosmos/`, `iop-soe/`, `llm-models/`, `llm-proxy/`, `herdr/`, `acli/`, `oracle/`, `gh/`) — the exact set grows over time, look at the directory
- `~/.pi/` — pi itself: a git repo with the harness (`harness/`, `pbb`, extensions, AGENTS.md); `~/.pi/agent/` holds agent config (trust.json, sessions, models, settings.json, telegram.json, extensions)

## Machine-level (stable)

- Herdr: workspace `w1` = 2IC floor (rivet + workers), `w2` = Ben's
- nix env on PATH (`.nix-profile/bin`): dotnet, octopus CLI
- Windows interop via `/mnt/c`; Windows-side az CLI works from WSL: `/mnt/c/Program Files/Microsoft SDKs/Azure/CLI2/wbin/az`
- Known quirk: net8 runtime broken on this box — test runs use `dotnet exec` on net10 (MTP fallback)
