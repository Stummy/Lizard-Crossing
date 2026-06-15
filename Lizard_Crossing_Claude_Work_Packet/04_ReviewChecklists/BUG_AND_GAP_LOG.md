# BUG AND GAP LOG

Claude should update this after each implementation pass.

## Current build status
2026-06-13 — Phase 1 vertical slice (Garden Escape) compiles clean in Unity
6000.4.10f1; **all 8 play-mode tests PASS** (5 smoke + 2 bot playtests + screenshot
capture). Hazard fairness now verified by automated bots, not just math (below).
Awaiting first human playtest.

2026-06-12 — Initial implementation; 5 smoke tests passing.

## Bugs found
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
