# Boot Recovery

Run after any session restart, before any dispatch. Goal: reconcile durable state
with reality so the floor is honest again.

## Steps

1. Verify the herdr server is running: `herdr status`. If it is down, the 2IC's
   own pane is dead too — Ben must re-attach (`herdr`); report and stop.
2. List live agents: `herdr agent list`.
3. Read `state/floor/*.json` — every dispatched item.
4. For each floor entry, cross-check agent + inflight + issue comments:
   * Agent alive + inflight fresh: WORKING — leave alone.
   * Agent alive + inflight stale: STALLED — nudge once.
   * Agent gone + no release: CRASHED — re-dispatch fresh (the session is likely
     lost; a fresh dispatch with the full task body is safer than a resume guess).
   * Release present: process the handback (handback.md), clear the floor entry.
5. Find issues with a `[worker-claim]` but no matching floor entry: ORPHAN —
   recover per dispatch/claim-protocol.md.
6. Check CI (PRCI) on open PRs from our workers.
7. Report the reconciled state to Ben in one short message: what's working, what
   you recovered, what needs him.
8. Confirm the wake tool process is running (`tooling/`); if missing, say so —
   do not run manual ticks forever.

## Rules

* Reconcile before dispatch: a dispatch onto an unreconciled floor double-books
  workers.
* When in doubt between resume and fresh re-dispatch, choose fresh — a clean
  worker with a complete task body beats a half-lost session.
* Never delete floor files during recovery except when the handback is verified.
