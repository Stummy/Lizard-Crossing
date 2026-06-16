# Lizard Crossing — Claude Context

**Permanent project context lives in `Lizard_Crossing_Claude_Work_Packet/`.
Read `Lizard_Crossing_Claude_Work_Packet/00_READ_ME_FIRST/CLAUDE_INSTRUCTIONS.md`
before doing any work.** Do not ask the owner to re-explain the game idea.

## CURRENT DESIGN DIRECTION (updated 2026-06-16, owner) — REALISTIC-SCALE CITY

The game evolved from "giant shoes in a garden alley" to a **realistic-scale city
crossing**. A *tiny* lizard (a real ~10 cm lizard) crosses a real city assembled
from the Downtown City kit (`Assets/Resources/CityKit/`), seen from its
speck's-eye POV. Everything else is true human/city scale, so the ratios are
REAL: lizard ≈ 0.15 u, person ≈ 1.8 u, car ≈ 4.5 u, building ≈ 17–28 u tall.

- **World scale convention changed:** 1 unit ≈ 1 metre (real world), NOT "1 lizard
  body length." The lizard is scaled DOWN to ~0.12–0.18 u so a person towers ~10×
  and a building ~150× over it. (Supersedes the old "1 u = 1 lizard, world authored
  oversized" convention in docs/DESIGN.md §3.)
- **Three hazard zones the forward (+Z) run alternates through** (hazards still
  cross ±X like traffic):
  1. **Sidewalk** → normal-size **pedestrians** walk across; being stepped on /
     walked into costs a heart. (Reuse `GiantPedestrian`, resized to ~1.8 u real
     human scale — it is no longer a "giant".)
  2. **Road** → **cars** drive across as cross-traffic (classic Crossy-Road
     timing); a car hit costs a heart. (NEW hazard to build.)
  3. **Alleyway** → narrow gaps between buildings; **dodge falling/scattered
     debris**. (NEW hazard to build.)
- City build: `CityFacade.cs` (assembles pre-made kit blocks — buildings both
  sides, kit roads + crosswalks at road lanes, sidewalk stretches between) called
  from `LevelBuilder`. The giant-shoe `SidewaysFootHazard` is retired as the hero
  hazard.
- **Status (2026-06-16):** city blocks placed, pedestrians walking, ambient-walk
  visibility fixed. **TODO:** shrink lizard to realistic ratio · resize
  pedestrians · add car traffic on roads · add debris in alleys · add lane TYPES
  (sidewalk / road / alley) to `LevelDefinition`/`LaneSpec`.

## Non-negotiable rules (from the packet)
- The lizard moves **forward (+Z)** toward the safe zone; primary hazards move
  **sideways (±X)** across its path, like cross-traffic. Never parallel walkers.
- The camera is the most important feature: very low third-person lizard POV
  (camera y < 3), lizard bottom-center, hazards must feel giant.
- Build in phases. No cosmetics/shop/ads/multiple lizards/level select/daily
  challenges until the Phase 1 vertical slice feels good.
- Review every change against `Lizard_Crossing_Claude_Work_Packet/01_GameDesignDocs/QUALITY_BAR.md`
  and update `Lizard_Crossing_Claude_Work_Packet/04_ReviewChecklists/BUG_AND_GAP_LOG.md`.

## Project facts
- Unity **6000.4.10f1**, Built-in RP (URP planned for the art pass), portrait mobile.
- The Boot scene (`Assets/Scenes/Boot.unity`) contains only a `Bootstrap` object;
  the entire world is constructed at runtime from code. Regenerate the scene via
  menu **Lizard Crossing → Generate Boot Scene** or batch
  `-executeMethod LizardCrossing.EditorTools.ProjectSetup.BatchSetup`.
- 3-hearts lives model. Free continuous movement + dash. Bugs are the currency.
- Textures: user-generated Higgsfield art goes in `Assets/Resources/GeneratedArt/`
  (prompts + filenames in `Art/HIGGSFIELD_PROMPTS.md`); procedural fallbacks
  in `ProceduralTextures` keep the game runnable with zero assets.
- Tests: play-mode smoke tests in `Assets/Tests/PlayMode` (adapted from the
  packet); editor validator menu **Lizard Crossing → Validate Phase 1 Scene**
  (run while in Play Mode — the world is runtime-built).
- Decision log: `docs/DECISIONS.md`. Design: `docs/DESIGN.md`. Phases: `docs/ROADMAP.md`.

## Batch verification
```
& "C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe" -batchmode -quit -nographics `
  -projectPath "C:\Users\snpvi\Documents\GitHub\Lizard-Crossing" `
  -executeMethod LizardCrossing.EditorTools.ProjectSetup.BatchSetup -logFile setup.log
```
