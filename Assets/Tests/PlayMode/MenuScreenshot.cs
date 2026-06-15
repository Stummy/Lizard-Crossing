using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LizardCrossing.Tests
{
    /// <summary>
    /// Renders the meta-UI screens (main menu, lizard shop, wardrobe) to PNGs so
    /// the front-end can be reviewed against the quality bar without a device.
    /// </summary>
    public class MenuScreenshot
    {
        private const int Width = 1080;
        private const int Height = 1920;

        [UnityTest]
        public IEnumerator CaptureMenuShots()
        {
            SceneManager.LoadScene("Menu", LoadSceneMode.Single);
            yield return null;
            yield return null;

            // give the player some currency so the shop shows buyable states
            MetaProgress.AddBugs(900);
            MetaProgress.AddGems(60);

            var camGo = new GameObject("CaptureCam");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.18f, 0.12f);
            cam.transform.position = new Vector3(0f, 0f, -10f);

            foreach (var canvas in Object.FindObjectsByType<Canvas>())
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
            }

            yield return WaitFrames(8);
            Capture(cam, "menu_1_main.png");

            InvokeButton("LizardsButton");
            yield return WaitFrames(8);
            ReattachCanvases(cam);
            Capture(cam, "menu_2_shop.png");

            InvokeButton("Close");
            yield return WaitFrames(4);
            InvokeButton("WardrobeButton");
            yield return WaitFrames(8);
            ReattachCanvases(cam);
            Capture(cam, "menu_3_wardrobe.png");
        }

        private static void ReattachCanvases(Camera cam)
        {
            foreach (var canvas in Object.FindObjectsByType<Canvas>())
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = cam;
                    canvas.planeDistance = 1f;
                }
            }
        }

        private static void InvokeButton(string name)
        {
            foreach (var b in Object.FindObjectsByType<Button>())
            {
                if (b.gameObject.name == name) { b.onClick.Invoke(); return; }
            }
            Debug.LogWarning("[MenuScreenshot] button not found: " + name);
        }

        private static IEnumerator WaitFrames(int frames)
        {
            for (int i = 0; i < frames; i++) yield return null;
        }

        private static void Capture(Camera cam, string fileName)
        {
            var rt = new RenderTexture(Width, Height, 24);
            var prevActive = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;

            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = prevActive;
            Object.Destroy(rt);

            string path = Path.Combine(Application.dataPath, "..", fileName);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log("[MenuScreenshot] saved " + Path.GetFullPath(path));
            Object.Destroy(tex);
        }
    }
}
