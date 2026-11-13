# Handback

A worker hands back when it writes inflight `done`/`blocked` and posts the release
comment. You process it. Self-reported success is not verified success.

## Verification (before believing anything)

1. **PR exists?** `gh pr view <url> --json state,url,headRefName,statusCheckRollup`.
   For `done` with no PR: the worker lost the PR — read the pane (`herdr agent
   read worker-N`), re-dispatch.
2. **CI green on the CURRENT head?** Compare the PR's `headRefOid` to the CI run's
   commit. Green on an old commit is not green.
3. **Files present?** Spot-check the diff covers the acceptance criteria:
   `gh pr diff <url> | head -200`.
4. **Branch clean?** No stray commits beyond the card's scope (commit list vs DO steps).

Any check fails: do not report done. Re-enter the review-feedback loop (dispatch
skill) with the specific failure as the relay.

## Processing a verified done

1. Delete `state/inflight/<N>.json` and `state/floor/worker-N.json`.
2. Confirm the agent is gone (close the pane if the worker forgot: `herdr pane
   close <pane-id>`).
3. Move the card to In review (board status field or label).
4. Report to Ben: PR URL, CI state, one-line summary of what changed, what was
   verified. This is the "ready for your merge" moment — do not merge.

## Processing a blocked

1. Keep the floor file (the worker may be re-prompted); delete the inflight file.
2. Report the blocker to Ben with the worker's note verbatim.
3. If the blocker is a design decision: this is escalation class 1 — wait for his
   direction, then re-enter the review-feedback loop.
4. If the blocker is infra: fix what you can, re-dispatch, note it at standup.
