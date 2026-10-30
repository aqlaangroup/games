#!/usr/bin/env python3
"""Gahwit Sitti: DRAFT voice lines made with ElevenLabs.

These recordings are placeholders only. Key voices (Teta, Salma, Lana,
Abu Fadi) are recorded by native actors before release (PLAN.md, risks;
design/COMPLETE_SPEC.md section 7). Replace every file this script makes.

Each line is listed in tools/voice_drafts.json. The spoken text is read
from the Ink knot, so the audio follows the story file.

Usage (from games/gahwit-sitti):
    python3 tools/voice_drafts.py --list
    python3 tools/voice_drafts.py voice_1_teta --dry-run   # text, cost, quota
    python3 tools/voice_drafts.py voice_1_teta             # regenerate
    python3 tools/voice_drafts.py --all

The API key is read from the ELEVEN_LABS_API_KEY environment variable.
It is never stored in this file or in the JSON. Example:
    set -a; source ../../.env; set +a; python3 tools/voice_drafts.py voice_1_teta

Standard library only.
"""

import argparse
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import urllib.error
import urllib.request

API = "https://api.elevenlabs.io/v1"
GAME_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CONFIG = os.path.join(GAME_DIR, "tools", "voice_drafts.json")
KEY_VAR = "ELEVEN_LABS_API_KEY"


def load_config():
    with open(CONFIG, encoding="utf-8") as f:
        return json.load(f)


def knot_text(ink_path, knot):
    """Spoken text of one Ink knot: tags, diverts, glue, comments and
    speaker prefixes removed. Fails on choices or logic, which a voice
    line should not have."""
    with open(os.path.join(GAME_DIR, ink_path), encoding="utf-8") as f:
        lines = f.read().splitlines()
    header = re.compile(r"^\s*={2,}\s*(\w+)\s*(?:\(.*\))?\s*=*\s*$")
    body, inside = [], False
    for line in lines:
        m = header.match(line)
        if m:
            if inside:
                break
            inside = m.group(1) == knot
            continue
        if inside:
            body.append(line)
    if not inside and not body:
        sys.exit(f"Knot '{knot}' not found in {ink_path}")

    spoken = []
    for line in body:
        line = re.sub(r"//.*$", "", line)               # comments
        line = re.sub(r"#.*$", "", line)                # tags
        line = re.sub(r"->.*$", "", line)               # diverts
        line = line.replace("<>", "").strip()           # glue
        if not line or line.startswith("~"):
            continue
        if line[0] in "*+-{" or "{" in line:
            sys.exit(f"Knot '{knot}' has choices or logic; a voice line must be plain text: {line}")
        line = re.sub(r"^[A-Za-z_][\w ]{0,30}:\s*", "", line)  # "Teta: " speaker prefix
        spoken.append(line)
    text = " ".join(spoken).strip()
    if not text:
        sys.exit(f"Knot '{knot}' has no spoken text")
    return text


def api_key():
    key = os.environ.get(KEY_VAR)
    if not key:
        sys.exit(f"{KEY_VAR} is not set. Load it from the git-ignored .env first.")
    return key


def request(method, path, key, body=None, query=""):
    url = f"{API}{path}{query}"
    data = json.dumps(body).encode("utf-8") if body is not None else None
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("xi-api-key", key)
    if data is not None:
        req.add_header("Content-Type", "application/json")
    try:
        with urllib.request.urlopen(req, timeout=180) as resp:
            return resp.read()
    except urllib.error.HTTPError as e:
        detail = e.read().decode("utf-8", "replace")[:800]
        sys.exit(f"ElevenLabs {method} {path} failed: HTTP {e.code} {detail}")


def quota_left(key):
    sub = json.loads(request("GET", "/user/subscription", key))
    return sub["character_limit"] - sub["character_count"], sub["character_limit"]


