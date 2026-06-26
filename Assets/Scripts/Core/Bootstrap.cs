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
            sun.color = new Color(1f, 0.95f, 0.84f);   // warm midday key
            sun.intensity = 1.18f;                       // bright but the HDRI ambient does a lot of the lift
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.62f;                  // soft, not crushed-black contact shadows
            sunGo.transform.rotation = Quaternion.Euler(48f, SunYaw, 0f); // high sun, raked off the run axis

            // Cool fill from the opposite side keeps the shadow side from going muddy and
            // reads the lizard's form. WO-7 (anti-silhouette): raised 0.22→0.36 so when a
            // giant pedestrian leg / boot occludes the warm key, the hero still gets a
            // wrap-around light from a DIFFERENT angle and never sinks to a black silhouette.
            // Kept cool + shadowless so it doesn't flatten the warm key contrast on lit
            // surfaces (the sun still owns the highlight side).
            var fillGo = new GameObject("Fill");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.66f, 0.76f, 0.95f);
            fill.intensity = 0.36f;
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
                RenderSettings.skybox = skybox;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
                // WO-7 (anti-silhouette): nudged 1.0→1.12 so the sky-derived SH ambient gives
                // the hero a slightly higher light floor under heavy occlusion (giant leg blocks
                // the key) without washing out the grade. Small, deliberate — the fill light does
                // the directional work; this just keeps the darkest shaded surfaces off pure black.
                RenderSettings.ambientIntensity = 1.12f;
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
