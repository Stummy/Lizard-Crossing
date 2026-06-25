# BUG AND GAP LOG

Claude should update this after each implementation pass.

## Current build status
2026-06-13 — Phase 1 vertical slice (Garden Escape) compiles clean in Unity
6000.4.10f1; **all 8 play-mode tests PASS** (5 smoke + 2 bot playtests + screenshot
capture). Hazard fairness now verified by automated bots, not just math (below).
Awaiting first human playtest.

2026-06-12 — Initial implementation; 5 smoke tests passing.

## Bugs found
- PARTIAL (2026-06-25, owner playtest "the red/orange fences"): the red panels flanking
  the run are a GLB material `Street_Assets` baked PURE RED (1,0,0) on two map-spanning
  street-furniture meshes (`Object_15` center≈(28,5,80) size≈228×10×180; `Object_16`
  flat decal). Same un-skinned-placeholder class as the magenta `Street_Assets.001`.
  - **DONE — recolor:** added `Street_Assets`→asphalt to `CityReskin.Map`; verified in
    Play the material now reads baseColor (1,1,1) + asphalt tex (was 1,0,0). Flat-red
    eyesore gone, 0 console errors.
  - **TODO — solid + close the left gap (needs owner's eyes):** owner wants the lizard +
    pedestrians to NOT pass through the fences (go around), and the open section "towards
    the left" closed. This is spatial: the lizard moves by analytic ground + an X-clamp
    (not physics), and peds avoid via `ObstacleField` (not colliders), so a raw MeshCollider
    won't stop either — the panels must be registered as obstacles at their real positions,
    or the corridor band tightened near them. Deferred to co-design with the owner (ties
    into the Stage-3 traffic/crossing flow) — confirm WHICH panels and WHERE the gap is on
    screen before wiring, rather than guessing blind from combined-mesh bounds.
- FIXED + FEATURE (2026-06-21, HIT ESCALATION + "lizard never got hit"): owner —
  driving forward, the lizard ran straight *through* pedestrians and was never hit.
  Root cause: the only pedestrian hazard was the overhead foot-PLANT squish
  (`GiantPedestrian.ResolveStomp`), which almost never lands on a tiny forward-running
  lizard, and pedestrian colliders are stripped in `BuildHuman` so there was no
  body/leg collision at all. Built the owner-requested themed escalation:
  1. **Foot-bump** (`GiantPedestrian.CheckFootBump`, radius `GameConst.FootBumpRadius`):
     running into a leg = a non-damaging stagger via `GameStateManager.FootBump`. 1st
     bump spawns the alley cat (`GameEvents.CatProvoked` → `Bootstrap` one-shot
     `Predator.Spawn`); every bump triggers `PlayerController.Stumble` (knockback +
     momentum loss + `LizardBody.Stumble` roll + camera shake). Player owns a
     `FootBumpCooldown` so one walk-through can't chain-stagger. **No HP/tail cost.**
  2. **Cat** now spawns ONLY when provoked (removed eager `Predator.Spawn` from
     Bootstrap). Scratch routes `HitPlayer(pos, DeathCause.Caught)`; visible lunge +
     claw puff + shake so the off-camera cat reads.
  3. **Shared tail/heart pool** unchanged: stomp / cat scratch / car each spend the
     tail first (autotomy), then hearts, then death. `HitPlayer` took a `DeathCause`
     overload; death panel + popup now name the cause (CAUGHT! / SQUISHED! / STOMPED!).
  4. **Cleanup:** removed the TEMP FPS debug overlay; set `Application.runInBackground`
     true so play mode ticks unfocused (lets the MCP bot harness drive playtests).
  VERIFIED via the bridge bot harness (`Lizard Crossing/Bot/*`): bump→cat spawns with
  Hearts unchanged; cat ladder tail→heart→heart→CAUGHT! death; stomp→tail; tail regrow
  ~14s; compiles clean. GAP: foot-bump rate is RNG on crowd spacing (organic bumps can
  be sparse); GC ~20KB/360 allocs/frame (crowd-dominated) still the flagged perf item.
- FIXED (2026-06-19, RUN ANIM + PLAYABILITY): owner — a "runner looked like he was
  walking", and the lizard movement felt unplayable (judder, not raw FPS).
  1. **Run looked like a walk.** `animator.speed = clamp(clipStep/stepDuration)` came
     out ~0.6–0.76 for runners, so the Run clip played in slow motion while the body
     slid forward — read as gliding walk. Fix: dropped the fragile step-duration
     coupling; gait TIER now sets clip speed directly (walk 0.85–1.1, run 1.0–1.2,
     sprint 1.15–1.35 — i.e. ≥1x so a run reads as a run) and a matching translation
     speed (walk 1.3–2.1, run 3.8–5.0, sprint 5.5–7.5 m/s ×scale) so feet don't skate.
  2. **Lizard judder = crowd too heavy.** Cut the sidewalk crowd 42→~26 (right 4×5,
     left 2×3) for playable framerate; cut per-pedestrian ground raycasts 3→1 (one
     body-centre `HeightAt` per frame shared by grounding + both feet). Density is the
     obvious knob in `StreetTraffic.Build` if the owner's machine can take more.
  NOTE: run cadence verified by construction (clip plays ≥1x in the Run/Sprint state,
  no missing-state errors, 7 Runners spawned); lizard smoothness needs the owner's
  focused playtest (editor FPS isn't measurable over MCP while unfocused).
- FIXED (2026-06-19, GROUNDING v2 + PERF + SPRINTERS): the previous grounding pass
  regressed — pedestrians still hovered AND the lizard movement turned choppy.
  1. **Root cause of both: `updateWhenOffscreen=true` on every sub-mesh.** It forced a
     full per-vertex skinned-bounds recompute on the main thread each frame for ~42
     modular characters (10+ SMRs each) → framerate hitch (the "lizard way off"), and
     the aggregate bounds dipped below the soles on some parts → body lifted (the hover).
  2. **Fix — ground off the ankle bones.** `GiantPedestrian.GroundBody` now pins the
     lower ankle bone's sole (`ankle.y - _ankleHeight`, `_ankleHeight = 0.05·height`) to
     `StreetGround.HeightAt` at that foot's XZ. Two bone reads, no skinned-bounds cost,
     no loose-mesh error. SMR caching/`updateWhenOffscreen` removed entirely.
  3. **More perf:** animator `cullingMode = CullUpdateTransforms` (was AlwaysAnimate) —
     off-screen peds skip rig sampling (they can't squish the lizard anyway).
  4. **Fast runners in the mix (owner ask).** Added a `Sprint` state to
     `Resources/NPC/PedestrianLocomotion.controller` (Kevin `HumanM@Sprint01_Forward`)
     and a sprint tier through `SpawnTrack`/`SpawnSidewalk`. `SidewalkStream` now rolls
     ~12% sprint / ~28% run / rest walk, sprinters at ~2.1 stride/step and a faster clip.
  NOTE: editor FPS can't be measured over MCP (unfocused Game view idle-throttles to
  ~100ms); smoothness confirmed by reasoning + needs the owner's focused playtest.
- FIXED (2026-06-19, GROUNDING + DENSER CROWD): owner feedback on the clip-animated
  crowd — pedestrians floated above the sidewalk/road and popped up/down at curbs, the
  crowd was too sparse, and the pace/direction variety needed restoring.
  1. **Float fixed.** `GiantPedestrian.GroundBody` seated the body by the BIND-pose
     lowest vertex, but the in-place walk/run clips plant the foot at the avatar's
     root/hip plane (~1u above bind feet on these rigs), so the animated feet hovered.
     Fix: cache the `SkinnedMeshRenderer[]` with `updateWhenOffscreen=true` and, each
     frame, drop the body so its lowest LIVE vertex (the planted sole) rests on
     `StreetGround.HeightAt` taken at the planted foot's own XZ. Exact for any clip pose;
     also smooths curb crossings (grounds per planted foot, not a body-center snap).
     Verified planted in side-on play-mode captures.
  2. **Denser crowd.** `StreetTraffic` sidewalk lanes raised 19→~42 (right 3×5→4×8,
     left 1×4→2×5); rolling window unchanged so it stays packed AHEAD the whole run.
  3. **Pace/direction variety restored.** Runner fraction 0.2→0.35 and cadence jitter
     re-widened (walkers 0.8–1.5×, runners 0.45–0.65×) in `SidewalkStream`. Two-way
     facing and the never-from-behind rolling recycle were already in place and kept.
- DONE (2026-06-19, REAL CHARACTERS + CLIP ANIMATION): owner feedback — too many
  pedestrians, moving too fast, and they should use the newly-imported character
  assets with proper walk/run animation.
  1. **Fewer, calmer crowd.** `StreetTraffic.SidewalkStream` lane counts cut (right
     5×12→3×5, left 2×8→1×4 ≈ 76→19 peds), base step durations lengthened, and the
     runner fraction dropped 50%→20% with gentler pace jitter.
  2. **Real characters.** `GiantPedestrian` now instantiates a random casual NPC
     (`npc_casual_set_00`, mixed male+female) duplicated into `Resources/NPC/ped_*`,
     instead of the procedural superhero rig. Imported Standard-shader materials render
     magenta under URP, so `MaterialCache.GetUrpEquivalent` remaps each submesh to
     URP/Lit preserving its own texture (face/shirt/pants/hair).
  3. **Clip-driven gait.** Procedural `Pose`/`Swing` gait retired. Kevin Iglesias
     Humanoid Walk/Run clips drive the rig via `Resources/NPC/PedestrianLocomotion`
     (hand-authored controller — the MCP `controller_add_state` left an empty state
     machine). Walkers play "Walk", runners "Run"; `animator.speed` is matched to each
     ped's pace to curb foot-skating; code still drives forward translation.
  4. **Footfall-synced squish (owner choice).** The kill no longer comes from a
     procedural gait phase; `TrackFoot` watches each live foot bone and resolves the
     stomp the instant a descending foot plants near the pavement, with the telegraph
     ramping as the foot drops. Clip-agnostic, so it stays correct for any animation.
     Grounding simplified to seating the body root on `StreetGround.HeightAt` (in-place
     clips keep the feet on that plane). Verified in play-mode captures (clothed crowd,
     mid-stride, planted, no errors). NOTE: squish timing wants a human playtest.
- FIXED (2026-06-18, SIDEWALK CROWD REWORK): owner feedback — NPCs hovered, walked
  up from behind the lizard (sneak-ups), and the crowd was too sparse.
  1. **Pedestrians still hovered.** The previous live-skinned-bounds grounding was
     read one frame stale and fought the per-frame body bob written in `Pose`, leaving
     a residual hover. Fix: removed the bob from `Pose`; `GroundFeet` now pins the
     lowest FOOT BONE's sole (the gait never rotates feet, so a fixed bone→sole drop
     `_footSoleDrop` measured at build is exact) to `StreetGround.HeightAt` under that
     foot — bone transforms update immediately, so no lag, no hover. Verified planted
     in play-mode capture.
  2. **NPCs streamed up from behind the lizard.** Old `PedLane` laid pedestrians on a
     fixed 0..length track pre-distributed over the whole avenue, so forward (+Z)
     walkers (esp. runners) spawned behind and overtook the player — confusing
     sneak-ups. Fix: new `GiantPedestrian.SpawnSidewalk` rolling mode + `RecycleRolling`
     keep every sidewalk ped in a player-relative window AHEAD of the lizard. Forward
     walkers stroll away (the lizard passes them); oncoming walkers come toward it;
     both recycle to a fresh spot ahead once overtaken or drifted off. Nobody ever
     approaches from behind. (`StreetTraffic.SidewalkStream`, initial spread starts at
     z≥1 so it holds even on frame one.)
  3. **Two-way foot traffic + wider speed spread.** Each lane now alternates oncoming
     and forward facing; runner/walker cadence spread widened (sprinters → strollers).
  4. **Denser crowd.** Per-lane counts raised (right sidewalk 8→12, left 6→8) and the
     ahead-window (5..60u) stays packed the whole run, so weaving to the end is a real
     challenge. All hazards: any pedestrian foot can still cost a heart, forward or
     oncoming (per owner).

- FIXED (2026-06-18, CITY CROSSING): four realistic-city issues in one pass.
  1. **Lizard blocked by a "wall" at curbs.** The NYCity curb riser carries a
     MeshCollider (CityGround layer); the tiny CharacterController (2 cm step) hit
     it as an unclimbable wall while the height-ride tried to lift the lizard over
     it — the two fought. Fix: put the lizard on its own `Lizard` layer and
     `Physics.IgnoreLayerCollision(Lizard, CityGround)` in `PlayerController.Init`,
     so the lizard is never wall-blocked by street geometry and scrambles up/down
     curbs purely via `StreetGround.HeightAt` (climb rate 28 up / 20 down).
  2. **Pedestrians hovered above the pavement.** `GroundFeet` used a once-measured
     rigid foot offset that went stale as the gait flexed knees/tilted feet. Fix:
     ground the body off the live `SkinnedMeshRenderer` bounds each frame (lowest
     point = planted foot) and sample the surface under that foot. Verified planted
     in play-mode capture.
  3. **Pedestrians "walked backwards" (moonwalk).** The sagittal swing axis was
     `Cross(up, walkDir)`, swinging the stride BACKWARD at the same phase the foot
     plants FORWARD. Fix: `Cross(walkDir, up)` so the stride reaches forward and
     matches the plant. Verified facing/striding correct from front + behind.
  4. **No run variety.** Added a per-pedestrian run profile (bigger thigh/knee/arm
     swing, more bob, forward lean, faster cadence) at ~50/50 with walkers, plus a
     wider speed spread, so the sidewalk reads as a real mixed-pace crowd.

- FIXED (2026-06-13, FAIRNESS): one stomp at lane z=92 was **unavoidable even with
  perfect telegraph-reading** — a careful bot still ate it. Root cause: a foot's
  warning lead equalled its stride flight time, so warnings shrank exactly as lanes
  sped up. Fix: a guaranteed minimum telegraph lead (`GameConst.MinWarningLead`,
  0.7 s) via a pre-launch wind-up in `SidewaysFootHazard` — the marker glows in
  place before the foot lifts. CautiousBot now finishes hitless (3/3 hearts).
- FIXED: `CollectibleBug` referenced an undefined `Part` helper (compile error).
- FIXED: `Bootstrap` never called `HazardLaneManager.Build`, so no hazards
  spawned (caught by the smoke tests).

## Gaps found
- Higgsfield textures not yet generated/dropped in `Assets/Resources/GeneratedArt/`
  — game currently uses procedural fallback textures (prompts ready in
  `Art/HIGGSFIELD_PROMPTS.md`).
- No HUMAN playtest yet. Hazard fairness is now validated by automated bots
  (`Assets/Tests/PlayMode/AutoPlaytest.cs`): a CautiousBot that reads telegraphs
  must finish (fairness floor) and a NaiveBot that sprints blind must be
  endangered (danger floor). Latest run — Cautious: Won 3/3 hearts, 0 hits;
  Naive: Won 2/3 hearts, 1 hit in the back-half gauntlet. Still needs a human
  feel pass (knockback, magnet radius, dash timing).
- Lizard/shoe/plant geometry is procedural primitives; reads stylized but below
  the ART_DIRECTION "premium" bar until the art pass (planned: AI 3D models or
  asset packs + Higgsfield concept direction).

## Screenshot self-review (SELF_TESTING_PLAN §5) — done 2026-06-12
Headless capture harness added (`Assets/Tests/PlayMode/ScreenshotCapture.cs`,
saves `shot_1..4.png` to project root). Issues found AND fixed from review:
- Idle pedestrians' pant legs parked off-corridor read as static gray towers →
  feet now hidden while idle.
- HUD hearts rendered as blobs (implicit curve failed) → rebuilt from
  circles + diamond, reads correctly.
- SAFE ZONE sign floated out of frame, then (after first fix) its world-space
  text inherited the scaled board's transform and became a giant wall →
  re-parented canvas, resized, mounted at arch crown; reads cleanly.
- Backdrop treeline was blocky → smoothed dual-canopy gradient at 512px.
- Lizard read blobby/too large → slimmer torso/legs/tail, camera back 3.2→4.0.
- Bug counter icon was a plain circle → tiny fly icon (body + wings).
Money shot confirmed: shot_3 shows a colossal boot sole towering directly over
the tiny lizard — the core fantasy reads in one frame.

## Visual weaknesses
- Through-the-arch view is a flat dark-green backdrop wall; the Higgsfield
  `garden_backdrop.png` will replace exactly this (prompt ready).
- Pavement procedural texture is clean but flat; `pavement_stone.png` upgrade
  pending from owner.

## Gameplay weaknesses
- Bot-verified fair + threatening, but tuned by a bot, not a human. Watch in the
  human pass: knockback strength on hit, magnet radius, dash cooldown feel, and
  whether the back-half density (z=92→185) reads as exciting vs. stressful.

## Next recommended fix
- Owner playtest in the editor (Boot scene → Play) to sign off feel, THEN the
  premium art/look pass: drop Higgsfield textures into
  `Assets/Resources/GeneratedArt/` and begin swapping procedural primitives for
  real 3D models (lizard, shoes, plants) per ART_DIRECTION.md.

## Code review 2026-06-25 (local max-effort) — open findings
Fixed in commit a16dea4: #1 stomp dead-zone (x-gate recentered), #2 cat i-frame
bypass, #4 cat multi-material remap, #10 FootBump doc. Remaining, tracked:
- **#3 Stomp detection is framerate-dependent** (`GiantPedestrian.TrackFoot`): the
  descending→plant edge (`-1e-4` threshold + strikeLift window) can be skipped on a
  frame hitch → a foot that visually lands resolves no kill. FIX DURING S2-6 device
  pass (needs a real low-FPS repro to tune safely — don't fix blind in-editor).
- **#5** Static `_prefabs`/`_controller` caches never reset on rebuild → stale refs
  after a menu→run cycle. Add a ClearRuntimeCache-style reset.
- **#6** `Physics.IgnoreLayerCollision(Lizard,CityGround)` set globally, never restored.
- **#7** `ObstacleField` never removes destroyed props (phantom obstacles).
- **#8** `ObstacleField.Avoidance` is O(peds×obstacles)/frame — perf tax (mobile).
- **#9** Left-sidewalk lanes x=-8.3 (on curb ramp) vs -8.8 ride at different heights.
- **#11** `PropObstacle.Update` per-prop per-frame cost + desync risk if attached
  without `ObstacleField.Add`.
- **#13** Altitude: FootBump/PropBump/HitPlayer are near-duplicate entry points —
  consolidate into one HitPlayer overload taking the FX/provoke behavior.
- **#14** Dead seams: `GiantPedestrian.Spawn`+`HazardLaneManager` (retired non-NYC
  path), `StreetGround.Configure` no-op.
- **#15** Chameleon camouflage unreachable under auto-run (`wish.sqrMagnitude<0.02`
  never true). Decide if camouflage ships; if so, gate on steer-only stillness.

