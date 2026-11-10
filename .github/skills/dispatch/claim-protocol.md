# Claim Protocol

Issue comments are the system of record. Latest marker wins.

## Marker format

```
[worker-claim]   worker=worker-N session=<uuid> started=<ISO8601>
[worker-release] worker=worker-N session=<uuid> finished=<ISO8601>
<note: one-line result + PR URL, or BLOCKED: reason>
```

The note line belongs to the release marker only.

## Pre-flight (before every dispatch)

1. `gh api repos/<owner>/<repo>/issues/<N>/comments --paginate` — read the last marker.
2. No marker: clear to dispatch.
3. Claim present:
   * Window `worker-N` alive (`tmux list-windows -t work`) and inflight file fresh
     (< 10 min): live owner. Do not dispatch; report who owns it.
   * Window dead or inflight stale: orphan. Run orphan recovery.
4. Release present: clear to dispatch. If Ben re-dispatches, a fresh claim supersedes.

## Orphan recovery

A claim with no live worker means the worker died without releasing.

1. Read `state/floor/worker-N.json` for the session_id (if present).
2. Try resume: `copilot -r <session_id>` in a fresh window at the worktree path.
   If the session has recent activity, let the worker finish what it started.
3. Session gone or work lost: post a `[worker-release]` comment noting
   `ORPHAN RECOVERY`, delete the floor file, re-dispatch fresh.
4. Never leave an orphaned claim unprocessed — it blocks the issue silently.

## Retroactive claim

If you discover a worker was dispatched without a claim comment (manual dispatch,
tooling bug), post the claim immediately with the real start time if known, or
`started=<now> RETROACTIVE`. The floor file must exist too.

## Release processing

Releases are processed by the supervise skill (handback verification), not here.
The claim side ends at dispatch.
