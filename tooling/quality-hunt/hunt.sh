#!/usr/bin/env bash
# Common Quality Loop — wave hunt.
# Indexes dotnet.common (read-only) with the Roslyn Indexer, runs the fixed
# query set over the wave projects, writes deterministic results.
#
# Usage:
#   COMMON_REPO=/home/bmazzarol/work/dotnet.common ./hunt.sh [--force-index]
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$HERE/../.." && pwd)"
COMMON_REPO="${COMMON_REPO:-/home/bmazzarol/work/dotnet.common}"
SOLN="$COMMON_REPO/src/dotnet/dotnet.common.slnx"
DB_DIR="$REPO_ROOT/quality/duckdb"
DB="$DB_DIR/dotnet.common.duckdb"
COMMIT_FILE="$DB_DIR/.indexed-commit"
OUT_DIR="$REPO_ROOT/quality/hunt-results"

# Wave 1: Foundation, Pipelines, AI (library projects only; test/benchmark
# projects are excluded from the debt hunt).
WAVE_DIRS=(
  "$COMMON_REPO/src/dotnet/Components/Foundation"
  "$COMMON_REPO/src/dotnet/Components/Pipelines"
  "$COMMON_REPO/src/dotnet/Components/AI"
)

FORCE_INDEX=0
[[ "${1:-}" == "--force-index" ]] && FORCE_INDEX=1

command -v duckdb >/dev/null || { echo "missing: duckdb"; exit 1; }
command -v dotnet >/dev/null || { echo "missing: dotnet"; exit 1; }
[[ -f "$SOLN" ]] || { echo "missing solution: $SOLN"; exit 1; }

SHA="$(git -C "$COMMON_REPO" rev-parse HEAD)"
mkdir -p "$DB_DIR" "$OUT_DIR"
RESULT_DIR="$OUT_DIR/$SHA"
mkdir -p "$RESULT_DIR"

# 1. Locate / build the roslyn-indexer tool from dotnet.common sources.
TOOL_PROJ="$COMMON_REPO/src/dotnet/Tools/RoslynIndexerTool/Bunnings.Common.Tools.RoslynIndexerTool"
if ! command -v roslyn-indexer >/dev/null; then
  echo ">> building roslyn-indexer tool (obj/bin in dotnet.common are gitignored)"
  (cd "$TOOL_PROJ" && dotnet pack -c Release)
  NUPKG="$(find "$TOOL_PROJ/bin/Release" -name '*.nupkg' -printf '%T@ %p\n' | sort -n | tail -1 | cut -d' ' -f2-)"
  STAGE="$(mktemp -d)/Bunnings.Common.Tools.RoslynIndexerTool"   # folder-source file names are case-sensitive to the package id
  mkdir -p "$STAGE"
  cp "$NUPKG" "$STAGE"   # dotnet tool install lowercases paths; use a plain dir
  VERSION="$(basename "$NUPKG" .nupkg | rev | cut -d. -f1-4 | rev)"
  dotnet tool install --global bunnings.common.tools.roslynindexertool --version "$VERSION" --source "$STAGE" >/dev/null
  export PATH="$PATH:$HOME/.dotnet/tools"
fi
command -v roslyn-indexer >/dev/null || { echo "roslyn-indexer unavailable"; exit 1; }

# 2. Collect wave projects (must exist in the slnx; the indexer rejects orphans).
PROJECTS=()
for d in "${WAVE_DIRS[@]}"; do
  while IFS= read -r p; do
    grep -q "Path=\"$p\"" "$SOLN" && PROJECTS+=("$p")   # skip slnx orphans (indexer rejects them)
  done < <(cd "$COMMON_REPO/src/dotnet" && find \
    "${d#$COMMON_REPO/src/dotnet/}" -name '*.csproj' \
    | grep -v -E '(Tests|Benchmark|TestHelpers)/' | sort)
done
echo ">> ${#PROJECTS[@]} wave projects"

# 3. Index (skip if tree unchanged).
NEED_INDEX=1
if [[ $FORCE_INDEX -eq 0 && -f "$DB" && -f "$COMMIT_FILE" ]]; then
  [[ "$(cat "$COMMIT_FILE")" == "$SHA" ]] && NEED_INDEX=0
fi
if [[ $NEED_INDEX -eq 1 ]]; then
  echo ">> indexing (this takes a while)"
  rm -f "$DB" "$DB.wal"
  P_ARGS=()
  for p in "${PROJECTS[@]}"; do P_ARGS+=(-p "$p"); done
  roslyn-indexer index "$SOLN" -d "$DB" "${P_ARGS[@]}" -j 4
  echo "$SHA" > "$COMMIT_FILE"
else
  echo ">> index up to date at $SHA; skipping"
fi

# 4. Run the fixed query set (deterministic: sorted, no timestamps).
for q in "$HERE"/queries/*.sql; do
  name="$(basename "$q" .sql)"
  echo ">> query: $name"
  duckdb "$DB" -noheader -markdown -c "$(cat "$q")" > "$RESULT_DIR/$name.md"
done

# 5. Record run metadata for traceability (the result files themselves are
# deterministic; this file records provenance).
{
  echo "commit=$SHA"
  echo "indexed_at=$(date -u +%Y-%m-%dT%H:%M:%SZ)"
  echo "projects=${#PROJECTS[@]}"
  sha256sum "$RESULT_DIR"/*.md | sed "s|$RESULT_DIR/||"
} > "$RESULT_DIR/MANIFEST"

echo ">> done: $RESULT_DIR"
