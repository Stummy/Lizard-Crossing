# CO-OP.md — the studio cooperation workflow (how the agents check each other)

> 🗺️ **At-a-glance team + loop map:** [`docs/STUDIO_MAP.svg`](docs/STUDIO_MAP.svg) — the org chart
> (Owner → Main Session → Art Dept / Code-Review Board / Production & QA) and the 8-step per-change loop.

> **Owner ask (2026-06-27):** "Make a workflow that you can check in between the agents and commit
> it, so you are always doing that." This is that workflow. The main session is the **lead /
> integrator**; the specialist agents (`.claude/agents/`) are the **section owners + reviewers**;
> the **Gemini video bot** (`Tools/gemini_review.py`) is the standing automated QA tester. Every
> visible change runs this loop before it's called done. This file is the single source of truth
> for *how we work together*; `docs/PROJECT_PLAN.md` is *what* we build; `Assets/Art/Concept/` +
> `docs/VISUAL_TARGET_SHEET.md` is *how good it must look*.

## The team (who owns / reviews what)
| Domain (a change touching this…) | Section owner = does it OR signs off |
|---|---|
| Lighting / post / exposure / DoF / grade | `lighting-post-artist` |
| HUD / camera framing / feel / juice / menus | `camera-ui-juice` |
| Surfaces / props / set-dressing / pedestrians / themes / city reskin | `environment-artist` |
| Sourcing or generating assets (CC0 / Meshy) | `asset-scout` |
| Gameplay regression gate + bot playthrough + rough frame budget | `gameplay-guardian` |
| Code correctness / bugs / removed guards / regressions (recall) | `code-reviewer` |
| Mobile performance: GC allocs, hot paths, draw calls, pooling | `perf-optimizer` |
| Architecture / code quality / conventions / faithful-to-design | `code-architect` |
| Cloud / CI-CD / builds / backend / analytics / release (reads `cloud.md`) | `cloud-engineer` |
| Claude / Anthropic capability + new updates (advisor) | `claude-advisor` |
| Unity 6 / URP / mobile expertise + new updates (advisor) | `unity-advisor` |
| Auditing our agents' OWN knowledge + sources (fact-check) | `knowledge-auditor` |
| Visual correctness vs the concept (lead reviewer) | `art-director` |
| Sequencing the push / what ships next | `studio-producer` |
| Automated QA on EVERY visual change (not an agent — a tool) | `Tools/gemini_review.py` |

