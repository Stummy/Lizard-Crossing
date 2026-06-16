# Lizard Crossing — Game Design Document

The owner's work packet (`Lizard_Crossing_Claude_Work_Packet/01_GameDesignDocs/`)
is the canonical vision. This document records how this codebase implements it.

> **REVISED 2026-06-16 — realistic-scale city.** The game now crosses a real
> city at true scale (see CLAUDE.md "CURRENT DESIGN DIRECTION"). The lizard is a
> genuine ~10 cm speck; people, cars and buildings are realistic-sized. Sections
> below marked *(revised)* reflect this; unmarked legacy text (giant shoes, garden
> alley) is superseded.

## 1. Core fantasy *(revised)*

You are a tiny lizard crossing a real, full-scale **city** from a speck's-eye POV
— a cinematic, lizard-POV take on Crossy Road's crossing logic, forward-running
and never open world. The thrill is the **size ratio**: a pedestrian's shoe, a
car tyre, a chunk of falling debris are all life-threatening because you are
1/10th of a person and 1/150th of a building. The run threads three kinds of
danger zone: **sidewalks** (pedestrians), **roads** (cars), **alleys** (debris).

**Pillars (priority order):**
1. **Scale fantasy** — camera, audio, and art constantly sell "I am 10 cm tall."
2. **Sideways danger** — the lizard runs forward (+Z); hazards cross perpendicular (±X) like traffic.
3. **Readable danger** — every footfall telegraphs (dark shadow + pulsing red ring) before it lands.
4. **Game feel** — squash & stretch, hit-stop, camera trauma, near-miss slow-mo, dust.
5. **Short sessions** — 30–90 s levels, instant retry, one-handed portrait play.

## 2. Camera (the most important system)

- Third-person follow, locked low: y ≈ 1.45 (packet tests require y < 3),
  ~3.2 behind, looking slightly up past the lizard toward the safe zone.
- Lizard sits bottom-center; shoes and pant legs exit the top of frame so
  humans stay mythic. Wide aspect-aware FOV keeps cross-traffic readable in portrait.
- **Trauma shake** (CameraShake): trauma², Perlin position + roll noise; stomps
  add trauma scaled by proximity.
- **FOV kick** +8° while dashing; **near-miss** = 0.35 s at 0.45× time + shake +
  "CLOSE CALL!"; **hit/death** = hit-stop + big trauma + squash.
- Warm sun, long soft shadows, light distance fog, garden backdrop.

## 3. World scale convention *(revised 2026-06-16)*

**1 unit ≈ 1 metre (real world).** The lizard is scaled DOWN to a realistic body
length; everything else is true-to-life city scale, so ratios are real.

| Thing | Size (units) | Ratio to lizard |
|---|---|---|
| Lizard (body) | ~0.15 | 1× |
| Pedestrian (person) | ~1.8 | ~12× |
| Car | ~4.5 long | ~30× |
| Sidewalk tile | 3 × 3 | — |
| Road crossing (kit Street_4Lane) | 18 wide × 6 deep | — |
| Building | 12–21 wide, 17–28 tall | ~150× tall |
| City run length (Garden Escape Z) | 205 | — |

Legacy (superseded): 1 u = 1 lizard body, corridor 18, shoe sole 11×4.5, stride
arc 7. The 18-wide play corridor is retained as the crossing width, but the lizard
now occupies a tiny fraction of it.

## 4. In-level loop

1. **Ready**: lizard idles at the start; "TAP TO GO".
2. **Run**: free 2-axis movement up a walled garden alley; stop and wait at lane
   edges, dash through gaps (dash: 14 u/s for 0.25 s, 2 s cooldown).
3. **Dodge**: 8 foot-traffic lanes, alternating left-to-right and right-to-left,
   faster and busier near the end. Each airborne foot grows a WarningMarker at
   its landing spot.
4. **Collect**: 12 bugs — safe-pocket pickups and risky in-lane prizes; small magnet.
5. **Finish**: cross into the garden opening (foliage arch + SAFE ZONE sign +
   glow + fireflies) → stars.

**Hearts:** 3. A stomp costs one heart, knocks the lizard back, grants 1.6 s of
blinking invulnerability. Third hit = squash + death panel + retry.

### Stars
- ★ Finish · ★★ ≥ 75% bugs · ★★★ 100% bugs **and** under par time.

## 5. Hazards *(revised 2026-06-16 — three zone types)*

Each crossing lane has a **type** that determines its hazard. The run alternates
zones for variety and rising pressure:

1. **Sidewalk → pedestrians.** Realistic-scale people walk across the lizard's
   path; being stepped on or walked into costs a heart. Implemented by
   `GiantPedestrian` (procedural Humanoid walk via the avatar bones), now sized
   to ~1.8 u real-human scale — no longer "giant." Each footfall still telegraphs
   a shadow at its landing spot; the kill test trims a forgiveness margin.
2. **Road → cars.** Cars drive across as cross-traffic (classic Crossy-Road
   timing — read the gap, dash across). A car hit costs a heart. *(to build)*
3. **Alleyway → debris.** Narrow building gaps where falling / scattered debris
   (crates, cans, bricks) must be dodged. *(to build)*

Retired: the **SidewaysFootHazard** giant-shoe hero hazard (kept only as the
asset-free fallback in `HazardLaneManager`). Later modifiers: puddle slow zones,
bird shadows, rain/night.

## 6. Progression & meta (future phases; save hooks exist now)

Stars/XP/bug-bank persist via `SaveSystem` (PlayerPrefs JSON, versioned).
Lizard roster with abilities, cosmetics, daily challenges, and ethical
monetization (rewarded revive/double/chest, cosmetic IAP, never pay-to-win)
are specified in the packet and scheduled in `ROADMAP.md` — deliberately not
built until Phase 1 feels good.

## 7. Art & audio direction

Per packet ART_DIRECTION.md: stylized-realistic, colorful, tropical, readable.
Phase 1 achieves it with high-effort procedural geometry + the TextureLibrary
seam: Higgsfield-generated pavement/backdrop/leaf/terracotta textures
(`Art/HIGGSFIELD_PROMPTS.md`) override procedural fallbacks. Danger telegraphs
are always near-black blobs + red rings. Audio: synthesized sub-bass stomps,
whooshes, chirpy pickups (authored audio in the polish phase).

## 8. Mobile performance budget

- Portrait, 60 fps target on mid-range devices (`targetFrameRate = 60`).
- Shared materials via `MaterialCache`; small generated textures; few particle
  systems reused via `Emit()`; no per-frame allocations in gameplay loops.
- Telegraphs are quads, not shadow maps; sun shadows are a quality toggle.
