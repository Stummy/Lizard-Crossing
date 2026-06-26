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
            sun.color = new Color(1f, 0.98f, 0.95f);   // 1,0.89,0.70 (golden) -> near-white daylight key
            // EXPOSURE FIX (real game-view ScreenCapture truth, not the brighter cam.Render() RT):
            // at 1.28 the key drove even a capped 0.46-albedo pedestrian (0.46 x ~2.2 light) PAST 1.0,
            // so the peds + the lizard's light dorsal clipped to glowing WHITE in the actual frame
            // (Gemini's persistent "white blob / not emerald" read — which the RT captures hid). Pull
            // the key to 1.0 so lit mid-tones land below clip; the warm grade keeps the golden look.
            sun.intensity = 1.1f;                        // 1.0->1.1: restore avenue brightness (the skybox tame removed the blow-risk)
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.55f;                  // soft, warm-ish (the warm grade tints the shadow too)
            sunGo.transform.rotation = Quaternion.Euler(34f, SunYaw, 0f); // LOW golden-hour rake (was 48° = high/flat)

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
            fill.color = new Color(0.82f, 0.86f, 0.92f); // 0.62,0.72,0.86 (cyan) -> near-neutral, faint cool
            // EXPOSURE-EVENNESS FIX: nudged 0.30->0.38 so a giant ped/building that occludes the warm
            // key gets enough wrap light to read as a shaded shape instead of going to "pure black"
            // (Gemini's under-exposed end of the flicker). Still well below the key, so lit planes
            // keep their warm-vs-cool contrast.
            fill.intensity = 0.38f;
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
                if (skybox.HasProperty("_Exposure")) skybox.SetFloat("_Exposure", 0.88f); // 0.85->0.88: a touch brighter sky so it reads day, not dusk
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
                RenderSettings.ambientIntensity = 0.85f;
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
            QualitySettings.shadowDistance = 95f;

            // --- world (HazardLaneManager now spawns the giant pedestrians as the
            //     walking cross-traffic hazard, replacing the old static humans) ---
            var levelRoot = LevelBuilder.Build(gm.Level);
            HazardLaneManager.Build(levelRoot, gm.Level);

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
