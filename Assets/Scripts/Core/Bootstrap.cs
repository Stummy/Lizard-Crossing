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

            // --- lighting (WO-1): warm key sun aligned to the HDRI sun + image-based
            //     ambient. The qwantani_puresky HDRI puts its sun high and to one side;
            //     the directional light is rotated to match so contact shadows agree
            //     with the baked-in sky highlight. Warm colour, soft shadows. ---
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.84f);   // warm midday key
            sun.intensity = 1.18f;                       // bright but the HDRI ambient does a lot of the lift
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.62f;                  // soft, not crushed-black contact shadows
            // pitch 48° (high sun), yaw -128° so the key rakes from the upper-right/behind,
            // matching the puresky sun disc — gives the lizard a readable lit/shadow side.
            sunGo.transform.rotation = Quaternion.Euler(48f, -128f, 0f);

            // gentle cool fill from the opposite side keeps the shadow side from going
            // muddy and reads the lizard's form (the HDRI ambient is the primary fill).
            var fillGo = new GameObject("Fill");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.66f, 0.76f, 0.95f);
            fill.intensity = 0.22f;
            fill.shadows = LightShadows.None;
            fillGo.transform.rotation = Quaternion.Euler(40f, 52f, 0f);

            // --- environment: image-based skybox + ambient (no baked GI; the world is
            //     built at runtime so we light it from the HDRI sky every session). The
            //     skybox material ships in Resources/Sky and uses the staged 2K HDRI. ---
            var skybox = Resources.Load<Material>("Sky/PureSkySkybox");
            if (skybox != null)
            {
                RenderSettings.skybox = skybox;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
                RenderSettings.ambientIntensity = 1.0f;
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
