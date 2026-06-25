# Lizard Crossing — Project Plan, Stages & ETA

_Owner: Vince • Build lead: Claude (Opus) + 7-agent art studio • Updated 2026-06-25_

> **THIS IS THE PLAN WE ALWAYS WORK TOWARD.** Per the CLAUDE.md North star, every task must
> trace to a stage + a ledger item here AND move the live game toward the concept deck
> (`Assets/Art/Concept/`). If a change advances neither, it's scope creep. Keep §5's ledger
> live: tick items off when verified+committed, and log new owner feedback into it immediately.

This is the living delivery plan. Companion docs: [`PROJECT_OVERVIEW.md`](PROJECT_OVERVIEW.md)
(what the game is), [`VISUAL_TARGET.md`](VISUAL_TARGET.md) (the look we're chasing),
[`VISUAL_TARGET_SHEET.md`](VISUAL_TARGET_SHEET.md) (per-state concept targets),
[`STUDIO_BOARD.md`](STUDIO_BOARD.md) (sprint board).

## 1. What "done" means
A polished, shippable portrait-mobile arcade runner: a tiny lizard auto-runs a
realistic-scale NYC street from a speck's-eye POV, dodging giant pedestrians, cars,
and debris, racing to the SAFE ZONE — at the visual quality of the Boardwalk concept
render. **v1.0 = the NYC theme shipped to a store.** Boardwalk is a fast-follow theme.

## 2. Stages (current status)

| Stage | Scope | Status |
|-------|-------|--------|
| **0 — Foundations & mechanics** | Auto-run corridor, steer/dash, 3-hearts + tail-autotomy, faceplant, pedestrians/cars/bugs, win/lose | ✅ DONE |
| **1 — First cinematic art pass** (Sprint 1) | HDRI sky, ACES grade, Bokeh DoF, hero framing fix, road reskin, Megascans props, HUD rebuild | ✅ DONE (~78% to target) |
| **2 — Polish to target** (Sprint 2) | DoF discipline, goal beacon, palette/placeholder cleanup, juice, HUD contrast, perf gate | 🔄 IN PROGRESS (~70%) — remaining: solid fences, DASH UI, near-miss check, perf gate |
| **3 — Content completeness** | Alley zone + debris hazard, lane TYPES, tail-drop FX, traffic-light/cross-traffic, audio pass, polished death/win screens | ⬜ NOT STARTED |
| **4 — Game-feel & UX** | Difficulty curve, onboarding/tutorial, title/menu/settings/pause, near-miss & combo juice | 🟡 PARTIAL (some juice exists) |
| **5 — Production hardening** | Mobile perf budget + on-device test, Android/iOS build pipeline, save/progression, store assets | ⬜ NOT STARTED |
| **6 — Boardwalk theme** | Theme-swap plumbing, beach surfaces/props/palette/HDRI (the concept setting) | ⬜ NOT STARTED |
| **7 — Soft launch / release** | Beta, store listing, launch | ⬜ NOT STARTED |

## 3. Delegated tasks (by agent)
The main session (Claude) orchestrates, does the in-engine work, and verifies every
result. Agents are dispatched for the work they're uniquely good at (the live Unity
editor is single-threaded, so in-engine tuning stays inline; agents own off-engine and
fresh-context work).

- **studio-producer** — owns the sprint board, sequences work-orders, reviews each stage gate.
- **art-director** — grades captured frames vs the target sheet, issues punch-lists.
- **lighting-post-artist** — lighting/post/DoF (Stage 2 + theme passes).
- **environment-artist** — surfaces, props, set-dressing, alley zone, theme-swap plumbing (Stages 2,3,6).
- **camera-ui-juice** — camera feel, HUD, juice, menus/screens (Stages 2,3,4).
- **gameplay-guardian** — regression + perf **gate** before any stage is called done (Stages 2,5).
- **asset-scout** — sources/generates assets (Meshy + CC0) feeding every stage.

## 4. Milestones & ETA
ETA is **effort-based** (focused work-days) plus a calendar estimate at **~12 hrs/week
part-time** (≈1.5 days/week). At full-time, divide calendar by ~5. Adjust to your hours.

| Milestone | Stages | Effort (focused days) | Part-time calendar |
|-----------|--------|----------------------|--------------------|
| **M1 — Hits the target look** | finish 2 | 3–5 | ~2–3 wks |
| **M2 — Content complete** | 3 | 8–12 | ~5–8 wks |
| **M3 — Feel & UX solid** | 4 | 6–10 | ~4–6 wks |
| **M4 — Ship-ready (NYC v1.0)** | 5 | 8–14 | ~5–9 wks |
| **M5 — Boardwalk theme** | 6 | 6–10 | ~4–6 wks |
| **M6 — Released** | 7 | variable | +2–4 wks |

**Headline ETA (part-time, ~12 hrs/wk):**
- 🎯 **Target look locked (M1):** ~late July 2026
- 🎮 **Polished, demo-able vertical slice (M1–M3):** ~Sept–Oct 2026
- 🚀 **NYC v1.0 ship-ready (M4):** ~Nov–Dec 2026
- 🏖️ **Boardwalk theme added (M5):** ~Jan 2026 +
- _Full-time pace compresses all of the above to roughly **8–12 weeks** total to M4._

