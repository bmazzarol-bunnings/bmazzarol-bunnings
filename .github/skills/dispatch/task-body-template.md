# Task Body Template

The task body is the worker's entire context. A worker sees nothing else: no
conversation history, no board, no Ben. If the body omits it, the worker guesses.

```
GOAL: <one sentence — what done looks like>
CARD: <repo>#<N> — <title>
BRANCH: worker/<N>-<slug> (already checked out at <worktree path>)
REPO: <owner>/<repo>

ENV:
- <export or setting the worker needs, or "none">

DO (in order):
1. <concrete step>
2. <concrete step>
3. Gate: run <test/lint/build command>; it must pass before continuing
4. <step>
5. Commit with message "<type>: <summary> (#<N>)"
6. Push the branch
7. Raise a DRAFT PR with gh:
   gh pr create --draft --title "<title> (#<N>)" --body "<card acceptance criteria + what changed>"

CONSTRAINTS:
- Do not merge. Ever. The PR stays draft until Ben says otherwise.
- Do not touch other branches or other issues' worktrees.
- <task-specific constraints from the tech review>

ON BLOCKER:
Write state/inflight/<N>.json {"issue": N, "status": "blocked", "step": "<where>", "note": "<why>", "ts": "<now>"},
post [worker-release] with "BLOCKED: <reason>", close your own pane (herdr pane close <pane-id>).

HANDBACK (after the PR is up — a PR without these three is an unfinished task):
1. Post on the card issue: [worker-release] worker=<name> session=<pi session id> finished=<UTC now> + one-line result + PR url
2. Write state/inflight/<N>.json {"issue": N, "status": "done", "step": "pr-raised", "note": "<PR + key facts>", "ts": "<now>"}
3. herdr pane close <your pane id>
```

Rules for writing it:

* If the task operates on a *different* repo (read-only target, etc.), never
  instruct the worker to make that tree clean. Instead: record the dirty files
  on arrival, and the gate becomes "no NEW dirty files on exit". A "git status
  must be clean" gate on a pre-dirty tree silently destroys uncommitted work.
* Numbered DO steps are ordered and exhaustive — the worker executes them top to
  bottom.
* Every gate names the exact command and what "pass" means.
* Constraints echo the tech review's decisions so the worker can't drift from them.
* Changelog rule (dotnet.common and similar): a `Changes/` entry is added ONLY when there is something a developer needs to know (API/behaviour change, migration, consumer-visible security fix). Internal-only fixes, suppressions, and identical-behaviour refactors get no entry — tell the worker the change-log CI gate may fail on that and the captain merges past it.
* The blocker path is always present — a worker that stalls silently is a
  supervision failure.
* Include a WAIT RULE when the task involves background jobs (builds, benches):
  "When a background job runs, do nothing — its completion is injected
  automatically. Never call the question tool to keep the session alive or to
  poll; check progress with the pbb CLI only."
* Include a NO-USER RULE: "This session has no interactive user. The question
  tool blocks until a human answers — it will never come. If you need a human
  decision, write BLOCKED instead."
* Require an inflight write at every step transition (not just at the end), so
  a done-but-unhandedback worker is distinguishable from a stalled one.
* Keep it under ~45 lines. If it's longer, the card was too big; split it.
