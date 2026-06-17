# Lizard Crossing — Project Status & Knowledge-Base Index

> Single up-to-date snapshot of the project. Start here, then dive into the
> linked docs. Last updated: **2026-06-16**.

## What this is

A 3D mobile arcade game: a **tiny (~10 cm) lizard crosses a realistic-scale
city** from a speck's-eye POV, dodging perpendicular cross-traffic — pedestrians
on sidewalks, cars on roads, debris in alleys — to reach safe zones. Crossy-Road
crossing logic, forward-running, cinematic low camera. Built in Unity, shipping
iOS-first, monetized later with rewarded ads + cosmetic IAP (never pay-to-win).

- **Engine:** Unity 6000.4.10f1
- **Target:** iOS / Android, portrait
- **Owner:** first-time solo dev, new to Unity/C# — keep code readable & commented
- **Status:** realistic-scale city crossing slice, in active development

## Repository / GitHub

- **Remote:** https://github.com/Stummy/Lizard-Crossing
- **Branches:** `main` and `feat/realistic-city-crossing` (active, latest work).
  As of this writing the realistic-city work lives on the feature branch; `main`
  trails it. Fast-forward `main` to the feature branch when ready to make it the
  canonical line.
- **Git LFS:** enabled (`.gitattributes`) for large binaries — models
  (`.glb/.fbx`), textures (`.png/.jpg/.exr`), audio, gaussian-splat data
  (`.ply/.bytes`), unitypackages. New large assets route into LFS automatically.
  (One pre-LFS 89 MB city `.glb` remains a normal blob in history — under the
  100 MB limit, left as-is.)

## Knowledge base — where things live

| Topic | File |
|---|---|
| **Canonical vision (owner's work packet)** | `Lizard_Crossing_Claude_Work_Packet/00_READ_ME_FIRST/CLAUDE_INSTRUCTIONS.md` |
| Working context & current design direction | `CLAUDE.md` |
| Game design document | `docs/DESIGN.md` |
| Roadmap & phase plan | `docs/ROADMAP.md` |
| Design/technical decisions log | `docs/DECISIONS.md` |
| Art direction | `docs/ART_DIRECTION_PLAN.md` |
| Asset pipeline | `docs/ASSET_PIPELINE.md` |
| Third-party asset attribution | `ATTRIBUTION.md` |
| Player-facing overview & controls | `README.md` |

## What's built (code map)

`Assets/Scripts/`:
- **Core/** — `Bootstrap` (composition root), `GameStateManager`, `GameEvents`,
  `GameConst`, menu/time helpers.
- **Player/** — `PlayerController` (hopper + feel), `LizardBody`.
- **Hazards/** — `Car`, `GiantPedestrian` (resized to real human scale),
  `StreetTraffic`, `DebrisHazard`, `SidewaysFootHazard`, `HazardLaneManager`,
  warning markers/telegraphs.
- **Level/** — `LevelBuilder`, `LevelDefinition`, `CityFacade` (city from the
  Downtown City kit), `StreetGround`, `SafeZoneTrigger`.
- **CameraRig/** — `LizardCameraController`, `CameraShake`, `CinematicPost`.
- **FX/** — procedural textures/audio, particles, material/model libraries.
- **Meta/** — `MetaProgress`, `PlayerProfile`, species/cosmetics, `AdService`
  stub, daily challenge (meta layer, monetization deferred).
- **UI/** — `MenuController`, `SimpleHUDController`, `UIFactory`.

## Current focus / next steps

See `docs/ROADMAP.md` for the authoritative list. In short: finish the
realistic-scale city crossing slice (lane types, cars + debris, scale/camera
retune), then polish loop → mobile build → monetization → store readiness.

## How work happens

Claude edits scripts and drives the Unity Editor via the unity-mcp tools
(self-test logic suite, scene builder, play-mode audits, scene-view
screenshots). Claude validates "is it broken"; the owner playtests "is it fun."
Keep the self-test suite green after every change. See `CLAUDE.md` for the
hard-won Unity gotchas (URP color handling, scene-save quirks, MCP reconnect).
