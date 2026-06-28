# Lizard Crossing — Claude Context

**Permanent project context lives in `Lizard_Crossing_Claude_Work_Packet/`.
Read `Lizard_Crossing_Claude_Work_Packet/00_READ_ME_FIRST/CLAUDE_INSTRUCTIONS.md`
before doing any work.** Do not ask the owner to re-explain the game idea.

**How the agents cooperate + the per-change QA loop is `CO-OP.md` (owner rule, 2026-06-27) — run
that loop (specialist gate → Gemini gate → machine gate → commit) on every visible change.**

**Cloud / CI-CD / builds / backend / analytics / release decisions: read `cloud.md` (owner rule,
2026-06-27) and route them past the `cloud-engineer` agent — lean, free-tier-first, solo-dev.**

## ⭐ SESSION BOOT PROTOCOL (owner rule, 2026-06-26) — RUN THIS EVERY TIME I'm told to work
Don't wing it. Before touching code each session, ORIENT against the three sources of truth —
the GOAL, the CONCEPT, and the TESTER — then work the loop. Be the agent that learns as we go:
update this file + memory whenever we learn a rule, fix, or gotcha.
1. **GOAL** — read `docs/PROJECT_PLAN.md` §0 (section board + the ONE **Active** section) and §5
   ledger, this file's rules, and `MEMORY.md`. Work ONLY the Active section; never edit a **Locked**
   one without owner OK (see "Section locking & change control").
2. **CONCEPT** — open the concept frame(s) for the state(s) I'm about to touch in
   `Assets/Art/Concept/` (`run`/`win`/`gameover`/`nearmiss`/`squished`/`faceplant`/`title`) +
   `docs/VISUAL_TARGET_SHEET.md`. That deck is the spec — every change must move the live game
   toward it; if it moves toward neither the plan nor the concept, it's scope creep.
3. **TESTER ("the video guy")** — the Gemini reviewer is our QA game tester, kept IN SYNC: it is
   fed the concept frames + spec and reports BUGS + CONCEPT-GAP + a punch-list. Read the latest
   `Temp/Recording/run.review.md` if fresh; otherwise, when the build runs, record a clip
   (menu **Lizard Crossing → Bot → Record MP4 (10s)**, Game view set to 9:16 portrait) and run
   `python Tools/gemini_review.py` (use `--state <name>` to compare a specific concept frame).
   **Log its findings into the §5 ledger** so nothing is lost.
4. **WORK THE LOOP** — pull the top ledger item for the Active section → change → `verify-and-ship`
   (compile clean → bot playthrough reaches the safe zone → **Bot → Invariant Check** PASS → 0
   console errors → proof frame) → commit+push → tick the ledger → **re-run the tester to confirm
   the gap closed** → checkpoint the owner with before/after vs the concept frame. Remind the owner
   to run `/code-review ultra` at each section/stage gate.

## CURRENT DESIGN DIRECTION (updated 2026-06-16, owner) — REALISTIC-SCALE CITY

The game evolved from "giant shoes in a garden alley" to a **realistic-scale city
crossing**. A *tiny* lizard (a real ~10 cm lizard) crosses a real city assembled
from the Downtown City kit (`Assets/Resources/CityKit/`), seen from its
speck's-eye POV. Everything else is true human/city scale, so the ratios are
REAL: lizard ≈ 0.15 u, person ≈ 1.8 u, car ≈ 4.5 u, building ≈ 17–28 u tall.

