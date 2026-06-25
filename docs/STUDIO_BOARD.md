# Lizard Crossing — Studio Board

> Living plan for the push to the visual target (`docs/VISUAL_TARGET.md`). Maintained by
> the **studio-producer** role. Status keys: TODO · IN PROGRESS · IN REVIEW · DONE.
> **Last updated: 2026-06-24.**

## Gap-to-target (one line)
Lighting + post + DoF landed (WO-1/WO-2 DONE — gameplay-guardian PASS): the frame now reads **warm, sunny, and
cinematic** with an HDRI sky, ACES grade, bloom, vignette, AO, and bokeh DoF (sharp hero /
blurred giant foreground). Framing tightened (WO-3 IN REVIEW — hero now large/bottom-center/
sharp). Remaining gap is mostly **set dressing** (barrier walls / crosswalk — WO-4) and **HUD
polish** (WO-5), plus an on-device frame-time confirm.

## Current grade (NYC theme, from this session's gameplay frames)
- ✅ Surfaces reskinned (cobblestone / brick / asphalt / granite), 0 magenta, slice runs
  start→safe-zone (z=140). POV cam well calibrated (snout + claw bottom-center).
- ✅ Lighting/post (WO-1): HDRI skybox + warm key sun + Skybox ambient + ACES grade + bloom +
  vignette + SSAO. Warm, sunny, no blown highlights. 0 magenta.
- ✅ Depth-of-field (WO-2): bokeh DoF — sharp hero, blurred giant foreground, soft bg
  (focus tracks camera→lizard; camera-ui-juice to drive it dynamically next).
- ✅ Third-person framing tightened (WO-3): hero now reads large, bottom-center & tack-sharp
  with a giant blurred foreground pedestrian; DoF focus smoothed so the hero never blurs. (Pending guardian PASS.)
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
| WO-1 | DONE (guardian PASS 2026-06-24) | lighting-post-artist | URP lighting + post Volume: warm key sun, ambient/skybox, ACES tonemap, color grade, gentle bloom, vignette, soft shadows + AO | `CinematicPost.cs`, lighting/skybox, URP Volume | Frame reads warm & sunny, soft contact shadow under the lizard, no blown highlights, 0 magenta |
| WO-2 | DONE (guardian PASS 2026-06-24) | lighting-post-artist (+camera-ui-juice) | Depth-of-field driven by camera→lizard focus: hero tack-sharp, close foreground hazards blurred, far bg soft | `CinematicPost.cs`, `LizardCameraController.cs` | Hero sharp, close hazard clearly blurred, bg soft; DoF tuned for mid-tier phone |
| WO-3 | DONE (verified 2026-06-24) | camera-ui-juice (+main: feed-forward fix) | Tighten third-person framing: hero bigger & bottom-center, central lane to safe zone clear, hazards still read giant at edges | `LizardCameraController.cs`, `GameConst` cam consts, `CinematicPost.cs` | Hero fills more of lower frame, lane/hazards readable, POV re-shot & intact |
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
- **2026-06-24 — WO-3 (camera-ui-juice + main), DONE/verified.** camera-ui-juice tightened the
  rig (CamBack 0.34→0.22, lower/zoomed FOV 62→55, smoothed dynamic DoF focus) but the hero still
  measured only **~10% frame width** — the constant +Z auto-run created a steady SmoothDamp
  following-lag (~0.22u) that doubled the camera distance. Fixed in `LizardCameraController` with
  **Z-velocity feed-forward** (cancels the lag at any run/dash speed; lateral lag kept so weaving
  still reads). Measured result: camera dist **0.438→0.235u**, hero **9.9%→23.5% frame width**,
  bottom-center (viewport y≈0.18), tack-sharp; **POV unchanged/calibrated**, lizard in-band
  (x=9.02), auto-run intact. Matches the reference hero framing.
