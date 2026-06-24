# Lizard Crossing — Visual Target (the north star)

> The shared quality bar every art/polish agent works toward. Pinned from the owner's
> reference render ("Boardwalk Rush", Level 7). **Last updated: 2026-06-24.**
>
> ⚠️ The reference is **concept art / a target mockup** (note the garbled "SURF SHACK
> SHACK" / "SAFE ZONE EEVNE" text and film-grade depth-of-field). It is a *north star*,
> not a literal real-time frame. The real-time mobile result will be a **cohesive,
> polished, stylized approximation** of this look — not photoreal. Judge work by whether
> it moves us toward this feel, not by pixel-matching a render.
>
> 📌 Save the actual reference image into `Lizard_Crossing_Claude_Work_Packet/05_ReferenceImages/`
> and link it here when added.

---

## 1. The feel in one line
A **warm, sunny, cinematic ground-level POV**: a sharp little hero lizard centered low in
frame, with **giant blurred foreground hazards** (a scooter wheel, a pedestrian's legs)
towering on either side, a **clear lane to a SAFE ZONE** vanishing toward the horizon, and
a **clean, friendly arcade HUD**. It should read instantly as "I am tiny in a huge world,
and I know exactly where to run."

## 2. What makes the reference "pop" (priority order = visual impact per effort)
1. **Lighting + exposure + color grade.** Warm midday sun, bright but not blown out, gentle
   global illumination, soft contact shadows under everything. A warm, slightly saturated
   grade (teal sky vs. warm planks/sand). This is the #1 lever.
2. **Depth of field.** The **hero lizard is tack-sharp**; the close foreground hazards
   (scooter, legs) are **strongly blurred**; the far background (palms, sea, safe-zone
   sign) is softly blurred. This single effect creates most of the "cinematic" read.
3. **Cohesive themed set + palette.** Everything belongs to one place (a beach boardwalk):
   planks, palms, surf shack, tiki bar, flowering shrubs, rope-and-post railings. No
   stray mismatched props.
4. **Composition.** Low camera (~lizard eye height), hero at bottom-center, a strong
   central vanishing line (the boardwalk) leading to the goal, framed by tall hazards.
5. **HUD polish.** See §5.
6. **Material + texture quality.** Believable planks, rubber tire, skin, fabric — but
   stylized, readable, not noisy.

## 3. Palette & lighting notes (Boardwalk theme)
- **Sky:** clean cyan→deeper blue gradient, few clouds.
- **Ground:** warm sand/grey stone pavers with subtle grout lines; weathered tan planks.
- **Accents:** hot pink/magenta flowers, painted surf-shack signage (teal, yellow, red,
  purple), turquoise sea band on the horizon.
- **Hero lizard:** vivid green with a yellow-and-black striped tail — keep it the most
  saturated thing in frame so the eye locks on it.
- **Light:** single warm key (sun) from high/behind-side, soft fill, ambient occlusion in
  crevices, soft blurred shadows. Avoid hard black shadows and flat unlit look.

## 4. Composition / camera rules (do not break the gameplay POV)
- Camera low at ~lizard eye height (the project's calibrated low TP / POV cam — **do not
  resize the lizard**, it de-calibrates the cam math; see CLAUDE.md).
- Hero lizard at **bottom-center**, sharp.
- Hazards enter from the **sides (±X)** and read as giant in the blurred foreground.
- A clear, mostly-unobstructed **central running lane** to the safe-zone marker. Never let
  set dressing block the playable lane or the read of oncoming hazards.

## 5. HUD target (from the reference)
- **Top-left:** hearts (3) — clean filled red hearts, current = lives.
- **Top-center:** level title ("BOARDWALK RUSH"), a **rounded progress bar** with a
  **gecko marker** showing position and a **checkered flag** at the goal, and "LEVEL n"
  under it.
- **Top-right:** bug-currency counter with a bug icon ("27 / 50").
- Style: rounded, soft-shadowed, friendly, high-contrast white text with subtle outline;
  unobtrusive, sits over the world without a heavy panel.

## 6. Themes (owner decision 2026-06-24: BOTH, as separate selectable themes)
The game supports **multiple level themes** sharing identical mechanics, swapped in
`LevelBuilder` (theme-swap support to be added). Each theme = a surface set + prop/furniture
kit + hazard skin + palette/grade, all hitting this same quality bar.

| Theme | Status | Ground | Hazards (±X cross-traffic) | Set dressing | Grade |
|---|---|---|---|---|---|
| **NYC sidewalk** | built; **polish to this bar now** | cobblestone / asphalt / crosswalk | pedestrians, cars, (alley) debris | brick facades, lamps, bus stop, bench, rubble, plants | cool-warm city daylight |
| **Boardwalk** (this reference) | **add later** as 2nd theme | planks / sand / stone pavers | scooters, beach-goers' legs, (boardwalk) carts | surf shack, tiki bar, palms, flowers, rope rails | warm sunny beach |

**Order of work:** (1) bring the **lighting/post/DoF/HUD/camera** quality up in the
**existing NYC theme** first — that's the highest-leverage, theme-independent win. (2) Add
**theme-swap** plumbing to `LevelBuilder`. (3) Build the **Boardwalk** kit to match this
render. Mechanics never change between themes.

## 7. Hard constraints (never violate while chasing the look)
- **Mobile budget:** URP, portrait, real device targets. maxTextureSize 2048; watch tris,
  draw calls, overdraw, and post-processing cost (DoF + bloom are not free on mobile —
  tune for a mid-tier phone). Anything in `Resources/` ships in the build.
- **Sacred mechanics are untouchable** (auto-run+steer, ±X hazards, corridor band, POV cam
  math, runtime world build, no hardcoded "Standard" shader). See CLAUDE.md §"Non-negotiable
  rules" and PROJECT_OVERVIEW.md §3. Art/polish is visual-only.
- **Verify every visual change in-engine** with a real gameplay screenshot before claiming
  it works — see the capture workflow in the agent prompts.
