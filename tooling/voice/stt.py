#!/usr/bin/env python3
"""On-device STT via faster-whisper (small model, offline-forced).

Usage: stt.py <file>            (plain transcript to stdout)
"""

import os
import sys

os.environ.setdefault("HF_HUB_OFFLINE", "1")

from faster_whisper import WhisperModel


def main() -> int:
    if len(sys.argv) != 2:
        print(f"usage: {sys.argv[0]} <file>", file=sys.stderr)
        return 2
    path = sys.argv[1]
    if not os.path.isfile(path):
        print(f"stt: no such file: {path}", file=sys.stderr)
        return 1
    model = WhisperModel("small", device="cpu", compute_type="int8")
    segments, _info = model.transcribe(path, language="en", beam_size=1)
    text = " ".join(seg.text.strip() for seg in segments).strip()
    if text:
        print(text)
    return 0


if __name__ == "__main__":
    sys.exit(main())
