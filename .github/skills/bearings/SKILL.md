---
name: bearings
description: "Produces a four-section status digest (workboard, fleet, decisions, health); file variant writes a dated report."
---

# Bearings

Status digest from live sources only — no assumption.

## When to Use

* Ben says "status?", "where are we?", or "bearings"
* Standup composition (format in AGENTS.md)
* A Telegram digest push

## Process

1. **Workboard**: `gh issue list --label delivery:task --state open --json number,title,labels`.
   Group: in flight (`state/floor/`), awaiting Ben review, ready, backlog.
2. **Fleet**: `herdr agent list` — per worker: issue, state (working/blocked/
   done/idle), inflight freshness.
3. **Decisions**: run the decisions skill; count open items, name the top one.
4. **Health**: latest laptop-watch readings (disk/memory/load), wake tool alive,
   herdr server up.

## Output

* Chat variant: four sections, 2-3 lines each, ~15 lines total.
* `file` variant ("bearings file", "write the report"): full detail to
  `reports/bearings-<YYYY-MM-DD>.md` — same sections, every item named.

## Notes

* Every line traces to a command output or state file; no "seems fine".
* Herdr server down: say so in Fleet — that is floor=DOWN, not silence.
