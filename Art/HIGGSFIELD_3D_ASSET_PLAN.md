# Higgsfield 3D Asset Plan — Premium model pass

Goal: replace the procedural primitive geometry (blob lizard, cylinder legs/shoes,
sphere plants) with real generated 3D models so the game matches the reference
target — photoreal-stylized low lizard POV, giant colorful sneaker with a red
warning ring on cobblestone, tropical alley, SAFE ZONE sign.

## Status / blocker
The connected Higgsfield workspace is on the **free plan**, which cannot run
generations (`"Requires basic plan or higher"`). Cost preflight works; actual
generation needs **PLUS or ULTRA**. Source images cost ~1.25 credits each; the
Meshy `image_to_3d` mesh step is the larger cost (quote once a plan is active).
Once upgraded, this whole plan runs end-to-end.

## Pipeline (per asset)
1. **Concept image** — `generate_image` model `recraft-v4-1`, `model_type:"utility"`,
   `background_color:"#FFFFFF"`, square. One clean, full, centered subject on a
   flat white background (best input for image→3D).
2. **Mesh** — `generate_3d` model `image_to_3d` (Meshy), pass the image's media/job
   id as role `image`. Params: `should_texture:true`, `enable_pbr:true`,
   `target_polycount` 8k–20k (mobile), `topology:"quad"`.
   - Lizard (character): add `pose_mode:"a-pose"`, `enable_rigging:true`; optional
     `enable_animation:true` with `animation_action_id` (run = 16 `RunFast`,
     idle = 0, jump = 466) — gives an animated rig for the player.
   - Props (sneaker, pot, sign): no rigging.
3. **Import** — download the GLB to `Assets/Art/Models/<name>.glb`. Add the
   glTF importer package `com.unity.cloud.gltfast` to `Packages/manifest.json`
   so Unity imports each GLB as a usable prefab.
4. **Wire in** — a new `ModelLibrary` (mirrors `TextureLibrary`): load the model
   prefab from `Resources`/`Art` if present, else fall back to the current
   procedural builder. Swap call sites in `LizardBody`, `HazardParts.BuildShoe`,
   and `LevelBuilder.BuildPottedPlant` / `BuildSafeSign`.

## Asset list (priority order)
1. **Hero lizard** (player) — *highest impact*.
   > Full-body 3D game character of a cute stylized green iguana/basilisk lizard,
   > vibrant lime-green scales, soft yellow belly, long striped tail, big friendly
   > eyes, neutral splayed A-pose on all fours, three-quarter front view, soft even
   > studio light, isolated on plain white background, crisp silhouette, premium
   > mobile game hero, stylized realism.
2. **Giant sneaker** (hazard) — *iconic danger; one model, recolor for variants*.
   > Single chunky colorful running sneaker, teal/magenta/yellow color-blocked
   > panels, white chunky sole with visible air unit, clean studio product shot,
   > centered, plain white background, slight 3/4 angle, stylized-realistic,
   > high detail, game asset.
3. **Potted tropical plant** — flanking prop.
   > Terracotta pot with lush tropical plant, big monstera/banana leaves and a few
   > bright hibiscus flowers, centered product shot, plain white background,
   > stylized-realistic mobile game asset.
4. **SAFE ZONE sign** — wooden garden sign board on two posts, carved "SAFE ZONE"
   text, little flowers, plain white background. (Text can also stay as the current
   uGUI overlay if the model text reads poorly.)
5. **Pedestrian leg** (optional) — a denim-jeans lower leg + the sneaker, so the
   hazard reads as a real person's stride rather than a floating shoe.

Textures (cobblestone, garden backdrop, leaf) stay on the `recraft-v4-1` /
texture path documented in `HIGGSFIELD_PROMPTS.md` → `Assets/Resources/GeneratedArt/`.

## Scale note
Models import at their own scale; the builders already place hazards/props at
the game's "1 unit ≈ 1 lizard length, world authored oversized" convention
(docs/DESIGN.md §3). `ModelLibrary` normalizes each prefab to a target bounds
size so a swapped model lands at the same on-screen size as the primitive it
replaces — no gameplay/collision retuning needed.
