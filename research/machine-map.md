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
- **dotnet.common changelog policy** (Ben, 2026-11-20): `Changes/` entries are added ONLY when there is something a developer needs to know (API/behaviour change, migration, security fix visible to consumers). Internal-only fixes, suppressions, refactors with identical behaviour: no entry — the change-log CI gate failing is expected and merged past.

### Repo inventory (snapshot 2026-11-19 — re-sweep, it moves)

| Group | Repos (remote org/repo) |
|---|---|
| **2IC / control plane** | `bmazzarol-bunnings/bmazzarol-bunnings` (this repo) |
| **Item domain** | `item` (Bunnings-Technology-Delivery) + `item-worktrees/`; personal: `azure-cosmos-dotnet-v3`, `pi` (harness fork), `llm-box-config` (llm-box org) |
| **Common** | `dotnet.common` + `dotnet.common.webapi` (Bunnings-Technology-Delivery) |
| **Supplier** | `supplier`, `supplier.feed` (company); `supplier-bm` (personal fork) |
| **Other domain** | `checkout.api`, `payment.api`, `pricing.api`, `communications`, `loyalty`, `authorisationserver.admin`, `documents.azure`, `dataplatform.vertex.ai` (all Bunnings-Technology-Delivery); `gcp.service-accounts` (Bunnings-Technology-Infrastructure) |
| **Engineering / standards** | `bunnings-agentic` (Bunnings-Engineering), `iop-development-soe`, `iop-standards`, `ipt-standards` (= integration-platform-team.standards), `integration.architecture.process.design.project`, `GHA.API.Templates`, `agentic-kb-template` (all Bunnings-Engineering) |
| **Data / other orgs** | `edp-dbt` (Bunnings-Data-and-Analytics) |
| **Personal / scratch** | `workato-labs`, `frontend-design-assistant` (assistant — see ASSISTANTS.md), `llm-box-config`, `pi` (bmazzarol-bunnings org) |

### Worktrees

None attached (2026-11-19 sweep — all removed; branches preserved where work is live: `enriched-read-cosmos` (item, PR #133 in review), `otel/harness-logging` + `otel/python-logging` (bunnings-agentic), `fix/docs-skills-card-toc-chevron` (dotnet.common, checked out in the main clone). Stale worker branches deleted: worker/56-cronos, worker/62-tg-offset, worker/64-pi-telegram, caf-sweep-map, worker/74-q1-fix, 1787.)

Repos sitting on non-main branches (work in flight or stale — check before assuming main): `GHA.API.Templates` (fix/mtp-trx), `agentic-kb-template` (feat/llm-wiki), `azure-cosmos-dotnet-v3` (users/…/fix-orderbyrank), `checkout.api` / `payment.api` / `dataplatform.vertex.ai` (upgrade/bunnings-common-*), `edp-dbt` (tokyo/…), `iop-standards` (rfc/0006), `ipt-standards` (fix/duplicate-home-nav), `supplier` (fix/search-cosmos-materialization), `dotnet.common` (fix/docs-skills-card-toc-chevron).

## Well-known config folders (stable)

- `~/.config/` — tool + project config; many important per-tool projects live here (e.g. `rivet/` — pi-telegram bot token, `octopus/`, `sonar/`, `bagc/`, `cosmos/`, `iop-soe/`, `llm-models/`, `llm-proxy/`, `herdr/`, `acli/`, `oracle/`, `gh/`) — the exact set grows over time, look at the directory
- `~/.pi/` — pi itself: a git repo with the harness (`harness/`, `pbb`, extensions, AGENTS.md); `~/.pi/agent/` holds agent config (trust.json, sessions, models, settings.json, telegram.json, extensions)

## Machine-level (stable)

- Herdr: workspace `w1` = 2IC floor (rivet + workers), `w2` = Ben's
- nix env on PATH (`.nix-profile/bin`): dotnet, octopus CLI
- Windows interop via `/mnt/c`; Windows-side az CLI works from WSL: `/mnt/c/Program Files/Microsoft SDKs/Azure/CLI2/wbin/az`
- Known quirk: net8 runtime broken on this box — test runs use `dotnet exec` on net10 (MTP fallback)
