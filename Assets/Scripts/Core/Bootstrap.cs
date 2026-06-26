using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Composition root (docs/DECISIONS.md D16): the Boot scene contains only
    /// this component; everything else — systems, level, player, camera, HUD —
    /// is constructed here in dependency order. Scene reload = full clean reset.
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        private void Awake()
        {
            GameEvents.Clear();
            InputProvider.Reset();

            // --- systems ---
            var systems = new GameObject("GameSystems");
            var gm = systems.AddComponent<GameStateManager>();
            gm.Init(new StubAdService());   // real ad SDK swaps in here (launch phase)
            gm.Level = LevelDefinition.GardenEscape();
            systems.AddComponent<TimeEffects>().Init();
            systems.AddComponent<GameAudio>().Init();
            systems.AddComponent<ParticleFx>().Init();

            // --- lighting: warm key sun raked OFF the forward (+Z) run axis + image-based
            //     ambient. READABILITY (owner call + 2 Gemini video reviews): with the old
            //     yaw the lizard auto-ran straight INTO the low sun, so the bright HDRI sun
            //     disc sat dead-ahead and glared the whole path into a white wall while
            //     pedestrians flattened to silhouettes against it. We now swing the sun to
            //     the SIDE-rear (SunYaw) AND spin the HDRI sky to match (SkyRotation, below)
            //     so the sun disc leaves the forward view — the glare wall is gone and peds
            //     get a lit side + a shadow side, so their shape/distance finally read.
            //     SunYaw/SkyRotation are tuned by eye from in-engine captures; co-rotate them
            //     together to keep shadows agreeing with the visible sun.
            const float SunYaw = 40f;        // key rakes from the left-rear (was -128 = dead-ahead)
            const float SkyRotation = 150f;  // spin the HDRI so its bright sun sits left/behind, out of +Z view
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            // GOLDEN-HOUR pass (Gemini #1 gap: the run read cold/dark "night/evening", peds glowed
            // white, hero green didn't pop — when the concept demands warm golden-hour sun). The
            // prior key was a near-neutral midday white (1,0.95,0.84) at a high 48° elevation, which
            // with the cool fill + cool HDRI ambient netted a COLD grey frame. Warm the key toward a
            // low golden sun and DROP its elevation so it rakes long and reads as late-afternoon gold.
            // v2 (Gemini re-review): the v1 sun (1,0.83,0.58) was too ORANGE — as the dominant light
            // it painted the whole avenue monochrome yellow. Golden, but pulled back toward a
            // late-afternoon gold so lit stone reads warm-grey, not orange.
            // OWNER COLOR OVERRIDE 2026-06-26: golden-hour sun REJECTED → clean NEUTRAL DAYLIGHT key.
            // The warm sun was the dominant source painting the avenue golden; make it near-white so the
            // frame reads as true-to-life daytime and the emerald lizard pops on its own colour.
            // REALISM PASS 2026-06-26: a hair cool-white (not the warm 0.98,0.95) so the daylight reads
            // clean midday, never yellow. The cool whisper offsets any residual warm cast from textures.
            sun.color = new Color(1f, 1f, 0.99f);        // near-pure white, faintly cool — kills the "yellow"
            // REALISM PASS — KEY/AMBIENT REBALANCE (the #1 grounding + realism lever): the prior stack
            // (sun 1.1 + FLAT ambient 1.25) had ambient doing most of the lighting, which floods every
            // crevice, flattens form and makes grounded contact shadows impossible (nothing reads as
            // sitting ON the ground). We invert that: DROP flat ambient to 0.55 and let the DIRECTIONAL
            // sun carry the lit surfaces (1.1->1.45). Strong key + low ambient = real directional
            // modelling, real shadows, real depth. The skybox-exposure pull below + ACES keep the lit
            // 0.4-0.5-albedo surfaces (incl. the capped peds + the emerald hero) just under clip so
            // nothing blows to the white blobs the real MP4 showed.
            sun.intensity = 1.45f;                       // 1.1->1.45: sun now dominates (ambient dropped to 0.55)
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.82f;                  // 0.55->0.82: real, readable contact shadows that GROUND everything
            sunGo.transform.rotation = Quaternion.Euler(38f, SunYaw, 0f); // 34->38: a touch higher so shadows aren't kilometre-long at the low POV

            // Cool fill from the opposite side keeps the shadow side from going muddy and
            // reads the lizard's form. WO-7 (anti-silhouette): raised 0.22→0.36 so when a
            // giant pedestrian leg / boot occludes the warm key, the hero still gets a
            // wrap-around light from a DIFFERENT angle and never sinks to a black silhouette.
            // Kept cool + shadowless so it doesn't flatten the warm key contrast on lit
            // surfaces (the sun still owns the highlight side).
            var fillGo = new GameObject("Fill");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            // GOLDEN-HOUR pass: the old deep-blue fill (0.66,0.76,0.95) was the main culprit in the
            // COLD cast — it dumped saturated blue onto the shadow side AND the broad ground plane, so
            // the whole frame read evening/cold. Soften it to a gentle sky-cyan (less blue, lifted
            // toward white) so it still keeps the shadow side off pure black + reads the hero's form,
            // but no longer fights the warm golden key. Slightly lower so the sun clearly owns the look.
            // OWNER COLOR OVERRIDE 2026-06-26: drop the cyan push toward a clean daylight balance. A faint
            // cool-neutral fill still reads the hero's shadow side without tinting the frame blue.
            fill.color = new Color(0.74f, 0.80f, 0.92f); // cool sky-bounce: lifts the shadow side toward a
                                                         // believable blue-sky fill (real shadows aren't grey)
            // REALISM PASS: with flat ambient dropped hard (1.25->0.55) the shadow side relies on this
            // DIRECTIONAL fill instead of flat flooding — directional fill models the form (gives the
            // shadow side a gradient) instead of washing it flat. Kept modest so the sun clearly owns the
            // lit side and the key-vs-fill contrast that reads as "sunny" survives.
            fill.intensity = 0.50f; // shadow-side modelling; lifts the camera-facing shadow sides (and replaces
                                    // the wrap the now-disabled streetlamp lights used to throw) without flooding
            fill.shadows = LightShadows.None;
            fillGo.transform.rotation = Quaternion.Euler(40f, SunYaw + 180f, 0f); // opposite the key (keeps the 180° relationship)

            // --- environment: image-based skybox + ambient (no baked GI; the world is
            //     built at runtime so we light it from the HDRI sky every session). The
            //     skybox material ships in Resources/Sky and uses the staged 2K HDRI. ---
            var skyboxAsset = Resources.Load<Material>("Sky/PureSkySkybox");
            if (skyboxAsset != null)
            {
                // instance the material so spinning the HDRI at runtime never dirties the shared asset
                var skybox = new Material(skyboxAsset);
                skybox.SetFloat("_Rotation", SkyRotation); // swing the baked sun disc off the +Z forward view
                // EXPOSURE-EVENNESS FIX (Gemini: the clip "flashes between blown-out white and pure
                // black" — the bright HDRI sky/sun is the hot end). Pull the skybox exposure down so
                // when the open sky/sun fills frame it no longer blows to white; this squeezes the
                // bright frames toward the shadowed ones for an EVEN exposure across the run. A soft
                // cyan tint keeps the "soft cyan sky" read without a saturated electric blue.
                // REALISM PASS: the real MP4 showed the distant sky / between-building gaps blowing to
                // pure-white BLOBS (the avenue-end explosion in before_4, the white pillar in before_140).
                // The 1.05 HDRI sky is brighter than the lit ground, so ACES can't pull it back. Drop it
                // so the sky reads as SKY (a bright surface) not a light source — the blobs go away and the
                // city behind them becomes legible. Pairs with the stronger sun owning the foreground.
                if (skybox.HasProperty("_Exposure")) skybox.SetFloat("_Exposure", 0.78f); // 1.05->0.78: kill the blown-white sky/gap blobs
                // OWNER COLOR OVERRIDE 2026-06-26: clean daylight sky. The cyan-leaning tint is pulled
                // toward a near-neutral pale blue-grey so the sky reads as true daytime, not a pushed
                // cyan that would lean the whole ambient blue. Still slightly blue (a real sky is), but
                // balanced — paired with the neutral key/grade this gives a clean true-to-life daylight.
                if (skybox.HasProperty("_Tint")) skybox.SetColor("_Tint", new Color(0.80f, 0.82f, 0.84f)); // 0.66,0.78,0.82 -> near-neutral pale sky
                RenderSettings.skybox = skybox;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
                // GOLDEN-HOUR pass: the 1.12 sky-derived ambient was over-lighting the pedestrians'
                // near-white albedo (the #1 "glowing white blob" read — flat sky-fill blows them out
                // before any sun/shadow can model them) AND pouring cool HDRI blue into the frame.
                // Pull it to 0.85 so the figures get MODELLED by the warm directional key instead of
                // washed flat by ambient, and the cool cast drops. The grade's postExposure lift +
                // the brighter warm key keep the avenue from going dark — net result is even, warm,
                // and the peds read as solid shaded people, not light blobs. Fill light keeps the
                // hero off pure-black under heavy occlusion, so lowering ambient is safe.
                // EXPOSURE FIX: ambient stacks ON TOP of the key, so a bright ambient was the other
                // half of the ped/hero white-clip. Pull to 0.78 — still keeps the cool cyan sky-light
                // that lifts shadows + breaks the warm-monochrome, but low enough that a lit surface
                // no longer sums past 1.0. Fill light still keeps the hero off pure-black under
                // occlusion. Net: peds/hero read as solid SHADED colour, not blown white.
                // REALISM PASS — the grounding lever: flat sky ambient at 1.25 was flooding every surface
                // and crevice with even fill, which is exactly why the frame read FLAT/BLOCKY and nothing
                // looked grounded (you can't have a real contact shadow when ambient relights it). Drop it
                // hard so the directional sun + its shadows do the modelling. The stronger sun (1.45) and
                // the directional cool fill (0.42) keep the occluded mid-run from going muddy, while the
                // shadowed sides finally read as shadow. This is the single biggest "realistic" move here.
                RenderSettings.ambientIntensity = 0.90f; // 1.25->0.90: trim the flat wash so the sun models form
                                                          // and shadows read, but keep enough sky-fill that the
                                                          // shadow-side surfaces the camera faces (building fronts,
                                                          // the lizard) don't fall to muddy dark on the real render
                DynamicGI.UpdateEnvironment(); // compute SH ambient from the skybox once
            }
            else
            {
                // fallback if the skybox asset is missing: keep a saturated gradient so
                // the game still reads as a sunny day instead of going black.
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.62f, 0.78f, 0.98f);
                RenderSettings.ambientEquatorColor = new Color(0.70f, 0.70f, 0.62f);
                RenderSettings.ambientGroundColor = new Color(0.34f, 0.28f, 0.22f);
            }
            // REALISM PASS: pull the shadow distance in from 95m to 42m. At the ~3cm POV the contact
            // shadows under the lizard/peds/props are what sell "grounded" — and a 2048 shadowmap spread
            // over 95m wastes texels on the far skyline where shadows barely read. Concentrating it to 42m
            // (with 4 cascades weighted near the camera) packs far more resolution into the near field, so
            // the contact shadows go crisp instead of soft-mush. Far buildings lose their (faint) shadow,
            // which is invisible at this scale. Mobile-friendly: same 2048 atlas, just denser where it counts.
            QualitySettings.shadowDistance = 42f;

            // --- world (HazardLaneManager now spawns the giant pedestrians as the
            //     walking cross-traffic hazard, replacing the old static humans) ---
            var levelRoot = LevelBuilder.Build(gm.Level);
            HazardLaneManager.Build(levelRoot, gm.Level);

            // REALISM PASS — kill the stray prop lights that wrecked the daytime render. The imported
            // streetlamp furniture prefab bakes its own NIGHT lighting: a Point light at intensity 8000
            // (range 10m) AND a duplicate Directional "FillSun" — six lamps line the curb, so the scene
            // carried ~6 blown-white point hotspots (the white BLOBS the real MP4 showed down the avenue)
            // PLUS 6 extra directional lights fighting our key/fill balance. We light the world from ONE
            // sun + ONE fill; lamps are unlit during day. Disable every Light under the level root except
            // ours, so the daytime scene is lit only by the intended rig. (Prefab untouched — a future
            // night theme can re-enable these.)
            foreach (var stray in levelRoot.GetComponentsInChildren<Light>(true))
            {
                if (stray == sun || stray == fill) continue;
                stray.enabled = false;
            }

            // --- player ---
            var playerGo = new GameObject("Lizard");
            // start at the centre of the straight sidewalk corridor, on the sidewalk surface
            playerGo.transform.position = new Vector3(GameConst.CorridorCenterX, StreetGround.SidewalkY, 2f);
            playerGo.AddComponent<CharacterController>();
            var player = playerGo.AddComponent<PlayerController>();
            player.Init();

            // --- predator: the alley cat sleeps until the lizard first bumps a
            //     pedestrian (GameStateManager.FootBump → CatProvoked). Only then does
            //     it spawn behind (-Z) and give chase; closing to striking range costs
            //     tail/heart. One-shot — Predator.Instance guards against re-spawn, and
            //     GameEvents.Clear() drops this handler on the next scene load. ---
            GameEvents.CatProvoked += () =>
            {
                if (Predator.Instance == null)
                    Predator.Spawn(levelRoot, playerGo.transform);
            };

            // --- camera + HUD ---
            LizardCameraController.Create(playerGo.transform);
            SimpleHUDController.Create();
        }

        private void Update()
        {
            InputProvider.Tick();

            // QUALITY_BAR screenshot test helper (editor/dev only)
            if (Input.GetKeyDown(KeyCode.F12))
            {
                string file = string.Format("screenshot_{0:yyyyMMdd_HHmmss}.png", System.DateTime.Now);
                ScreenCapture.CaptureScreenshot(file);
                Debug.Log("[LizardCrossing] Screenshot saved to project root: " + file);
            }
        }
    }
}
