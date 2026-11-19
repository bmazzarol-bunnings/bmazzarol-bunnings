# Retro — #72 BaseExecutor static scratch buffer (Q1-001)

Date: 2026-11-15 · Outcome: SHIPPED (PR dotnet.common#1799, merged 13:09 UTC)

## Timeline
- 14:45 Nov 14: worker-1 dispatched (wave-1)
- 15:04: fix done, bench run 1 +22% under load → re-run kicked off → **question-tool placeholder stall (~8h)**
- 06:59 Nov 15: Ben answered in worker's Telegram thread → resumed
- 07:04: run 3 on quiet host, within 5%, PR raised
- ~08:50: nudge #1 (handback protocol not done) → restated done, no action
- 00:52 UTC: handback executed (release + inflight + pane close) after nudge #2 with literal commands
- 13:09 UTC: Ben merged

## What worked
- Option 1 (drop reuse) chosen as intended; single-file diff; 3-run bench with quiet-host run as decider
- Worker self-diagnosed host load as the +22% cause and re-ran instead of forcing a pass

## What didn't
- **question-as-keepalive**: worker called the question tool with a placeholder to "keep the session active" while a bg job ran → 8h stall. Root cause: model doesn't know pi auto-injects bg completion
- **Handback skipped twice**: finished at 07:04 but no release/inflight/pane-close until explicitly ordered with literal commands

## Improvements
1. DONE — task-body template: WAIT RULE (bg completion is injected; never question-as-keepalive)
2. DONE — task-body template: NO-USER RULE (question blocks forever; write BLOCKED instead)
3. DONE — task-body template: HANDBACK block (3 steps in the body itself)

## Follow-ups
- Heartbeat should detect "idle agent + open card + no inflight update > 1h" as stalled-with-possible-finished (distinguish from true stall by checking for a PR)