- **World scale convention changed:** 1 unit ≈ 1 metre (real world), NOT "1 lizard
  body length." The lizard is scaled DOWN to ~0.12–0.18 u so a person towers ~10×
  and a building ~150× over it. (Supersedes the old "1 u = 1 lizard, world authored
  oversized" convention in docs/DESIGN.md §3.)
- **Three hazard zones the forward (+Z) run alternates through** (hazards still
  cross ±X like traffic):
  1. **Sidewalk** → normal-size **pedestrians** walk across; being stepped on /
     walked into costs a heart. (Reuse `GiantPedestrian`, resized to ~1.8 u real
     human scale — it is no longer a "giant".)
  2. **Road** → **cars** drive across as cross-traffic (classic Crossy-Road
     timing); a car hit costs a heart. (NEW hazard to build.)
  3. **Alleyway** → narrow gaps between buildings; **dodge falling/scattered
     debris**. (NEW hazard to build.)
- City build: `CityFacade.cs` (assembles pre-made kit blocks — buildings both
  sides, kit roads + crosswalks at road lanes, sidewalk stretches between) called
  from `LevelBuilder`. The giant-shoe `SidewaysFootHazard` is retired as the hero
  hazard.
- **Status (2026-06-24):** straight sidewalk corridor + realistic ratios live;
  pedestrians walk and now **steer around props** (`ObstacleField`); cars cross on
  road lanes; lizard **faceplants** solid props (`PropObstacle`) and shoe-bumps cost
  a heart (shared tail→heart pool). **Megascans/Fab art pass (Phases 0–6) DONE +
  verified** — full bot playthrough reached the safe zone, 0 magenta materials (see below).
  **TODO:** add lane TYPES (sidewalk / road / alley) to `LevelDefinition`/`LaneSpec`,
  build the **alley zone**, then wire the staged `gravel`/`ground_rubble` ground surfaces
  + a falling/scattered-**debris** hazard there (no home until the alley exists).

### Megascans / Fab asset integration (2026-06-23 → 24)
Plan + manifest: `Lizard_Crossing_Claude_Work_Packet/05_AssetPlan/ASSET_INTEGRATION_PLAN.md`.
Done and verified in-engine (0 console errors, mechanics intact):
- **City surfaces are reskinned, NOT a filename drop-in.** The visible sidewalk/road/
  walls are baked onto the imported **NYCity GLB's own materials** (lizard runs on an
  *invisible* collider over them). Those materials use shader
  **`Shader Graphs/glTF-pbrMetallicRoughness`** (props `baseColorTexture` /
  `baseColorFactor` / `normalTexture` — NOT `_BaseMap`/`_MainTex`/`_BumpMap`).
  `Assets/Scripts/FX/CityReskin.cs` (called from `LevelBuilder` nyc branch) maps GLB
  material-name → `Resources/GeneratedArt/` texture; shader-agnostic, runtime-only.
  Extend `CityReskin.Map` to reskin more city materials.
- **Scattered obstacles** are real concrete **rubble** chunks (`Resources/Models/rubble`,
  via `LevelBuilder.BuildRubblePile`), replacing primitive trash cans; keep `PropObstacle`
  + `ObstacleField`.
- **Edge furniture** (bus stop, street lamp, phone booth, bench) in `Resources/Models/Furniture/`,
  placed by `BuildEdgeFurniture`/`PlaceFurniture` at the band edges, colliders STRIPPED.
- **Plants** (Megascans raspberry, alpha-cutout leaves) scattered by `BuildPlants`.
- **Gotchas (see memory `megascans-integration`):** AI "stylized tier" GLBs are poly bombs
  (1.5M tris — rejected); `Unity_ImportExternalModel` Height normalization is unreliable
  (re-normalize at placement) and bakes a Z-up→Y-up rotation (compose yaw, never overwrite
  `.rotation`); clamp surface maps to 2048 + flag normals as NormalMap.

## Workflow rules (owner)
- **Verify before "done" — the `verify-and-ship` loop** (owner rule, 2026-06-25): after
  every completed unit of work, run the verification loop BEFORE calling it done —
  compile clean (0 console errors) → bot playthrough reaches the safe zone (`State==Won`)
  → scene validator → 0 console errors → capture a proof frame. Packaged as the
  `verify-and-ship` skill (`.claude/skills/verify-and-ship/`). NEVER mark work done with
  console errors or a failed playthrough. The owner judges *feel*; this loop checks
  *correctness* so "approved" is never "unchecked."
