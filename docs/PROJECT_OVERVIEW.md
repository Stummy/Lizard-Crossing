# Lizard Crossing — Project Overview (canonical knowledge pack)

> **This is the single self-contained briefing for the project.** Upload *this one
> file* to a Claude.ai web Project's knowledge, and keep it refreshed. It is also a
> clean onboarding doc for Claude Code. It is a snapshot — the live source of truth
> is always the repo (`CLAUDE.md` + `Lizard_Crossing_Claude_Work_Packet/` + `docs/`).
>
> **Last updated:** 2026-06-24 · **Engine:** Unity 6000.4.10f1 · **RP:** URP 17.4.0 ·
> **Target:** iOS/Android, portrait mobile · **Branch:** `feat/realistic-city-crossing`

---

## 1. The pitch

A 3D mobile arcade game. A **tiny (~10 cm, ~0.12 unit) lizard auto-runs forward** down
a **realistic-scale New York City sidewalk**, seen from a **very low speck's-eye POV**.
Everything else is true human/city scale, so the size ratios are real and the world
*towers*:

| Thing | Scale |
|---|---|
| Lizard (hero) | ~0.12–0.18 u |
| Person | ~1.8 u (~10× the lizard) |
| Car | ~4.5 u |
| Building | ~17–28 u (~150× the lizard) |

**1 unit = 1 metre.** The fun is Crossy-Road-style timing: thread perpendicular
cross-traffic to reach the safe zone, while giant feet, cars, and debris sweep across
your path. The owner is a **first-time solo dev, new to Unity/C#** — keep code
readable and commented.

## 2. How it plays

- **Auto-run + steer.** The lizard always scurries forward (+Z) on its own. The player
  only **steers ◀/▶** (and **dashes**) to weave the crowd and thread gaps. There is no
  manual forward/back.
- **Hazards cross sideways (±X)** across the lizard's path like traffic — never parallel
  walkers. Three hazard zone types the forward run alternates through:
  1. **Sidewalk** → normal-scale **pedestrians** walk across; being stepped on or
     walked into costs damage.
  2. **Road** → **cars** drive across as cross-traffic (classic Crossy-Road timing).
  3. **Alleyway** (planned) → narrow gaps between buildings; **dodge falling/scattered
     debris**.
- **3 hearts + a shared "tail → heart" damage pool.** The first hit drops the tail (no
  heart lost; anoles really do this); it regrows after a stretch unhurt, restoring the
  free-hit buffer. **Bugs are the currency.**
- **Reaching the safe zone** at the end of the corridor wins the run.

## 3. NON-NEGOTIABLE RULES (the "sacred mechanics")

Any change is visual/scenery only and must **not** alter this logic:

1. **Auto-run forward; player only steers ◀/▶ + dashes.** (`input.y` is ignored.)
2. **Hazards cross ±X**, never parallel to the run.
3. **The camera is the #1 feature:** very low third-person lizard POV (camera y < 3),
   lizard at **bottom-center**, hazards must feel giant. A first-person "reptile-eye"
   POV toggle rides the snout/eye line looking forward.
4. **Do NOT resize the lizard** — the POV camera math is derived from the lizard's
   measured head metrics; resizing de-calibrates it.
5. **The world is built at runtime from code** (`Bootstrap` → `LevelBuilder`); the Boot
   scene (`Assets/Scenes/Boot.unity`) holds only a `Bootstrap` object.
6. **Never hardcode the "Standard" shader** (magenta trap) — always go through
   `MaterialCache` / the `LitShader` resolver.
7. **Real-world scale:** normalize every new asset to metres.
8. **Build in phases.** No cosmetics / shop / ads / level-select / daily challenges
   until the Phase 1 vertical slice *feels* good.
9. Review changes against the work packet's `QUALITY_BAR.md`; log issues in
   `BUG_AND_GAP_LOG.md`.

## 4. Current status (2026-06-24)

**The realistic-city vertical slice runs end-to-end.** A full auto-run from the start to
the safe-zone threshold (z = 140) works, verified via the in-editor bot harness.

Done & verified:
- **Straight sidewalk corridor** with an analytic ground profile (decoupled from the
  crooked imported city mesh); the lizard's X is clamped to a Z-aware corridor band.
- **Rigged quadruped lizard** with a run-speed-driven walk cycle; third-person + POV
  cameras calibrated.
- **Hazards:** pedestrians cross and now **steer around obstacles** (`ObstacleField`);
  cars cross on road lanes; shoe-bumps cost a heart; the lizard **faceplants** solid
  props (`PropObstacle`); an alley-cat predator rubber-bands from behind.
- **Megascans / Fab art pass (Phases 0–6), DONE + verified, 0 magenta materials:** city
  surfaces reskinned (sidewalk→cobblestone, road→asphalt, facades→worn brick, building
  concrete→granite), concrete-rubble scatter props, edge street furniture (bus stop,
  lamp, phone booth, bench), and alpha-cutout plants. See `05_AssetPlan/ASSET_INTEGRATION_PLAN.md`.

**Next up (the main remaining design gap):** there is no **alley zone** yet. It's the
home for the staged-but-unwired `gravel` / `ground_rubble` ground surfaces and a debris
hazard. It needs **lane TYPES** (sidewalk / road / alley) added to `LevelDefinition` /
`LaneSpec` first, then the alley geometry, then wiring those surfaces + a debris hazard.
After that: polish the loop → mobile build → monetization (rewarded ads + cosmetic IAP,
never pay-to-win) → store readiness. Authoritative list: `docs/ROADMAP.md`.