## Owner playtest feedback 2026-06-25 (first real play)
1. **Pedestrians look flat/yellow + blocky.** The warm sun/grade tints people yellow,
   they read low-quality, and they're hard to see. → diagnose source (global warm grade
   vs ped material remap vs low-poly model) and improve ped readability/quality.
2. **Overall quality could be higher** — fun-first agreed, but keep pushing the look.
3. **No cat appears at all.** Predator never shows (not provoking, or spawns behind the
   cam, or model missing). Owner OK deferring the cat — decide later if it stays.
4. **UI needs work.** Owner wants a nicer UI — generate via Unity AI (game-ui-elements-flux
   / game-ui-essentials-2) and/or source a free kit (Kenney etc., asset-scout). YES, Unity
   AI can generate UI sprites.
5. **Orange fences/barricades are pass-through.** Lizard AND people can walk through them
   (and through an open gap on the left). Owner wants them SOLID → go around. Make the
   barricades real colliders + close the left gap.
6. **No cars cross the crosswalk / wants a cross-traffic TRAFFIC SYSTEM** (task #24):
   randomized waves of people OR cars crossing (both present), with a traffic light. Concern:
   at the fenced area, people waiting at a light could pile up in a small space — must ensure
   the lizard can fit (squeeze between people / around). Design together + playtest.
