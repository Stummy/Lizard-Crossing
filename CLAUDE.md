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
- **Status (2026-06-24):** straight sidewalk corridor + realistic ratios live;
  pedestrians walk and now **steer around props** (`ObstacleField`); cars cross on
  road lanes; lizard **faceplants** solid props (`PropObstacle`) and shoe-bumps cost
  a heart (shared tail→heart pool). **Megascans/Fab art pass (Phases 0–6) DONE +
  verified** — full bot playthrough reached the safe zone, 0 magenta materials (see below).
  **TODO:** add lane TYPES (sidewalk / road / alley) to `LevelDefinition`/`LaneSpec`,
  build the **alley zone**, then wire the staged `gravel`/`ground_rubble` ground surfaces
  + a falling/scattered-**debris** hazard there (no home until the alley exists).

### Megascans / Fab asset integration (2026-06-23 → 24)
Plan + manifest: `Lizard_Crossing_Claude_Work_Packet/05_AssetPlan/ASSET_INTEGRATION_PLAN.md`.
Done and verified in-engine (0 console errors, mechanics intact):
- **City surfaces are reskinned, NOT a filename drop-in.** The visible sidewalk/road/
  walls are baked onto the imported **NYCity GLB's own materials** (lizard runs on an
  *invisible* collider over them). Those materials use shader
  **`Shader Graphs/glTF-pbrMetallicRoughness`** (props `baseColorTexture` /
  `baseColorFactor` / `normalTexture` — NOT `_BaseMap`/`_MainTex`/`_BumpMap`).
  `Assets/Scripts/FX/CityReskin.cs` (called from `LevelBuilder` nyc branch) maps GLB
  material-name → `Resources/GeneratedArt/` texture; shader-agnostic, runtime-only.
  Extend `CityReskin.Map` to reskin more city materials.
- **Scattered obstacles** are real concrete **rubble** chunks (`Resources/Models/rubble`,
  via `LevelBuilder.BuildRubblePile`), replacing primitive trash cans; keep `PropObstacle`
  + `ObstacleField`.
- **Edge furniture** (bus stop, street lamp, phone booth, bench) in `Resources/Models/Furniture/`,
  placed by `BuildEdgeFurniture`/`PlaceFurniture` at the band edges, colliders STRIPPED.
- **Plants** (Megascans raspberry, alpha-cutout leaves) scattered by `BuildPlants`.
- **Gotchas (see memory `megascans-integration`):** AI "stylized tier" GLBs are poly bombs
  (1.5M tris — rejected); `Unity_ImportExternalModel` Height normalization is unreliable
  (re-normalize at placement) and bakes a Z-up→Y-up rotation (compose yaw, never overwrite
  `.rotation`); clamp surface maps to 2048 + flag normals as NormalMap.

## Workflow rules (owner)
- **Commit AND push after every completed unit of work** (owner rule, 2026-06-25):
  once a change is made and verified in-engine, commit it with a clear message and
  `git push` to the GitHub repo (`origin`, branch `feat/realistic-city-crossing`).
  Keep commits narrowly scoped; never leave verified work sitting only on the local
  machine. Do NOT push to `main` or merge without asking.

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
- Unity **6000.4.10f1**, **URP 17.4.0** (a render-pipeline asset is assigned in
  GraphicsSettings — the art pass is on URP now), portrait mobile. URP material props
  are `_BaseMap`/`_BaseColor`/`_BumpMap`; the imported NYCity GLB uses the glTF Shader
  Graph instead (see Megascans note). Never hardcode the "Standard" shader (magenta
  trap) — go through `MaterialCache`/`LitShader`.
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
