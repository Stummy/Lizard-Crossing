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

## 0. Sections & lock status (owner-ratified 2026-06-26) — THE WORKING AGREEMENT
The game is split into **sections**, each owned by ONE agent. We finish a section, the owner
signs off, it **Locks**, and we don't reopen it without a reason worth the cost (see CLAUDE.md
"Section locking & change control"). **One Active section at a time.** This is how we stop
re-breaking finished work.

| Section | Owner agent | Status | Scope |
|---------|-------------|--------|-------|
| **Lizard** | asset-scout | 🔒 Locked | model · rig · run cycle |
| **Controls + camera** | camera-ui-juice | 🔒 Locked | auto-run · steer · dash · low POV |
| **World + corridor** | environment-artist | 🟧 **ACTIVE** | straight walled run · real wall/fence colliders · safe zone |
| **Hazards** | environment-artist | 🟦 Open | crowd ✅ · cars/cross-traffic ✅ (crosswalk crossings + traffic light, 2026-06-26) · alley/debris ⬜ |
| **Lighting + grade** | lighting-post-artist | 🟦 Open | golden-hour · readability (near sign-off) |
| **HUD + juice** | camera-ui-juice | 🟦 Open | hearts · dash btn · shake · hit-stop (near sign-off) |
| **Screens + flow** | camera-ui-juice | 🟦 Open | start / death / win panels |
| **Audio** | asset-scout | ⬜ Backlog | sfx · ambience · music |
| **Meta + ship** | studio-producer | ⬜ Backlog | title · settings · perf · store |

