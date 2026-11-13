# TelegramGateway

Two-way Telegram access to the 2IC (`rivet`) via herdr.

## What it does

**IN (Telegram → rivet):** long-polls the Bot API (`getUpdates`, offset, 30s
server-side timeout) using `HttpClient` only — no NuGet packages. A text
message from an allowed user is injected into the rivet agent:

```
herdr agent prompt rivet <content> --wait --timeout 120000
```

If the text equals a task file basename in `tooling/WakeTool/tasks/*.md`
(e.g. `heartbeat`), the file's content is used instead of the raw text. A
short ack (e.g. `injected to rivet`, or the herdr exit status) is posted back
on Telegram. Messages from non-allowed users are ignored and logged.

**OUT (rivet → Telegram):** polls `state/telegram/outbox/` every 2s. Each
file's content is posted to the captain on Telegram; on successful send the
file is moved to `state/telegram/sent/`. Failed sends leave the file in place
for the next poll. Both directories are created if missing.

## Start

```
dotnet run -c Release
```

The 2IC manages the process lifecycle (start/stop/restart); there is no
service wrapper.

## Config sources

| Setting | Env var | File fallback |
|---|---|---|
| Bot token | `TELEGRAM_BOT_TOKEN` | `/home/bmazzarol/.config/rivet/telegram-bot-token` |
| Allowed Telegram user IDs (comma-separated) | `TELEGRAM_ALLOWED_USERS` | `/home/bmazzarol/.config/rivet/telegram-allowed-users` |
| Repo root (locates `state/` and `tooling/WakeTool/tasks/`) | `RIVET_REPO_ROOT` | walks up from CWD to `AGENTS.md` |

The token is never logged, committed, or written to the repo. Logs go to
console only; message bodies are logged as a short prefix (≤40 chars).

## Files

- `Program.cs` — all of it: config, Bot API client, IN/OUT loops.
- `TelegramGateway.csproj` — .NET 11 console app, zero package references.
