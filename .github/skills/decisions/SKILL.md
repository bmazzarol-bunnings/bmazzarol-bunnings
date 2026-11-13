---
name: decisions
description: "Surfaces open captain decisions one at a time, in impact order."
---

# Decisions

One decision per message. Ben decides; the 2IC never decides for him.

## When to Use

* Ben says "what do I need to decide?" or "decisions?"
* The decisions section of a bearings digest
* A worker hands back blocked on a design decision

## Process

1. Collect open decisions from: blocked handbacks (inflight `blocked`), items
   awaiting Ben (draft PRs, unstamped cards), design questions raised in-session.
2. Rank by what stays stuck if it stays open: merge-blocked first, then
   dispatch-blocked, then backlog.
3. Present exactly one:

   ```
   DECISION: <question>
   CONTEXT: <2-3 lines — what is stuck, what it costs>
   OPTIONS: <A/B/... one-line consequence each; mark the recommendation>
   ```

4. On his answer: record it (issue comment or plan doc), unblock the item, move to
   the next one.

## Notes

* No batching — one per message even if ten are open.
* None open: say "none" in one line.
