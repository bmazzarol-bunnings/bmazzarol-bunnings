#!/usr/bin/env bash
# Harvest open CodeQL alerts (code scanning / GHAS) for the wave components.
# CI runs "CodeQL Analysis" (reusable workflow); SARIF lands in code scanning,
# so the alert API is the access path.
#
# Usage: OUT_DIR=<repo>/quality/codeql ./codeql-harvest.sh [component-prefix ...]
set -euo pipefail

REPO="${COMMON_REPO_SLUG:-Bunnings-Technology-Delivery/dotnet.common}"
OUT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)/quality/codeql"
PREFIXES=("$@"); [[ ${#PREFIXES[@]} -eq 0 ]] && PREFIXES=("src/dotnet/Components/Foundation" "src/dotnet/Components/Pipelines" "src/dotnet/Components/AI")
mkdir -p "$OUT_DIR"

PAGES=()
for p in 1 2 3 4 5; do
  f="$(mktemp)"
  gh api "repos/$REPO/code-scanning/alerts?per_page=100&state=open&page=$p" > "$f"
  n="$(jq length "$f")"
  PAGES+=("$f")
  echo ">> page $p: $n"
  [[ $n -lt 100 ]] && break
done

RE=$(printf '%s|' "${PREFIXES[@]}")
jq -s 'add | map({
  number,
  severity: .rule.severity,
  rule: .rule.id,
  tags: .rule.tags,
  path: .most_recent_instance.location.path,
  start_line: .most_recent_instance.location.start_line,
  message: .most_recent_instance.message.text,
  commit: .most_recent_instance.commit_sha
})' "${PAGES[@]}" > "$OUT_DIR/codeql-alerts-open.json"

jq "[.[] | select(.path != null and (.path | test(\"^($RE)\")))] | sort_by(.path, .start_line)" \
  "$OUT_DIR/codeql-alerts-open.json" > "$OUT_DIR/wave1-alerts.json"

echo ">> wave alerts: $(jq length "$OUT_DIR/wave1-alerts.json")"
rm -f "${PAGES[@]}"
