# Art Direction Plan — "Boardwalk Rush" photoreal pass

> How we get Lizard Crossing from procedural-placeholder art to something that
> reads like `05_ReferenceImages/boardwalk_rush_sideways_hazards.png` on a
> mid-range phone. Decided 2026-06-15. Path: **free-first marketplace assets,
> ~$100 reserved for the hero lizard.** Budget: small (<$100 total).

## North star + honest scope

The reference image is essentially **AI key art** — closer to a movie poster
than a real-time render. A solo mobile game at 60fps on a mid-range phone will
**not** match it 1:1. The realistic goal: *reads like the reference at a glance.*

The single most important realization:

> **~60% of the reference's "wow" is lighting, camera, and post-processing —
> not model quality.** Low camera + shallow depth-of-field + motion blur + warm
> color grade. Those are settings we can add in ~a day. The other ~40% is having
> believable assets instead of primitives.

### Why the current models look like playdough (diagnosis, not bad luck)
1. The generation prompt asked for *"cute stylized… stylized realism"* — so it
   produced a toon blob. Realism needs a **photoreal concept image** as input.
2. Single-image→3D AI (Meshy at low polycount) **smooths detail into blobs.**
   Fine for simple props; wrong tool for a hero character or realistic humans.

## Strategy: right source per asset (don't AI-gen everything)

| Asset | Best source | Why |
|---|---|---|
| Environment (planks, palms, ocean, shacks) | **Free marketplace / photoscan** | Photoreal already exists; AI-gen can't beat it |
| Human hazards (legs striding) | **Mixamo** (free, rigged + animated) | Real walk cycles, free, commercial-OK |
| Scooter, sneakers | **Sketchfab / Fab free** | Plenty of good free ones |
| Lighting / sky / reflections | **Poly Haven HDRI (CC0)** | Free, instantly cinematic |
| **Hero lizard** | Free CC lizard → else AI (Rodin/Tripo) → else commission | The one asset worth the <$100 |

---

## Phase 0 — Lock ONE target shot (½ day)
Nail **Boardwalk Rush** as a single vertical slice first. Do NOT spread across
all 5 levels. One level looking photoreal > five looking placeholder.

## Phase 1 — Cinematic rendering (biggest payoff, ~1–2 days)
This transforms the *feel* before we touch a single model.
1. **Migrate Built-in RP → URP.** (Currently Built-in per D1; URP is the
   mobile-realistic target and enables the post stack.) ⚠️ Material reconversion
   needed — set colors via `_BaseColor`, not `material.color` (magenta trap; we
   hit this in the old `number won` project).
2. **Camera:** low + close to the lizard, FOV ~50–60, slight downward tilt.
3. **Post-processing Volume:**
   - **Depth of Field (Bokeh)** ← THE signature reference look (blurred scooter/legs)
   - **Motion Blur** (scooter streak)
   - **Bloom** (soft tropical highlights)
   - **Color Adjustments + Tonemapping (ACES)** → warm sunny grade
   - **Vignette** + **SSAO** (contact shadows, grounds objects)
4. **Lighting:** Poly Haven HDRI sky for ambient + reflections; one directional
   "sun" with soft shadows; bake GI on static geometry.

## Phase 2 — Environment from free photoreal assets
- **Boardwalk planks:** Quixel Megascans wood (free w/ Epic account) or Poly Haven.
- **Palms / tropical plants / hibiscus:** Sketchfab (CC0/CC-BY), Unity Asset Store free nature packs.
- **Ocean / horizon:** simple plane + URP water shader or a free water asset; sky from HDRI.
- **Surf shacks / signage:** Sketchfab/Fab free props; or build simple + photoreal textures.

## Phase 3 — Hazards
- **Humans (real legs striding across):** **Mixamo** — free rigged characters +
  walk/run animations from Adobe. Solves the "real person stride" cleanly.