**Lock = frozen:** no edits to a Locked section's files without owner OK. Before any change, name
its section; if it reaches into a Locked one, weigh vs. the concept deck and ask first. After any
change, run the section checks + the Foundation-invariant gate; if a Locked section regressed,
revert. **Current Active focus: World + corridor** — authored straight, walled corridor + the
Foundation-invariant regression validator, finishing the "lizard leaves the sidewalk / through
walls / fences passable / no cars" cluster, before anything else.

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
| **3 — Content completeness** | Alley zone + debris hazard, lane TYPES, tail-drop FX, traffic-light/cross-traffic, audio pass, polished death/win screens | 🟡 PARTIAL — tail-drop FX ✅, **crosswalk traffic + traffic light ✅ (2026-06-26)**; remaining: alley/debris + lane types, audio, death/win screens |
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
| ✅ | Pedestrian readability — kill the blown-out "glowing blob" look (albedo cap + matte + bloom down) | 2 | run | owner + Gemini #1; peds now read as solid figures |
| 🔄 | Pedestrian model fidelity — blocky/low-poly read | 3 | run | **asset-scout diagnosed (`docs/reviews/PED_FIDELITY_SCOUT.md`): not a bad pack — peds were OVERSIZED (2.5u, ~39% over the 1.8u spec). A1 = scale→1.8u ✅ done+committed (squish verified intact). A2/A3 (re-import normals @2048 + smoothness 0.08→0.16) = follow-up IF the Gemini tester says A1 isn't enough.** No better ped pack on disk; hero foreground peds would need Meshy@15-20k + Mixamo re-rig (Option B, deferred) |
| ✅ | Footfall telegraph clarity — widen window (0.22→0.6, whole descent) + floor brightness + bolder ring | 2 | run | Gemini asked 3×; 8/8 cautious-bot wins prove it reads (commit 1c10629) |
| ✅ | Forward-glare washout — lizard auto-ran INTO the low sun (glare wall + ped blobs); raked the sun OFF-axis + spun the HDRI to match + cut bloom hard + re-warmed grade | 2 | run | owner picked off-axis; spawn view now reads peds as PEOPLE, lizard pops, ground textured |
| ⬜ | Collision / hit JUICE — Gemini reads hits as "clips under the foot then OUCH" w/ weak feedback; add recoil + shake + heart-loss anim + SFX | 4 | squished/faceplant | Gemini's new #2; some juice exists, needs punch |
| ✅ | UI pass — premium hearts + steer arrows (Unity AI) | 2/4 | all (HUD) | `ui_heart`/`ui_arrow` wired |
| ✅ | Fences **recolor** — kill the flat-red `Street_Assets` placeholder (→ asphalt via CityReskin) | 2 | run | red (1,0,0) eyesore gone, verified in Play |
| ✅ | Fences **solid + close left gap** — SUPERSEDED by the authored straight corridor (W2 real Box-collider walls right + left curb/fence, W3 peds steer via ObstacleField). Lizard + peds confined; **Invariant Check PASS** (maxX≤11.4, minX≥5.8, straight). | 3 | run | the old crooked-GLB fence problem is gone; corridor is the wall now |
| ✅ | DASH button UI — premium amber disc + glossy rim + MOBA-style dark cooldown sweep + DASH label | 4 | all (HUD) | verified in Play; dropped the baked-"BOOST" sprite (no generation needed) |
| ✅ | DASH button **overlapping the hero** — disc was dead bottom-center, on top of the lizard every frame → re-anchored bottom-right above the right steer arrow (commit 13b39a7) | 4 | run/all (HUD) | observed in the real game-view; HUD corners now clean |
| ⬜ | More HUD via Unity AI — progress bar, bug icon, panels | 4 | all (HUD) | same generate→bg-remove→wire pipeline |
| ✅ | **Concept-aware Gemini tester RUN** (2026-06-26, portrait 720×1280 dodging clip vs `run_target.png`; report `Temp/Recording/run.review.md`). Added `Bot/Set Game View 9:16` menu (`GameViewPortrait.cs`); confirmed `RunRecorder` already drives a dodging bot. | 2 | run | the record→watch→fix loop is LIVE end-to-end |
| ✅ | **Gemini #1 lighting → golden-hour pass DONE** (lighting-post-artist, commit 1adf08a): warm sun + cyan sky + EVEN exposure (clip exposure spread 108→18, killed the start-blows/avenue-dark paradox); hero green pops. Gemini moved lighting #1→#2; INVARIANT PASS, 0 err/magenta, zero new perf cost. | 2 | run | verified on the REAL frames + MP4 (NOT RT cam.Render — it renders brighter, see memory) |
| ✅ | Gemini #2 **peds glowing white → FIXED** by the lighting pass (over-LIGHTING clip, not albedo): ped albedo cap 0.66→0.38 + lower light level. Solid dark figures now (confirmed `real_v7_mid.png`). | 2/3 | run | scout A2/A3 (normals) now optional polish |
| ⬜ | **Greybox buildings** — facades are flat grey untextured blocks vs the concept's detailed NYC; now the biggest remaining VISIBLE gap in the run frames. | 3 | run | → environment-artist (Open); next visible win |
| 🔒 | **Residual warm sun-glare hotspot** — when a ped eclipses the low sun / the run turns sunward, the sun disc glares a beat. Killing it needs auto-exposure OR raking the sun yaw further off-axis = a SHARED lighting + camera/readability call. | 2 | run | sun yaw tuned for forward-glare readability → owner/camera call before changing |
| ℹ️ | **Gemini reviewer unreliable on lighting** — hallucinated "streetlights/night" (none exist); verdict oscillated across 5 passes while measured exposure steadily improved. Weight measured metrics + real frames over its raw lighting label. | — | run | don't over-fit an asset-anchored signal |
| ✅ | **OWNER rejected golden → NEUTRAL daylight + fixed the deep-run DARK-OUT** (2026-06-26, commit 2e05f89). Concept-review pass: art-director flagged the occluded mid/deep run falling to near-night as the #1 gap; lighting-post-artist specified the values. Lifted skybox exposure 0.78→0.95, tint→soft-cyan, ambient 0.90→1.05; grade postExposure 0→0.15, WB −6→−2. Verified on the REAL MP4: whole run reads clean neutral daylight, NOT golden; Gemini's "goes dark/night" complaint GONE. | 2 | run | lighting-post-artist signed off; owner to eyeball |
| ✅ | **Environment "generic grey" → warm stone** (2026-06-26, commit 1ae43fc). Warmed corridor sidewalk strip (restrained, no "yellow"), darkened the pale left curb (was reading as scaffolding), warmed the crossing asphalt. environment-artist + art-director review. | 2/3 | run | quick-win tints, no new assets |
| ✅ | **HUD top-edge cleanup** (2026-06-26, commit 038e6e6). camera-ui-juice review: removed the "DOWNTOWN DASH" banner + the dev POV button; shrank/repositioned/outlined the CLOSE CALL/TAIL DROPPED/OUCH popups so they stop obstructing the lane. | 4 | run (HUD) | bar+flag+LEVEL n+hearts+bug remain |
| ℹ️ | **Concept-review studio pass (owner-requested 2026-06-26)** — Gemini + art-director + environment-artist + lighting-post-artist + camera-ui-juice all reviewed vs the deck. Safe non-locked fixes applied above. **Remaining gaps are OWNER-GATED:** lizard reads as a "frog not gecko" + stiff anim + foot-clip (🔒 Lizard = S4, needs OK); creamy DoF + lower POV (🔒 camera, owner-tuned); HUD art sprites (hearts/bug/gecko-marker) + better yellow-cab + faceplant/car-hit juice (asset-scout + camera-ui-juice). | — | run | full reports captured this session |
| ▶ | **OWNER "why's it zoomed in" = camera BURIES into rubble props.** The low close follow-cam clips INTO the faceted rubble piles (one sits at the spawn) → frame fills with a brown close-up. Mid-run OPEN frames (giant shoe + tiny hero, `look_fresh1.png`) are on-concept. Fix: camera de-clip/occlusion + clear spawn rubble + thin in-band rubble. | 3/4 | run | camera-ui-juice (Controls+camera LOCKED, owner-authorized) + environment-artist; queued after the regrade |
| ⬜ | Gemini: **no SAFE-ZONE gate visible** in the run clip (bot died ~5.5s / gate not reading from the lane) — confirm the amber gate reads from lizard-eye height down the lane. | 2 | win/run | may need a longer/winning run to see it |
| ⬜ | Gemini: **faceplant into a side obstacle reads janky** ("abruptly rotates / gets stuck", not a clear splat) — punch up the faceplant pose + recover. | 4 | faceplant | collision-juice item |
| ⬜ | Gemini: camera a touch **high/distant** + lizard slides off-center on weave; near-miss wants more (speed-lines / recoil / "!"). | 4 | run/nearmiss | **Controls+camera LOCKED** — weigh vs concept + ask owner before touching |
| ✅ | close_calls — near-miss now fires (leg pass-by + car sweep); manual weave = CloseCalls 2 | 2 | nearmiss | AutoPlaytest still shows 0 by design (AI dodges with margin) |
| ⬜ | S2-6 on-device frame-time confirm (gameplay-guardian gate) → **closes Stage 2** | 2/5 | — | needs owner device |
| ⬜ | art-director re-grade vs target sheet → if it reads the target, **M1 hit** | 2 | all | gate |

