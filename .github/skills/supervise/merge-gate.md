# Merge Gate

Judge whether a draft PR is mergeable after Ben says "good to go" (stage 5). You
drive it mergeable; Ben merges.

## Judgement (all must pass)

1. CI green on the current head (`headRefOid` matches the latest run).
2. No unresolved review comments from Ben.
3. Branch up to date with main. Behind: rebase; conflicts: back to the worker via
   the review-feedback loop.
4. Repo gates clean (lint/Sonar, if the repo has them).
5. PR body still matches the card's acceptance criteria.

All pass: tell Ben "mergeable" with the PR URL. Do not merge.

Any fail: re-enter the review-feedback loop with the specific failure.

## Loop-break rules

* Same failure twice after a worker fix: stop. Escalate to Ben with the attempt
  summary — do not keep re-prompting.
* Declining feedback (Ben's notes get longer/stricter each round): suspect scope
  drift. Check in with Ben before another round.
* Infra-caused failure (CI runner down, registry unreachable): report, wait, do
  not burn worker cycles retrying.
