#!/usr/bin/env python3
"""
gemini_review.py — upload a recorded gameplay clip to Gemini and print a critique.

The "feel" half of the record -> watch -> fix loop: RunRecorder writes a real MP4
(Temp/Recording/run.mp4 via the in-editor MediaEncoder); this hands that clip to a
Gemini model so MOTION / pacing / jank get critiqued, not just stills.

KEY HANDLING (never in this file, never committed):
  the API key is read from  ~/.lizard_secrets/gemini_api_key  (one line, no quotes),
  or the GEMINI_API_KEY env var. Put it there yourself:
      mkdir -p ~/.lizard_secrets && printf '%s' 'YOUR_KEY' > ~/.lizard_secrets/gemini_api_key

USAGE:
  pip install google-genai
  python Tools/gemini_review.py                      # reviews Temp/Recording/run.mp4
  python Tools/gemini_review.py path/to/clip.mp4     # reviews a specific clip
  python Tools/gemini_review.py --model gemini-2.5-flash

Output is also written next to the clip as <clip>.review.md so it can be pasted back
into the Claude Code session.
"""
import os
import sys
import time
import pathlib
import argparse

DEFAULT_CLIP = pathlib.Path(__file__).resolve().parent.parent / "Temp" / "Recording" / "run.mp4"
KEY_FILE = pathlib.Path.home() / ".lizard_secrets" / "gemini_api_key"

PROMPT = """You are a senior mobile-game art director and game-feel critic. This is
portrait gameplay footage of "Lizard Crossing" — a tiny emerald lizard auto-runs
forward down a realistic-scale NYC sidewalk from a very low, speck's-eye POV,
weaving through giant pedestrians and dodging cross-traffic, racing to a safe zone.
The bar is a polished, cinematic Crossy-Road-style arcade runner.

Critique the CLIP (use the motion, not just single frames). Be specific and blunt.
Cover, with concrete timestamps where you can:
1. Game feel & pacing — does it read fast/responsive/fair? any stutter, hitching, or
   moments that feel unfair or unreadable?
2. Camera & readability — is the hero lizard clearly the focus? are hazards readable
   in time from this low POV?
3. Visual quality vs a premium arcade target — lighting, color cohesion, jank,
   placeholder-looking bits, popping, anything that breaks the illusion.
4. HUD/juice — do hits, near-misses, and the dash read clearly?
5. The single highest-impact fix to do next, then a short prioritized punch-list.

Keep it tight and actionable — this goes straight to the dev."""


def read_key() -> str:
    env = os.environ.get("GEMINI_API_KEY")
    if env:
        return env.strip()
    if KEY_FILE.exists():
        return KEY_FILE.read_text(encoding="utf-8").strip()
    sys.exit(
        f"No API key. Put it in {KEY_FILE} (one line) or set GEMINI_API_KEY.\n"
        "Get a key at https://aistudio.google.com/ -> 'Get API key' (starts with AIza...)."
    )


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("clip", nargs="?", default=str(DEFAULT_CLIP), help="path to the .mp4 clip")
    ap.add_argument("--model", default="gemini-2.5-flash", help="Gemini model id")
    args = ap.parse_args()

    clip = pathlib.Path(args.clip)
    if not clip.exists():
        sys.exit(f"Clip not found: {clip}\nRecord one first: Bot -> Record MP4 (10s).")

    try:
        from google import genai
    except ImportError:
        sys.exit("Missing SDK. Run:  pip install google-genai")

    client = genai.Client(api_key=read_key())

    print(f"Uploading {clip.name} ({clip.stat().st_size // 1024} KB) ...")
    f = client.files.upload(file=str(clip))

    # video files need server-side processing before they can be used
    while f.state.name == "PROCESSING":
        time.sleep(2)
        f = client.files.get(name=f.name)
    if f.state.name == "FAILED":
        sys.exit("Gemini failed to process the video.")

    print(f"Asking {args.model} for a critique ...\n")
    resp = client.models.generate_content(model=args.model, contents=[f, PROMPT])
    text = resp.text or "(no text returned)"

    print("=" * 70)
    print(text)
    print("=" * 70)

    out = clip.with_suffix(".review.md")
    out.write_text(f"# Gemini review of {clip.name}\n\n{text}\n", encoding="utf-8")
    print(f"\nSaved -> {out}  (paste this into the Claude session)")


if __name__ == "__main__":
    main()
