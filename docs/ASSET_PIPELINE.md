# Free-Asset Replacement Pipeline

Goal: get the game as close to the target vision as possible using the **best
free** asset per category (CC0 first, CC-BY with attribution), replacing the
procedural placeholder geometry — without blocking on paid generation.

## Categories
1. Hero lizard model
2. Giant sneaker / shoe hazard
3. Scooter / stroller / car wheel hazard
4. Sidewalk / cobblestone / patio / boardwalk environment (meshes or CC0 PBR ground textures)
5. Props — drain, trash bin, patio chair, sign, planter, leaves

## Source priority (per asset)
1. Highest-quality CC0 / CC-BY downloadable model — Poly Pizza, Quaternius, Kenney,
   Sketchfab (download + CC only); CC0 PBR textures — ambientCG, Poly Haven.
2. Free game-asset libraries (Quaternius / Kenney) when an individual model isn't ideal.
3. Free-tier generation (Meshy / Tripo) only if no strong free download exists.
4. Blender cleanup/optimization before Unity import if needed.

## Selection rule
Compare ≥3 candidates per category on silhouette, texture quality, stylized-realism
fit, low-POV readability, premium-mobile suitability, and ease of optimization.
Pick one cohesive art direction (stylized-realistic, colorful, premium mobile) —
do **not** mix wildly inconsistent styles. Record the pick, 1–3 backups, the
rationale, the cleanup needed, and the vision fit. This selection is produced by
the `free-asset-sourcing` workflow and recorded below as it completes.

## Unity import
- GLB/glTF: import via the glTF importer package (confirmed by the pipeline agent),
  which generates a prefab + Built-in RP materials. FBX/OBJ import natively.
- Drop the resulting prefab/GLB into `Assets/Resources/Models/<key>` (keys in
  `ModelLibrary`). `ModelLibrary.TryBuild` normalizes scale to the game's target
  footprint, seats the base on the ground, and strips imported colliders;
  builders add a simple primitive collider.
- Ground textures (env category) go to `Assets/Resources/GeneratedArt/` and are
  applied to the sidewalk material via `TextureLibrary`.

## Optimization
Reduce poly count where heavy, ASTC-compress textures for mobile, use simple
box/capsule colliders, keep one shared Built-in RP "Standard" shader family,
rename files to the key convention, and keep folders tidy.

## Integration is fallback-safe
Every call site keeps its procedural version: if a model isn't present,
`ModelLibrary.TryBuild` returns null and the game builds the primitive instead.
So assets can be added one category at a time and compared in-engine.

## Selected assets (from sourcing workflow, adversarially verified)

| Category | Primary pick | License | Downloadable headless? | Chosen / fallback |
|---|---|---|---|---|
| **Lizard** | Sketchfab "Low Poly Animated & Rigged Gecko" (Msassasa), 9.7k tris, rigged+idle | CC-BY 4.0 | ✗ Sketchfab login | **Use fallback: Poly Pizza "Spotted Gecko" (Jeff Larson) — CC0**, static low-poly |
| **Sneaker** | Sketchfab "Generic Sneakers (5 shoes)", 86.8k tris | CC-BY 4.0 | ✗ .blend/login | **Use fallback: Poly Pizza "Sneakers" / "Trainer" — CC-BY 3.0** |
| **Wheels** | **Kenney Car Kit** (8 wheels) | CC0 | ✓ direct zip | Kenney `kenney_car-kit.zip` |
| **Environment** | **ambientCG PavingStones065** (Prague cobbles, PBR) | CC0 | ✓ direct | 2K JPG maps → ground material |
| **Props** | **Quaternius Downtown City MegaKit** (153 props) | CC0 | ~ itch (JS) | fallback: Kenney City/Furniture kits (direct zip) |

**Notes / gaps:** No free *photoreal* sneaker or iguana exists under a clean,
headlessly-downloadable license — the cohesive choice is **stylized clean-low-poly**
(Poly Pizza / Kenney / Quaternius / ambientCG), a big step up from primitives and
internally consistent. Sketchfab picks are higher-poly but gated behind login, so
the verified CC0/CC-BY backups on Poly Pizza/Kenney are used instead. Backups per
category recorded in the workflow journal.

## Selected assets — acquisition log

| Category | Asset | Status | Notes |
|---|---|---|---|
| Lizard | Poly Pizza "Spotted Gecko" (CC-BY 3.0) | **imported + wired** | `Resources/Models/lizard.glb`, 115 renderers, model-mode bob/sway animation, fallback = procedural gecko. In-game ✓. Cosmetics-on-model TODO. |
| Sneaker | Poly Pizza "Sneakers" (CC-BY 3.0) | **imported + wired** | `Resources/Models/sneaker.glb`, 40 renderers, normalized to sole length, fallback = procedural sneaker. Towers over lizard ✓. Greyscale — could tint/replace with colorful variant. |
| Wheels | Kenney Car Kit (CC0) | downloaded | `wheel.glb` did not load via Resources (glTFast main-asset quirk for this file) — retry with the FBX/OBJ variant. |
| Environment | ambientCG PavingStones065 (CC0) | downloaded | 2K PBR maps staged; next: apply Color+Normal+Roughness to the ground material via `TextureLibrary` + Standard shader normal slot. |
| Props | Quaternius Downtown City / Kenney kits | pending | itch.io needs a non-curl path; Kenney bundles are direct-zip backups. |

Pipeline tech: **glTFast `com.unity.cloud.gltfast@6.4.0`** added to `Packages/manifest.json`
(resolved clean, pulled Burst 1.8.29). GLB drops into `Resources/Models/<key>` import
automatically; `ModelLibrary` normalizes + reseats + strips colliders.

**Known optimization debt (mobile):** imported models bring many renderers
(sneaker 40 × up to 16 feet on screen + lizard 115). Needs a mesh-combine pass in
`ModelLibrary` (merge child meshes per material) before device builds — tracked for
the optimization step.
