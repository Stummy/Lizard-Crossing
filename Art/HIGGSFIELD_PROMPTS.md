# Higgsfield Texture Generation — Prompts & Specs

Generate these images in Higgsfield and save them (PNG) into:

```
Assets/Resources/GeneratedArt/
```

using the **exact file names** below. The game picks them up automatically on
the next Play — no code changes needed. Until a file exists, the game uses a
built-in procedural fallback, so you can add these one at a time and compare.

> Tip: after dropping a file in, check Unity's Inspector for the texture and
> make sure **Alpha Is Transparency** is ticked for the decal images (leaf).

---

## 1. `pavement_stone.png` — the ground (most important)
- **Size:** 1024×1024 · **Must be seamless/tileable**
- The camera is 2 cm off this surface — it carries the whole scale fantasy.

> Seamless tileable texture, top-down view of warm beige stone sidewalk paving
> slabs, large square stones with thin mortar gaps, subtle cracks and small
> pits, scattered fine sand grains, sunny daylight, stylized-realistic mobile
> game art, crisp detail, no shadows of objects, no people, flat lighting,
> perfectly tileable edges.

## 2. `garden_backdrop.png` — far-end vista behind the safe zone
- **Size:** 2048×1024 (2:1 landscape)

> Lush tropical garden wall seen from very low to the ground, giant monstera
> and banana leaves, bright pink and orange hibiscus flowers, warm sunlight
> rays through foliage, soft depth-of-field haze, stylized-realistic mobile
> game art, inviting and safe mood, no characters, bottom third is dark green
> foliage, top is bright hazy sky.

## 3. `leaf_decal.png` — fallen leaf scattered on the pavement
- **Size:** 512×512 · **Transparent background (alpha)** · single leaf, centered

> Single fallen tropical leaf viewed from directly above, slightly curled and
> dry at the edges, green fading to amber, stylized-realistic mobile game art,
> isolated on transparent background, soft ambient occlusion baked into the
> leaf only, no drop shadow.

## 4. `terracotta.png` — flower-pot surface
- **Size:** 1024×1024 · **Seamless/tileable**

> Seamless tileable terracotta clay surface texture, warm orange-brown, subtle
> horizontal throwing lines, light weathering and white mineral specks, sunny
> ambient light, stylized-realistic mobile game art, perfectly tileable edges.

---

## Later (art pass, not needed for Phase 1)
- `sneaker_side.png` / shoe detail sheets — once we swap procedural shoes for
  modeled ones (AI 3D via Meshy/Tripo or asset packs), Higgsfield concept
  frames will art-direct them.
- Level-theme backdrops for Boardwalk Rush, Curb Gauntlet, Midnight Dash,
  Patio Panic (see packet `01_GameDesignDocs/ART_DIRECTION.md`).
