using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LizardCrossing.EditorTools
{
    /// <summary>
    /// One-shot project setup: generates the Boot scene (a single Bootstrap
    /// GameObject — everything else is built at runtime), registers it in build
    /// settings, and applies player settings. Also the -executeMethod entry
    /// point for unattended batch setup.
    /// </summary>
    public static class ProjectSetup
    {
        private const string ScenePath = "Assets/Scenes/Boot.unity";
        private const string MenuScenePath = "Assets/Scenes/Menu.unity";

        [MenuItem("Lizard Crossing/Generate Scenes")]
        public static void GenerateScenes()
        {
            GenerateBootScene();
            GenerateMenuScene();
            // Menu is the entry point (index 0); Boot is the gameplay scene.
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true),
            };
            Debug.Log("[LizardCrossing] Scenes generated (Menu entry + Boot gameplay).");
        }

        public static void GenerateBootScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrapGo = new GameObject("Bootstrap");
            bootstrapGo.AddComponent<Bootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[LizardCrossing] Boot scene generated at " + ScenePath);
        }

        public static void GenerateMenuScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var menuGo = new GameObject("MenuBootstrap");
            menuGo.AddComponent<MenuBootstrap>();

            EditorSceneManager.SaveScene(scene, MenuScenePath);
            Debug.Log("[LizardCrossing] Menu scene generated at " + MenuScenePath);
        }

        /// <summary>
        /// Turns the white background of a Canva-exported logo transparent via an
        /// edge flood-fill (preserves interior whites like the mascot's eyes).
        /// Reads GeneratedArt/logo_raw.png, writes GeneratedArt/logo.png as a sprite.
        /// </summary>
        public static void ProcessLogo()
        {
            const string dir = "Assets/Resources/GeneratedArt";
            string rawPath = dir + "/logo_raw.png";
            string outPath = dir + "/logo.png";
            if (!System.IO.File.Exists(rawPath)) { Debug.LogError("[ProcessLogo] missing " + rawPath); return; }

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(System.IO.File.ReadAllBytes(rawPath));
            int w = tex.width, h = tex.height;
            var px = tex.GetPixels32();
            var visited = new bool[w * h];
            var stack = new System.Collections.Generic.Stack<int>();
            for (int x = 0; x < w; x++) { stack.Push(x); stack.Push((h - 1) * w + x); }
            for (int y = 0; y < h; y++) { stack.Push(y * w); stack.Push(y * w + w - 1); }

            const byte thresh = 234;
            int cleared = 0;
            while (stack.Count > 0)
            {
                int i = stack.Pop();
                if (visited[i]) continue;
                visited[i] = true;
                var c = px[i];
                if (c.r < thresh || c.g < thresh || c.b < thresh) continue; // hit the logo
                px[i] = new Color32(c.r, c.g, c.b, 0);
                cleared++;
                int x = i % w, y = i / w;
                if (x > 0) stack.Push(i - 1);
                if (x < w - 1) stack.Push(i + 1);
                if (y > 0) stack.Push(i - w);
                if (y < h - 1) stack.Push(i + w);
            }
            tex.SetPixels32(px);
            tex.Apply();
            System.IO.File.WriteAllBytes(outPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);
            var ti = AssetImporter.GetAtPath(outPath) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.SaveAndReimport();
            }
            Debug.Log(string.Format("[ProcessLogo] wrote {0} ({1}x{2}), cleared {3} bg px", outPath, w, h, cleared));
        }

        /// <summary>Logs whether each imported model under Resources/Models loads.</summary>
        public static void VerifyModels()
        {
            AssetDatabase.Refresh();
            string[] keys = { "lizard", "sneaker", "wheel", "drain", "trashbin", "chair", "sign", "planter" };
            foreach (var k in keys)
            {
                var go = Resources.Load<GameObject>("Models/" + k);
                if (go == null) { Debug.Log("[VerifyModels] (absent) " + k); continue; }
                var rends = go.GetComponentsInChildren<Renderer>(true);
                var bounds = new Bounds();
                bool has = false;
                foreach (var r in rends)
                {
                    if (!has) { bounds = r.bounds; has = true; }
                    else bounds.Encapsulate(r.bounds);
                }
                Debug.Log(string.Format("[VerifyModels] LOADED {0}: renderers={1} size=({2:0.00},{3:0.00},{4:0.00})",
                    k, rends.Length, bounds.size.x, bounds.size.y, bounds.size.z));
            }
        }

        /// <summary>
        /// Sets correct import settings on imported art: the pavement normal map
        /// must be NormalMap type (else lighting reads it as flat color), and the
        /// ground textures tile (Repeat). Safe to run repeatedly.
        /// </summary>
        public static void ConfigureArtImports()
        {
            ConfigureTexture("Assets/Resources/GeneratedArt/pavement_normal.jpg", TextureImporterType.NormalMap, true);
            ConfigureTexture("Assets/Resources/GeneratedArt/pavement_stone.jpg", TextureImporterType.Default, true);
            ConfigureTexture("Assets/Resources/GeneratedArt/wall_normal.jpg", TextureImporterType.NormalMap, true);
            ConfigureTexture("Assets/Resources/GeneratedArt/wall_stone.jpg", TextureImporterType.Default, true);
        }

        private static void ConfigureTexture(string path, TextureImporterType type, bool repeat)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) { Debug.LogWarning("[ConfigureArtImports] no importer for " + path); return; }
            ti.textureType = type;
            ti.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            ti.maxTextureSize = 2048;
            ti.SaveAndReimport();
            Debug.Log("[ConfigureArtImports] " + path + " -> " + type + (repeat ? " (repeat)" : ""));
        }

        public static void BatchSetup()
        {
            ConfigureArtImports();
            GenerateBootScene();
            GenerateMenuScene();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true),
            };

            PlayerSettings.productName = "Lizard Crossing";
            PlayerSettings.companyName = "LizardCrossing";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            AssetDatabase.SaveAssets();
            Debug.Log("[LizardCrossing] Batch setup complete.");
        }
    }
}
