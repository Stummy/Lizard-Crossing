# Pedestrian Fidelity — Asset Scout Report

_Off-engine inventory + recommendation. Author: asset-scout. Date: 2026-06-26.
Scope: PURE files-on-disk analysis — nothing in the project was changed, no Unity
Editor was touched. The main session executes the integration in-engine._

Problem being solved: pedestrians are the game's HERO HAZARD (tiny ~0.15u lizard weaves
through giant pedestrians from a ~3cm speck's-eye POV). All three Gemini QA reviews + the
art-director flag them as the #1 remaining VISUAL gap: they read "placeholder / blocky /
low-poly." Target style: "realistic cartoonish," person ≈ 1.8u.

---

## TL;DR — recommended pick

**Keep the on-disk pack (it's the only pedestrian-grade content we have) and fix the THREE
things actually causing the "placeholder" read — none of which is the mesh being unfixably
bad.** In priority order:

1. **Scale them DOWN to spec.** `GiantPedestrian.height = 2.5f` is the live default and the
   sidewalk crowd spawns at scale `1f`, so peds stand **2.5u** — ~39% taller than the 1.8u
   spec and ~16× the lizard instead of ~10×. Oversized + close to a 3cm camera is the single
   biggest "blocky giant" amplifier. **One-line fix → 1.8f.**
2. **Re-import the body/clothing meshes with normals + a mild smoothing pass, and confirm
   2048 albedo+normal are actually applied.** The mesh is ~3.5–4k tris (fine for mobile), but
   if normals import as "Calculate / hard" or the 2K maps aren't clamped-on, a 4k-tri torso
   reads faceted. The detail to close the gap is in the **normal map**, not more polygons.
3. **Only if 1+2 don't clear the bar:** generate 2–3 hero "foreground" pedestrians via Meshy
   at higher poly (the few peds that fill the frame), and keep the casual set for the
   mid/background crowd. Most peds are blurred by DoF anyway — spend polys only where the
   camera is sharp.

There is **no higher-fidelity realistic-pedestrian pack already on disk** to swap to. The
only other human meshes are a stylized **"Superhero" body-suit** character (wrong content
for a street crowd) and the lizard. So Option A = "use what we have, correctly"; sourcing a
better pack (Option B) is a real but slower path if the bar still isn't met.

---

## 1. INVENTORY — character/NPC assets on disk

| Asset | Path | Type | Rig | ~Tris | Textures | Anim | Fit |
|---|---|---|---|---|---|---|---|
| **npc_casual_set_00** (live source) | `Assets/npc_casual_set_00/` | Modular low-poly casual humans (2 body types × 3 sub-types, 7 cloth pieces, hair, shoes) | **Humanoid, rigged** (full finger bones) | **~3.5–4k tris/char (lod0)**, LODs 0–4 | up to **2048** PBR (albedo `_c`, normal `_n`, metallic `_m`, AO `_o`); body maps are 2K | none in pack | **Best available.** Realistic casual street people. Low-poly is the fidelity ceiling. |
| **Resources/NPC/ped_*.prefab** (12 wired) | `Assets/Resources/NPC/ped_01f_01 … ped_02m_03.prefab` | Pre-assembled pedestrians (casual-set lineage; baked mesh GUID `1c331bab…`, distinct from the raw casual FBX `800c7968…`) | **Humanoid** | same ~3.5–4k class | same 2K casual maps | none (driven by shared controller) | **This is what ships today.** `GiantPedestrian` loads all of these via `Resources.LoadAll("NPC")`. |
| **Kevin Iglesias — Human Basic Motions FREE** | `Assets/Kevin Iglesias/Human Animations/` | Humanoid locomotion clips (Walk/Run + Idle/Jump/Social), full 8-direction set + RootMotion variants, M+F, avatar masks, `HumanM/F_Model.fbx` demo bodies | Humanoid clips | n/a (animation only) | n/a | **Walk / Run / Sprint clips — already wired** | **Animation source. Solid. Keep.** |
| Superhero_Male/Female_FullBody | `Assets/Art/Imported/Characters/` (+ `Assets/Resources/Models/Human/Superhero_Male_FullBody.fbx`) | Stylized **superhero body-suit** character | likely humanoid | `human_albedo/normal/rough.png` (1–3.6MB ≈ 2K) + body-suit base colors | none bundled | **Wrong content** for a realistic street crowd (it's a hero/RPG body, not a casual pedestrian). Not a pedestrian candidate. |
| Mirza | `Assets/Mirza/` | **VFX only** (AERO volumetric fog, VFX toolkit) | — | — | — | Not characters. |
| PolyRonin | `Assets/PolyRonin/Downtown Cars Pack` | **Cars** | — | — | — | Not characters (car hazard pack). |
| ExternalModels | `Assets/ExternalModels/{busstop,phonebooth,streetlamp}` | **Street furniture** | — | — | — | Not characters. |
| GeneratedAssets/, Resources/Models/Generated | hashed Meshy outputs + `hydrant/newspaper_box/traffic_cone/…glb` | **Props** | — | — | — | Not characters. |

**License:** casual set ships only `readme.txt` (no explicit EULA file). It is a vendor
**"free demo"** modular set ("The free demo includes 2 body types… This package serves as
the base for every other character set"). Kevin Iglesias is the well-known **"Human Basic
Motions FREE"** asset-store pack. Both are free-tier store assets the owner downloaded.
**ACTION for owner:** confirm both carry the Unity Asset Store standard EULA (commercial use
+ redistribution in a built app is allowed under it) before ship. Log final license + source
in `ATTRIBUTION.md`. Neither is CC0, so this is an owner-verify, not a scout-verify, item —
flagging it rather than asserting it.

---

## 2. HOW pedestrians are built TODAY (diagnosis)

`Assets/Scripts/Hazards/GiantPedestrian.cs` builds every ped:

- `BuildHuman()` (line 168) → `Resources.LoadAll<GameObject>("NPC")` loads all 12
  `Resources/NPC/ped_*.prefab`, picks one at random (line 182), instantiates it.
- Controller: `Resources.Load("NPC/PedestrianLocomotion")` — a 3-state machine
  (**Walk / Run / Sprint**) whose motions are Kevin Iglesias humanoid clips (verified in
  `PedestrianLocomotion.controller`). Forward translation is code-driven; feet are tracked
  live off the ankle bones to sync the squish to the real footfall. **This pipeline is good —
  the animation is not the problem.**
- Normalize to height: `go.transform.localScale = height / measuredHeight`, then base on
  ground (lines 189–194).
- Materials: each submesh remapped to URP via `MaterialCache.GetUrpEquivalent()` +
  `CalmMaterial()` which **caps albedo at 0.66 and forces matte** (smoothness 0.08, metallic
  0) to kill the earlier "glowing blob" bloom blowout. URP props (`_BaseColor`/`_Smoothness`)
  — correct for these Standard-origin materials.

**Why it reads "placeholder / blocky" — concretely, in priority order:**

1. **Oversized.** `public float height = 2.5f` (line 27). Sidewalk crowd spawns via
   `StreetTraffic.SpawnSidewalk(... )` → `SpawnTrack(... scale = 1f)`, so peds render at the
   full **2.5u** — ~1.39× the 1.8u realistic-human spec, ~16× the 0.15u lizard. A too-big,
   too-close body from a 3cm camera maximizes how much its low-poly silhouette/faceting fills
   the frame. The comment even admits it was sized to "the kit's grand building doors," not to
   a real person. **This is the dominant contributor and a one-line fix.**
2. **Low-poly geometry is the hard ceiling.** ~3.5–4k tris/char is genuinely low for a hero
   hazard the camera sits *under* and stares up at. Knees, shoe silhouettes, and jacket
   edges — exactly what the low POV frames against the sky — are where facets show. No
   material trick fully hides a faceted silhouette.
3. **Normal/detail likely under-applied.** Body maps are 2K and a normal (`_n`) exists per
   piece, but `maxTextureSizeSet: 0` on the body texture .meta means import size is Unity's
   default, not an explicit clamp, and `CalmMaterial` only sets color/smoothness/metallic —
   it does **not** assert the normal map is assigned/flagged as a NormalMap on the URP
   material after the remap. If the remap drops the normal, the surface goes flat and the
   low-poly faceting has nothing breaking it up. Worth verifying the URP material actually
   has `_BumpMap` + keyword on after `GetUrpEquivalent`.
4. **Matte cap is conservative.** `CalmMaterial` clamps albedo to 0.66 and smoothness to 0.08
   for everyone. Necessary to stop the bloom blowout, but applied flat it can read a touch
   chalky/clay under the warm grade — fine for background, slightly "modelly" up close.

The concept `run_target.png` shows **photoreal-leaning** giant legs (denim, brown leather
shoes, soft DoF), heavily blurred. That's the bar: the **sharp few** foreground peds must
hold up; the blurred many can stay low-poly. This is the lever for Option B's hybrid.

---

## 3. RECOMMENDATION (ranked)

### Option A — use what's on disk, correctly (DO THIS FIRST)
Nothing better exists on disk, and the pack is decent — the look is being sabotaged by scale
+ import, not by the pack being unusable. Three changes, cheapest-first:

**A1. Scale to spec (highest impact / lowest cost).**
- `GiantPedestrian.cs:27` → `public float height = 1.8f;` (real human). This alone makes them
  ~10× the lizard (the documented ratio) and shrinks how much faceted silhouette fills frame.
- The footfall/telegraph math scales off `height/2.5f` (lines 394–395, 457) — already
  relative, so it follows the new height; re-verify the stomp still triggers after the change.

**A2. Re-import meshes + assert the normal map.**
- On `Resources/NPC/ped_*` source FBX (and/or the casual `npc_csl_00_character_*.fbx`): import
  **Normals = Import** (use the authored normals), Smoothing from the file; if seams show, try
  **Calculate, ~60° smoothing angle**. This softens the faceting without adding polygons.
- After `MaterialCache.GetUrpEquivalent()`, confirm the URP/Lit material has **`_BumpMap`
  assigned + `_NORMALMAP` keyword on**, and the normal texture is flagged **NormalMap** type.
  If the remap loses it, re-bind it — this is the cheapest fidelity gain after scale.
- Confirm albedo+normal are **2048** (they are 2K source; set `maxTextureSize 2048` explicitly
  so they're never auto-downsized).

**A3. Soften the matte cap slightly for the close peds (optional polish).**
- `CalmMaterial` smoothness 0.08 → ~0.15–0.20 keeps the bloom safe while letting fabric catch
  a little of the golden key, reducing the "clay" read. Tune in-engine against the live grade.

> Realistic expectation: A1+A2 should clear the "oversized blocky placeholder" complaint and
> get the crowd to "believable but modest." It will **not** reach photoreal `run_target.png`
> legs for the 1–2 peds that fill the frame sharply — that's Option B.

### Option B — source/generate the hero-foreground few (if A doesn't clear the bar)
Keep the casual set for the **mid/background** crowd (DoF-blurred — low-poly is invisible
there). Add **2–3 higher-fidelity pedestrians for the sharp foreground**:

- **Generate via Meshy** (owner has the paid plan; `Tools/meshy.sh`, see `docs/MESHY_PIPELINE.md`):
  one clean standalone object per gen, mobile-sane polycount.
  - Prompt style: `a single realistic casual adult pedestrian in jeans and a jacket and
    sneakers, standing relaxed, full body, standalone, realistic game character, neutral
    pose` — `realistic` mode, **target_polycount 15000–20000** (props budget is 8–20k; a hero
    ped can sit at the top of that). One object, no scene words, no `"` chars.
  - **Catch:** Meshy gives a static mesh with no humanoid rig → it won't drive the existing
    Walk/Run controller without a rig + avatar. For an auto-running crowd that needs animated
    legs (the squish is synced to footfalls), an unrigged hero ped is **not drop-in**. Either
    (a) use Meshy hero peds only as **static set-dressing** at the band edges, or (b) rig them
    (Mixamo auto-rig → re-import Humanoid) before they can replace an animated crowd member.
    This is why Option B is the slower path and why A comes first.
- **CC0 alternative (no rig either, same caveat):** none of the CC0 direct sources (Poly
  Haven / ambientCG / Kenney / Quaternius) ship realistic rigged adult pedestrians — Kenney/
  Quaternius are stylized low-poly (a step *down* in realism, not up). The realistic-rigged-
  pedestrian niche lives behind **account-gated stores**: Unity Asset Store free char packs,
  or **Fab/Quixel Megascans MetaHuman-adjacent** scanned people. Those need the **owner to
  claim** (I cannot log in / accept EULA / click claim). If we go this route I hand back a
  claim-list; owner claims; I import + vet (tris/tex/rig) before wiring.

**Ranked verdict:** **A1 → A2 (do now, one session, no new assets) → re-judge against
`run_target.png` → only then A3 / Option B.** Don't source a new pack until A is proven
insufficient; the evidence says scale+import, not the pack, is the failure.

---

## 4. INTEGRATION PLAN for the TOP pick (Option A — main session executes in-engine)

**Files & exact changes:**
1. `Assets/Scripts/Hazards/GiantPedestrian.cs`
   - Line 27: `public float height = 2.5f;` → `1.8f`. (Realistic human; ~10× lizard.)
   - In `BuildHuman()` material loop (lines 202–211), after `GetUrpEquivalent`, assert the
     normal survived: if source material had a normal/bump, ensure the URP material has
     `_BumpMap` set + `EnableKeyword("_NORMALMAP")`. (Verify in `MaterialCache.GetUrpEquivalent`
     first — it may already carry it; if so, no change.)
   - Optional A3: `CalmMaterial` smoothness `0.08f` → ~`0.16f` (both `_Smoothness` and
     `_Glossiness` lines 275–276).

**Model/prefab paths involved (no path change needed — keep the Resources contract):**
   - Live prefabs: `Assets/Resources/NPC/ped_01f_01.prefab` … `ped_02m_03.prefab` (12).
   - Source meshes (if re-importing normals): `Assets/Resources/NPC/` baked FBX (mesh GUID
     `1c331bab91255334980c78217115db75`) and/or `Assets/npc_casual_set_00/Mesh/npc_csl_00_character_0{1,2}{f,m}.fbx`.

**Import settings (apply in-engine, per the project's mobile rules):**
   - FBX: **Normals = Import** (or Calculate @ ~60°), Material = keep (already remapped at
     runtime), Rig = **Humanoid** (it already is — required for the foot-bone tracking).
   - Body/clothing textures (`Assets/npc_casual_set_00/Textures/*_c.tif`, `*_n.tif`):
     **maxTextureSize 2048**, `*_c` (albedo) **sRGB ON**, `*_n` (normal) **Texture Type =
     Normal map, sRGB OFF**.

**Animation hookup:** unchanged — `Resources/NPC/PedestrianLocomotion.controller` (Walk/Run/
Sprint, Kevin Iglesias clips) already drives the rig. After the height change, the foot-track
math (`height/2.5f` scaling) auto-follows; just **re-verify the stomp + telegraph** fire at
1.8u (it's the squish hazard — must still kill on a planted foot).

**Material/shader notes:** these are Standard-origin materials remapped at runtime to URP via
`MaterialCache.GetUrpEquivalent()` → URP/Lit props (`_BaseColor`, `_BaseMap`, `_BumpMap`,
`_Smoothness`). This is the correct path (NOT the glTF Shader Graph used by the NYCity GLB,
and NOT hardcoded Standard). Do not touch the city's CityReskin path.

**Mobile budget check:** ~3.5–4k tris/char × on-screen crowd. At a typical ~10–20 visible
peds that's ~40–80k tris for the crowd — comfortably in mobile budget; animation is culled
off-screen (`CullUpdateTransforms`). 2K albedo+normal per character is acceptable for the few
*sharp* peds; if memory pressure shows in-engine, clamp background-crowd textures to 1024 (the
DoF blur hides it). **No poly-bomb risk in Option A.** Option B (Meshy 15–20k hero peds) stays
in budget only if limited to 2–3 sharp foreground figures, not the whole crowd.

**Hand-off:** this is a gameplay/character-section change (touches `GiantPedestrian.cs` — the
hero hazard), so it should run through the normal verify-and-ship loop (compile → bot
playthrough reaches safe zone → invariant check incl. "a planted foot still squishes" → 0
console errors → proof frame), then re-run the Gemini tester with `--state run` to confirm the
"placeholder pedestrian" gap closed against `run_target.png`. Log the final license/source for
both packs in `ATTRIBUTION.md`.
