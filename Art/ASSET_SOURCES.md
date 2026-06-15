# Asset Sources & Licensing

Every imported (non-procedural) asset, its source, and license. Procedural
fallbacks in `ProceduralTextures`/`MaterialCache` need no attribution.

## Imported art (in `Assets/Resources/GeneratedArt/`)

| File | Used for | Source | License / rights |
|---|---|---|---|
| `logo.png` | Game logo (menu + start screen) | Canva (user account) — `logo_raw.png` exported from Canva, background made transparent via `ProjectSetup.ProcessLogo` (edge flood-fill) | Canva-generated content; usage rights per the owner's Canva subscription terms |
| `menu_bg.png` | Main-menu full-screen background | Canva AI `generate-design` (phone_wallpaper), exported 1080×1920 | Canva-generated; owner's Canva terms |
| `garden_backdrop.png` | In-game far vista behind the safe zone (`LevelBuilder` → `TextureLibrary.Backdrop`) | Canva AI `generate-design` (desktop_wallpaper), exported 1920×1080 | Canva-generated; owner's Canva terms |

> Canva content is generated through the **owner's** Canva account, so usage is
> governed by their Canva plan's content-license terms — not CC0/CC-BY. If this
> project is ever distributed, confirm the Canva license covers commercial game
> use, or replace these with CC0 equivalents.

## Generated textures (procedural, in `Assets/Resources/GeneratedArt/`)

| File | Used for | Source | License |
|---|---|---|---|
| `pavement_stone.png` | Sidewalk ground (`TextureLibrary.Pavement`) | Generated procedurally in Python (seamless toroidal-Voronoi flagstones + tileable sinusoidal grain) — `Art/` pipeline | Original, fully owned (no third-party rights) |
| `terracotta.png` | Flower-pot surface (`TextureLibrary.Terracotta`) | Generated procedurally in Python (tileable clay mottling + throwing lines) | Original, fully owned |

These are seamless by construction (verified with 2×2 tile previews) and a strong
upgrade over the runtime `ProceduralTextures` fallbacks. Canva was **not** usable
for tileable textures — it produces decorative *designs*, not repeating ground.

## What Canva is good / not good for (learned this pass)
- **Great:** illustrated full-scene art — logos, menu/level backgrounds, far
  backdrops, marketing art, character/prop concept frames.
- **Not good:** seamless tileable PBR textures (it produces decorative
  *designs*, not repeating ground textures) and 3D models. A cobblestone attempt
  came out as an abstract wallpaper and was discarded — the **procedural
  `StoneTiles`** (genuinely seamless) remains the ground texture.

## Still procedural (good enough, or awaiting a better free source)
- Ground pavement (`ProceduralTextures.StoneTiles`) — seamless, fine for now.
- Pots/terracotta, leaf decals — procedural; could use CC0 textures later.
- All 3D geometry (lizard, shoes, plants, props) — still procedural primitives.

## Planned free 3D pipeline (not yet done)
Real 3D needs a glTF importer (`com.unity.cloud.gltfast`) + CC0 models from
Quaternius / Kenney (CC0) or Meshy/Tripo free tiers, then cleanup. Tracked in
`Art/HIGGSFIELD_3D_ASSET_PLAN.md`. Prefer CC0; if a CC-BY asset is used, add its
attribution row to the table above.
