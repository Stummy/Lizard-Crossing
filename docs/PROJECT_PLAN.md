# Lizard Crossing — Project Plan, Stages & ETA

_Owner: Vince • Build lead: Claude (Opus) + 7-agent art studio • Updated 2026-06-25_

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
| **2 — Polish to target** (Sprint 2) | DoF discipline, goal beacon, palette/placeholder cleanup, juice, HUD contrast, perf gate | 🔄 IN PROGRESS (~55%) |
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

## 5. Immediate next (this sprint)
1. **S2-5** — juice (tail-drop, near-miss slow-mo punch, hit-stop) + HUD contrast.
2. **S2-4** — prop/set-dressing density + scale audit (palette already fixed).
3. **S2-6** — on-device frame-time confirm (gameplay-guardian gate) → closes Stage 2.
4. art-director re-grade vs the target sheet → if it reads the target, M1 is hit.

## 6. Risks / unknowns
- **On-device perf** (Stage 5): DoF + Bloom + crowd of skinned pedestrians is the budget
  risk on mid-tier phones; the `SetLite` path exists but needs a real device test.
- **Scope creep**: the sacred-mechanics list is locked; new features wait until v1.0.
- **Art consistency across themes**: theme-swap plumbing must keep gameplay identical.
