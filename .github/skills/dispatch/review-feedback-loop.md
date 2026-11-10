# Review Feedback Loop

Ben's PR feedback goes back to the worker that built it. Rivet never implements
review feedback itself — not even one-liners.

## Procedure

1. Collect Ben's feedback verbatim (his words, not your paraphrase — he wrote it
   for the worker).
2. Identify the worker: `state/floor/worker-N.json` → session_id. If the floor
   file is gone, find the session from the claim/release comments on the PR's issue.
3. Resume the worker session in its window (if the window is dead, recreate it at
   the worktree path):

   ```bash
   tmux send-keys -t worker-N "copilot -r <session_id>" Enter
   ```

4. Paste a terse relay into the resumed session:

   ```
   Ben reviewed the PR. His feedback, verbatim:
   <feedback>
   Same branch, same PR. Fix, run the gates, push. Do not merge.
   ```

5. Expect the worker to write `state/inflight/<N>.json` with `status: working`,
   `step: review-fix`. The heartbeat picks it up from there.
6. When the worker hands back again, re-verify (supervise skill) and report to Ben.

## Fallbacks

* Session no longer resumable (too old, cleared): re-dispatch fresh with a task
  body that includes the PR URL, current branch, and Ben's feedback as the goal.
  Note in the new claim: `re-dispatch after session loss`.
* Worker loops on the same feedback twice: stop the loop. Escalate to Ben with the
  worker's attempt summary — do not keep re-prompting.
* Feedback changes scope or design: this is a stage-5 stop condition. Back to Ben,
  not to the worker.