- **Scooter:** Sketchfab/Fab free (e-scooter / kick-scooter, lots available).
- **Sneakers:** Sketchfab/Fab free, or reuse the Mixamo character's shoes.

## Phase 4 — Hero lizard (the reserved <$100 spend)
Try in this order, stop at the first that looks right:
1. **Free realistic lizard** — Sketchfab, filter Downloadable + license that
   allows **commercial** use (CC0 or CC-BY; avoid NC). Iguana/basilisk/gecko.
2. **AI, done right** — **Rodin (Hyper3D)** or **Tripo** (both beat Meshy for
   realism). Feed a **photoreal** concept image (NOT "cute/stylized"), high
   polycount, PBR on. Clean up in Blender. One paid month fits the budget.
3. **Commission** — Fiverr / CGTrader / ArtStation, ~$50–150 for a custom
   rigged lizard. Highest quality if 1 & 2 fall short.
- **Rig/animate:** Mixamo auto-rigger, or keep the existing hop and add a subtle
  idle/scuttle. A believable hop reads better than a perfect static mesh.

## Phase 5 — Integrate + mobile-optimize
- Import GLB/FBX via `com.unity.cloud.gltfast` (already planned).
- Build **`ModelLibrary`** mirroring `FX/TextureLibrary`: load model from
  Resources/Art if present, else fall back to the current procedural builder.
  Swap call sites in `LizardBody`, `HazardParts.BuildShoe`,
  `LevelBuilder.BuildPottedPlant`/`BuildSafeSign`. Normalize each prefab to a
  target bounds size so gameplay/collision tuning is unchanged.
- **Mobile perf:** LODs, ASTC texture compression, bake lighting, atlas
  materials, keep DoF/motion blur tuned cheap. Profile for 60fps mid-range.

---

## Free tool / asset source cheat-sheet

| Tool | Use | Cost | Notes |
|---|---|---|---|
| **Quixel Megascans** | Photoscanned surfaces, props, plants | Free | Needs Epic account; commercial-OK |
| **Poly Haven** | HDRIs, textures, models | Free CC0 | No attribution needed; huge for lighting |
| **Mixamo** | Rigged + animated humans | Free | Adobe; commercial-OK |
| **Sketchfab** | Everything (lizard, scooter, props) | Free + paid | ⚠️ filter license to commercial-OK |
| **Fab (Epic)** | Unified marketplace | Free + paid | Replaced Unreal Mktpl + Sketchfab Store + Quixel |
| **Unity Asset Store** | Native Unity packs | Free + paid | Best Unity-native import |
| **ambientCG / Poly Pizza** | CC0 textures / low-poly models | Free CC0 | Quick fills |
| **Rodin / Tripo** | AI 3D (realistic-ish) | Paid (~$/mo) | The reserved-budget option for the lizard |
| **Blender** | Cleanup, retopo, export | Free | For fixing AI/marketplace meshes |

## ⚠️ Licensing — required before App Store release
This ships commercially, so **every asset must allow commercial use.**
- **Safest:** CC0 (Poly Haven, ambientCG, many Sketchfab).
- **OK with attribution:** CC-BY → must credit in an in-game/Store credits screen.
- **AVOID:** CC-BY-NC (non-commercial), "editorial only," unclear licenses.
- Megascans + Mixamo: commercial use allowed under their EULAs.
- Keep a running `Art/ASSET_SOURCES.md` with each asset's URL + license + author.

## Honest risks / caveats
- **URP migration churn** — material reconversion, some shader fixups.
- **Mobile perf vs. DoF/motion blur** — these cost; tune or scale by device tier.
- **Asset cohesion** — mixing free sources can look mismatched; unify everything
  with consistent lighting + one color grade (this is what "sells" the realism).
- **AI lizard may still need Blender cleanup** — budget time, not just money.

## Suggested first move
Phase 1 (URP + post + lighting) on the Boardwalk slice. It's the highest
payoff, needs zero new art, and reframes everything that follows. I can start
there on your go.
