#!/usr/bin/env python3
"""Gahwit Sitti: DRAFT music and sound effects made with ElevenLabs.

These files are placeholders until the audio freelancers deliver
(design/COMPLETE_SPEC.md section 7). Replace every file this script makes.

Each cue is listed in tools/audio_drafts.json with its prompt and length.
Sound effects use the sound effects API; music uses Eleven Music, instrumental.
Existing files are skipped unless --force is given.

Usage (from games/gahwit-sitti):
    python3 tools/audio_drafts.py --list
    python3 tools/audio_drafts.py ui_tap merge        # make these if missing
    python3 tools/audio_drafts.py --all
    python3 tools/audio_drafts.py music_memory --force

The API key is read from the ELEVEN_LABS_API_KEY environment variable.
It is never stored in this file or in the JSON. Example:
    set -a; source ../../.env; set +a; python3 tools/audio_drafts.py --all

Standard library only.
"""

import argparse
import json
import os
import sys
import time
import urllib.error
import urllib.request

API = "https://api.elevenlabs.io"
HERE = os.path.dirname(os.path.abspath(__file__))
GAME = os.path.dirname(HERE)
MANIFEST = os.path.join(HERE, "audio_drafts.json")


def post(path, body, key):
    request = urllib.request.Request(
        API + path, method="POST", data=json.dumps(body).encode("utf-8"),
        headers={"xi-api-key": key, "Content-Type": "application/json"})
    with urllib.request.urlopen(request, timeout=300) as reply:
        return reply.read()


def generate(cue, key):
    if cue["kind"] == "sfx":
        return post("/v1/sound-generation?output_format=mp3_44100_128", {
            "text": cue["prompt"],
            "duration_seconds": cue["seconds"],
            "prompt_influence": 0.5,
            "loop": cue.get("loop", False),
        }, key)
    return post("/v1/music?output_format=mp3_44100_128", {
        "prompt": cue["prompt"],
        "music_length_ms": int(cue["seconds"] * 1000),
        "force_instrumental": True,
    }, key)


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("ids", nargs="*", help="cue ids to make")
    parser.add_argument("--all", action="store_true", help="make every cue")
    parser.add_argument("--list", action="store_true", help="list cues and whether their file exists")
    parser.add_argument("--force", action="store_true", help="remake files that already exist")
    args = parser.parse_args()

    cues = json.load(open(MANIFEST, encoding="utf-8"))["cues"]
    if args.list:
        for cue in cues:
            exists = os.path.exists(os.path.join(GAME, cue["output"]))
            print(f"{cue['id']:<22} {cue['kind']:<6} {cue['seconds']:>5}s  {'made' if exists else '-'}")
        return

    known = {cue["id"] for cue in cues}
    unknown = [i for i in args.ids if i not in known]
    if unknown:
        sys.exit("Unknown cue ids: " + ", ".join(unknown))
    chosen = cues if args.all else [cue for cue in cues if cue["id"] in args.ids]
    if not chosen:
        sys.exit("Name cue ids, or pass --all or --list.")

    key = os.environ.get("ELEVEN_LABS_API_KEY")
    if not key:
        sys.exit("Set ELEVEN_LABS_API_KEY (see the usage at the top of this file).")

    for cue in chosen:
        out = os.path.join(GAME, cue["output"])
        if os.path.exists(out) and not args.force:
            print(f"skip {cue['id']} (exists)")
            continue
        os.makedirs(os.path.dirname(out), exist_ok=True)
        audio = None
        for attempt in range(4):
            try:
                audio = generate(cue, key)
                break
            except urllib.error.HTTPError as error:
                if error.code == 429 and attempt < 3:  # rate limited: wait and try again
                    time.sleep(10 * (attempt + 1))
                    continue
                print(f"FAIL {cue['id']}: HTTP {error.code} {error.read().decode('utf-8', 'replace')[:300]}")
                break
        if audio is None:
            continue
        with open(out, "wb") as f:
            f.write(audio)
        print(f"made {cue['id']}: {len(audio) // 1024} KB -> {cue['output']}")
        time.sleep(2)


if __name__ == "__main__":
    main()
