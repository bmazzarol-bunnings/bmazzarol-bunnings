---
name: prqueue
description: "Renders Ben's PR review queue on demand — 'my PRs?', 'what do I need to review?', 'PR queue'."
---

# PR Queue

On-demand PR review queue. Every line traces to gh output; no assumptions.

## When to Use

* Ben says "my PRs?", "what do I need to review?", "PR queue"

## Query

One call per repo in the list below:

```
gh pr list --repo <r> --state open --json number,title,author,isDraft,headRefName,reviewDecision,statusCheckRollup
```

Repos: bmazzarol-bunnings/bmazzarol-bunnings, Bunnings-Technology-Delivery/dotnet.common,
Bunnings-Engineering/bunnings-agentic, Bunnings-Technology-Infrastructure/coredevops.infra —
plus any repo a current worker reports.

## Classify

One group per PR; test in this order, first match wins:

1. BLOCKED — any statusCheckRollup entry COMPLETED+FAILURE, or unanswered Copilot review
2. READY TO MERGE — reviewDecision APPROVED (or Ben said good-to-go), not merged
3. NEEDS REVIEW — worker PR (author bmazzarol-bunnings, or headRefName starts with `worker/`),
   reviewDecision empty or REVIEW_REQUIRED
4. AWAITING OTHERS — Ben's PR (author bmazzarol-bunnings, not a `worker/` branch),
   reviewDecision PENDING or REVIEW_REQUIRED

PRs matching no group (other authors awaiting other reviewers): omit.

## Render

* Grouped table, terse; per PR: `#<n> <short title> — repo — one-line state`.
* No timestamps, no filler, no process narration. Omit empty groups.
* End with `next: <most actionable PR>` on one line, if one is obvious.
