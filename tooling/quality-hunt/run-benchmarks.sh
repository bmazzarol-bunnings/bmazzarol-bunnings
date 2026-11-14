#!/usr/bin/env bash
# Run the existing BenchmarkDotNet projects for the wave components and store
# baselines under quality/benchmarks/<date>/ with full provenance.
#
# Usage: COMMON_REPO=/home/bmazzarol/work/dotnet.common ./run-benchmarks.sh
#
# Environment note: `dotnet run -c Release` fails on this machine — the
# SDK-store runtimes require GLIBC_2.44 while the system glibc is older.
# We build with `dotnet build -c Release` and execute the app dll directly
# with DOTNET_ROOT pointed at the nix dotnet-combined runtime store
# (resolved automatically below). BenchmarkDotNet runs with its defaults
# (Job=Medium, 15s, default warmup).
set -euo pipefail

COMMON_REPO="${COMMON_REPO:-/home/bmazzarol/work/dotnet.common}"
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
DATE="$(date -u +%Y-%m-%d)"
OUT="$REPO_ROOT/quality/benchmarks/$DATE"
mkdir -p "$OUT"
SHA="$(git -C "$COMMON_REPO" rev-parse HEAD)"

# Locate a working runtime store: the first store whose net8 app actually
# starts (on this host the SDK-store runtimes need a newer glibc than present,
# so `dotnet run` dies at CoreCLR bind; the probe below detects that).
BENCH_DIR="$COMMON_REPO/src/dotnet/Components/Pipelines/Bunnings.Common.Pipelines.Benchmarks"
find_runtime() {
  (cd "$BENCH_DIR" && dotnet build -c Release >/dev/null 2>&1)
  local root
  local roots
  roots="$(dotnet --list-runtimes 2>/dev/null | grep 'Microsoft.NETCore.App 8' | sed 's/ \[//; s/\]//; s|/shared/.*||' | sort -u)"
  for root in ${DOTNET_ROOT:-} $roots; do
    [[ -x "$root/dotnet" ]] || continue
    if (cd "$BENCH_DIR" && DOTNET_ROOT="$root" "$root/dotnet" bin/Release/net8.0/Bunnings.Common.Pipelines.Benchmarks.dll --list >/dev/null 2>&1); then
      echo "$root"; return 0
    fi
  done
  return 1
}
RUNTIME_ROOT="$(find_runtime)" || { echo "no working runtime store found"; exit 1; }
echo ">> runtime root: $RUNTIME_ROOT"

# Build the pipelines benchmark first: the runtime probe runs its dll.

{
  echo "date: $DATE"
  echo "dotnet-sdk: $(dotnet --version)"
  echo "runtime-root: $RUNTIME_ROOT"
  echo "runtime: $(ls "$RUNTIME_ROOT/shared/Microsoft.NETCore.App/" | sort -V | tail -1) (net8 app roll-forwarded as needed)"
  echo "common-repo-commit: $SHA"
  echo "hardware: $(lscpu | grep 'Model name' | cut -d: -f2 | xargs)"
  echo "cpus: $(lscpu | grep '^CPU(s):' | cut -d: -f2 | xargs)"
  echo "invocation: dotnet build -c Release; DOTNET_ROOT=$RUNTIME_ROOT $RUNTIME_ROOT/dotnet bin/Release/net8.0/<App>.dll --filter '*'"
  echo "note: dotnet run -c Release used as specified on the card fails on this host (GLIBC_2.44); direct dll execution is the exact equivalent"
} > "$OUT/metadata.txt"

for bench in \
  "pipelines:$COMMON_REPO/src/dotnet/Components/Pipelines/Bunnings.Common.Pipelines.Benchmarks" \
  "ai:$COMMON_REPO/src/dotnet/Components/AI/Bunnings.Common.AI.Benchmarks"
do
  name="${bench%%:*}"; dir="${bench#*:}"
  echo ">> benchmark: $name"
  (cd "$dir" && dotnet build -c Release >/dev/null 2>&1)
  (cd "$dir" && DOTNET_ROOT="$RUNTIME_ROOT" "$RUNTIME_ROOT/dotnet" "bin/Release/net8.0/${name}.dll" --filter '*') > "$OUT/$name-benchmark-output.txt" 2>&1
  rm -rf "$dir/BenchmarkDotNet.Artifacts"   # keep the target repo clean (dir is not gitignored there)
done

echo ">> stored under $OUT"