## 5. Tech facts

- **Unity 6000.4.10f1, URP 17.4.0** (a render-pipeline asset is assigned in
  GraphicsSettings). URP material props are `_BaseMap` / `_BaseColor` / `_BumpMap`. The
  imported NYCity city GLB instead uses the **`Shader Graphs/glTF-pbrMetallicRoughness`**
  shader (props `baseColorTexture` / `baseColorFactor` / `normalTexture`) — reskinned at
  runtime by `CityReskin.cs`, not by dropping files into `TextureLibrary`.
- **Regenerate the Boot scene:** menu **Lizard Crossing → Generate Boot Scene**, or batch
  `-executeMethod LizardCrossing.EditorTools.ProjectSetup.BatchSetup`.
- **Textures:** user-generated/scanned art in `Assets/Resources/GeneratedArt/`;
  procedural fallbacks in `ProceduralTextures` keep the game runnable with zero assets.
  Note: `Resources/` ships *everything* in the build — keep heavy unused assets out.
- **Git:** remote `https://github.com/Stummy/Lizard-Crossing`; LFS enabled for large
  binaries (`.glb/.fbx/.png/.jpg/...`). `main` trails `feat/realistic-city-crossing`.

## 6. Code map (key scripts — current)

`Assets/Scripts/`:
- **Core/** — `Bootstrap` (composition root, builds the world), `GameStateManager`
  (hearts, tail pool, hit/foot-bump/faceplant routing, win/lose), `GameEvents`
  (event bus), `GameConst` (all tuning constants), `TimeEffects`, `MenuBootstrap`.
- **Player/** — `PlayerController` (auto-run, steering, corridor-band clamp, dash,
  stumble/faceplant), `LizardBody` (model + animation poses).
- **GameInput/** — `InputProvider` (steer/dash input; supports a bot override).
- **Hazards/** — `GiantPedestrian` (real-human-scale walker, foot-bump + avoidance),
  `Car`, `StreetTraffic` + `HazardLaneManager` (±X cross-traffic spawners),
  `PropObstacle` (solid prop the lizard faceplants), `ObstacleField` (static registry
  pedestrians steer around), `Predator` (alley cat), `DebrisHazard`, warning markers.
  *(`SidewaysFootHazard` = the retired giant-shoe hero hazard.)*
- **Level/** — `LevelBuilder` (runtime world build: corridor, surfaces, props,
  furniture, plants), `LevelDefinition` (lanes + level length, `Length` = safe-zone z),
  `StreetGround` (analytic height profile), `SafeZoneTrigger`. *(`CityFacade` = older
  kit-assembly path; the current build uses the NYCity GLB backdrop + reskin.)*
- **CameraRig/** — `LizardCameraController` (low TP + POV cam), `CameraShake`,
  `CinematicPost`.
- **FX/** — `CityReskin` (runtime reskin of the city GLB's materials), `MaterialCache`
  (`LitShader` resolver — the anti-magenta path), `ModelLibrary`, `TextureLibrary`,
  `ProceduralTextures`, `GameAudio`, `ParticleFx`.
- **Meta/** — `MetaProgress`, `PlayerProfile`, `LizardSpecies`, `CosmeticItem`,
  `AdService` (stub), `DailyChallenge`. *(Monetization deferred — do not build out yet.)*
- **UI/** — `MenuController`, `SimpleHUDController` (hearts + messaging), `UIFactory`.

## 7. How work happens (Claude Code)

Claude edits scripts and drives the Unity Editor through the **unity-mcp** tools:
`Unity_RunCommand` (editor C#), `Unity_ManageEditor` (Play/Stop/state),
`Unity_ManageMenuItem` (the `Lizard Crossing/Bot/*` playtest harness +
`Assets/Refresh`), and scene/gameplay screenshots. Claude validates *"is it broken"*;
the **owner playtests *"is it fun."*** Hard-won gotchas (URP shader props, the bot
harness, the runtime build, MCP quirks) live in `CLAUDE.md` and the project memory.

## 8. Where the full docs live

| Topic | File |
|---|---|
| Working context & Unity gotchas (Claude Code reads automatically) | `CLAUDE.md` |
| Canonical vision (owner's work packet) | `Lizard_Crossing_Claude_Work_Packet/00_READ_ME_FIRST/CLAUDE_INSTRUCTIONS.md` |
| Quality bar (review every change against this) | `Lizard_Crossing_Claude_Work_Packet/01_GameDesignDocs/QUALITY_BAR.md` |
| Bug & gap log | `Lizard_Crossing_Claude_Work_Packet/04_ReviewChecklists/BUG_AND_GAP_LOG.md` |
| Asset integration plan (Megascans) | `Lizard_Crossing_Claude_Work_Packet/05_AssetPlan/ASSET_INTEGRATION_PLAN.md` |
| Design / decisions / roadmap | `docs/DESIGN.md` · `docs/DECISIONS.md` · `docs/ROADMAP.md` |
| Status & knowledge-base index | `docs/PROJECT_STATUS.md` |

---

### Keeping this file fresh
When a session changes direction: (1) update `CLAUDE.md` / the relevant repo doc, (2)
update §4 (Current status) here, (3) re-upload this one file to the web Project,
replacing the old copy. The non-negotiables (§3) rarely change — paste them once into
the web Project's custom-instructions field.