Reviews are **read-only and run in parallel** (they don't touch the editor). The **live editor is
single-threaded** — only the main session (or one delegated specialist) makes the in-engine change.

## The code-review board (the code mirror of the art team)
Just as `art-director` + the visual specialists grade every *look* change against the concept, a
**code-review board** grades every *code* change against correctness, performance, and quality — so
code is checked **multiple times, for specific reasons**, and optimized when it needs to be. The board
is **five lenses**, routed by what the change touches (not every change needs all five):

| Lens | Agent | The question it answers |
|---|---|---|
| Correctness / bugs | `code-reviewer` | "Is it a bug?" — null/destroyed derefs, removed guards, lifecycle/timing footguns, cross-file breakage. Recall-focused: a missed bug ships. |
| Performance | `perf-optimizer` | "Is it fast on a phone?" — per-frame GC allocs, hot-path lookups, pooling, draw calls/overdraw, Resources bloat. Profiler-grounded when the editor is live. |
| Architecture / quality | `code-architect` | "Is it the RIGHT, clean implementation — and does it build what the concept calls for?" — altitude, reuse, layering, conventions, scope. |
| Gameplay regression | `gameplay-guardian` | "Did it break the game or blow the rough frame budget?" — sacred mechanics + bot playthrough + Invariant Check. |
| Infra / secrets / release | `cloud-engineer` | "Is anything touching builds, CI, backend, or secrets safe and lean?" — reads `cloud.md`. |

**How the board runs (same shape as the art team):**
- **Route by domain.** A gameplay/systems diff → `code-reviewer` + `gameplay-guardian` (+ `perf-optimizer`
  if it's per-frame/spawn code). A refactor/new-system → add `code-architect`. A CI/build/secrets diff →
  `cloud-engineer`. Always ask "is this fast enough on a phone?" for anything in `Update`/spawning.
- **Parallel + read-only.** The reviewers don't edit; they hand back ranked punch-lists. They can run
  at the same time since they only read.
- **The main session synthesizes** (the heart of "check between agents"): dedupe overlapping findings,
  rank correctness/regression bugs above perf above cleanup, sequence the fixes so they don't fight,
  apply them, then **re-run the affected lens** to confirm the finding closed and nothing reopened.
- **Optimize when it needs it, not always.** `perf-optimizer` calls out what's *fine as-is* so we don't
  churn good code; `code-architect` praises what's clean. The bar is high quality, not max churn.
- **On-demand heavyweight:** the `/code-review` skill (and owner-run `/code-review ultra` at gates) is
  the multi-angle cloud review; the standing board is the per-change, in-session equivalent.

## The advisory board (keeping the studio current)
Two read-only experts whose job is to catch what the main session misses and keep us on the *current*
best path — they recommend, they don't change code:
| Advisor | Owns | Defining rule |
|---|---|---|
| `claude-advisor` | Claude models, Claude Code (skills/hooks/subagents/MCP), Agent SDK, Anthropic API, prompt/agent design, context+cost | **Verify before asserting** — training cutoff is Jan 2026; check the `/claude-api` reference + official docs/changelog before quoting any model id, price, or feature. |
| `unity-advisor` | Unity 6 / URP 17 / mobile: engine + package features, the right built-in for a job, profiling, our exact-version gotchas | **Verify before asserting** — check the Unity Manual / Scripting API / release notes via web before quoting a version-specific fact. |
- **They pair up.** When a recommendation spans both halves of our tooling (driving the Unity MCP
  bridge better from Claude, automating the record→review→verify loop, CI for Unity), they co-author it.
- **They're proactive.** Invoke them to ask "what are we missing / what's new that would help?" — not
  just to answer a question. Each recommendation carries a freshness tag (VERIFIED <source> / UNVERIFIED).
- **Owner-gated outward steps.** Installs, upgrades, and paid tiers are recommended by them, approved
  by the owner, integrated by the main session — same as `cloud-engineer`.

## Standing cadences — the meta-bots are PART OF THE PROCESS (not just on-call)
The advisory board and the knowledge-auditor are wired into the loop on a fixed cadence, so they keep
the studio current and correct automatically instead of waiting to be asked:
| Bot(s) | Fires… | Produces | Acted on by |
|---|---|---|---|
| `claude-advisor` **+** `unity-advisor` (paired) | at **every stage gate** (alongside `/code-review ultra`), and at **session boot** when a session opens a new stage | a ranked **"what's new / what we're missing"** list (each item VERIFIED/UNVERIFIED) — new Claude/Unity models, features, packages, or practices worth adopting | main session triages; **owner** approves any install / upgrade / paid tier |
| `knowledge-auditor` | **after ANY change to an agent's canon** (mandatory) and at **every stage gate** | a per-agent scorecard + ranked corrections (bad/stale sources, errors, gaps, bloat) | main session applies the high-value fixes, then **re-runs it to confirm** |
- **The gate rule:** a stage gate is not "done" until (a) the **advisory board** has scanned for what we're missing and (b) the **auditor** has re-blessed any canon we changed. New facts get logged — advisor findings → `docs/PROJECT_PLAN.md` §5 + the relevant knowledge base (`cloud.md`, `MEMORY.md`); auditor fixes → applied straight to the agent files.
- **Cost-aware:** these are spawned agent runs (they cost tokens), so they fire on the **gate cadence**, not on every change — the per-change loop already has its specialist + Gemini + machine gates. The owner can also invoke any of the three on demand at any time ("what are we missing?", "audit the agents").

## The loop — run this for every visible change, in order
1. **ORIENT** (session boot): read `docs/PROJECT_PLAN.md` §0 (the ONE Active section) + §5 ledger,
   `CLAUDE.md`, `MEMORY.md`; open the concept frame(s) for the state(s) being touched. Name which
   stage + ledger item + concept frame the change advances. No orphan work.
2. **CHANGE**: make the smallest change that moves the live game toward the plan **and** the concept.
   It belongs to exactly one section — if it reaches into a **Locked** section, stop and get owner OK.
3. **SPECIALIST GATE (check BETWEEN agents)**: the corresponding section owner(s) from the table
   above **do the change or review + sign off** on it — *at minimum the corresponding one(s)*. A
   change spanning domains gets a say from each owner. The main session **synthesizes** every
   agent's punch-list: dedupe, rank by severity-vs-concept, sequence, and **re-check after each fix**
   so one agent's fix doesn't reopen another's finding. This synthesis step IS "checking between the
   agents."
4. **GEMINI QA GATE** (standing, non-optional): set portrait `Lizard Crossing/Bot/Set Game View
   9:16` → `Bot/Record MP4 (14s)` → wait for "VIDEO DONE" / file-size-stable → `python
   Tools/gemini_review.py --state <run|win|…>`. Read its **BUGS / CONCEPT-GAP / PUNCH-LIST** and the
   **`## REGRESSION CHECKLIST`** section. **Any `[Rn]` PRESENT is a regression** — fix it, or
   consciously accept a knowingly-open locked item and say so. **Log findings into `docs/PROJECT_PLAN.md`
   §5** so nothing is lost. Caveats: Gemini is unreliable on lighting (it has hallucinated night /
   streetlights) — weight concrete BUG reports over vibe; judge the look on the **real MP4**, never
   the brighter-than-real RT capture; the **owner's stated preference supersedes the spec**.
5. **MACHINE GATE (verify-and-ship)**: compile clean (0 console errors, 0 magenta) → bot playthrough
   reaches the safe zone (`State==Won`) → `Bot/Invariant Check` PASS → capture a real proof frame.
   The machine-gated regressions `[R28]/[R29]/[R30]` are asserted here, not by Gemini.
6. **AUTO-ADD EVERY MISTAKE**: the moment any new bug/regression is found (owner, Gemini, an agent,
   or me), add it as a new `[Rn]` to `docs/REGRESSION_CHECKLIST.md` before moving on. The list only
   grows; anything hit even once becomes a permanent watch item.
7. **COMMIT + PUSH**: narrowly-scoped commit, push to `origin feat/realistic-city-crossing` (never
   `main`/merge without owner OK). Tick the §5 ledger.
8. **CHECKPOINT / GATE**: at each section or stage gate — remind the owner to run `/code-review ultra`
   (Claude can't launch it); **fire the advisory board** (`claude-advisor` + `unity-advisor`: "what's
   new / what are we missing?") and the **knowledge-auditor** (re-verify any canon we changed) per the
   **Standing cadences** below; show before/after vs the concept frame. The gate isn't done until those
   scans are in and their high-value findings are logged/applied.

## When to spawn an agent vs. do it inline
- Spawn the specialist when the owner asks for it, when the change is squarely in that domain and
  needs that lens, or to parallelize independent read-only reviews. Brief it with the **objective**:
  the concept reference + the generated target deck (`Assets/Art/Concept/`) + `docs/VISUAL_TARGET*.md`
  + the specific purpose of the asset/state. An agent that doesn't know the objective can only judge
  polish, not correctness.
- Do it inline when it's a small, single-domain fix you can verify yourself — don't pay for a cold
  spawn to re-derive context you already have.

## The synthesis pass (the heart of "check between agents")
After the parallel reviews come back, the lead (main session) produces ONE ranked punch-list:
- **Dedupe** overlapping findings; keep the one with the most concrete failure/concept-gap.
- **Rank** correctness/regression bugs above polish; rank concept-gaps by how far from the frame.
- **Sequence** so fixes don't fight (e.g. lighting before grade-dependent judgments).
- **Route** each item to its section owner (do or sign-off), fix, then **re-run the affected gate**
  (Gemini for look, machine gate for mechanics) to confirm the item closed and nothing reopened.
- **Log** the PRESENT→FIXED shifts into `docs/PROJECT_PLAN.md` §5.

> Keep this file live. When we learn a better way to cooperate, update CO-OP.md — it's a living
> contract, not a one-time artifact.
