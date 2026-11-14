# Quality Hunt (Common Quality Loop — wave tooling)

Repeatable scan of the dotnet.common components in the current wave. Produces:

- `quality/duckdb/dotnet.common.duckdb` — Roslyn Indexer graph (rebuilt per run)
- `quality/hunt-results/<commit-sha>/` — one file per fixed DuckDB query, deterministic output
- `quality/codeql/` — GHAS CodeQL alerts for the wave components (fetched from CI)
- `quality/benchmarks/` — benchmark baselines (separate: `run-benchmarks.sh`)

The hunt makes **zero writes to dotnet.common source**. Building the
roslyn-indexer tool writes only gitignored `obj/`/`bin/` artifacts there.

## Prerequisites

- `dotnet` SDK (matches dotnet.common global.json), `duckdb` CLI, `gh` with access to Bunnings-Technology-Delivery/dotnet.common
- dotnet.common checkout at `/home/bmazzarol/work/dotnet.common` (override with `COMMON_REPO`)

## Run a full hunt

```
COMMON_REPO=/home/bmazzarol/work/dotnet.common ./tooling/quality-hunt/hunt.sh
```

Steps: build + install `roslyn-indexer` from the dotnet.common tool project →
index the wave projects into the DuckDB store → run the fixed query set
(`queries/*.sql`) sorted and written to `quality/hunt-results/<sha>/`.

Skip re-indexing if the source tree is unchanged (the script compares the
indexed commit recorded in `quality/duckdb/.indexed-commit` against
`git -C $COMMON_REPO rev-parse HEAD`; pass `--force-index` to rebuild anyway).

## Re-run a wave (next wave)

1. Update `WAVE_PROJECTS` in `hunt.sh` to the new component dirs.
2. `COMMON_REPO=... ./tooling/quality-hunt/hunt.sh --force-index`
3. Diff `quality/hunt-results/<old-sha>/` vs `<new-sha>/` and refresh
   `quality/pool-<date>.md`. Deterministic query entries must reproduce
   identically on re-run over the same commit; scout-sourced pool entries may
   be re-listed as-is.

## CodeQL

`codeql-harvest.sh` pulls open CodeQL alerts from code scanning (GHAS) for the
wave components into `quality/codeql/wave1-alerts.json`. CI runs
`CodeQL Analysis` on main; the SARIF is consumed by code scanning, so the alert
API is the access path (no raw SARIF artifact is published from the reusable
workflow).

## Determinism

Every query file: explicit `ORDER BY`, no `now()`/randomness, output via
`duckdb -noheader -markdown`. Pool ranking is derived from these outputs plus
hand-assigned effort.

## Tooling gaps (recorded, not dropped)

- **Cyclomatic complexity** — Roslyn Indexer has no direct complexity column;
  `queries/complexity.sql` computes a branch-based proxy (1 + branch
  statements) from the `statements` table.
- **Duplicate code** — no duplication detector in the Roslyn Indexer and the
  org CodeQL config has no `cs/duplicate-*` rules. Gap: duplication findings
  come from the scout pass only.
- **CodeQL hotspots / dead stores** — the org CodeQL config does not enable
  `cs/hotspot-*` or dead-store rules; only the alert rules in the code
  scanning stream are available (see `quality/codeql/`).
- **Unused public surface** — computed within the wave-1 index scope only;
  the 33 indexed projects do not include every consumer (other components,
  org repos), so "zero references" means unused *in-scope*.
