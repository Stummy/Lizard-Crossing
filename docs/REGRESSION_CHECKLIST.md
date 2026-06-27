# Regression Checklist — "have any old problems come back?"

**Purpose (owner rule, 2026-06-26):** past issues kept silently re-appearing. This is the
SINGLE canonical list of every recurring problem we've fought before. It is:
1. **Fed to the Gemini video bot every run** (`Tools/gemini_review.py` injects this file) — it
   reports each `[Rn]` as **PRESENT / FIXED / CAN'T-TELL** against the clip, so no old bug ships
   unnoticed.
2. **Checked in the verify loop** before any visual change is "done" (see CLAUDE.md "Regression
   checklist").
3. **Kept LIVE** — when a NEW recurring issue is found, add an `[Rn]` here so it's watched forever.

Statuses below are the LAST KNOWN state; the Gemini run is the live re-check. Machine-gated items
(`[Rn]` tagged ⚙️) are asserted by the Invariant Check / magenta scan / bot playthrough, NOT by Gemini.

---

## HERO LIZARD
- **[R1]** Lizard FACES the camera or shows its side — it must keep its BACK/TAIL to the camera, head up-street, NEVER facing the viewer.
- **[R2]** Lizard reads as a "frog" / wrong species / low-detail blob, not a vivid emerald GECKO. *(known PRESENT — locked Lizard section, Stage 4)*
- **[R3]** Run animation looks like a WALK / stiff / slidey — feet skate, no clear mid-stride leg motion. *(known PRESENT — locked)*
- **[R4]** Lizard feet or tail CLIP / sink into the ground. *(known PRESENT — locked)*
- **[R5]** On impact the lizard clips INTO a wall/prop instead of a readable faceplant (smush → hold → recover).

## CROWD / PEDESTRIANS
- **[R6]** Pedestrians blown out / "glowing white blobs" (over-exposed near-white albedo).
- **[R7]** Pedestrians low-poly / blocky / flat, or tinted YELLOW.
- **[R8]** Pedestrians HOVER / float above the pavement, or pop up/down at curbs.
- **[R9]** Pedestrians POP in/out (visible spawn/despawn), or stream up from BEHIND the lizard.
- **[R10]** Pedestrians read as small full figures instead of GIANT towering legs/shoes from the low POV.

## CAMERA / FRAMING  (🔒 locked + owner-tuned — Gemini FLAGS only; do not retune without owner OK)
- **[R11]** Camera too HIGH / not the ~3 cm speck's-eye POV (the world doesn't tower).
- **[R12]** Camera BOB / jitter / unstable, or the lizard slides off-centre unreadably.
- **[R13]** "Zoomed in" / framing too tight (no deep open avenue). *(owner's exact past complaint)*

## LIGHTING / GRADE
- **[R14]** GOLDEN / warm / yellow cast — OWNER REJECTED. Must be NEUTRAL natural daylight.
- **[R15]** Too COOL / blue / desaturated, or flat / overcast — not clean true-to-life daylight.
- **[R16]** A deep / occluded / canyon stretch goes DARK / near-night while the start is bright (inconsistent exposure along the run).
- **[R17]** Sky not soft cyan — harsh electric blue, OR blown-white sky / between-building gap blobs.
- **[R18]** DoF flat — no creamy bokeh background / no blurred giant foreground; the hero is NOT the only tack-sharp thing.

## ENVIRONMENT / SET
- **[R19]** Sidewalk / pavement reads generic GREY (not warm stone), or the ground texture stretches / warps / is low-res.
- **[R20]** Buildings generic / blocky-modern, lacking NYC character (brownstone / warm stone).
- **[R21]** Flat-RED / ORANGE / MAGENTA placeholder materials (the Standard-shader magenta trap; the old flat-red fences).
- **[R22]** No yellow CAB / NYC street identity visible in or near the run.
- **[R23]** Safe-zone goal NOT a glowing beacon down the lane; OR Central Park reads as a greybox / the grey GLB building wall shows behind it.

## HUD / JUICE
- **[R24]** HUD placeholder: hearts / bug-icon unstyled; progress bar lacks the little gecko marker + checkered finish flag.
- **[R25]** Extraneous text / dev UI over the lane (a level-title banner, POV / debug buttons).
- **[R26]** On-screen popups (CLOSE CALL! / TAIL DROPPED! / OUCH!) too LARGE / unstyled / obstructing the lane.
- **[R27]** Near-miss has no juice (no speed-lines / whoosh / blue-white "!" flash); a hit reads weakly ("clips then OUCH" / fade-to-black, not a distinct impact).

## MECHANICS / SPATIAL  (⚙️ machine-gated — asserted by Invariant Check / bot / scan, not Gemini)
- **[R28]** ⚙️ Lizard leaves the sidewalk / passes through a wall / the run band drifts or loses its floor. → `Bot/Invariant Check` must PASS.
- **[R29]** ⚙️ A crosswalk crossing is impassable (no safe gap opens) OR a car hit does NOT cost a heart. → bot playthrough reaches the safe zone; car-hit→HitPlayer verified.
- **[R30]** ⚙️ Any magenta / un-skinned material in the scene. → renderer scan = 0 bad materials.

---

### How to read a Gemini regression report
The bot returns a `## REGRESSION CHECKLIST` section listing each `[Rn]` it can judge as PRESENT
(with a timestamp) or FIXED. **Any PRESENT item is a regression** — fix it, or consciously
accept it (e.g. R2/R3/R4 are knowingly open behind the locked Lizard section until Stage 4) and
say so. Log notable shifts into `docs/PROJECT_PLAN.md` §5. Weight concrete PRESENT reports over
vibe; remember Gemini is unreliable on lighting (R14–R17) — confirm those on the real MP4.
