# City Assets Scout — NYC sidewalk + road (CC0)

**Date:** 2026-06-26 · **Scout:** asset-scout (off-engine) · **Branch:** feat/realistic-city-crossing
**Goal:** source license-clean, mobile-budget 3D assets for a dense, realistic NYC street to
replace the procedural primitive box-car (`Assets/Scripts/Hazards/Car.cs`) and dress the corridor.
**Target look:** `Assets/Art/Concept/run_target.png` — warm daylight NYC street, yellow taxi,
sedans/SUVs, sidewalk furniture, clear central lane.

All downloaded assets are **CC0 (public domain)** — free for commercial use, redistributable in a
built app, no attribution required (logged in `ATTRIBUTION.md` for provenance anyway). Nothing
account-gated or paid was downloaded.

---

## TL;DR recommendation

- **Cars/taxi → Kenney "Car Kit" (CC0).** Use `taxi.glb` for the NYC yellow cab + `sedan.glb`,
  `suv.glb`, `suv_luxury.glb`, `van.glb` as generic traffic. ~2,000–2,500 tris each, **1 material,
  1 texture atlas** — ideal mobile. Self-contained GLBs (no external deps). This is the clean
  replacement for the procedural `Car.cs` body.
- **Street furniture → KayKit "City Builder Bits" (CC0).** One cohesive low-poly set sharing a
  single 19.8 KB texture atlas: `firehydrant`, `bench`, `trash_A/B`, `dumpster`, `trafficlight_A/B`,
  `streetlight`, `bush`. 18–636 tris each. Authored tiny (building-block scale) → all need the same
  uniform upscale to metres at placement.
- **Signage/cones → Kenney "City Kit (Roads)" + Car Kit.** `sign_post.glb`, `traffic_cone.glb`,
  `barrier.glb`.
- **Mailbox → not downloaded** (no clean, license-confirmed CC0 US mailbox in the direct-download
  sources). Shortlisted below — recommend the env-artist generate one via Meshy (matches the
  existing WO-4 generated-prop set) or grab a vetted poly.pizza one when convenient.

Everything is staged in the project at **`Assets/Resources/Models/CityKit/`** (total 1.4 MB).

---

## Saved file paths (all CC0, in-project)

### Vehicles — `Assets/Resources/Models/CityKit/Vehicles/`
| File | Native dims (x,y,z) | Tris | Mats/Img | In-game use |
|---|---|---|---|---|
| `taxi.glb` | 1.5 × 1.65 × 2.75 | 2072 | 1 / 1 atlas | **NYC yellow taxi** (hero vehicle, concept frame) |
| `sedan.glb` | 1.5 × 1.45 × 2.55 | 2032 | 1 / 1 | Generic sedan cross-traffic |
| `suv.glb` | 1.5 × 1.4 × 2.55 | 2474 | 1 / 1 | Generic SUV cross-traffic |
| `suv_luxury.glb` | 1.5 × 1.47 × 2.85 | 2086 | 1 / 1 | SUV variant (color/shape variety) |
| `van.glb` | 1.5 × 1.45 × 2.75 | 2082 | 1 / 1 | Van / delivery variety |
| `delivery_truck.glb` | 1.5 × 2.5 × 3.25 | 2476 | 1 / 1 | Box truck (taller, breaks up the skyline of cars) |

> **Scale note:** Kenney cars are ~2.75 u long native. The procedural `Car.cs` uses
> `Length = 4.4`, `Width = 1.9`. Apply a uniform **× ~1.6** at import/placement so the GLB reads as
> a real ~4.4 u car (taxi 2.75 → 4.4). Travel axis = local **+Z** (matches `Car.cs` `_holder` frame,
> "local +Z = travel"); verify orientation on import (glTF is Y-up; Kenney cars face +Z).