> These are honest ranges, not promises — game polish and on-device testing always
> surface surprises. The plan re-baselines at each milestone gate.

## 5. Live task ledger (the working backlog)
The ordered, concrete to-do list — the thing we actually pull from. Tick items off as they're
verified+committed; add owner feedback here the moment it lands. Each item names the stage it
advances and the concept frame it moves toward. **`▶` = next up.**

### Stage 2 — Polish to target (finish this first)
| ✓ | Item | Stage | Concept target | Notes |
|---|------|-------|----------------|-------|
| ✅ | S2-1 DoF discipline + tame vignette | 2 | run | done |
| ✅ | S2-2 visible safe-zone goal beacon | 2 | win | x=0→x=9 fix |
| ✅ | S2-3/4 palette + placeholder + prop audit | 2 | run | done |
| ✅ | S2-5 juice (tail-drop, near-miss, hit-stop) + damage flash | 2 | squished/faceplant/nearmiss | done |
| ✅ | Sun-washout + hero-prominence + contact shadow | 2 | run | done |
| ✅ | Yellow-pedestrians grade fix | 2 | run | warmth pulled back |
| ✅ | UI pass — premium hearts + steer arrows (Unity AI) | 2/4 | all (HUD) | `ui_heart`/`ui_arrow` wired |
| ✅ | Fences **recolor** — kill the flat-red `Street_Assets` placeholder (→ asphalt via CityReskin) | 2 | run | red (1,0,0) eyesore gone, verified in Play |
| ▶ | Fences **solid + close left gap** — register the panels as obstacles (lizard X-clamp / ObstacleField) so lizard + peds go around; close the open left section | 3 | run | spatial — co-design w/ owner on screen (ties into traffic/crossing flow) |
| ✅ | DASH button UI — premium amber disc + glossy rim + MOBA-style dark cooldown sweep + DASH label | 4 | all (HUD) | verified in Play; dropped the baked-"BOOST" sprite (no generation needed) |
| ⬜ | More HUD via Unity AI — progress bar, bug icon, panels | 4 | all (HUD) | same generate→bg-remove→wire pipeline |
| ✅ | close_calls — near-miss now fires (leg pass-by + car sweep); manual weave = CloseCalls 2 | 2 | nearmiss | AutoPlaytest still shows 0 by design (AI dodges with margin) |
| ⬜ | S2-6 on-device frame-time confirm (gameplay-guardian gate) → **closes Stage 2** | 2/5 | — | needs owner device |
| ⬜ | art-director re-grade vs target sheet → if it reads the target, **M1 hit** | 2 | all | gate |

### Stage 3 — Content completeness (the big build)
| ✓ | Item | Stage | Concept target | Notes |
|---|------|-------|----------------|-------|
| ⬜ | **Traffic system** — randomized cross-traffic waves (people/cars) + traffic light; solve pile-up at fences so lizard can squeeze through | 3 | run/nearmiss | owner request #6 (ledger #24) |
| ⬜ | Lane TYPES in LevelDefinition/LaneSpec (sidewalk/road/alley) | 3 | run | prerequisite for alley |
| ⬜ | Alley zone + falling/scattered debris hazard | 3 | faceplant | needs lane types first |
| ⬜ | Cars as a distinct hazard beat (side impact ≠ ped stomp) | 3 | nearmiss | per VISUAL_TARGET_SHEET §4 |
| ⬜ | Polished death/win SCREENS (panel + stats + RETRY / SAFE! banner) | 3/4 | gameover/win | in-engine UI, match concept |
| ⬜ | Audio pass (SFX + ambience + music bed) | 3 | — | ElevenLabs/Lyria via Unity AI |
| 🅿️ | Cat / Predator — decide if it stays (never appears today) | 3 | — | owner-deferred |

### Stage 4+ — Feel/UX, hardening, themes, ship (see §2)
Onboarding/tutorial, title/menu/settings/pause, difficulty curve → then mobile perf budget +
on-device, build pipeline, save/progression, store assets → Boardwalk theme → soft launch.

> **Owner playtest feedback (2026-06-25), mapped above:** yellow peds ✅ · UI ✅ · solid
> fences ▶ · traffic system (Stage 3) · cat deferred. Full detail in
> `Lizard_Crossing_Claude_Work_Packet/04_ReviewChecklists/BUG_AND_GAP_LOG.md`.

## 6. Risks / unknowns
- **On-device perf** (Stage 5): DoF + Bloom + crowd of skinned pedestrians is the budget
  risk on mid-tier phones; the `SetLite` path exists but needs a real device test.
- **Art/build weight** (Stage 5): `Assets/Resources/` is ~361 MB and EVERYTHING there ships
  in the build; plus ~1.3 GB of untracked third-party asset-store packs (npc_casual_set_00,
  Kevin Iglesias, Mirza, 404-gen, etc.) that are re-downloadable and deliberately not in git.
  Needs a deliberate curation pass: move unused/heavy assets out of `Resources/`, decide LFS
  vs re-import for third-party, and a `.gitignore` for the re-downloadable packs. The
  AI-generated irreplaceable assets (lizard, rubble, Meshy props, GeneratedArt, FLUX concepts)
  are already committed.
- **Scope creep**: the sacred-mechanics list is locked; new features wait until v1.0.
- **Art consistency across themes**: theme-swap plumbing must keep gameplay identical.