def duration_seconds(path):
    """Duration via macOS afinfo, or None elsewhere."""
    if not shutil.which("afinfo"):
        return None
    out = subprocess.run(["afinfo", path], capture_output=True, text=True).stdout
    m = re.search(r"estimated duration:\s*([\d.]+)", out)
    return float(m.group(1)) if m else None


def generate(line, text, key, out_path):
    body = {"text": text, "model_id": line["model_id"], "voice_settings": line["voice_settings"]}
    if line.get("language_code"):
        body["language_code"] = line["language_code"]
    if line.get("seed") is not None:
        body["seed"] = line["seed"]
    query = "?output_format=" + line.get("output_format", "mp3_44100_128")
    audio = request("POST", f"/text-to-speech/{line['voice_id']}", key, body, query)

    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    fd, tmp = tempfile.mkstemp(dir=os.path.dirname(out_path), suffix=".part")
    with os.fdopen(fd, "wb") as f:
        f.write(audio)
    os.chmod(tmp, 0o644)
    os.replace(tmp, out_path)
    return out_path


def main():
    p = argparse.ArgumentParser(description="Regenerate DRAFT voice lines (placeholders for native actors).")
    p.add_argument("ids", nargs="*", help="line ids from voice_drafts.json")
    p.add_argument("--all", action="store_true", help="every listed line")
    p.add_argument("--list", action="store_true", help="list lines and exit")
    p.add_argument("--dry-run", action="store_true", help="show text, characters and quota; spend nothing")
    p.add_argument("--max-chars", type=int, default=2000, help="refuse a run that would spend more (default 2000)")
    # Overrides for trying takes. They do not change the JSON.
    p.add_argument("--model", help="override model_id")
    p.add_argument("--stability", type=float, help="override voice_settings.stability")
    p.add_argument("--seed", type=int, help="override seed")
    p.add_argument("--out", help="write here instead of the listed output (one line only)")
    args = p.parse_args()

    cfg = load_config()
    lines = {l["id"]: l for l in cfg["lines"]}
    if args.list:
        for l in cfg["lines"]:
            print(f"{l['id']}: {l['character']}, knot {l['ink_knot']}, {l['model_id']} -> {l['output']}")
        return
    ids = list(lines) if args.all else args.ids
    if not ids:
        p.error("give line ids or --all")
    unknown = [i for i in ids if i not in lines]
    if unknown:
        sys.exit(f"Unknown line ids: {', '.join(unknown)}")
    if args.out and len(ids) != 1:
        sys.exit("--out works with one line only")

    jobs = []
    for i in ids:
        line = dict(lines[i])
        line["voice_settings"] = dict(line["voice_settings"])
        if args.model:
            line["model_id"] = args.model
        if args.stability is not None:
            line["voice_settings"]["stability"] = args.stability
        if args.seed is not None:
            line["seed"] = args.seed
        text = knot_text(line["ink_file"], line["ink_knot"])
        out = os.path.abspath(args.out) if args.out else os.path.join(GAME_DIR, line["output"])
        jobs.append((line, text, out))

    chars = sum(len(t) for _, t, _ in jobs)
    for line, text, _ in jobs:
        print(f"{line['id']} ({len(text)} chars, {line['model_id']}): {text}")
    if chars > args.max_chars:
        sys.exit(f"This run needs {chars} characters, more than --max-chars {args.max_chars}.")

    key = api_key()
    left, limit = quota_left(key)
    print(f"Characters this run: {chars}. Quota left: {left} of {limit}.")
    if args.dry_run:
        return
    if chars > left:
        sys.exit("Not enough quota left.")

    for line, text, out in jobs:
        generate(line, text, key, out)
        secs = duration_seconds(out)
        shown = f"{secs:.2f} s" if secs is not None else "duration unknown"
        where = os.path.relpath(out, GAME_DIR) if out.startswith(GAME_DIR + os.sep) else out
        print(f"Wrote {where} ({shown}). DRAFT: replace with a native actor recording.")
    left, _ = quota_left(key)
    print(f"Quota left now: {left}.")


if __name__ == "__main__":
    main()
