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

            // --- lighting: warm late-afternoon sun, long readable shadows ---
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.93f, 0.76f);
            sun.intensity = 1.32f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.74f;
            sunGo.transform.rotation = Quaternion.Euler(38f, -34f, 0f);

            // cool fill from the opposite side gives the giant shoes & lizard form
            var fillGo = new GameObject("Fill");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.6f, 0.72f, 0.95f);
            fill.intensity = 0.35f;
            fill.shadows = LightShadows.None;
            fillGo.transform.rotation = Quaternion.Euler(40f, 150f, 0f);

            // tropical sky/ground ambient gradient: saturated, with depth
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.72f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.64f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.34f, 0.28f, 0.22f);
            QualitySettings.shadowDistance = 95f;

            // --- world (HazardLaneManager now spawns the giant pedestrians as the
            //     walking cross-traffic hazard, replacing the old static humans) ---
            var levelRoot = LevelBuilder.Build(gm.Level);
            HazardLaneManager.Build(levelRoot, gm.Level);

            // --- player ---
            var playerGo = new GameObject("Lizard");
            // start on the right sidewalk in the real-city street (else centered)
            float startX = GameObject.Find("NYCity") != null ? 6f : 0f;
            playerGo.transform.position = new Vector3(startX, 0.02f, 2f); // low: the lizard is ~0.04u tall
            playerGo.AddComponent<CharacterController>();
            var player = playerGo.AddComponent<PlayerController>();
            player.Init();

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
