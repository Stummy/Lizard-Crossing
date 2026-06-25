# Meshy AI asset-generation pipeline

> On-demand 3D asset generation for Lizard Crossing via the Meshy API. Owned by the
> **asset-scout** agent (generation is another sourcing method). **Last updated: 2026-06-24.**

## Key & safety
- The Meshy API key lives **OUTSIDE the repo**: `~/.lizard_secrets/meshy_api_key`
  (`C:\Users\snpvi\.lizard_secrets\meshy_api_key`). **Never** commit it or paste it into
  source/docs. `.gitignore` also blocks `*.key` / `*api_key*` / `.env` as a backstop.
- `Tools/meshy.sh` reads the key from there (override with `MESHY_KEY_FILE=...`). The script is
  safe to commit — it contains no secret.

## When to use what (sourcing decision)
1. **CC0 direct download** (asset-scout → Poly Haven / ambientCG / Kenney / Quaternius) — best
   for surfaces, HDRIs, and anything that already exists clean and free. Fastest, zero credits.
2. **Meshy generate** — for **bespoke props that don't exist as good free assets**: themed
   street dressing, the Boardwalk kit (surf shack, tiki signs, scooter…), unique hazards.
   Costs credits; every mesh needs the cleanup below.
3. **Unity Asset Store / account-gated** — owner claims via browser; scout imports + vets.

## Helper commands (`Tools/meshy.sh`)
```
bash Tools/meshy.sh balance                                   # remaining credits
bash Tools/meshy.sh t23 "<prompt>" [realistic] [polycount]    # text->3D PREVIEW (no " in prompt)
bash Tools/meshy.sh wait <task_id>                            # poll until SUCCEEDED/FAILED
bash Tools/meshy.sh refine <preview_task_id>                  # PREVIEW -> textured model (new id)
bash Tools/meshy.sh wait <refine_task_id>
bash Tools/meshy.sh download <task_id> Assets/Art/Imported/Generated/<name>.glb
```
- **Two-stage:** `t23` makes an untextured PREVIEW mesh (cheap, fast); `refine` adds PBR
  textures (more credits). Review the preview before spending on refine.
- **Polycount:** always pass a mobile-sane `target_polycount` (default 20000; props 8k–20k).
  This is the guardrail against the 1.5M-tri poly-bomb lesson — Meshy remeshes to the target.
- Image-to-3D endpoints exist too (`/v1/image-to-3d`); add to the script if we want to turn a
  concept image (e.g. the Boardwalk reference) directly into a mesh.

## Post-generation cleanup (MANDATORY — same as any imported mesh)
A generated GLB is raw. Before it ships it must be:
1. **Imported** under `Assets/Art/Imported/Generated/` (NOT in `Resources/` until chosen —
   everything in `Resources/` ships in the build). Move the final keeper into `Resources/Models/`.
2. **Vetted:** check tri count + texture size in-editor. Clamp textures to **2048**, normals
   flagged NormalMap. Reject/regenerate if it's a poly bomb or a messy blob.
3. **Normalized:** Meshy/`ImportExternalModel` height normalization is unreliable — re-normalize
   to a real-world target height via combined bounds at placement. It bakes a Z-up→Y-up rotation
   — compose yaw (`AngleAxis(yaw,up) * rot`), never overwrite `.rotation`. (See memory
   `megascans-integration` — same gotchas.)
4. **Wired** through the existing pipeline: props → `PropObstacle` + `ObstacleField`; edge
   furniture → colliders stripped + `ObstacleField`; materials via `MaterialCache` (never raw
   "Standard"). Foliage → two-sided URP/Lit `_ALPHATEST_ON`.
5. **Attribution:** Meshy output is yours to use commercially under your plan — still log
   generated assets in `ATTRIBUTION.md` for provenance.

## Budget note
~1,260 credits at setup (2026-06-24). A preview is cheap, a refine more; check `balance`
periodically. Generate deliberately — a clear, specific, single-object prompt ("a single
weathered NYC newspaper vending box, standalone") yields far better, cleaner meshes than a busy
scene prompt.