### Furniture — `Assets/Resources/Models/CityKit/Furniture/` (KayKit; gltf + .bin + shared `citybits_texture.png`)
| File | Native dims (x,y,z) | Tris | In-game use |
|---|---|---|---|
| `firehydrant.gltf` | 0.14 × 0.22 × 0.13 | 180 | **Fire hydrant** — in-band solid prop (`PropObstacle`/`ObstacleField`), normalize to ~0.6 u |
| `bench.gltf` | 0.40 × 0.10 × 0.15 | 44 | **Bench** — sidewalk edge furniture, normalize to ~0.8 u long |
| `trash_A.gltf` | 0.13 × 0.05 × 0.13 | 18 | **Trash can** (lidded) — in-band/edge prop, ~0.9 u |
| `trash_B.gltf` | 0.07 × 0.04 × 0.07 | 18 | Trash can (smaller variant) |
| `dumpster.gltf` | 0.57 × 0.32 × 0.35 | 126 | NYC dumpster — alley/edge prop, ~1.4 u |
| `trafficlight_A.gltf` | 0.17 × 0.73 × 0.15 | 508 | **Traffic light** (single head) — road-corner landmark, ~4.5 u tall |
| `trafficlight_B.gltf` | 0.28 × 0.96 × 0.15 | 636 | Traffic light (with cross-arm) — bigger intersection variant |
| `streetlight.gltf` | 0.27 × 0.96 × 0.07 | 176 | **Street lamp** — sidewalk edge, ~4.5 u tall (alt to existing `Furniture/streetlamp.prefab`) |
| `bush.gltf` | 0.19 × 0.38 × 0.20 | 72 | Planter greenery — safe-zone/edge decoration |

> KayKit models are authored at "building-block" scale (hydrant 0.22 u tall) — **uniformly upscale
> to metres at placement** per the project's §2.7 scale convention. All 9 share one 19.8 KB atlas
> (`citybits_texture.png`, already co-located in the folder) — extremely cheap, 1 material each.
> The `.gltf` references its `.bin` and the atlas by bare local filename; keep the three together.

### Signage / cones — `Assets/Resources/Models/CityKit/Signage/`
| File | Source | Native dims | Tris | In-game use |
|---|---|---|---|---|
| `sign_post.glb` | Kenney City Roads (`sign-highway-detailed`) | 0.13 × 0.82 × 1.0 | 256 | Street signpost (rescale; sign face is blank — can re-texture to a NYC street sign) |
| `traffic_cone.glb` | Kenney Car Kit (`cone`) | 0.48 × 0.6 × 0.48 | 172 | Traffic cone — in-band obstacle |
| `barrier.glb` | Kenney City Roads (`construction-barrier`) | 0.10 × 0.12 × 0.22 | 60 | Police/construction barrier — edge dressing |

---

## Sources & licenses (per pack)

| Pack | Source URL | License | Formats | Notes |
|---|---|---|---|---|
| **Kenney — Car Kit (3.1)** | https://kenney.nl/assets/car-kit | **CC0** (License.txt verified) | GLB + FBX + OBJ | 45+ vehicles incl. taxi/sedan/suv/van/delivery/cone; self-contained GLBs, 1 atlas each |
| **Kenney — City Kit (Roads)** | https://kenney.nl/assets/city-kit-roads | **CC0** | GLB + FBX + OBJ | Roads, traffic lights (tile-scale, not used — KayKit's are better), signs, barriers, cones |
| **Kenney — City Kit (Commercial)** | https://kenney.nl/assets/city-kit-commercial | **CC0** | GLB + FBX + OBJ | **Buildings only** — downloaded + staged but NOT promoted (NYCity GLB is the backdrop); available if a building kit is ever wanted |
| **KayKit — City Builder Bits (1.0)** | https://github.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0 · https://kaylousberg.com/game-assets/city-builder-bits | **CC0** (LICENSE.txt verified) | glTF + FBX + OBJ | 32+ street props; cohesive single-atlas set; the street-furniture backbone |

Raw download zips are staged (not in Resources) at
`Assets/Art/Imported/_incoming/citykit/` — keep out of the build; can be deleted once promoted
assets are verified in-engine.

---

## Mapping: model → in-game use (the acquisition intent)

