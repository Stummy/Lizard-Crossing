# Roadmap

## DIRECTION CHANGE — 2026-06-16: realistic-scale city (CURRENT FOCUS)
The hero concept moved from "giant shoes / garden alley" to a **realistic-scale
city crossing** (see CLAUDE.md "CURRENT DESIGN DIRECTION" + DESIGN.md revisions).
A tiny lizard crosses a real city built from the Downtown City kit, with three
hazard-zone types. Work order:
1. [done] Walking pedestrians (`GiantPedestrian`) + ambient-walk visibility fix.
2. [done] City assembled from pre-made kit blocks (`CityFacade` — buildings,
   roads, crosswalks, props) replacing the procedural garden walls.
3. [next] **Realistic scale**: shrink the lizard to its true ratio; resize the
   pedestrians from "giant" to ~1.8 u human scale; retune camera + hop grid.
4. [next] **Lane types** in `LevelDefinition`/`LaneSpec` (sidewalk / road / alley).
5. [next] **Cars** crossing the road lanes; **debris** to dodge in alley lanes.
6. Then resume the polish loop below.

## Phase 1 — Vertical slice (legacy framing)
Garden Escape: one lizard, one stone-alley level, low lizard POV camera, eight
sideways foot-traffic lanes (both directions) with shadow + red-ring warnings,
12 collectible bugs, garden safe-zone finish with arch + sign, 3 hearts,
win/lose + stars, HUD, camera shake, hit-stop, near-miss slow-mo, smoke tests.
**Exit criteria (packet PHASE_1_VERTICAL_SLICE.md):** Play Mode starts without
errors; move/dodge/collect/die/restart/win all work; warnings precede danger;
HUD updates hearts/progress/bugs; screenshot passes QUALITY_BAR.

## Phase 1.5 — Feel & visual polish loop (with owner playtests)
Iterate on the packet's self-review prompt: camera drama, hazard fairness,
near-miss tuning, Higgsfield texture integration, lighting, prop density.
Stay inside Phase 1 scope until it "feels good".

## Phase 2 — Content & systems
- Level system: ~20 `LevelDefinition`s across Garden Escape + Sidewalk Shuffle themes.
- New lanes from the packet LEVEL_DESIGN.md: stroller wheels, scooter wheels,
  puddle slow lanes, bird shadows, falling debris.
- Level select, stars track, XP, second/third lizards with abilities.
- Object pooling pass, device QA at 60 fps on mid-range Android.

## Phase 3 — Look & feel upgrade
- URP migration, post-processing; authored/AI-generated 3D models (Meshy/Tripo
  or asset packs) replacing procedural lizard/shoes/plants; Higgsfield concept
  frames art-direct the pass.
- Authored SFX/music, haptics; cosmetics pipeline + wardrobe UI.
- Themes: Boardwalk Rush, Curb Gauntlet, Patio Panic, Midnight Dash.

## Phase 4 — Meta, monetization, launch
- Rewarded ads (revive / double rewards / bonus chest) behind a service
  interface; cosmetic IAP; no pay-to-win, no forced interstitials.
- Daily challenges, replayable objectives, leaderboards, cloud save, analytics.
- Store assets, soft-launch tuning, release.
