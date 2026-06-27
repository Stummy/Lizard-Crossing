#!/usr/bin/env python3
"""
gemini_review.py — our QA game tester. Hands a recorded gameplay clip to Gemini
ALONG WITH the concept-target frames + the design spec, and gets back a structured
report: BUGS, CONCEPT-GAP (how far from the target look/feel + how to close it), and a
prioritized punch-list. This is "the video guy" from the record -> watch -> fix loop,
kept IN SYNC with the project so it tests against what we're actually building toward.

It is fed, every run:
  - the gameplay CLIP (Temp/Recording/run.mp4 by default; motion, not just stills)
  - the CONCEPT-TARGET image(s) from Assets/Art/Concept/ (the exact bar we're matching)
  - the SPEC text from docs/VISUAL_TARGET_SHEET.md (per-state design intent)
so its report drives the live game toward the concept, like a tester filing bugs +
polish notes against a design doc.

KEY HANDLING (never in this file, never committed):
  the API key is read from  ~/.lizard_secrets/gemini_api_key  (one line, no quotes),
  or the GEMINI_API_KEY env var. Put it there yourself:
      mkdir -p ~/.lizard_secrets && printf '%s' 'YOUR_KEY' > ~/.lizard_secrets/gemini_api_key

USAGE:
  pip install google-genai
  python Tools/gemini_review.py                          # clip=run.mp4, concept=run_target.png
  python Tools/gemini_review.py path/to/clip.mp4
  python Tools/gemini_review.py --state win              # compare vs win_target.png
  python Tools/gemini_review.py --concept run,nearmiss   # attach several target frames
  python Tools/gemini_review.py --model gemini-2.5-flash

Output is written next to the clip as <clip>.review.md so it can be pasted back into
the Claude Code session (and read by the session-boot protocol next login).
"""
import os
import sys
import time
import pathlib
import argparse

# Windows consoles default to cp1252 and crash on the model's unicode (em-dashes, arrows,
# curly quotes) when stdout is redirected to a file — that silently ate a whole review once.
# Force UTF-8 so the report always prints + saves regardless of the host code page.
try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

REPO = pathlib.Path(__file__).resolve().parent.parent
DEFAULT_CLIP = REPO / "Temp" / "Recording" / "run.mp4"
CONCEPT_DIR = REPO / "Assets" / "Art" / "Concept"
SPEC_FILE = REPO / "docs" / "VISUAL_TARGET_SHEET.md"
CHECKLIST_FILE = REPO / "docs" / "REGRESSION_CHECKLIST.md"  # canonical list of recurring past issues
KEY_FILE = pathlib.Path.home() / ".lizard_secrets" / "gemini_api_key"

GOAL = """GAME: "Lizard Crossing" — a polished, cinematic Crossy-Road-style mobile arcade
runner. A TINY emerald-green lizard (~10 cm) AUTO-RUNS forward (+Z) down a realistic-scale
NYC sidewalk seen from a very low, speck's-eye POV (camera ~3 cm off the pavement). It weaves
through GIANT pedestrians (their shoes can squish it), dodges cross-traffic, and races to a
SAFE ZONE (NYC's Central Park). 3 hearts + a droppable tail. Portrait 9:16. The lizard shows
its BACK/TAIL to the camera with its head pointing UP the street — it must NEVER face the
viewer in gameplay. The attached image(s) are the EXACT target look/feel we are building toward."""

PROMPT = """You are the dedicated QA GAME TESTER + art-director for "Lizard Crossing".
You have been given THREE things: (1) the design GOAL + SPEC text, (2) the CONCEPT-TARGET
image(s) — the exact bar we are matching, and (3) a CLIP of the current build's gameplay.

Your job is to test the build against that target and file a report. Watch the CLIP using
its MOTION (not just single frames) and compare it directly to the concept image(s). Be
blunt, specific, and reference timestamps.

Return the report in EXACTLY these sections:

## REGRESSION CHECKLIST
This is the MOST IMPORTANT section. You have been given a REGRESSION WATCHLIST of issues we have
fought before (each tagged [R1], [R2], ...). Go through it and, using the clip's motion, report
for EACH item you can judge: `[Rn] PRESENT @<timestamp> — <what you see>` if the old problem is
visible now, or `[Rn] FIXED/good` if it's clearly resolved. Skip items you genuinely can't tell
(don't pad). List PRESENT ones FIRST — these are regressions. Do not invent issues not on the
watchlist here (put novel ones under BUGS). Be strict: a PRESENT item means an old bug came back.

## BUGS / BROKEN
Anything broken, janky, unfair, unreadable, placeholder-looking, clipping, popping,
mis-timed, or just wrong that is NOT already covered by an [Rn] above. Each as a one-line item
with a timestamp. (If none, say "none spotted".)

## CONCEPT GAP
How the clip DIFFERS from the concept target, point by point — framing, composition, the
hero lizard's size/prominence/facing, depth, lighting/color, the avenue, the pedestrians,
the safe-zone gate, the HUD. For each gap name the concrete change that closes it.

## PRIORITIZED PUNCH-LIST
The ordered list of fixes (highest impact first) to make the build look/feel like the
concept. Tight and actionable — this goes straight to the dev.

## ONE NEXT THING
The single highest-impact fix to do right now."""


