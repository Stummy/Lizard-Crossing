# Perf audit — 2026-06-28 (static pass, perf-optimizer lens)

**Scope:** every per-frame path in `Assets/Scripts` (all `Update`/`LateUpdate`/`FixedUpdate`),
the spawn/recycle patterns, and the asset budget. **Method:** static read only — the Unity MCP
bridge was revoked this session, so there is **no profiler capture here**; numbers that need the
profiler/device are flagged ⏱️ for the S2-6 gate. **No code was changed** (can't compile-check).

## Verdict — the C# is already lean; do NOT churn it
The hot paths were written with real perf discipline. Micro-optimizing them would add risk for
no measurable win. Evidence from this pass:

- **No per-frame GC allocations** in any `Update`/`LateUpdate` read: `PlayerController`,
  `SimpleHUDController`, `GiantPedestrian`, `Car`, `CollectibleBug`, `PropObstacle`, `ObstacleField`.
  All use struct math (`new Vector3`/`Vector2`/`Color` are stack, not heap). The HUD notably does
  **not** build strings every frame (the classic HUD GC trap) — progress/dash/danger are width &
  fill-amount writes.
- **No `GetComponent`/`Find`/`Camera.main` in any hot path.** Every `GetComponent`/`Resources.Load`/
  `Instantiate` hit is in build/spawn code (one-time), not per frame. (`HazardLaneManager` does a
  `GameObject.Find("NYCity")` but at build, not in Update.)
- **Hazards recycle, they don't churn.** `GiantPedestrian` and `Car` use a `_resting`/`PlaceAtStart`
  reposition + `SetActive` pattern — no `Instantiate`/`Destroy` per spawn, so no allocation spikes or
  GC from traffic. `ObstacleField` is a flat struct list scanned with index `for` loops (no iterator
  alloc).
- **The crowd is already culled.** `GiantPedestrian` sets `_animator.cullingMode =
  CullUpdateTransforms` (GiantPedestrian.cs:241) — deliberately, over the `updateWhenOffscreen`
  bounds that "tanked the framerate" (its own comment). One shared ground raycast feeds both feet.

> If you want a single number to defend "it's fine": there are **0** per-frame heap allocations in
> the scripts driving the run. The GC pressure on a phone will come from the engine/animator/UI
> layer, not our gameplay code — which is exactly what the profiler (S2-6) measures.

## The real levers (⏱️ profiler/device-gated — this IS the S2-6 checklist)
These are where a mobile frame actually goes; none are knowable without a capture on a real device.
Ranked by likely cost:

1. **Skinned pedestrian crowd** — N× `SkinnedMeshRenderer` + `Animator`. Even with
   `CullUpdateTransforms`, on-screen peds pay skinning + animator eval + a draw call each. Measure:
   GPU skinning %, SetPass calls, tris. Levers if hot: cap concurrent visible peds, an impostor/LOD
   at distance, GPU instancing for identical meshes, fewer bones, or `CullCompletely` for peds that
   are fully behind (accepting they freeze while culled). **Decide with the frame debugger, not a guess.**
2. **Post-processing stack** (`CinematicPost`) — Gaussian DoF (already the cheapest mode) + Bloom +
   tonemap + vignette. Mobile fill-rate cost scales with resolution. The `SetLite` path exists; the
   S2-6 job is to confirm which effects survive on a mid-tier phone at portrait res.
3. **Draw calls / batching / overdraw** — the NYC GLB + reskinned materials + props + crowd + the
   transparent foliage (alpha-cutout). Verify SRP Batcher compatibility (URP Lit + glTF Shader Graph
   should batch) and watch alpha overdraw from plants/particles. Frame debugger gate.
4. **Build size + memory: `Assets/Resources/` is ~361 MB and ALL of it ships** (everything under
   `Resources/` is force-included). This is a load-time + memory + APK-size cost. A curation pass
   (move genuinely-unused assets out of `Resources/`, address with explicit refs or Addressables) is
   the biggest *non-frame* win — but it needs the editor to re-check references safely, so it is
   **NOT** a blind off-engine edit. Queue it behind the live editor. See PROJECT_PLAN §6.
5. **Shadows** — one 2048 shadowmap (Bootstrap). Cheap and worth keeping for grounding; just confirm
   the cascade/distance on device.

## Safe to apply when the editor is back (low-risk, then verify)
None of these is a blind edit — apply with the bot playthrough + a frame check:
- Confirm pedestrian + car **pools are capped** to what the camera can ever see at once (if the
  spawner can stack more than ~the on-screen budget, cap it). Static read couldn't bound the live count.
- Evaluate `CullCompletely` vs `CullUpdateTransforms` for peds that spawn fully behind the lizard
  (they're off-camera at the low POV until the lizard passes) — a free win **iff** it doesn't cause a
  visible pop when they enter frame. A/B on a recorded clip.
- The AA-OFF → MSAA/SMAA item (PROJECT_PLAN Stage 1) is a quality change that *costs* perf; treat it
  as a look decision through the Gemini/owner gate, budgeted against the above — not a "perf win".

## Bottom line
Nothing to optimize in the gameplay C# — it's already where you'd want it. The perf story is a
**device-profiling task (S2-6)** plus a **Resources/ curation pass**, both of which need the live
editor/profiler and a phone. This audit is the running order for that gate; until then, "optimize the
code" would be motion without progress.
