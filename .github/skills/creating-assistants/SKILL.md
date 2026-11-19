---
name: creating-assistants
description: "Creates discipline assistants — git-synced folders under /work that pi opens as specialized experts (testing, frontend design, ...). Use when asked to 'create an assistant', 'new assistant', or 'set up a <discipline> assistant'."
---

# Creating Assistants

A **discipline assistant** is a folder under `/home/bmazzarol/work/` that pi opens as a specialized expert. The expert lives in a **discipline** (the nature of testing, the craft of frontend design), not a domain — disciplines are durable, so the core context does not go stale quickly. The folder is its own git repo synced to the captain's profile: the expert's context evolves in reviewable diffs and survives machine death.

## When to Use

- The captain wants a new assistant for a discipline he works in repeatedly
- The captain asks how an assistant should be structured

## Pattern

1. **Folder**: `/home/bmazzarol/work/<discipline>-assistant` (e.g. `frontend-design-assistant`)
2. **Repo**: `git init` + push to the captain's GitHub profile (one repo per assistant)
3. **Use**: open a pi session **from that folder** — pi picks up the folder's `AGENTS.md`, skills, and `.pi/` config; that directory IS the expert. No new machinery.

## Standard layout

```
<discipline>-assistant/
  AGENTS.md       # the expert contract (see assistant-agents-template.md)
  skills/         # discipline skills (pi reads .github/skills or skills per its config)
  references/     # durable discipline knowledge: patterns, principles, examples
  briefs/         # incoming work requests
  work/           # in-flight deliverables
  reviews/        # completed reviews/outputs
  decisions/      # recorded judgement calls
```

The work areas (`briefs/` `work/` `reviews/` `decisions/`) match the existing `frontend-design-assistant` precedent. Create the ones the discipline needs — not all of them.

## Two operating modes

- **Self-contained** — deliverables live in the folder (frontend design → `work/`).
- **Target-repo** — the persona lives in the folder, the code lives in a domain repo referenced by absolute path (testing assistant reviewing item PRs). State the mode in the `AGENTS.md` contract.

## Freshness rule (encode in every contract)

Discipline core is stable; anything domain-specific inside `references/` (a repo's test harness, a framework version) can rot. The contract must carry: *verify against current code before advising; when you learn something durable, update `references/` in the same session.*

## 2IC relationship

Assistants are the **captain's direct lanes** — he opens them himself. The 2IC floor stays for orchestrated ship work. If the captain asks the 2IC to run one, spawn a herdr pane from the assistant folder (same mechanics as a crewmate dispatch, different home); the assistant's `AGENTS.md` then governs that session.

## Registry

The living list of assistants + when to call on each: `ASSISTANTS.md` at the repo root. Every new assistant gets a row there before it's considered done; retired assistants get marked RETIRED. Any agent session discovers and invokes assistants via the global `using-assistants` skill (reads this registry).

## Process

1. Confirm the discipline name + one-sentence role + operating mode with the captain.
2. Create the folder + git repo + push (verify push access first; the captain's profile repos are his).
3. Write `AGENTS.md` from [assistant-agents-template.md](./assistant-agents-template.md) — the contract is the whole expert; keep it tight.
4. Seed `references/` with the durable discipline knowledge you can state truthfully; leave gaps marked rather than inventing content.
5. Commit, push, report the folder + what to fill in next.
6. Add the assistant to the `ASSISTANTS.md` registry (path, discipline, mode, when to call on it).

## Notes

- Naming: `<discipline>-assistant`, lowercase-hyphenated.
- Only add context the model doesn't already have — a testing assistant's `references/` should hold *this captain's* testing conventions and taste, not a generic testing tutorial.
- Keep `AGENTS.md` under ~150 lines; move depth into `references/` files.
