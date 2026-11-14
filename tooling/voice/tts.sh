#!/usr/bin/env bash
# Text-to-speech wrapper for pi-telegram outboundHandlers.
#
#   tts.sh <text> [out.ogg]    text as argument (default out: /tmp/pi-telegram-tts-XXXXXX.ogg)
#   tts.sh - [out.ogg]         text from stdin (pi-telegram composed-pipeline form:
#                              template `["tts.sh", "-", "{ogg}"]`, text arrives on stdin)
#
# Output: OGG/Opus voice-note file (Telegram sendVoice requirement).
# Prints the output path to stdout. Keyless edge-tts (Microsoft endpoint;
# accepted trade for keyless TTS — reply text is sent to that endpoint).
# The tool venv is created lazily on first use (not committed).
set -euo pipefail

text_arg="${1:-}"
out="${2:-}"

if [[ "$text_arg" == "-" || -z "$text_arg" ]]; then
    text="$(cat)"
else
    text="$text_arg"
fi

if [[ -z "$out" ]]; then
    out="$(mktemp --suffix=.ogg /tmp/pi-telegram-tts-XXXXXX)"
fi

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VENV="$DIR/.venv"
if [[ ! -x "$VENV/bin/python" ]]; then
    uv venv "$VENV" >/dev/null
fi
uv pip show --python "$VENV/bin/python" edge-tts >/dev/null 2>&1 ||
    uv pip install --quiet --python "$VENV/bin/python" edge-tts

# ffmpeg: PATH first, then any nix store build (kept locally via nix).
if ! command -v ffmpeg >/dev/null 2>&1; then
    store_ff="$(ls -1 /nix/store/*/bin/ffmpeg 2>/dev/null | sort | tail -1 || true)"
    if [[ -z "$store_ff" || ! -x "$store_ff" ]]; then
        echo "tts: ffmpeg not found (install via nix)" >&2
        exit 1
    fi
    ffmpeg="$store_ff"
else
    ffmpeg="$(command -v ffmpeg)"
fi

tmp_mp3="${out}.tmp.mp3"
"$VENV/bin/python" "$DIR/tts.py" "$text" "$tmp_mp3"
"$ffmpeg" -y -loglevel error -i "$tmp_mp3" -c:a libopus -b:a 32k -ar 16000 -ac 1 "$out"
rm -f "$tmp_mp3"
echo "$out"