def read_key() -> str:
    env = os.environ.get("GEMINI_API_KEY")
    if env:
        return env.strip()
    if KEY_FILE.exists():
        return KEY_FILE.read_text(encoding="utf-8").strip()
    sys.exit(
        f"No API key. Put it in {KEY_FILE} (one line) or set GEMINI_API_KEY.\n"
        "Get a key at https://aistudio.google.com/ -> 'Get API key'."
    )


def load_concept_frames(states):
    """Return [(name, bytes), ...] for each requested concept frame that exists."""
    frames = []
    for st in states:
        st = st.strip()
        if not st:
            continue
        p = CONCEPT_DIR / (st if st.endswith(".png") else f"{st}_target.png")
        if p.exists():
            frames.append((p.name, p.read_bytes()))
        else:
            print(f"  (concept frame not found, skipping: {p.name})")
    return frames


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("clip", nargs="?", default=str(DEFAULT_CLIP), help="path to the .mp4 clip")
    ap.add_argument("--model", default="gemini-2.5-flash", help="Gemini model id")
    ap.add_argument("--state", default="run",
                    help="concept state to compare against (run/win/gameover/nearmiss/squished/faceplant/title)")
    ap.add_argument("--concept", default=None,
                    help="comma-separated concept frames to attach (overrides --state), e.g. run,nearmiss")
    args = ap.parse_args()

    clip = pathlib.Path(args.clip)
    if not clip.exists():
        sys.exit(f"Clip not found: {clip}\nRecord one first: Bot -> Record MP4 (10s).")

    try:
        from google import genai
        from google.genai import types
    except ImportError:
        sys.exit("Missing SDK. Run:  pip install google-genai")

    states = (args.concept.split(",") if args.concept else [args.state])
    frames = load_concept_frames(states)
    spec = SPEC_FILE.read_text(encoding="utf-8") if SPEC_FILE.exists() else ""
    checklist = CHECKLIST_FILE.read_text(encoding="utf-8") if CHECKLIST_FILE.exists() else ""

    client = genai.Client(api_key=read_key())
    size = clip.stat().st_size

    # build the multimodal request: spec text + concept image(s) + the video + the task prompt
    parts = [GOAL]
    if spec:
        parts.append("DESIGN SPEC (per-state target):\n" + spec)
    if checklist:
        parts.append("REGRESSION WATCHLIST — known past issues to check EACH run (report status "
                     "per the ## REGRESSION CHECKLIST instructions below):\n" + checklist)
    for name, data in frames:
        parts.append(f"CONCEPT TARGET FRAME — {name} (match this):")
        parts.append(types.Part.from_bytes(data=data, mime_type="image/png"))

    def ask(contents):
        last = None
        for attempt in range(1, 5):
            try:
                return client.models.generate_content(model=args.model, contents=contents)
            except Exception as e:
                last = e
                print(f"  attempt {attempt} failed ({type(e).__name__}); retrying ...")
                time.sleep(3 * attempt)
        sys.exit(f"Request failed after 4 tries: {last}")

    frame_names = ", ".join(n for n, _ in frames) or "(none)"
    if size < 18 * 1024 * 1024:
        # short clips: send the video INLINE — the most reliable path for our few-MB clips
        print(f"Testing {clip.name} ({size // 1024} KB) vs concept [{frame_names}] on {args.model} ...\n")
        contents = parts + [
            "CURRENT BUILD GAMEPLAY CLIP (test this against the target above):",
            types.Part.from_bytes(data=clip.read_bytes(), mime_type="video/mp4"),
            PROMPT,
        ]
        resp = ask(contents)
    else:
        print(f"Uploading {clip.name} ({size // 1024} KB) via Files API ...")
        f = None
        for attempt in range(1, 5):
            try:
                f = client.files.upload(file=str(clip)); break
            except Exception as e:
                if attempt == 4: sys.exit(f"Upload failed after {attempt} tries: {e}")
                print(f"  upload attempt {attempt} failed ({type(e).__name__}); retrying ...")
                time.sleep(3 * attempt)
        while f.state.name == "PROCESSING":
            time.sleep(2); f = client.files.get(name=f.name)
        if f.state.name == "FAILED":
            sys.exit("Gemini failed to process the video.")
        print(f"Testing vs concept [{frame_names}] on {args.model} ...\n")
        contents = parts + ["CURRENT BUILD GAMEPLAY CLIP (test this against the target above):", f, PROMPT]
        resp = ask(contents)

    text = resp.text or "(no text returned)"

    print("=" * 70)
    print(text)
    print("=" * 70)

    out = clip.with_suffix(".review.md")
    out.write_text(
        f"# QA tester report — {clip.name} vs concept [{frame_names}]\n\n{text}\n",
        encoding="utf-8")
    print(f"\nSaved -> {out}  (read by the session-boot protocol / paste into Claude)")


if __name__ == "__main__":
    main()
