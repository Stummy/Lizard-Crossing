# Studio workflow audit — 2026-06-28 (claude-advisor + unity-advisor, paired)

Owner asked the advisory board to find how to optimize our workflow for **quality / workflow / returns**.
Both advisors verified every version-specific claim against official docs (Jan-2026 cutoff + live web).
This is the durable record; the apply-order is at the bottom. Gating tags: **[NOW]** safe off-engine /
**[EDITOR]** needs the live editor+MCP / **[$]** needs an owner install or cost decision.

## ⭐ Headline — the MCP-drop root cause is a one-flag fix
`ProjectSettings/EditorSettings.asset` has `m_EnterPlayModeOptions: 0` while the *feature* is enabled
(`m_EnterPlayModeOptionsEnabled: 1`) — i.e. configurable Enter-Play-Mode is ON but **no options are set,
so a full domain reload still fires on every Play**. That domain reload is the exact event that revokes
the Unity-MCP approval (per memory `unity-mcp-connection`). Disabling domain reload kills the #1 friction
**and** makes Play-entry near-instant.
**Caveat (why it's [EDITOR], not a blind flip):** with domain reload off, `static` state persists between
Play sessions and must self-reset — `GameStateManager.Instance`, `PlayerController.Instance`,
`InputProvider.MoveOverride`/`StartOverride`, etc. Guard them with
`[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` resets, then re-run
`Invariant Check` + `Auto-Playtest` to confirm clean state. Do scene-reload-disable first (zero risk),
then domain-reload-disable + the static-reset audit (route through `code-architect`/`code-reviewer`).
Source: Unity 6 Manual "Enter Play mode with domain reload disabled" / "configurable enter play mode details" (2026-06-28).

---

## Claude-side (claude-advisor) — verified against /claude-api + Claude Code docs
1. **[$ decision] Tier the subagent models.** All 14 agents are `model: opus`; every gate fires 6–8 Opus
   spawns. Keep Opus for judgment roles (`art-director`, `code-architect`, `code-reviewer`, the 2 advisors);
   drop mechanical roles to **Sonnet** (`knowledge-auditor`, `asset-scout`, `studio-producer`,
   `gameplay-guardian`) and consider **Haiku** for high-volume passes (Anthropic's own Explore runs on
   Haiku). `effort:` frontmatter can trim further. **Biggest returns win; lowers spend.** (S)
2. **[NOW] Lock down agent tools.** Omitting `tools:` inherits ALL tools — so every "read-only" reviewer can
   `Write`/`Edit`/spawn agents. Add `tools: Read, Grep, Glob, Bash[, WebSearch, WebFetch]` to the pure
   reviewers/advisors/auditor. **Caveat found by main session:** `perf-optimizer` genuinely needs the
   `Unity_Profiler_*` MCP tools — exclude it or give it the Unity set. (S)
3. **[NOW-ish] Enforce gates with hooks.** There is **no project `.claude/settings.json`** — "always run the
   loop / commit+push / never push to main" are willpower, not mechanics. Add `SessionStart` (inject the boot
   protocol + Active section), `PreToolUse` on git (block push to `main`), `Stop` (remind to tick the ledger /
   run Gemini). Hooks are harness-enforced, so they can't be forgotten. (M; test the shell commands.)
4. **[NOW] Permission allowlist** in `.claude/settings.json` for read-only Unity-MCP + safe Bash (or run the
   built-in `/fewer-permission-prompts`) to kill the per-call prompt friction in the record→review loop. (S)
5. **[NOW] Promote the capture/Gemini procedure to a Skill** (`.claude/skills/record-and-review/`) so it loads
   on-demand (lower tokens/spawn) with one authoritative copy — like `verify-and-ship`. (M)
6. **[NOW] Code-board recall tweak.** On Opus 4.8, "be conservative / only high-severity" *lowers* review
   recall — the documented fix is "report EVERY finding with confidence+severity; a downstream step filters."
   Add that line to the code-board agents; hand them the diff inline instead of cold-refetching. (S)
7. **[NOW] Background-agent the gate reviews** (already started this session) — independent read-only scans run
   async + notify, so editor work isn't blocked waiting. (S)
8. **[radar] Managed Agents / Agent SDK** could one day host a *reusable* studio (versioned agent configs,
   scheduled runs) — but it's a beta re-platforming; not now. (UNVERIFIED fit.)

## Unity-side (unity-advisor) — verified against Unity 6 Manual / package docs
1. **[EDITOR] Disable domain reload on Play** — the headline fix above. (S flag + S/M static audit)
2. **[EDITOR] Headless batchmode verify** — run the machine gate as `Unity.exe -batchmode -runTests` (like CI)
   instead of driving Play through the fragile bridge; the bridge becomes authoring-only. Works locally because
   the Hub login supplies the Personal license to local `Unity.exe` (the dead-activation limit is remote-only).
   Caveat: can't run while the editor holds the license — best paired with #4. (M)
3. **[EDITOR] Asset-pipeline hygiene** — Auto Refresh → "Enabled Outside Playmode"; codify edit-batch → ONE
   refresh → wait-for-ready → ReadConsole (fewer revocation windows). (S)
4. **[$ install] Promote the Bot gates to `[UnityTest]` PlayMode tests + add `com.unity.test-framework.performance`**
   (2.8.x) — one headless `-runTests` call covers Invariant/Revive/Auto-Playtest, turns CI red on regression
   automatically, and adds a **frame-budget gate** (`Measure.Frames()`). The missing automated half of perf. (M)
5. **[$ install + device] Stand up S2-6 device profiling** — `com.unity.memoryprofiler` (1.1.x) for the 361 MB
   `Resources/` question + `com.unity.adaptiveperformance` (5.1.x, Samsung/Android provider) for thermal
   throttling (what actually tanks a held runner) + a Development Build w/ Autoconnect Profiler on a real phone.
   Unblocks the entire perf audit. (M setup)
6. **[EDITOR] Real-camera POV capture seam** — a `Bot/Capture POV` menu item that renders the ACTUAL game
   camera (with the real post stack) to a PNG — removes both the `Unity_Camera_Capture` failure AND the
   "RT renders brighter" discrepancy, so proof frames match the MP4. (S)
7. **[$ conditional] URP 17 GPU Resident Drawer + GPU occlusion culling** — strong fit (repeated kit buildings +
   occluded-behind-buildings crowd) BUT requires **Vulkan-only** on Android (no GLES) → narrows device coverage.
   Decide only AFTER #5's profile says draw calls are the bottleneck + the device floor supports Vulkan. (M)
8. **[EDITOR] Addressables for `Resources/`** — the biggest non-frame memory/build win; editor-gated (moving
   assets out of `Resources/` can break `Resources.Load` paths — needs the live editor + Memory Profiler). (L)

**Not worth it (verified):** Input System migration (auto-run + 2 hold-buttons gains nothing), `UnityEngine.Pool`/
Cinemachine retrofits (hazards already recycle; camera is a bespoke locked rig) — correctly NOT hand-rolling.

## Recommended apply-order
1. **[NOW, this/next session]** Claude #2 tool-lock (minus perf-optimizer) + #6 recall tweak + #4 allowlist + #7 background reviews → then #1 model-tier ($ your call) + #3 hooks + #5 skill.
2. **[EDITOR, once MCP re-approved]** Unity #1 domain-reload fix + static-reset audit (the friction killer) → #3 pipeline hygiene → #6 POV capture seam → #2/#4 headless `[UnityTest]` verify loop.
3. **[$ when ready]** Unity #5 device profiling (unblocks S2-6 + the real perf work) → #8 Addressables → #7 GPU drawer (conditional on the profile).
