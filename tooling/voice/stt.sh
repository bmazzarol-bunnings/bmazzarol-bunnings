#!/usr/bin/env bash
# On-device speech-to-text wrapper for pi-telegram inboundHandlers.
#
#   stt.sh <file>          (or: stt.sh --file <file>)
#
# Stdout: plain transcript text. Uses the cached faster-whisper `small`
# model (HF_HUB_OFFLINE=1 — audio never leaves the laptop). The tool venv
# is created lazily on first use (not committed).
set -euo pipefail

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Resolve input path: support both `stt.sh FILE` and `--file FILE`.
file=""
if [[ "${1:-}" == "--file" ]]; then
    file="${2:?usage: stt.sh <file> | stt.sh --file <file>}"
else
    file="${1:?usage: stt.sh <file> | stt.sh --file <file>}"
fi

VENV="$DIR/.venv"
if [[ ! -x "$VENV/bin/python" ]]; then
    uv venv "$VENV" >/dev/null
fi
uv pip show --python "$VENV/bin/python" faster-whisper >/dev/null 2>&1 || {
    # av is pinned: faster-whisper 1.2.x passes metadata_errors to av.open,
    # which av >= 18 removed (no cp314 wheels below 19, so 17.1.0 is the
    # compatible latest).
    uv pip install --quiet --python "$VENV/bin/python" faster-whisper "av==17.1.0"
}

exec "$VENV/bin/python" "$DIR/stt.py" "$file"