| In-game role | Recommended model | Why |
|---|---|---|
| **Yellow taxi** (hero vehicle) | `Vehicles/taxi.glb` | Real NYC cab silhouette + yellow atlas; 2,072 tris |
| **Cross-traffic cars** | `sedan.glb`, `suv.glb`, `suv_luxury.glb`, `van.glb`, `delivery_truck.glb` | Variety of body shapes/heights; all ~2k tris, share the same shader path |
| **Traffic light** | `Furniture/trafficlight_A.glb` (corners), `trafficlight_B` (intersections) | Full standalone street-prop, 508–636 tris |
| **Fire hydrant** | `Furniture/firehydrant.gltf` | Classic red hydrant, 180 tris, in-band solid |
| **Trash can** | `Furniture/trash_A.gltf` (+ `trash_B` variant) | 18 tris; pairs with `ObstacleField` |
| **Dumpster** | `Furniture/dumpster.gltf` | NYC dumpster for alley/edge |
| **Bench** | `Furniture/bench.gltf` | Sidewalk edge furniture |
| **Street lamp** | `Furniture/streetlight.gltf` | Edge landmark (alt to existing procedural `streetlamp.prefab`) |
| **Planter / greenery** | `Furniture/bush.gltf` | Safe-zone + edge dressing |
| **Traffic cone / barrier / signpost** | `Signage/traffic_cone.glb`, `barrier.glb`, `sign_post.glb` | In-band + edge dressing |
| **Mailbox** | — (not sourced; see shortlist) | Generate via Meshy or vet a poly.pizza one |

---

## Shortlist for the owner (NOT downloaded — your call)

### Mailbox (US-style street mailbox) — ALREADY EXISTS, no sourcing needed
A Meshy-generated **`Assets/Resources/Models/Generated/usps_mailbox.glb`** already exists from WO-4
(logged in `ATTRIBUTION.md`, "owned, commercial OK"). Use that — no need to download one. The
`Generated/` folder also has `hydrant.glb`, `aframe_sign.glb`, `police_barricade.glb`,
`newspaper_box.glb`, `cardboard_boxes.glb` that overlap parts of this CC0 set — the env-artist
should pick whichever reads best per slot (the CC0 KayKit/Kenney set is cohesive single-atlas and
lower-tri; the Generated set is more photoreal but heavier — vet tris/texture before choosing).

### Optional upgrades (account-gated / not needed now)
- **Fab / Quixel Megascans** scanned vehicles & detailed street props — higher fidelity but
  account-gated and heavier; the CC0 low-poly set above already fits the "tidy low-poly is fine,
  mobile-budget" brief. Only pursue if the look needs more realism than the cohesive low-poly set.
- **Quaternius "LowPoly Cars" / "Modular Streets"** (CC0) — good alternates; the Kenney/KayKit set
  is already cohesive so not downloaded to avoid style-mixing. Sources:
  https://quaternius.com/packs/cars.html · https://quaternius.com/packs/modularstreets.html

---

## Handoff to `environment-artist`

1. **Import settings:** glTF/GLB importer; for the KayKit `citybits_texture.png` set sRGB (albedo),
   maxTextureSize 2048 (it's only ~256², trivial). Kenney car atlases likewise sRGB albedo, clamp
   2048. No normal maps in these packs (flat-shaded low-poly) — nothing to flag as NormalMap.
2. **Scale to metres at placement** (do NOT trust raw GLB scale): cars ×~1.6 → ~4.4 u long; furniture
   per §2.7 (hydrant ~0.6, trash ~0.9, bench ~0.8, traffic light ~4.5, street lamp ~4.5).
3. **Car wiring:** `Car.cs` builds a procedural box today. Swap the visual: instantiate `taxi.glb`
   (+ variants) under the `_holder` (local +Z = travel), keep the existing oriented-box `KillCheck`
   /near-miss logic and dimensions (`Length 4.4 / Width 1.9`). Route materials through
   `MaterialCache`/`LitShader` — never hardcode "Standard" (magenta trap). Verify orientation: GLB
   forward should align with `_holder`'s +Z.
4. **Furniture wiring:** reuse the existing pipeline — `PropObstacle` + `ObstacleField.Add` for solid
   in-band props (hydrant, trash, cone); place tall items (traffic light, street lamp, bench) at the
   **band edges** so they don't narrow the playable lane or occlude the low POV. Strip colliders on
   pure-dressing, add box colliders + register large ones in `ObstacleField` so peds route around.
5. **Verify in-engine** (your job, not mine): tri/texture in Unity, no magenta, band/POV unchanged,
   bot reaches safe zone, framerate sane. I reported the sources' stated specs + my GLB-header
   probe; confirm in-editor.

**Plan trace:** advances the **World + corridor** Active section + the "cars actually cross"
foundation invariant; moves toward `run_target.png` (yellow taxi + dense NYC furniture). Mechanics
untouched — this is art sourcing only.
