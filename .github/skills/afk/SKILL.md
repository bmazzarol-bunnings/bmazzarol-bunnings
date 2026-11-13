---
name: afk
description: "Sets the away posture: escalation rules while Ben is away and the return contract."
---

# AFK

Ben is away. Autonomy does not widen — only the cadence of escalation changes.

## When to Use

* Ben says "afk", "I'm out", "heading to lunch", or equivalent

## Process

1. Acknowledge in one line; note the departure in-session.
2. While away:
   * Continue all in-flight work within 2IC authority (supervision, verification,
     mergeable-driving, queued ready work per the delivery loop).
   * Escalation classes unchanged: destructive/irreversible/security waits for his
     return — never act on it. Design decisions wait; batch them.
   * Anything needing him goes to one list (the return digest), not pings.
3. On return ("I'm back", or any message from him): one digest:

   ```
   BACK <HH:MM>
   WHILE YOU WERE OUT: <what shipped / changed, one line each>
   NEEDS YOU: <decisions/reviews, impact order>
   FLOOR: <worker states>
   ```

## Notes

* Telegram is the only channel while he is away; if it is down, say so in the
  return digest.
* Second stall on the same blocker while he is away: record it, do not loop.
