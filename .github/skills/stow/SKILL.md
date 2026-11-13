---
name: stow
description: "Persists durable knowledge into plans/ and research/ files and curates session checkpoints."
---

# Stow

Session context dies; files don't. Persist what a future session needs.

## When to Use

* End of a card (post-merge or post-drop)
* Context sprawl approaching compaction
* A finding or decision lands that survives the session

## Process

1. Classify:
   * Living plans (phases, decisions, evolution log) → `plans/<NN>-<slug>.md` —
     update the existing plan; append the evolution log.
   * Findings (research, investigations, lessons) →
     `research/<slug>-<YYYY-MM-DD>.md`.
   * Pointers, not explanations: what, where, one-line why.
2. Update the plan's Current state so a fresh session reads the truth.
3. Checkpoints: name the session (`/name`); on long cards, checkpoint at each
   stage boundary so a restart resumes from a named point.

## Notes

* No secrets in files — record where a credential lives, never its value.
* Durable means a future session would ask for it. If it wouldn't, don't stow it.
