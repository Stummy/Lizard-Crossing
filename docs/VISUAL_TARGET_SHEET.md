# Visual Target Sheet — per-state concept frames

_Updated 2026-06-25._ These are the **target screenshots** we build toward — one per
game state. Generate each in your image tool (the same one used for the Boardwalk
concept) by pasting the **STYLE BLOCK** + the state's prompt. The goal is a consistent
set that matches the concept's *style and quality* (NOT necessarily its beach setting —
ours is a realistic NYC street).

> Companion: [`VISUAL_TARGET.md`](VISUAL_TARGET.md) holds the palette/lighting/DoF/HUD
> spec these prompts encode. Stylized inline mockups (composition/HUD guides) were
> produced alongside this sheet in chat.

> **DESIGN CORRECTIONS (owner review, 2026-06-25)** — the v1 deck looked great in *style/
> quality* but several frames were design-WRONG. Fixes (the spec going forward):
> - **Squished:** the lizard must be COMICALLY PANCAKE-FLATTENED (paper-thin, 2D, tongue out,
>   spiral/X eyes) — v1 just had it lying down. Flatten it.
> - **Faceplant:** the lizard must be SMUSHED *into* a wall/obstacle (face pressed flat on the
>   surface, body vertical against it) — v1 splatted it in open space, which reads as nonsense.
> - **Win / safe zone = CENTRAL PARK.** The safe zone is NYC's big park; winning = escaping the
>   street back to the green of the park (grass, trees, park path, skyline behind) — NOT a
>   generic glowing street arch. This also informs the IN-GAME safe zone art (future work).
> - **Game over:** must read like a real game's GAME-OVER SCREEN (dark panel, "GAME OVER",
>   Distance/Bugs stats, a bright RETRY button) — v1 was just a mood photo. The UI is built
>   in-engine; the concept should show that screen, not only a scene.
> - **Getting hit by a car** should read distinctly from a pedestrian stomp (impact from the
>   side, vehicle-specific) — keep it its own beat.
> Style/quality/graphics were approved as-is — keep them; fix only the design logic.

> **Generated target deck (2026-06-25):** 7 frames rendered via Unity AI (FLUX 2 dev,
> 720×1280) live in `Assets/Art/Concept/` — `run`, `squished`, `faceplant`, `win`,
> `gameover`, `nearmiss`, `title`. These ARE the bar; build the in-engine states to match
> them. Reference art only (outside `Resources/`, won't ship). Regenerate/iterate any
> frame with the matching prompt below via the Unity AI `GenerateImage` tool.

---

## STYLE BLOCK (prepend to every prompt)
```
Stylized-realistic 3D game render, mobile arcade quality, in the style of a polished
Crossy-Road-meets-cinematic concept. Portrait 9:16 framing. VERY LOW ground-level
"speck's-eye" camera (lens ~3 cm off the pavement) looking slightly up a realistic-scale
city street. A tiny vivid emerald-green gecko is the hero, bottom-centre of frame, the
ONLY tack-sharp object. Everything else towers over it at true human/city scale.
Warm golden-hour sunlight, soft cyan sky, gentle film grain, shallow depth of field
(creamy bokeh background, blurred giant foreground hazards), subtle bloom on highlights,
ACES filmic colour, warm-and-cohesive palette (warm stone + asphalt grey + cyan sky +
the green hero pops). Clean readable mobile HUD overlay. High detail, no text artifacts,
no watermark.
```

## Shared HUD overlay (describe consistently)
- **Top-left:** 3 heart icons in sockets (lost hearts read as empty sockets).
- **Top-centre:** level progress bar with a little gecko marker travelling toward a
  checkered finish flag, "LEVEL 1" beneath.
- **Top-right:** bug/currency counter with a bug icon.

---

## 1 — Core gameplay (the hero shot)
```
[STYLE BLOCK]
The emerald gecko mid-stride down a sunlit NYC sidewalk, tail trailing, motion in its
legs. Towering blurred pedestrians (just legs/shoes, giant from this angle) crossing
ahead, a yellow cab blurred on the road to the left, warm stone buildings rising out of
focus, a glowing "SAFE ZONE" gate tiny in the far distance dead-ahead. Full HUD: 3 hearts
top-left, progress bar + gecko marker + checkered flag top-centre, bug counter top-right.
Triumphant, alive, cinematic.
```

## 2 — Squished (stepped on by a pedestrian)
```
[STYLE BLOCK]
The gecko flattened comically under a giant descending sneaker sole that fills the upper
frame, a puff of dust and a few cartoon stars, the gecko's eyes wide, splayed flat on the
pavement (stylized, not gory). A bright red damage vignette pulses around the screen
edges. One heart in the top-left HUD is shattering/emptying (2 left). Impactful, punchy,
readable — clearly "you got stepped on."
```

## 3 — Wall / prop faceplant (hit a solid obstacle)
```
[STYLE BLOCK]
The gecko spread-eagled flat against a solid obstacle it just ran into — a fire hydrant /
concrete rubble chunk / brick wall corner — limbs splayed star-shape, dazed swirl of
stars above its head, a small impact shockwave ring and dust. Slight screen-shake motion
blur. Red damage flash at the screen edges, a heart emptying in the HUD. Comedic but
clearly a painful stop. The obstacle is sharp-ish (close), background creamy bokeh.
```

## 4 — Near-miss (a hazard just barely whiffs)
```
[STYLE BLOCK]
A giant yellow taxi or a huge shoe sweeping ACROSS frame just past the gecko, intense
motion-blur streaks on the hazard, the gecko ducking/recoiling at the very last instant,
a bright speed-line whoosh and a brief blue/white "!" near-miss flash. Time feels slowed.
HUD intact (no heart lost). Heart-pounding, close-call energy.
```

## 5 — Win (reached the SAFE ZONE)
```
[STYLE BLOCK]
The gecko leaping joyfully across a checkered finish line into a glowing green SAFE ZONE
gate — emissive amber posts, bright "SAFE ZONE" sign, leafy arch, drifting fireflies and
golden confetti/sparkles, warm rim-light haloing the hero. The blurred city falls away
behind. A celebratory "SAFE!" banner and the bug-reward count popping up. Euphoric,
rewarding, screensaver-pretty.
```

## 6 — Game over (out of hearts)
```
[STYLE BLOCK]
The gecko down on the pavement, all 3 HUD hearts empty, the world desaturating slightly
and the bokeh deepening, a soft dark vignette closing in. A clean "GAME OVER" card with
"Distance: 84 m / Bugs: 12" stats and a bright "RETRY" button, styled to match the HUD.
Melancholy but inviting-to-retry, not grim.
```

## 7 — Title / main menu
```
[STYLE BLOCK]
Same low NYC street, the gecko posed heroically front-and-centre looking up the avenue at
golden hour, a big stylized "LIZARD CROSSING" logo (chunky, playful, warm) across the
upper third, a glowing "TAP TO PLAY" prompt, small settings cog. Inviting, premium,
app-store-hero-shot quality.
```

---

## Project-stage progression (for the deck, describe — don't need to generate all)
- **Greybox (Stage 0):** flat grey primitives, no grade, HUD boxes — function only.
- **First art pass (Stage 1, ~where we are):** HDRI sky, warm grade, DoF, real props,
  rebuilt HUD — reads as a real street but not yet "concept-tight."
- **Target (Stages 2–4):** the frames above — cohesive palette, tight framing, juicy
  feedback, polished HUD. THIS is the bar.
