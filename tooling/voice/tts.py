#!/usr/bin/env python3
"""Keyless TTS via the edge-tts library (Microsoft endpoint, no API key).

Usage: tts.py <text> <out.mp3> [voice]

The edge-tts library pins its SSL context to the certifi bundle, which lacks
the corp proxy's self-signed CA (MITM). We re-point it at the system trust
store (SSL_CERT_FILE if set, else /etc/ssl/certs/ca-certificates.crt) so the
TLS handshake verifies under the corp network.
"""

import asyncio
import os
import ssl
import sys

import edge_tts
import edge_tts.communicate as ec

cafile = os.environ.get("SSL_CERT_FILE") or "/etc/ssl/certs/ca-certificates.crt"
ec._SSL_CTX = ssl.create_default_context(cafile=cafile)


async def run(text: str, out_mp3: str, voice: str) -> None:
    comm = edge_tts.Communicate(text, voice)
    with open(out_mp3, "wb") as f:
        async for chunk in comm.stream():
            if chunk["type"] == "audio":
                f.write(chunk["data"])


def main() -> int:
    if len(sys.argv) < 3:
        print("usage: tts.py <text> <out.mp3> [voice]", file=sys.stderr)
        return 2
    text, out_mp3 = sys.argv[1], sys.argv[2]
    voice = sys.argv[3] if len(sys.argv) > 3 else os.environ.get("TTS_VOICE", "en-US-AriaNeural")
    if not text.strip():
        print("tts: empty text", file=sys.stderr)
        return 1
    asyncio.run(run(text, out_mp3, voice))
    return 0


if __name__ == "__main__":
    sys.exit(main())
