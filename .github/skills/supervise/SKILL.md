---
name: supervise
description: Handles heartbeat ticks and worker liveness: handback verification, stalled/crash/orphan recovery, boot recovery, merge-gate judgement.
---

# Supervise

Keep the floor honest: verify what workers claim, recover what broke, and judge
what is ready for Ben. This is the standing duty behind every heartbeat tick.

For details, see:

* [handback.md](./handback.md) — verification and processing of worker handbacks
* [boot-recovery.md](./boot-recovery.md) — reconcile state after a restart
* [merge-gate.md](./merge-gate.md) — judging whether a draft PR is mergeable

## When to Use

* A wake-tool tick prompt arrives (heartbeat, change-gated)
* Ben says "check workers", "how's the floor?", or "anything stalled?"
* After any session restart, before dispatching anything (boot recovery)
* A draft PR needs a mergeable judgement

## Fingerprint reference

The heartbeat tick carries a fingerprint of floor state. Act on what changed:

| Signal | Meaning | Action |
|---|---|---|
| WORKING | inflight fresh (< 10 min) | nothing |
| STALLED | inflight stale (>= 10 min), window alive | nudge once; if stale again next tick, kill + re-dispatch |
| HANDBACK | inflight done/blocked, release comment posted | verify + process (handback.md) |
| CRASHED | window dead, no release, inflight working | read pane tail if possible, kill, re-dispatch |
| NO-STATUS | no inflight file for a claimed issue | grace one tick; if still absent, treat as CRASHED |
| ORPHAN | claim with no live worker | orphan recovery (dispatch/claim-protocol.md) |
| PRCI | CI state changed on an in-flight PR | gate judgement (merge-gate.md) |
| FLOOR=DOWN | `work` session missing | boot recovery (boot-recovery.md) |

Nudge discipline: never nudge a worker whose inflight status is done or blocked —
those are handbacks, not stalls.

## Nudge

```bash
tmux send-keys -t worker-N "Still working? Write state/inflight/<N>.json with your current step, or release if blocked." Enter
```

One nudge per stall episode. A second stall after a nudge means kill + re-dispatch,
not another nudge.

## Tick response convention

* Nothing needs Ben: respond with exactly `[SILENT]`.
* Something needs Ben: one short message — what happened, what you did, what you need.
* Never edit a skill from a tick. A suspected procedure bug is flagged at the next standup.

## Standup

First tick of the day (08:00–16:00) with no standup yet (check
`state/standup-state.json`): send the standup digest (format in AGENTS.md), then
write today's date.

## Error Handling

* tmux unavailable / `work` session gone: run boot recovery before any other action.
* gh API errors mid-tick: retry once; if still failing, note it in the next standup.
* State files corrupted (bad JSON): treat as missing; reconstruct from issue
  comments + tmux, and flag the corruption at standup.