- **2026-06-24 — WO-3 (camera-ui-juice), IN REVIEW.** Tightened the third-person framing so the
  hero reads LARGE, bottom-center, and tack-sharp (it was a tiny speck on empty pavement).
  Changes are camera/post only — **lizard NOT resized, FP/POV math untouched, no gameplay/band/
  hazard edits.** (1) `GameConst` cam consts: pulled the rig closer (`CamBack` 0.34→0.22) and
  lower (`CamHeight` 0.14→0.105), narrowed the FOV (`CamBaseFov` 62→55, `CamMaxFov` 76→72,
  `CamTargetHorizontalFov` 58→52) so the lizard renders ~40% larger while the city still towers,
  lowered the look point a touch (`CamLookHeight` 0.06→0.052) to anchor the hero bottom-center,
  and opened the central lane to the goal (`CamLookAhead` 0.5→0.55). (2) `LizardCameraController`:
  tighter follow SmoothDamp (0.12→0.06) so the constant +Z auto-run doesn't let the hero lag far
  back behind the closer rig (steady-state camBack settles ~0.40u). (3) `CinematicPost`: DoF now
  **smoothed** — focus eases (SmoothDamp 0.10s, unscaled time) toward the camera→lizard distance
  so dash FOV kicks / near-miss slow-mo / shake can't pulse the hero out of focus; widened the
  lens for the now-closer hero (`DofFocalLength` 45→38mm, `DofAperture` f6.5→f9, fallback 1.8→1.2m)
  so the WHOLE lizard stays crisp while the ~0.2-0.3u foreground hazard and the far city still
  blur strongly. **Verified in-engine** (fresh Play, Start Run + auto-run): hero big/sharp/bottom-
  center with a giant blurred pedestrian and an open lane (`Temp/Shots/after_tp_mid.png` vs
  `before_tp_mid.png`); **POV re-shot and pixel-for-pixel unchanged** (FP camPos +0.04 fwd/+0.03
  up of the lizard, pitch 24°, FpFov 95 — `after_pov.png` vs `before_pov.png`); lizard auto-runs
  +Z, x=9.0 inBand [5.70,11.00] the whole way, camY 0.225 (low POV), **0 console errors, 0 magenta
  across 1695 materials**. **Handed to gameplay-guardian:** confirm control feel (closer/tighter
  follow + narrower FOV) and the frame-time budget are still in bounds. **Note for lighting-post-
  artist:** the bigger/closer hero already helps the "giant-leg-occludes-the-sun silhouette" risk
  the WO-1/WO-2 guardian flagged — exposure/fill left UNCHANGED here; if the hero still sinks to
  silhouette in heavy-occlusion frames, that min-exposure/fill floor is yours to add. Full regression +
  budget gate. (1) Diff review: all changes visual-only. `CinematicPost.cs` is pure post (one
  global Volume: ACES, warm Color Adjustments/WhiteBalance/Lift-Gamma-Gain, half-res Bloom,
  Vignette, Bokeh DoF with camera→lizard focus in LateUpdate, `SetLite`). `Bootstrap.cs` lighting
  block (sun/fill/Skybox ambient/DynamicGI/shadowDistance) touches no gameplay — world build,
  player spawn (`CorridorCenterX`/`SidewalkY`), predator wiring all unchanged. `LizardCameraController.cs`
  WO lines = `clearFlags SolidColor→Skybox` + fog push-out (60/260 → 110/320) only; the camera
  TRANSFORM/POV math (`DesiredPosition`, `LookPoint`, FP/TP update) is unchanged by this pass.
  SSAO renderer feature = half-res / AfterOpaque / Samples 2 / Intensity 0.5 (cheapest tier). No
  hardcoded "Standard"; no corridor-band/scale/lizard-size/hazard-direction edits. (2) Bot
  playthrough (fresh Play, Start Run → Move Forward): auto-ran +Z from z=2 to z≈115, X clamped in
  band (e.g. x=8.97 vs band [5.70,11.00], inBand=True the whole way), 31 peds + 8 cars crossing
  ±X and lethal (bot died of hazard hits at z≈115 — expected; bot can't dodge), low POV cam (camY
  0.26) tracking lizard X, **0 console errors, 0 magenta across 1477 renderers** in two fresh
  sessions. Start frame: hero tack-sharp bottom-center, warm cobble/brick, soft-blurred bg = WO-1+
  WO-2 target met (`Temp/Shots/guard_start.png`, `guard_mid.png`). (3) Budget: editor profile of
  the full DoF+Bloom+SSAO stack ≈ **9ms GPU median** at the 540×960 capture res; game CPU
  (PlayerLoop) ≈16ms with the main thread WaitForJobGroupID-bound (render/GPU is the limiter, not
  game logic), EditorLoop ~6ms is editor-only and won't ship. **Recommendation:** ship the current
  stack on mid/high tier; **Bokeh DoF + SSAO are fill-rate-heavy and WILL cost more at 1080×2400 on
  a real mid-tier phone** than in this editor capture — wire `CinematicPost.SetLite(true)` (drops
  DoF+Bloom) to a device-tier/quality toggle for low-end, and consider also gating SSAO off in that
  same lite path (the grade alone is ~free and keeps the warm read). On-device frame-time confirm
  still owed before final low-tier sign-off, but mechanics + magenta + editor budget all PASS, so
  WO-1/WO-2 are marked **DONE**. Non-blocking art note for art-director/lighting: mid-run frames go
  quite dark when a giant pedestrian leg fills the foreground and occludes the key — consider a touch
  more ambient/fill or a min-exposure floor so the hero never sinks into silhouette.
- **2026-06-24 — WO-1 + WO-2 (lighting-post-artist), IN REVIEW.** The flat-midday gap is
  closed. Added an image-based HDRI skybox (`qwantani_puresky_2k`, set Cube/lat-long/linear)
  → `Assets/Resources/Sky/PureSkySkybox.mat`; camera now clears to Skybox; sun realigned to
  the HDRI sun (warm key, soft shadows) with Skybox ambient + `DynamicGI.UpdateEnvironment`
  (no baked GI). One global URP Volume in `CinematicPost.cs`: ACES tonemap, warm Color
  Adjustments (+exposure/contrast/sat, warm filter), WhiteBalance, Lift-Gamma-Gain grade
  toward §3 palette, half-res sun-kissed Bloom, subtle Vignette, and **Bokeh DoF** (hero
  tack-sharp, close foreground hazards strongly blurred, far bg soft; focus tracks
  camera→lizard each frame). Added URP **SSAO** renderer feature (half-res, AfterOpaque,
  cheapest settings) so nothing floats. Verified in-engine across start/mid/safe-zone (TP) +
  POV: **0 console errors, 0 magenta**, slice still runs start→safe-zone (z=140). Removed the
  old MotionBlur override to save mobile cost. **Open items handed off:** (1) camera-ui-juice
  to drive `focusDistance` dynamically + tighten TP framing (WO-3 — hero reads small);
  (2) gameplay-guardian to confirm the DoF+Bloom+SSAO frame-time budget on a mid-tier device
  and decide whether SSAO stays or the `SetLite()` path is wired to a quality toggle.