- **ALWAYS run the Gemini video reader as a standing 2nd-brain QA gate (owner rule, 2026-06-26).**
  It is not optional and not just for big art passes — on EVERY change that is visible in-game
  (and at every stage gate), after the change verifies: record a portrait clip (menu `Lizard
  Crossing/Bot/Set Game View 9:16` → `Bot/Record MP4 (10s)`) and run `python Tools/gemini_review.py
  [--state run|win|...]` on it. Read its **BUGS / CONCEPT-GAP / punch-list**, LOG findings into the
  §5 ledger, and fix the real ones before calling the unit done. It is the owner's second pair of
  eyes that spots bugs + judges the look so nothing ships unseen. **Caveats baked in from hard
  experience:** (1) the reviewer is sometimes unreliable on lighting (it has hallucinated
  "streetlights/night" that don't exist) — weight its concrete BUG reports + measured frames over a
  vague vibe label, and treat the OWNER's stated preference (e.g. neutral over golden) as
  superseding the spec; (2) judge the look on the REAL MP4 / ScreenCapture, never the RT capture
  (it renders brighter than the real game and has fooled passes — see commit 62ccfff). Every agent
  brief that touches visuals must include this Gemini step (the agent runs it, or the main session
  runs it on the agent's clip).
- **REGRESSION CHECKLIST — stop old bugs from silently coming back (owner rule, 2026-06-26).**
  Past issues kept re-appearing because nothing re-checked for them. The canonical list of every
  recurring problem we've fought lives in **`docs/REGRESSION_CHECKLIST.md`** (items `[R1]…[Rn]`).
  It is now wired into `Tools/gemini_review.py` (auto-injected), so EVERY Gemini run returns a
  **`## REGRESSION CHECKLIST`** section marking each `[Rn]` PRESENT / FIXED. The loop:
  1. On every in-game-visible change, after the Gemini run, READ that section. **Any `[Rn]` PRESENT
     is a regression** — either FIX it before "done", or, if it's a knowingly-open locked item
     (e.g. `[R2]/[R3]/[R4]` lizard model/anim until Stage 4), explicitly say "accepted, still open"
     — never let it slide silently.
  2. The machine-gated items `[R28]/[R29]/[R30]` (lizard confinement, crossing fairness + car-hit,
     magenta) are asserted by the **Invariant Check + bot playthrough + magenta scan**, not Gemini —
     run those in the verify loop.
  3. **AUTO-ADD EVERY MISTAKE — the list only grows (owner rule, 2026-06-26).** The MOMENT any new
     bug, mistake, or regression is found — by the owner, the Gemini bot, a specialist agent, or me —
     **immediately add it as a new `[Rn]` to `docs/REGRESSION_CHECKLIST.md` before moving on.** No
     exceptions, no "I'll remember it." Anything we have hit even ONCE becomes a permanent watch item
     so it can never silently come back. Log notable PRESENT→FIXED shifts into `docs/PROJECT_PLAN.md` §5.
- **Commit AND push after every completed unit of work** (owner rule, 2026-06-25):
  once a change is made and verified in-engine, commit it with a clear message and
  `git push` to the GitHub repo (`origin`, branch `feat/realistic-city-crossing`).
  Keep commits narrowly scoped; never leave verified work sitting only on the local
  machine. Do NOT push to `main` or merge without asking.
- **`/code-review ultra` at every sprint/stage gate** (owner rule, 2026-06-25): at the
  end of each sprint or stage (see `docs/PROJECT_PLAN.md`), and before any merge to
  `main`, REMIND the owner to run `/code-review ultra` (multi-agent cloud review of the
  branch) — it's the owner's "second pair of eyes." Claude CANNOT launch it (owner-
  triggered + billed); prompt the owner at the gate, never attempt to run it. **Also at each gate,
  fire the advisory board (`claude-advisor` + `unity-advisor`) and the `knowledge-auditor` per
  `CO-OP.md` "Standing cadences"** — scan for what's new / what we're missing, and re-verify any
  agent canon we changed; log/apply their high-value findings before the gate is "done."

## North star — ALWAYS WORK THE PLAN (owner, 2026-06-25)
- **Every task must trace to the plan AND the concept deck. No orphan work.** The single
  source of truth for *what to build and in what order* is **`docs/PROJECT_PLAN.md`** (stages,
  milestones, ETA, and the **live task ledger** in §5). The single source of truth for *how
  good it must look/feel* is the concept target deck **`Assets/Art/Concept/`** (run / squished /
  faceplant / win / gameover / nearmiss / title) + `docs/VISUAL_TARGET_SHEET.md`. Before starting
  ANY change, name which stage + ledger item it advances and which concept frame it moves toward;
  if it does neither, it's scope creep — don't do it (or add it to the ledger first). This is the
  prime directive: we are always building toward that plan and that final goal.
- **Keep the plan live.** When a unit of work is verified+committed, tick it off the ledger in
  `docs/PROJECT_PLAN.md`; when the owner adds feedback, log it into the ledger so nothing is lost.
  The plan is a living doc, not a one-time artifact — it always reflects reality.
- **Quality bar is the concept, not "it works."** Don't just "make it work" — make it *high
  quality and matching the concept*. Record the game, watch it, and fix/optimize against the
  frames continuously. The owner judges *feel*; the plan + verify loop keep *correctness*.

## Section locking & change control (owner, 2026-06-26) — STOP THE LOOP
The game is divided into **sections** (see `docs/PROJECT_PLAN.md` §0 "Sections & lock status"),
each owned by ONE agent. Real teams finish a section, lock it, and don't reopen it without a
reason worth the cost. We work the SAME way now — this is how we stop re-breaking finished work.
- **Locked = frozen.** Do NOT edit a Locked section's files without explicit owner OK. Currently
  Locked: **Lizard** (model/rig/animation) and **Controls + camera** (auto-run/dash/low POV).
- **Before any change**, name which section it belongs to. If it would reach into a Locked
  section, STOP and weigh it against the concept deck: not clearly worth it → don't; worth it →
  ask the owner first. Never silently touch a locked section.
- **After any change**, run that section's checks + the Foundation-invariant regression gate
  (see below). If it regressed a Locked section, REVERT — verified work is never silently broken.
- **One Active section at a time.** Finish it → owner signs off → it Locks → move to the next.
  No jumping sections / adding unrelated "new" work mid-section. The current Active section is
  named in PROJECT_PLAN §0; right now it's **World + corridor**.
- **Foundation invariants are machine-checked, not hoped.** The verify loop must assert (and a
  regression validator must hard-fail on): lizard physically cannot leave the sidewalk band
  (real wall/fence colliders, not a math clamp); the run band is straight end-to-end; fences are
  solid to BOTH lizard and pedestrians; cars actually cross. A green "bot reached the safe zone"
  is NOT enough — the bot never tries to walk through a wall, so spatial invariants must be tested
  explicitly. This is why finished foundation work kept silently regressing.

## Agent usage rules (owner, 2026-06-25)
- **The corresponding specialist has a SAY in every change to their domain (owner rule, 2026-06-26).**
  No solo changes in a specialist's area. Before a change is committed, the section-owner agent for
  the domain it touches (see the routing table in `docs/STUDIO_BOARD.md` "THE TEAM" + PROJECT_PLAN §0)
  must EITHER do the change (delegated to them) OR review + sign off on it — **at minimum the
  corresponding one(s).** Routing: lighting/post/exposure → `lighting-post-artist`; HUD/camera/feel/
  juice/menus → `camera-ui-juice`; surfaces/props/set-dressing/pedestrian-art/themes →
  `environment-artist`; sourcing/generating assets → `asset-scout`; gameplay regression gate + bot
  playthrough → `gameplay-guardian`; **code correctness/bugs → `code-reviewer`; mobile perf/GC/draw
  calls → `perf-optimizer`; architecture/quality/conventions/faithful-to-design → `code-architect`**
  (these three + guardian + cloud-engineer are the **code-review board** — see `CO-OP.md`; route code
  changes through the relevant lenses, multi-pass, then synthesize); cloud/CI/builds/backend/release →
  `cloud-engineer`; **Claude/Anthropic capability + new updates → `claude-advisor`; Unity 6/URP/mobile
  expertise + new updates → `unity-advisor`** (the read-only **advisory board** — they scan for updates
  and recommend what we miss); auditing our agents' OWN knowledge/sources (fact-check) →
  `knowledge-auditor`; visual correctness vs the concept → `art-director` (with the Gemini "video guy"
  `Tools/gemini_review.py` as the standing automated QA on every visual change). Reviews are read-only/
  off-engine so they parallelize; the live editor stays single-threaded for whoever does the in-engine
  work. The main session is the lead/integrator — it may make a change, but routes it past the owner
  before it's "done." A change touching several domains gets a say from each corresponding owner.
- **Every AI-GENERATED asset gets a design-review before it's accepted/committed.** Concept
  frames, textures, models, sprites — all are CANDIDATES, not final. Before committing, the
  `art-director` (or the main session acting as it) must check the asset depicts the intended
  game state / mechanic / setting *correctly*, not just that it looks pretty: a "squished"
  frame must show a flattened lizard; a faceplant must be *into an obstacle*, not open space;
  the safe zone must be the right place (**Central Park** — see VISUAL_TARGET); a game-over
  must read like a real game-over *screen* (panel + stats + RETRY), not a sad photo.
- **Never dispatch a visual agent blind. Brief it with the objective.** Any agent judging or
  making visuals (esp. `art-director`) MUST be fed: the concept reference + the generated
  target deck (`Assets/Art/Concept/`), `docs/VISUAL_TARGET*.md`, AND the specific purpose of
  the asset (what state/mechanic/setting it represents). An agent that doesn't know the
  objective can only judge polish, not correctness — that's how the bad frames shipped.
- **The owner's concept is the spec.** Match it. When a generated asset is wrong, fix the
  prompt to bake in the design logic and regenerate; don't accept "close but nonsensical."

## Non-negotiable rules (from the packet)
- The lizard moves **forward (+Z)** toward the safe zone. Hazards come on TWO axes:
  a dense **sidewalk crowd walks ALONG ±Z** (the lizard weaves through giant feet —
  this is now the hero hazard) AND **cross-traffic moves ±X** across its path
  (jaywalkers, cars, falling debris), classic Crossy-Road timing. (This supersedes the
  packet's original "primary hazards move ±X / never parallel walkers" rule — the
  realistic-city redesign made the parallel crowd the primary challenge. Keep BOTH axes
  readable from the low POV; don't let the crowd become an unreadable wall of bodies.)
- The camera is the most important feature: very low third-person lizard POV
  (camera y < 3), lizard bottom-center, hazards must feel giant.
- Build in phases. No cosmetics/shop/ads/multiple lizards/level select/daily
  challenges until the Phase 1 vertical slice feels good.
- Review every change against `Lizard_Crossing_Claude_Work_Packet/01_GameDesignDocs/QUALITY_BAR.md`
  and update `Lizard_Crossing_Claude_Work_Packet/04_ReviewChecklists/BUG_AND_GAP_LOG.md`.

## Project facts
- Unity **6000.4.10f1**, **URP 17.4.0** (a render-pipeline asset is assigned in
  GraphicsSettings — the art pass is on URP now), portrait mobile. URP material props
  are `_BaseMap`/`_BaseColor`/`_BumpMap`; the imported NYCity GLB uses the glTF Shader
  Graph instead (see Megascans note). Never hardcode the "Standard" shader (magenta
  trap) — go through `MaterialCache`/`LitShader`.
- The Boot scene (`Assets/Scenes/Boot.unity`) contains only a `Bootstrap` object;
  the entire world is constructed at runtime from code. Regenerate the scene via
  menu **Lizard Crossing → Generate Boot Scene** or batch
  `-executeMethod LizardCrossing.EditorTools.ProjectSetup.BatchSetup`.
- 3-hearts lives model. Free continuous movement + dash. Bugs are the currency.
- Textures: user-generated Higgsfield art goes in `Assets/Resources/GeneratedArt/`
  (prompts + filenames in `Art/HIGGSFIELD_PROMPTS.md`); procedural fallbacks
  in `ProceduralTextures` keep the game runnable with zero assets.
- Tests: play-mode smoke tests in `Assets/Tests/PlayMode` (adapted from the
  packet); editor validator menu **Lizard Crossing → Validate Phase 1 Scene**
  (run while in Play Mode — the world is runtime-built).
- Decision log: `docs/DECISIONS.md`. Design: `docs/DESIGN.md`. Phases: `docs/ROADMAP.md`.

## Batch verification
```
& "C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe" -batchmode -quit -nographics `
  -projectPath "C:\Users\snpvi\Documents\GitHub\Lizard-Crossing" `
  -executeMethod LizardCrossing.EditorTools.ProjectSetup.BatchSetup -logFile setup.log
```
