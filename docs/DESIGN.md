# Lizard Crossing — Game Design Document

The owner's work packet (`Lizard_Crossing_Claude_Work_Packet/01_GameDesignDocs/`)
is the canonical vision. This document records how this codebase implements it.

## 1. Core fantasy

You are tiny. The world is huge. Each level is a short, dangerous crossing of a
human sidewalk from the eye level of a lizard — a revamped, cinematic, lizard-POV
take on Crossy Road's crossing logic, level-based and never open world.

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

## 3. World scale convention

1 unit ≈ 1 lizard body length (~10 cm). World authored oversized.

| Thing | Size (units) |
|---|---|
| Lizard (body) | 1.0 |
| Corridor (playable width) | 18 |
| Shoe sole | 11 × 4.5 (giant: ~11× lizard) |
| Stride arc height | 7 |
| Garden Escape length | 205 (≈ 45–60 s) |

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

## 5. Hazards

Phase 1 ships the hero hazard; the rest come from the packet's lane list in Phase 2+.
- **SidewaysFootHazard** — a pair of giant shoes striding across the corridor.
  Each footfall: warning grows during flight → slam (dust, thud, trauma by
  proximity) → planted shoe is a solid obstacle → next stride. Fairness: the
  kill test trims a forgiveness margin off the sole edge; resolution happens at
  the landing instant only.
- Later: stroller/scooter wheels, car tires, puddle slow zones, bird shadows,
  chair legs, falling debris, rain/night modifiers.

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