### Stage 3 — Content completeness (the big build)
> **Approved 5-stage plan (`~/.claude/plans/mighty-prancing-pie.md`, owner-locked 2026-06-26):**
> S1 polish/fixes ✅ · S2 realistic city dressing ✅ · **S3 crosswalk traffic ✅ (this session)** ·
> S4 realistic hero models (lizard/tail/bug/car) ⬜ (locked Lizard — needs owner OK) ·
> **S5 Central Park finale ✅ (this session — real CC0 trees, commit a926434)**.

| ✓ | Item | Stage | Concept target | Notes |
|---|------|-------|----------------|-------|
| ✅ | **Crosswalk traffic system** — cars sweep ±X across the lizard's lane at each ROAD lane (z=40/76/112); a per-crossing **traffic light** cycles cars-go↔safe + GATES the cars (Car goGate) so a recurring telegraphed gap opens; painted asphalt+zebra crosswalk; car hit = a heart (Car.KillCheck→HitPlayer). | 3 | run/nearmiss | owner request #6 (ledger #24). Commits 8db8aa6 (crossings), 4c899dc (traffic light), 2039e12 (recorder reaches crossing). Verified: compile clean · **Invariant PASS** · cross-traffic + red light + heart-loss confirmed on the 14s clip · Gemini gate = no regression. **Owner to judge feel** (auto-run can't stop at red, so it's dodge-the-gap, not stop-and-wait). |
| ⬜ | Cars **distinct hazard beat** polish — side-impact juice ≠ ped stomp; crowd should PART at the crossing (peds wait at the curb) instead of walking through the cross-street | 3 | nearmiss | next Stage-3 polish; crowd-gap not built yet |
| ⬜ | **Traffic system** — (kept for context) randomized cross-traffic waves (people/cars) + traffic light — core ✅ above; remaining: alley/debris lane | 3 | run/nearmiss | owner request #6 (ledger #24) |
| ⬜ | Lane TYPES in LevelDefinition/LaneSpec (sidewalk/road/alley) | 3 | run | prerequisite for alley |
| ⬜ | Alley zone + falling/scattered debris hazard | 3 | faceplant | needs lane types first |
| ⬜ | Cars as a distinct hazard beat (side impact ≠ ped stomp) | 3 | nearmiss | per VISUAL_TARGET_SHEET §4 |
| ✅ | **Central Park finale (Stage 5)** — real CC0 trees/bushes (Kenney Nature Kit) + grass + path replace the primitive flower-arch safe zone; the run bursts into a green park. `PlaceNature()` + `Resources/Models/Nature`. | 5 | win | commit a926434. Invariant PASS, trees render (not magenta), capture-verified vs win concept. Follow-ups: denser/lusher pass; a record-from-goal clip so the Gemini gate can judge it; next-level-starts-in-park hook |
| ⬜ | Polished death/win SCREENS (panel + stats + RETRY / SAFE! banner) | 3/4 | gameover/win | in-engine UI, match concept |

