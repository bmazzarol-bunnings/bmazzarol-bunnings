# Delivery Plans

One plan per top-level bucket (delivery map). These are the shared, unified
understanding of what each stream is aiming for — context for anyone working
in the area, including agents. They change over time; the evolution log
records how.

## The buckets

| # | Plan | Map issue | Jira |
|---|------|-----------|------|
| 2 | [Agentic Engineering & Platform Tooling](02-agentic-engineering-platform-tooling.md) | [#2](https://github.com/bmazzarol-bunnings/bmazzarol-bunnings/issues/2) | INTPLAT-375 (parent), 1205, 1206, 406, 555 |
| 3 | [Search & Discovery](03-search-discovery.md) | [#3](https://github.com/bmazzarol-bunnings/bmazzarol-bunnings/issues/3) | INTPLAT-1, INTPLAT-51, TPP-2053 |
| 4 | [AsyncAPI / Event Documentation Pipeline](04-asyncapi-event-docs.md) | [#4](https://github.com/bmazzarol-bunnings/bmazzarol-bunnings/issues/4) | INTPLAT-760–765 |
| 25 | [Data Engineering Agentic Craft](25-data-engineering-agentic-craft.md) | [#25](https://github.com/bmazzarol-bunnings/bmazzarol-bunnings/issues/25) | INTPLAT-1758 (under TPP-2077) |
| 26 | [Digital 2IC](26-digital-2ic.md) | [#26](https://github.com/bmazzarol-bunnings/bmazzarol-bunnings/issues/26) | — (internal infra, GH-only) |

## Standard (DRAFT — evolves as we use it)

Each plan doc contains:

1. **Vision** — what the stream is aiming for, in plain language. The thing
   everyone should agree on before touching work in the area.
2. **Scope** — in / out.
3. **Current state** — frontier work, open questions, recent direction.
   Kept short; the map issue holds the full decision log.
4. **Evolution log** — dated entries when the vision or scope changes.

Rules of thumb:

- The plan is the *why*; the map issue is the *what happened*; tickets are the *how*.
- If a ticket doesn't fit any plan's vision, that's a signal — either the
  ticket is misplaced or the plan needs an evolution-log entry.
- Plans are versioned in git like everything else; no separate review gate,
  but big vision shifts get flagged to Ben.

Open design question (parked): whether tickets should carry a `project:`
metadata field (bucket id) so boards can slice by bucket directly. Decide
once work is categorised enough to see if the label noise is worth it.
