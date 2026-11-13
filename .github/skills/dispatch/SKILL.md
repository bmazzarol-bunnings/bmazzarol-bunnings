---
name: dispatch
description: "Dispatches workboard items to crewmates or subagents: readiness gate, claim protocol, worker launch, review-feedback loop."
---

# Dispatch

Send a workboard item to the right worker lane with a readiness gate, a complete
task body, and a clean claim. This is the only door work passes through to reach
a worker.

For details, see:

* [claim-protocol.md](./claim-protocol.md) — pre-flight checks, orphan recovery, retroactive claims
* [review-feedback-loop.md](./review-feedback-loop.md) — relaying Ben's PR feedback back to the worker
* [task-body-template.md](./task-body-template.md) — task body scaffold

## When to Use

* Ben says "dispatch #N", "ship this", or "send this to a worker"
* A card is stamped `tech-review:approved` and ready to leave the bench
* Ben gives PR feedback on a draft a worker built (review-feedback loop)

## Process

### 1. Pick the lane

* Bounded one-pass work (scout, review, quick fix, research): **subagent** —
  isolated `pi --print` one-shot (or the subagent extension once adopted), run in
  background via bash; the task body is the prompt.
* An hour or more, or unsupervised iteration: **crewmate** — separate pi session
  in a herdr pane (agent `worker-N`), own worktree.

### 2. Readiness gate (hold until all true)

1. Card exists with acceptance criteria (raise it first if not).
2. `tech-review:approved` stamp present. No stamp, no dispatch — report the gap.
3. Tech review premises verified against current code (paths, APIs, existing
   behavior). A stale premise is a dispatch defect.
4. Capacity: fewer than 4 live crewmates (`herdr agent list`), or the lane is
   subagent.
5. No live claim on the issue (pre-flight in claim-protocol.md).

### 3. Prepare the worktree (crewmates only)

```bash
git fetch origin
git worktree add ../<repo>-<issue-N> -b worker/<issue-N>-<slug> origin/main
```

Branch name matches the card slug so handback mapping is trivial.

### 4. Write the task body

Use task-body-template.md. The body is the worker's entire context: goal, paths,
env, numbered DO steps with test/lint gates, PR plumbing, constraints echo,
no-merge gate. Write it as if Ben is the reader.

### 5. Launch the crewmate (exact sequence)

```bash
herdr pane split --current --direction right --cwd <worktree> --no-focus
herdr agent start worker-N --kind pi --pane <returned-pane-id>
herdr agent prompt worker-N "<task body>" --wait --timeout 3600000
```

Gotchas:

* The herdr server must be running (`herdr status`); otherwise run boot recovery.
* The pane must be an idle shell prompt; `agent start` never creates layout.
* Agent names match `[a-z][a-z0-9_-]{0,31}` and are unique among live agents.
* `agent start` returns only after pi is detected and ready; `agent_not_ready`
  means startup blocked — read the pane, resolve, wait for idle before prompting.
* Reasoning: default low; raise only for genuinely hard design work or a prior
  failure — state the reason in the task body.

### 6. Claim the issue

Post the claim comment (format in claim-protocol.md), then write
`state/floor/worker-N.json`:

```json
{"issue": 42, "worker": "worker-1", "session_id": "<pi session id>", "agent": "worker-1", "worktree": "../bmazzarol-bunnings-42", "reviewer": "ben", "dispatched_at": "<ISO8601>", "mode": "ship", "pr": null}
```

### 7. Hand off to supervision

Do not poll the pane. The heartbeat (supervise skill) tracks liveness via
`state/inflight/<issue>.json`. Dispatch is complete when the worker has a live
session and a claim comment.

## Error Handling

* Pane split or agent start fails: check `herdr status` and `herdr agent list`;
  free the name (close the stale pane) and retry once; if it fails again, report
  to Ben.
* `agent prompt` returns `agent_blocked`: read the pane, ask Ben before answering
  the dialog — never guess an approval.
* `agent prompt` returns `agent_prompt_stalled` or `timeout`: read the pane tail
  (`herdr agent read worker-N`); if no activity, close the pane, re-dispatch.
* Pre-flight finds a live claim: stop, report who owns it, do not dispatch.
* Worktree add fails (branch exists): the card was dispatched before — run orphan
  recovery in claim-protocol.md.

## Notes

* Workers never merge. The no-merge gate in the task body is non-negotiable.
* One issue per worker. Never assign two issues to one pane.
* Small mid-flight changes: relay one message into the pane (`herdr agent
  prompt`) or queue them for the review-feedback loop — do not re-dispatch for
  small deltas.
