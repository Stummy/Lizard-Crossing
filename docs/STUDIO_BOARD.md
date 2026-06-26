# Lizard Crossing — Studio Board

> Living plan for the push to the visual target (`docs/VISUAL_TARGET.md`). Maintained by
> the **studio-producer** role. Status keys: TODO · IN PROGRESS · IN REVIEW · DONE.
> **Last updated: 2026-06-26.**

## ▶ WHERE WE ARE RIGHT NOW (read this first on login)
**Active section:** World + corridor — the loop-bug ("lizard through walls / sidewalk shifts /
no floor / peds through fences / no cars") is **structurally fixed and machine-locked**: an
authored straight walled corridor (real colliders) replaced the crooked GLB, and the
**Invariant Check PASSES** (lizard can't leave the band, run is straight). Verify done; only the
owner's sign-off to **Lock** the section remains.
**Just shipped (2026-06-26):** DASH button moved off the hero (was dead-center on the lizard);
pedestrians corrected to real human scale (2.5u→1.8u — the scout's fix for the #1 "placeholder
ped" complaint), squish verified intact.
**The ONE next thing:** record a sustained dodging clip and run the **concept-aware Gemini tester**
(`python Tools/gemini_review.py --state run`) — it adjudicates the three open visual reads
(start-spot washout/ped-bloom, the 1.8u ped fidelity, the DASH reposition) against `run_target.png`
so we stop eyeballing single frames. Then work its punch-list.
**Quick status by section:** Lizard 🔒 · Controls+camera 🔒 · World+corridor ✅ (sign-off pending) ·
Hazards 🟦 (crowd ✅, cars/traffic ⬜) · Lighting 🟦 · HUD+juice 🟦 (DASH ✅) · Screens 🟦
(start/death panels exist, win polish ⬜) · Audio ⬜ · Meta/ship ⬜.

## 🧑‍🤝‍🧑 THE TEAM — who I delegate what to (the lead routes; the Unity editor is single-threaded, so
**one** in-engine job at a time — off-engine jobs run in parallel via background agents)
| When the work is… | I route it to | Runs where |
|---|---|---|
| "why doesn't it look like the reference?" — grade frames, punch-list | **art-director** | off-engine (reads frames) |
| sourcing/generating assets (models, textures, HDRI, peds, props) | **asset-scout** | off-engine (files/Meshy/CC0) |
| lighting · post · DoF · exposure/washout calls | **lighting-post-artist** | in-engine (serialize) |
| surfaces · materials · props · set-dressing · alley/theme | **environment-artist** | in-engine (serialize) |
| camera feel · HUD · juice · menus/screens | **camera-ui-juice** | in-engine (serialize) |
| regression + perf **gate** before a section/stage is "done" | **gameplay-guardian** | in-engine (serialize) |
| sequencing the board / what-ships-next | **studio-producer** | off-engine (planning) |
| the QA "video guy" — record→watch→file bugs+concept-gap | `Tools/gemini_review.py` | off-engine (CLI) |
The **main session (me)** is the build lead + integrator: I hold the live editor, do the in-engine
work + verify-and-ship, and dispatch agents for off-engine work (so it parallelizes) or for a
fresh-context judgment call. Proven this session: scout ran in the background and diagnosed the
ped problem while I fixed the HUD in-engine — no editor conflict.

## Gap-to-target (one line)
**~78% to target (art-director re-grade, WO-6 DONE 2026-06-25).** Sprint 1 "Cinematic NYC" closed
the big theme-independent levers: the run now reads **warm, sunny, cinematic** with a sharp
hero lizard bottom-center, a **genuinely giant blurred foreground hazard** (the mid-run boot and
the POV pedestrian both nail "I am tiny in a huge world"), grounded mid-tones, and a reference-
layout HUD. **The single gating issue is DoF discipline: the background is over-blurred** (38mm/f9
Bokeh blurs the whole city to a soft wash), which erases two target reads — the **clear lane to a
visible SAFE-ZONE marker** (there is no goal marker in frame at all) and the **crosswalk** (flat
stripes at distance dissolve in the blur). Secondary gaps: heavy top vignette crushing the
skyline to near-black, set-dressing/palette cohesion + prop-scale spot checks, and HUD top-bar
contrast. **Next:** Sprint 2 "Cohesive NYC" (below) + the still-owed on-device frame-time confirm.

## Current grade (NYC theme, from this session's gameplay frames)
- ✅ Surfaces reskinned (cobblestone / brick / asphalt / granite), 0 magenta, slice runs
  start→safe-zone (z=140). POV cam well calibrated (snout + claw bottom-center).
- ✅ Lighting/post (WO-1): HDRI skybox + warm key sun + Skybox ambient + ACES grade + bloom +
  vignette + SSAO. Warm, sunny, no blown highlights. 0 magenta.
- ✅ Depth-of-field (WO-2): bokeh DoF — sharp hero, blurred giant foreground, soft bg
  (focus tracks camera→lizard; camera-ui-juice to drive it dynamically next).
- ✅ Third-person framing tightened (WO-3): hero now reads large, bottom-center & tack-sharp
  with a giant blurred foreground pedestrian; DoF focus smoothed so the hero never blurs. (Pending guardian PASS.)
- ✅ Road-zone barrier walls skinned (WO-4): the grey curb/barrier (`CityGen_Curb`) now reads
  as granite stone, the flat-orange crosswalk band (`CityGen_lanes_secondary_color`) + a pink
  ground-decal layer (`Street_Assets.001`) reskinned to asphalt, stripes (`CityGen_lanes_white`)
  forced to clean painted white. Plus 8 generated NYC props scattered as set dressing.
- ✅ HUD rebuilt to reference (WO-5): hearts (TL, filled red + greyed sockets), "DOWNTOWN DASH"
  title + rounded green progress bar with a live **gecko marker** + **checkered goal flag** +
  "LEVEL 1" (TC), "n / total" bug counter w/ fly icon (TR). Soft shadows + dark text outlines,
  no opaque panel, safe-area inset. Binds live to GameStateManager hearts/bugs + lizard.z/Length.

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
| WO-4 | DONE (verified 2026-06-24) | environment-artist | Skin the grey road-zone barrier walls + make the crosswalk read as a real crosswalk (+ import/wire 8 generated NYC props) | `CityReskin.cs`, `LevelBuilder.cs`, `Resources/Models/Generated` | No flat-grey walls, crosswalk legible, 0 magenta, real-world scale |
| WO-5 | DONE (verified 2026-06-24) | camera-ui-juice | HUD polish to match reference: hearts (TL), rounded level progress bar + gecko marker + checkered flag + "LEVEL n" (TC), bug counter (TR) | `SimpleHUDController.cs`, `UIFactory.cs`, `ProceduralTextures.cs` | Matches VISUAL_TARGET §5, crisp at portrait, safe-area aware |
| WO-6 | DONE (2026-06-25) | art-director | Re-grade the full run vs target; update gap-to-target; scope Sprint 2 | (review) | New % to-target + named next sprint |
| WO-7 | DONE (verified 2026-06-25) | lighting-post-artist | Exposure/bloom touch-up: kill the washed-out/over-bloomed haze (asphalt stays dark) + add an ambient/fill floor so the hero never sinks to silhouette under heavy occlusion | `CinematicPost.cs`, `Bootstrap.cs` | Mid-tones grounded (no near-white asphalt), still warm/sunny, hero never black-silhouette, 0 errors/magenta |

**Dispatch order:** WO-1 → WO-2 → WO-3 → WO-4 → WO-5 → WO-6. (1 & 2 are the big pop; do them first.) WO-7 (exposure/bloom touch-up) done after WO-4's washout flag.

---

## SPRINT 2 — "Cohesive NYC" (scoped by art-director, WO-6, 2026-06-25)
Goal: convert Sprint 1's cinematic base into a *legible, cohesive* frame — restore the reads the
strong DoF currently eats (goal marker, lane, crosswalk), tighten set/palette cohesion, and add
the missing game-feel juice. Each work-order ends with a `gameplay-guardian` PASS (mechanics +
frame-time). **Dispatch order: S2-1 → S2-2 → S2-3 → S2-4 → S2-5 → S2-6.** S2-1 is the single
highest-leverage change (it gates the "% to target").

| # | Status | Owner | Work-order | Files (likely) | Acceptance test |
|---|---|---|---|---|---|
| S2-1 | TODO | lighting-post-artist (+camera-ui-juice for focus target) | **DoF discipline + readable lane.** Background is over-blurred (38mm/f9 Bokeh washes the whole city). Pull the far falloff back so the mid-ground city + the running lane read as a recognizable place, while KEEPING the hero tack-sharp and the close foreground hazard strongly blurred. Tune focal length/aperture/`focusDistance` curve (or a far-blur clamp). Also tame the top vignette so the skyline isn't crushed to near-black. | `CinematicPost.cs` (`DofFocalLength` 38 / `DofAperture` f9 / focus track), Vignette override | Hero + close hazard read exactly as now; the mid-ground city/lane is recognizable (not a wash); skyline not black-crushed; still cinematic; 0 magenta; DoF still mobile-tunable via `SetLite` |
| S2-2 | TODO | environment-artist (+camera-ui-juice) | **Visible SAFE-ZONE goal marker.** Target §1/§5 demand a clear destination vanishing toward the horizon; there is currently NO goal marker in the near-safe-zone frame. Add a readable safe-zone marker/sign/arch at z≈140 (and a faint progress read down the lane) so the player always knows where to run. Visual-only — do not move the z=140 threshold or narrow the band. | `LevelBuilder.cs` (safe-zone build), `CityReskin.cs`/props, `GameConst` | A safe-zone marker is clearly visible from ~10-20u out down the central lane; reads at lizard-eye height; band/threshold unchanged; loop `gameplay-guardian` |
| S2-3 | TODO | environment-artist | **Crosswalk that reads from the game camera.** WO-4 made the stripes correct in data, but flat-on-ground stripes at distance dissolve under the low POV + DoF (see `grade_crosswalk.png`). Make the crossing read AS a crosswalk from the actual camera — bolder/wider high-contrast stripes, a curb/stop-line cue, or a subtle raised/edge read so it survives foreshortening + blur. | `CityReskin.cs` (`CityGen_lanes_white`/`_secondary`), road-zone build | From a TP frame approaching a road lane, the crosswalk is unmistakably a crosswalk; no flat-grey road; 0 magenta |
| S2-4 | TODO | environment-artist | **Set-dressing cohesion + palette discipline + prop-scale audit.** One cohesive NYC block: spot-check each generated prop for scale/orientation/texture quality (hydrant verified glTF/2048/not-magenta this pass — confirm the rest), kill any mismatched/clashing props, and enforce the §3 palette so the hero green stays the most saturated thing in frame. | `LevelBuilder.cs` (`BuildStreetProps`/furniture), `CityReskin.cs` | Every placed prop is real-world-scale, correctly oriented, ≤2048 textures, palette-consistent; nothing out-competes the hero green; 0 magenta |
| S2-5 | TODO | camera-ui-juice | **Tail-drop + near-miss juice + HUD contrast.** Add the tasteful game-feel the reference implies: tail-drop/heart-loss feedback, near-miss slow-mo/hit-stop, light screen shake/particles on hit — all visual, no mechanic/feel change to steer/dash/band. Also lift the top-HUD text/icon contrast (title + gecko marker + fly icon read marginally over bright scenes) and confirm the gecko marker is always visible on the bar. | `camera-ui-juice` FX, `SimpleHUDController.cs`, `CinematicPost.cs` (slow-mo hook) | Hit/near-miss have clear feedback; HUD legible over bright/dark backgrounds; gecko marker always visible; sacred mechanics untouched (loop `gameplay-guardian`) |
| S2-6 | TODO | gameplay-guardian | **On-device frame-time confirm (still owed from Sprint 1).** Confirm the DoF+Bloom+SSAO stack (post S2-1 tuning) holds frame budget on a real mid-tier phone at 1080×2400, and validate the `CinematicPost.SetLite(true)` low-tier path drops DoF+Bloom (+gate SSAO) cleanly. | `CinematicPost.cs` `SetLite`, build/profiler | Mid-tier device hits target frame time; lite path verified; sign-off recorded |

## BACKLOG (later sprints)
- **Sprint 3 — "Theme-swap plumbing":** environment-artist adds a data-driven theme system to
  `LevelBuilder` (surface set + prop/furniture kit + hazard skin + palette/grade per theme),
  mechanics unchanged.
- **Sprint 4 — "Boardwalk":** build the Boardwalk kit to the reference (planks/sand/pavers,
  surf shack, tiki bar, palms, flowers, rope rails; scooter + beach-goer hazards; warm grade).

## Review log
- **2026-06-25 — WO-6 (art-director), DONE. Sprint 1 capstone re-grade + Sprint 2 scope.**
  Fresh in-engine grade of the full NYC run vs `VISUAL_TARGET.md` — captured `Camera.main`→RT→PNG
  frames (the low POV cam can't use `Unity_Camera_Capture`) at start, mid-run, road, crosswalk,
  near-safe-zone, a hydrant close-up, and one POV; HUD via a full-screen `ScreenCapture`. Fresh
  Play session, `Time.timeScale=0.25` for clean frames, restored to 1; **0 console errors, 0
  magenta across 1678 materials**; POV cam calibrated (camY 0.152, pitch 24°, FpFov 95). Frames:
  `Temp/Shots/grade_start.png`, `grade_mid.png`, `grade_road.png`, `grade_crosswalk.png`,
  `grade_safe.png`, `grade_pov.png`, `grade_hydrant.png`, `grade_hud.png`.
  **Verdict: ~78% to target. Gating issue = DoF over-blur of the background.**
  **Per-lever grade (VISUAL_TARGET §2):**
  - **Lighting/exposure/grade — ~90% (AT BAR).** Warm, sunny, grounded mid-tones; cobble/asphalt
    read as textured grey stone (no wash), buildings keep warm/cool separation, hero green is the
    most saturated thing in frame. WO-7's exposure/bloom tame held. Soft contact shadow under the
    hero. (`grade_start.png`, `grade_pov.png`.)
  - **Depth of field — ~70% (the lever that's now OVER-applied).** Hero is tack-sharp, the close
    foreground hazard blurs strongly and beautifully (mid-run boot + POV pedestrian both nail the
    giant-foreground read). BUT the far falloff is too aggressive (38mm/f9 Bokeh) — the entire
    city/lane becomes a soft wash (`grade_safe.png`, `grade_crosswalk.png`), erasing the lane-to-
    goal read. This is the #1 Sprint 2 fix (S2-1).
  - **Cohesive themed set + palette — ~75%.** Warm brick NYC reads as one place in the POV/road
    frames; hydrant verified (glTF shader, 2048 albedo, baseColorFactor white, not magenta). Needs
    a full prop scale/quality/palette audit (S2-4).
  - **Composition/framing — ~80%.** Hero bottom-center & large (WO-3 feed-forward win), low POV,
    giant side hazards — all on target. Two misses: NO visible safe-zone goal marker at the lane
    end (`grade_safe.png`), and a heavy top vignette crushing the skyline to near-black
    (`grade_start.png`). (S2-1 vignette + S2-2 goal marker.)
  - **HUD — ~80%.** Reference layout present: hearts TL, "DOWNTOWN DASH" + green progress bar +
    checkered flag + "LEVEL 1" TC, "n/total" bug counter TR, friendly game-over (STOMPED!/REVIVE/
    TRY AGAIN/HOME). Top-bar text/icon contrast is marginal over bright scenes and the gecko marker
    is hard to spot (`grade_hud.png`). (S2-5.)
  - **Materials/textures — ~80%.** Believable, not noisy at the mobile clamp; the crosswalk is the
    one surface that fails to read from the game camera (flat stripes + distance + blur). (S2-3.)
  **Sprint 2 "Cohesive NYC" scoped above** (S2-1…S2-6): DoF discipline + lane read (gating),
  safe-zone goal marker, camera-readable crosswalk, set/palette cohesion + prop audit, tail-drop/
  near-miss juice + HUD contrast, and the on-device frame-time confirm. **No code changed in this
  WO — review-only** (timeScale restored to 1, exited Play cleanly; the only console errors were my
  own malformed MCP calls, not game/compile errors).
  **Sprint 1 retro:** the dispatch order was right — lighting/post/DoF first delivered the biggest
  pop and got us from "flat grey midday" to a genuinely cinematic ground-level POV. The two
  best frames (`grade_mid.png` boot, `grade_pov.png` pedestrian) already look like the reference's
  feel. The lesson for Sprint 2: **the same DoF that created the cinematic pop is now over-applied
  and eating legibility** (lane, goal, crosswalk) — Sprint 1 maximized "cinematic," Sprint 2 must
  rebalance toward "cinematic AND readable." Set-dressing/HUD landed structurally but need a
  cohesion/contrast polish pass, not a rebuild. On-device frame-time confirm is still owed and is
  now folded into Sprint 2 (S2-6) so it can't slip again.
- **2026-06-25 — WO-7 (lighting-post-artist), DONE/verified.** Touch-up on the WO-1/WO-2
  lighting after gameplay frames read **washed-out / over-bloomed** (hazy glow over the scene,
  sunlit pavement near-white) and the guardian flagged the hero **sinking toward silhouette**
  when a giant pedestrian leg occludes the sun. **Visual-only — no gameplay/camera-transform/
  lizard-scale edits.** Exact values changed: **(1) `CinematicPost.cs`** — Color Adjustments
  `postExposure` **0.35→0.15** (the +0.35 lift was blowing mid/bright surfaces); Bloom
  `threshold` **1.10→1.30** and `intensity` **0.45→0.30** so only true highlights (sky, chrome,
  sun glints) bloom instead of the whole sunlit street hazing over. Tonemap/DoF/Vignette/
  WhiteBalance/Lift-Gamma-Gain left as-is. **(2) `Bootstrap.cs`** — cool Fill light `intensity`
  **0.22→0.36** (wrap-around light from a different angle than the key, so an occluded sun
  doesn't leave the hero black) and Skybox `ambientIntensity` **1.0→1.12** (small, direction-
  uniform light floor). Both kept deliberately gentle — a sweep showed pushing fill/ambient
  harder (0.50/1.20, 0.60/1.30) did **not** lift the occluded hero further (it stayed ~0.31
  luma) and actually *flattened* the lit look, so the modest values are the right call.
  **Verified in-engine** (fresh Play session, Start Run + Move Forward, `Time.timeScale=0.25`
  for clean frames, restored to 1): start/mid/road-zone frames re-shot via `Camera.main`→RT→PNG
  (the low POV cam can't use Unity_Camera_Capture). The cobblestone/pavement now reads as
  grounded textured grey stone (was a bright bloom-hazed pale wash); buildings keep warm-vs-cool
  separation instead of glowing flat; hero green stays the most saturated thing in frame; still
  warm & sunny, no longer hazy/blown. **Anti-silhouette measured A/B on the same geometry** (key
  sun forced to 0 = full boot occlusion): lizard-green luma OLD lighting **0.300** → NEW **0.309**
  (never the <0.05 of a true black silhouette; the bigger win is the exposure tame — calmer
  bright background means the shadowed hero no longer reads as a black cut-out against blown-white
  pavement). **0 console errors, 0 magenta (1702 materials)**, `Time.timeScale` restored to 1,
  lights restored to shipped values. Frames: `Temp/Shots/before_start.png`,`before_mid.png`,
  `before_road.png` vs `after_start.png`,`after_mid.png`,`after_road.png`,`after_occluded.png`.
  **Perf:** zero new cost — these are parameter changes on the existing Volume/lights (a *higher*
  bloom threshold is if anything marginally cheaper). DoF+Bloom remain the mobile-budget watch
  items per WO-3's guardian note; `CinematicPost.SetLite(true)` still drops both for low-tier.
  **Remaining gap to target:** lighting/exposure now grounded; next is WO-6 (art-director re-grade
  + Sprint 2 scope) and the still-owed on-device frame-time confirm.
- **2026-06-24 — WO-5 (camera-ui-juice), DONE/verified.** Rebuilt the in-run HUD to match
  VISUAL_TARGET §5 — **visual/UI only, no gameplay logic touched** (still purely presents existing
  GameStateManager / PlayerController / LevelDefinition state; sacred mechanics untouched).
  **(1) `ProceduralTextures.cs`:** added two cached procedural sprites — a side-on **gecko**
  silhouette (capsule body + curling tail + feet, faces +x = run dir) for the progress marker and
  a rounded **checkered race-flag** for the goal end (plus a `SegDist` helper). **(2) `UIFactory.cs`:**
  added `CreateSafeArea` + a `SafeAreaFitter` MonoBehaviour that continuously insets a full-rect
  child to `Screen.safeArea` (notch/home-indicator aware; only re-applies on change). **(3)
  `SimpleHUDController.Build`:** top HUD now parents to the safe-area inset. Hearts (TL): clean
  filled-red hearts over dim "socket" slots so a lost life still reads as an empty slot, dark
  Outline so they pop; tail pip kept beside them. Top-center: a "DOWNTOWN DASH" title (NYC name;
  falls back to `LevelDefinition.Name` if it's ever non-"Garden Escape"), a rounded dark progress
  track with a green fill (`lizard.z / Level.Length`, Length=140), a **gecko marker** that rides
  the fill (pivot at its feet so it stands on the bar, lifted above the green so it never
  disappears), a **checkered flag** pinned to the goal end, and "LEVEL 1" under it (no level-index
  field exists yet → defaults to 1). Top-right: bug counter reformatted to "n / total" with the
  fly icon, right-aligned. All text gets a subtle dark Outline; bar gets a soft Shadow; no opaque
  panel. `Update()` now also slides the gecko marker with the fill each frame. **Verified
  in-engine** (fresh Play, Start Run + bot auto-run, `ScreenCapture` of the overlay HUD — the low
  POV cam can't RT-capture but the screen-space overlay shows in a full-screen grab): hearts
  decremented live 3→1 via `PlayerHit`, bug counter climbed 0→12/12 via `BugCollected`, the
  progress fill + gecko advanced to 71% from the real lizard z (z=98.8 / 140), checkered flag at
  the goal, "DOWNTOWN DASH"/"LEVEL 1" legible. SafeArea container live (anchors track
  `Screen.safeArea`; full-screen on a notchless display). **0 console errors** (the only red lines
  were my own malformed MCP `Unity_ReadConsole types` calls, not game/compile errors). Frames:
  `Temp/Shots/wo5_before.png` (old thin bar, no title/gecko/flag) vs `wo5_after_3x.png` /
  `wo5_after_final.png` (new hearts+sockets, title, rounded bar with gecko + flag, "LEVEL 1", bug
  counter). **Note for gameplay-guardian:** changes are HUD-render-only (no InputProvider/steer/
  dash/band/cam edits) so control feel is unaffected, but flagging per the loop-guardian rule.
  **Open:** WO-6 art-director re-grade + Sprint 2 scope; on-device frame-time confirm still owed.
- **2026-06-24 — WO-4 (environment-artist), DONE/verified.** Two jobs, both visual-only — no
  gameplay/band/hazard edits. **(A) Road-zone walls + crosswalk.** Diagnosed by tinting the
  NYCity GLB's own materials and surveying: the flat-grey "barrier walls" are the GLB material
  `CityGen_Curb` (concrete curb/barrier, was un-skinned grey) and the "orange crosswalk" is the
  white-stripe geometry `CityGen_lanes_white` sitting on an orange-brown band
  `CityGen_lanes_secondary_color`, with a separate flat ground-decal layer `Street_Assets.001`
  splotching the crossing magenta-pink. All fixed in `CityReskin.Map` (runtime, shader-agnostic,
  glTF `baseColorTexture`/`baseColorFactor`): curb → granite scan; secondary band + the pink
  decal layer → asphalt; stripes → forced clean painted white (added a Tint-only Skin path to
  `CityReskin` for texture-keeping recolors). Verified at runtime — curb tex=granite, band
  tex=asphalt, stripes col≈white; the road zone now reads as stone barriers + bright stripes on
  dark asphalt, no flat grey, no pink. **(B) 8 generated NYC props.** Imported the Meshy GLBs
  (hydrant 15.2k / mailbox 11.2k / newspaper box 9.2k / cone 11.8k / trash bags 11.5k / A-frame
  10.4k / barricade 10.9k / boxes 11.2k tris; each one 2048² baked albedo, glTF shader = not
  magenta) into `Assets/Art/Imported/Generated/`, vetted tris+tex, moved keepers to
  `Resources/Models/Generated/`. New `LevelBuilder.BuildStreetProps`/`PlaceGeneratedProp`: each
  prop re-normalized to a real-world height via combined bounds (hydrant 0.6 / cone 0.7 / bags
  0.7 / mailbox 1.2 / newspaper 1.2 / A-frame 0.9 / barricade 1.0 / boxes 0.8 m) with the
  Z-up→Y-up yaw composed (`AngleAxis(yaw,up)*rot`, never overwritten); GLB's own glTF materials
  kept (no raw Standard). Small SOLID props (hydrant/cone/bags) scattered sparsely IN the band
  (`PropObstacle` on a footprint-centred child + `ObstacleField`, like rubble); larger props are
  pure EDGE dressing along the curb (x≈4.0, colliders stripped + `ObstacleField`) — moved OFF the
  building side after finding the facade swallows ground props there. **Verified in-engine:** 0
  console errors, 0 magenta (1400+ renderers), fresh Play bot run auto-ran +Z (z 2→56) with x
  clamped in-band (8.96–9.00, inBand=True throughout — bot then died to un-dodgeable cross-traffic
  as expected, not to a prop), 11 props placed at correct heights/positions, 9 PropObstacles live.
  Frames: `Temp/Shots/wo4_before_start.png` (grey wall + orange band) vs `wo4_after2_roadzone.png`
  (clean stone/asphalt), `wo4_props_closeup.png` (hydrant at scale by pedestrians),
  `wo4_curbline2.png` (cone+barricade+props lining the curb). **Budget:** +~91k tris total across
  8 unique props (instanced sparsely along the run), 8× 2048² albedos (at the mobile clamp) — only
  the ~11 placed instances ship/draw; props are LOD-free so consider an LOD/cull pass if draw
  count climbs. **Remaining gap (not WO-4):** the asphalt road reads washed-out/bright under the
  current midday exposure — a lighting/grade call for lighting-post-artist, not a surface issue.
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
