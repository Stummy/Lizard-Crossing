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

            // --- world ---
            var levelRoot = LevelBuilder.Build(gm.Level);
            HazardLaneManager.Build(levelRoot, gm.Level);

            // --- giant pedestrians (real human models, CC0) — towering set dressing;
            //     they become moving hazards once walk animation is wired ---
            if (ModelLibrary.HasHuman)
            {
                PlaceHuman(levelRoot, new Vector3(6f, 0f, 74f), -90f);
                PlaceHuman(levelRoot, new Vector3(-6f, 0f, 96f), 90f);
                PlaceHuman(levelRoot, new Vector3(7f, 0f, 162f), -100f);
            }

            // --- player ---
            var playerGo = new GameObject("Lizard");
            playerGo.transform.position = new Vector3(0f, 0.1f, 2f);
            playerGo.AddComponent<CharacterController>();
            var player = playerGo.AddComponent<PlayerController>();
            player.Init();

            // --- camera + HUD ---
            LizardCameraController.Create(playerGo.transform);
            SimpleHUDController.Create();
        }

        private static void PlaceHuman(Transform parent, Vector3 pos, float yaw)
        {
            var h = ModelLibrary.TryBuildHuman(parent, 11f, yaw);
            if (h != null) h.position = pos;
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
