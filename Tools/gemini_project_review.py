#!/usr/bin/env python3
"""
gemini_project_review.py — the STRATEGIC, whole-project counterpart to gemini_review.py.

Where gemini_review.py is the per-change video gate (does THIS clip match the concept?),
this hands Gemini the WHOLE picture — the project plan (goals, the 'done' bar, the live ledger
+ known gaps), every concept-target state, the design spec, the regression watchlist, AND a
current gameplay clip — and asks it, as creative director + QA lead, for a gap-analysis +
sequenced roadmap to close the build to the vision. Fire it at stage gates or whenever you want
a "where are we vs the goal, what's next" read.

Key: read from ~/.lizard_secrets/gemini_api_key (one line) or $GEMINI_API_KEY. Never committed.
Output: docs/reviews/PROJECT_REVIEW_latest.md  (+ printed).

USAGE:
  python Tools/gemini_project_review.py                 # clip = Temp/Recording/run.mp4
  python Tools/gemini_project_review.py path/clip.mp4
Tip: record a WINNING run first (so the clip shows the park / win), or the review judges only
what the clip reaches.
"""
import os, sys, time, pathlib
try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

REPO = pathlib.Path(__file__).resolve().parent.parent
CONCEPT_DIR = REPO / "Assets" / "Art" / "Concept"
KEY_FILE = pathlib.Path.home() / ".lizard_secrets" / "gemini_api_key"
STATES = ["run", "win", "gameover", "nearmiss", "squished", "faceplant", "title"]

GOAL = ("GAME: \"Lizard Crossing\" — a polished, cinematic Crossy-Road-style portrait-mobile arcade "
        "runner. A TINY emerald-green lizard auto-runs forward down a realistic-scale NYC sidewalk from "
        "a very low speck's-eye POV, weaving GIANT pedestrians, dodging cross-traffic at crosswalks, "
        "racing to a Central Park SAFE ZONE. 3 hearts + a droppable tail. The lizard shows its BACK to "
        "the camera, head up-street. The attached concept frames are the EXACT target look/feel.")

PROMPT = """You are the CREATIVE DIRECTOR + QA LEAD for "Lizard Crossing". You have the project's
GOALS (the project plan), the CONCEPT TARGET frames for the game's states, the design spec, the
regression watchlist, and a CLIP of the CURRENT build's gameplay.

Give a STRATEGIC, WHOLE-PROJECT review — not a per-frame bug list. Be honest, specific, and prioritize
by impact toward shipping the vision. Use the clip's MOTION + timestamps, and the concept frames for
states the clip doesn't show. Weight the OWNER's stated goals + the concept over generic advice.

Return EXACTLY these sections:
## WHERE WE STAND
A frank 3-5 sentence read: how close is the build to the concept vision + the 'done' bar? What's working?
## THE BIGGEST GAPS (ranked)
The 5-8 biggest gaps between the current build and the goals/concept, ranked by impact. For each: what's
wrong, why it matters, and the concrete change that closes it. Span visuals, game-feel, and content.
## ROADMAP TO CLOSE THEM
A prioritized, sequenced plan grouped NEAR-TERM / MID-TERM / BEFORE-SHIP. Concrete + actionable.
## THE ONE THING
The single highest-leverage move to do next, and why.
## QUICK WINS
2-4 small, cheap changes that punch above their weight."""


def read(p):
    return p.read_text(encoding="utf-8", errors="replace") if p.exists() else ""


def main():
    clip = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else REPO / "Temp" / "Recording" / "run.mp4"
    if not clip.exists():
        sys.exit(f"No clip at {clip}. Record one (Bot -> Record MP4) first.")
    key = os.environ.get("GEMINI_API_KEY") or (KEY_FILE.read_text(encoding="utf-8").strip() if KEY_FILE.exists() else "")
    if not key:
        sys.exit(f"No Gemini key. Put it in {KEY_FILE} (one line) or set GEMINI_API_KEY.")
    from google import genai
    from google.genai import types
    client = genai.Client(api_key=key)

    parts = [GOAL]
    for label, rel in [("PROJECT GOALS / PLAN (stages, 'done' bar, live ledger, known gaps)", "docs/PROJECT_PLAN.md"),
                       ("DESIGN SPEC (per-state target)", "docs/VISUAL_TARGET_SHEET.md"),
                       ("REGRESSION WATCHLIST (recurring issues)", "docs/REGRESSION_CHECKLIST.md")]:
        t = read(REPO / rel)
        if t:
            parts.append(label + ":\n" + t)
    got = []
    for st in STATES:
        p = CONCEPT_DIR / f"{st}_target.png"
        if p.exists():
            parts.append(f"CONCEPT TARGET — {st} (the bar for this state):")
            parts.append(types.Part.from_bytes(data=p.read_bytes(), mime_type="image/png"))
            got.append(st)
    print("concept frames:", got, "| clip:", clip.name, clip.stat().st_size // 1024, "KB", flush=True)
    print("calling Gemini (1-3 min for a video + 7 images + the docs) ...", flush=True)
    parts += ["CURRENT BUILD GAMEPLAY CLIP (assess against the goals + concept above):",
              types.Part.from_bytes(data=clip.read_bytes(), mime_type="video/mp4"), PROMPT]

    last = None
    for attempt in range(1, 5):
        try:
            resp = client.models.generate_content(model="gemini-2.5-flash", contents=parts); break
        except Exception as e:
            last = e; print(f"  attempt {attempt} failed ({type(e).__name__}); retry...", flush=True); time.sleep(4 * attempt)
    else:
        sys.exit(f"failed after retries: {last}")

    text = resp.text or "(no text)"
    outdir = REPO / "docs" / "reviews"; outdir.mkdir(parents=True, exist_ok=True)
    out = outdir / "PROJECT_REVIEW_latest.md"
    out.write_text(f"# Whole-project review (Gemini) — clip {clip.name}, concepts {got}\n\n{text}\n", encoding="utf-8")
    print("=" * 60 + "\nSAVED -> " + str(out) + "\n" + "=" * 60 + "\n" + text, flush=True)


if __name__ == "__main__":
    main()
