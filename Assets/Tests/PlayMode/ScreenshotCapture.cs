using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LizardCrossing.Tests
{
    /// <summary>
    /// QUALITY_BAR screenshot test harness (packet SELF_TESTING_PLAN sec.5):
    /// renders portrait frames of key moments to the project root so the
    /// composition can be reviewed against the quality bar without a device.
    /// Run via Test Runner or batch -runTests; PNGs land next to Assets/.
    /// </summary>
    public class ScreenshotCapture
    {
        private const int Width = 1080;
        private const int Height = 1920;

        [UnityTest]
        public IEnumerator CaptureQualityBarShots()
        {
            // clean hero by default (premium look) — reset any persisted cosmetics
            foreach (var id in new[] { "hat_none", "glasses_none", "pattern_none", "tail_default", "pack_none", "trail_none" })
                MetaProgress.Equip(id);
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var cam = Camera.main;
            Assert.IsNotNull(cam, "No main camera.");

            // include the HUD in captures: overlay canvases don't render to RT
            foreach (var canvas in Object.FindObjectsByType<Canvas>())
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = cam;
                    canvas.planeDistance = 1f;
                }
            }

            // 1) title / ready state
            yield return WaitFrames(10);
            Capture(cam, "shot_1_start.png");

            // 2) mid-level, foot traffic crossing nearby
            var gm = GameStateManager.Instance;
            Assert.IsNotNull(gm, "No GameStateManager.");
            gm.StartRun();
            yield return null;
            TeleportPlayer(new Vector3(1.5f, 0.1f, 62f));
            yield return WaitSeconds(0.8f); // camera settle
            yield return WaitForNearbyFootfall(62f, 10f);
            Capture(cam, "shot_2_midlevel.png");

            // 3) deep lanes, busier traffic
            TeleportPlayer(new Vector3(-2f, 0.1f, 150f));
            yield return WaitSeconds(0.8f);
            yield return WaitForNearbyFootfall(150f, 10f);
            Capture(cam, "shot_3_latelanes.png");

            // 4) approaching the garden safe zone
            TeleportPlayer(new Vector3(0f, 0.1f, 192f));
            yield return WaitSeconds(1.0f);
            Capture(cam, "shot_4_safezone.png");

            // 5) win / results card
            if (gm != null) gm.WinRun();
            yield return WaitFrames(24);
            Capture(cam, "shot_5_win.png");
        }

        private static void TeleportPlayer(Vector3 pos)
        {
            var player = PlayerController.Instance;
            Assert.IsNotNull(player, "No player.");
            var cc = player.GetComponent<CharacterController>();
            cc.enabled = false;
            player.transform.position = pos;
            cc.enabled = true;
        }

        private static IEnumerator WaitFrames(int frames)
        {
            for (int i = 0; i < frames; i++) yield return null;
        }

        /// <summary>
        /// Waits until a shoe is planted on the corridor ahead of the player -
        /// the hero composition (giant shoe + leg towering in frame).
        /// </summary>
        private static IEnumerator WaitForNearbyFootfall(float playerZ, float timeout)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (Time.realtimeSinceStartup < end)
            {
                foreach (var t in Object.FindObjectsByType<Transform>())
                {
                    if (t.name != "Shoe" || !t.gameObject.activeInHierarchy) continue;
                    Vector3 p = t.position;
                    if (p.y < 0.4f && Mathf.Abs(p.x) < 10f && p.z > playerZ + 2f && p.z < playerZ + 26f)
                        yield break;
                }
                yield return null;
            }
        }

        private static IEnumerator WaitSeconds(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        private static void Capture(Camera cam, string fileName)
        {
            var rt = new RenderTexture(Width, Height, 24);
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;

            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            Object.Destroy(rt);

            string path = Path.Combine(Application.dataPath, "..", fileName);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log("[ScreenshotCapture] saved " + Path.GetFullPath(path));
            Object.Destroy(tex);
        }
    }
}
