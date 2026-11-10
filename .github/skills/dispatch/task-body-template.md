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
post [worker-release] with "BLOCKED: <reason>", kill your own window (tmux kill-window).
```

Rules for writing it:

* Numbered DO steps are ordered and exhaustive — the worker executes them top to
  bottom.
* Every gate names the exact command and what "pass" means.
* Constraints echo the tech review's decisions so the worker can't drift from them.
* The blocker path is always present — a worker that stalls silently is a
  supervision failure.
* Keep it under ~40 lines. If it's longer, the card was too big; split it.
