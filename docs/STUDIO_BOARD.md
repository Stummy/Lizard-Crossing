# Lizard Crossing — Studio Board

> Living plan for the push to the visual target (`docs/VISUAL_TARGET.md`). Maintained by
> the **studio-producer** role. Status keys: TODO · IN PROGRESS · IN REVIEW · DONE.
> **Last updated: 2026-06-24.**

## Gap-to-target (one line)
Surfaces/mechanics are solid, but the frame is **flat daylight with no grade, no bloom, and
no depth-of-field** — the reference's entire "cinematic" read is missing. Lighting + post + DoF
is the gating issue.

## Current grade (NYC theme, from this session's gameplay frames)
- ✅ Surfaces reskinned (cobblestone / brick / asphalt / granite), 0 magenta, slice runs
  start→safe-zone (z=140). POV cam well calibrated (snout + claw bottom-center).
- ❌ Lighting: flat midday daylight — no warm exposure, color grade, bloom, or AO.
- ❌ Depth-of-field: none (reference's biggest "pop" — sharp hero, blurred giant foreground).
- ❌ Third-person framing loose (hero small) vs tight cinematic bottom-center hero.
- ❌ Grey un-skinned concrete barrier walls in the road/crossing zone.
- ❌ Crosswalk reads as flat orange-red bands, not a legible crosswalk.
- ❔ HUD: needs comparison/polish vs reference (hearts / progress bar+gecko+flag / bug counter).

---

## SPRINT 1 — "Cinematic NYC" (meet the bar in the existing theme)
Goal: make a real gameplay frame of the current NYC run read like the reference's *lighting,
depth, framing, and HUD* — theme-independent wins, biggest pop first. Every work-order ends
with a `gameplay-guardian` PASS (mechanics + frame-time budget).

| # | Status | Owner | Work-order | Files (likely) | Acceptance test |
|---|---|---|---|---|---|
| WO-1 | TODO | lighting-post-artist | URP lighting + post Volume: warm key sun, ambient/skybox, ACES tonemap, color grade, gentle bloom, vignette, soft shadows + AO | `CinematicPost.cs`, lighting/skybox, URP Volume | Frame reads warm & sunny, soft contact shadow under the lizard, no blown highlights, 0 magenta |
| WO-2 | TODO | lighting-post-artist (+camera-ui-juice) | Depth-of-field driven by camera→lizard focus: hero tack-sharp, close foreground hazards blurred, far bg soft | `CinematicPost.cs`, `LizardCameraController.cs` | Hero sharp, close hazard clearly blurred, bg soft; DoF tuned for mid-tier phone |
| WO-3 | TODO | camera-ui-juice | Tighten third-person framing: hero bigger & bottom-center, central lane to safe zone clear, hazards still read giant at edges | `LizardCameraController.cs`, `GameConst` cam consts | Hero fills more of lower frame, lane/hazards readable, POV re-shot & intact |
| WO-4 | TODO | environment-artist | Skin the grey road-zone barrier walls + make the crosswalk read as a real crosswalk | `LevelBuilder.cs`, `CityReskin.cs`, `Resources/GeneratedArt` | No flat-grey walls, crosswalk legible, 0 magenta, real-world scale |
| WO-5 | TODO | camera-ui-juice | HUD polish to match reference: hearts (TL), rounded level progress bar + gecko marker + checkered flag + "LEVEL n" (TC), bug counter (TR) | `SimpleHUDController.cs`, `UIFactory.cs` | Matches VISUAL_TARGET §5, crisp at portrait, safe-area aware |
| WO-6 | TODO | art-director | Re-grade the full run vs target; update gap-to-target; scope Sprint 2 | (review) | New % to-target + named next sprint |

**Dispatch order:** WO-1 → WO-2 → WO-3 → WO-4 → WO-5 → WO-6. (1 & 2 are the big pop; do them first.)

---

## BACKLOG (later sprints)
- **Sprint 2 — "Cohesive NYC":** material/texture quality pass, set-dressing cohesion (kill
  mismatched props, palette discipline), light tail-drop/juice polish, near-miss slow-mo feel.
- **Sprint 3 — "Theme-swap plumbing":** environment-artist adds a data-driven theme system to
  `LevelBuilder` (surface set + prop/furniture kit + hazard skin + palette/grade per theme),
  mechanics unchanged.
- **Sprint 4 — "Boardwalk":** build the Boardwalk kit to the reference (planks/sand/pavers,
  surf shack, tiki bar, palms, flowers, rope rails; scooter + beach-goer hazards; warm grade).

## Review log
- _(empty — first review after WO-1.)_