#### Gemini findings — 2026-06-26 crossing clip (logged, routed; not Stage-3 regressions)
| ✓ | Item | Stage | Concept target | Notes |
|---|------|-------|----------------|-------|
| ⬜ | **Lighting reads too COOL/blue** — Gemini (which earlier pushed golden) now calls the current grade cold/flat-blue vs the owner's NEUTRAL daylight. Caveat: Gemini unreliable on lighting; judge on the real MP4. | 2 | run | → **lighting-post-artist + owner judgment**; nudge toward neutral, not cool |
| ⬜ | **Lizard feet clip/sink into the ground** (recurring) — soles penetrate the sidewalk strip; foot plant vs StreetGround.SidewalkY (0.12). | 4 | run | 🔒 **Lizard LOCKED** — needs owner OK; pairs with the Stage-4 model/anim pass |
| ⬜ | **CLOSE CALL! / TAIL DROPPED! text too LARGE** — obstructs upcoming hazards. Shrink + reposition. | 4 | run/nearmiss | → **camera-ui-juice** (HUD/juice) |
| ⬜ | Near-field **ground texture warps/stretches** under the lizard — heavy foreground DoF + low near-res. | 2/3 | run | → lighting-post (DoF) + environment (texture res) |
| ℹ️ | Gemini did **NOT** flag the crosswalk / cross-traffic / traffic light as broken — the new intersection reads correctly. | 3 | run | Stage-3 verification capstone |
| ⬜ | Audio pass (SFX + ambience + music bed) | 3 | — | ElevenLabs/Lyria via Unity AI |
| 🅿️ | Cat / Predator — decide if it stays (never appears today) | 3 | — | owner-deferred |

### Stage 4+ — Feel/UX, hardening, themes, ship (see §2)
Onboarding/tutorial, title/menu/settings/pause, difficulty curve → then mobile perf budget +
on-device, build pipeline, save/progression, store assets → Boardwalk theme → soft launch.

> **Owner playtest feedback (2026-06-25), mapped above:** yellow peds ✅ · UI ✅ · solid
> fences ▶ · traffic system (Stage 3) · cat deferred. Full detail in
> `Lizard_Crossing_Claude_Work_Packet/04_ReviewChecklists/BUG_AND_GAP_LOG.md`.

### Session 2026-06-28 — code-correctness + infra pass (machine-verified)
Pure correctness + tooling work done while the visual run is owner-gated; all gated by the machine checks.
| ✓ | Item | Verify | Commit |
|---|------|--------|--------|
| ✅ | **Controls cleanup** (locked section, owner-OK'd): removed dead `IsStumbling`/`_stumbleUntil`; Stumble/Faceplant → one `BeginStagger`; magic numbers `0.7`/`-4` → `GameConst`; collapsed the no-op corridor ramp → `CorridorLeftX`; consistent `Body` guards. | Invariant PASS · 6/8 bot wins · 0 errors | `9aabcc1` |
| ✅ | **[R33] revive fix machine-gated** — new `Bot/Revive Check` (`ReviveRegressionCheck`): die tail-less → fast-forward past the 14s autotomy delay → revive → assert the tail stays dropped. | Revive Check PASS | `9aabcc1` |
| ✅ | **CI speedup** — `clean: false` on the self-hosted checkout so Unity's `Library/` + LFS bytes persist between runs (kills the 20-40 min cold-import stall). Applies at the next `main` PR. | YAML validated; cloud-engineer to confirm at gate | `672386e` |
| ✅ | **Capture recipe consolidated + all 5 agents pointed at it** → `docs/CAPTURE_RECIPE.md` (single source vs. 5 duplicated copies; bakes in the "RT renders brighter — judge tone on the real MP4" gotcha, now inline in each agent too). | docs | `1c70064` / `a59ac7a` |
| ✅ | **Perf audit** → `docs/reviews/PERF_AUDIT_2026-06-28.md`. Verdict: gameplay C# already lean (0 per-frame allocs, crowd Animators `CullUpdateTransforms`, hazards recycle) — micro-opt = churn. Real budget is GPU/asset + profiler-gated → documented as the S2-6 running order. | static pass (no profiler — MCP down) | `a59ac7a` |
| ⏸️ | **Camera locked dead-centre** (owner D4): `CamMaxLateralLead` 0.13 → 0 so the hero stays pinned bottom-centre (no off-centre weave drift). **APPLIED on disk, HELD uncommitted** pending owner: re-approve Unity MCP → record a clip → owner eyes → commit. | pending (MCP was revoked mid-verify) | — |

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
