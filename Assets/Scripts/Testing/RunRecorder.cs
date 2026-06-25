using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LizardCrossing.Testing
{
    /// <summary>
    /// Records a bot run as a CONTACT SHEET — a grid of camera frames sampled across the run —
    /// plus a few full-res key frames, written to Temp/Recording/. Lets the whole playthrough be
    /// reviewed in one image (animation, framing, juice, visual quality) against the concept deck.
    ///
    /// Dev tool (launched from RunCommand / the Bot menu). Camera-only (no HUD overlay) so it
    /// compares directly to the Assets/Art/Concept frames; the HUD is reviewed separately.
    /// </summary>
    public class RunRecorder : MonoBehaviour
    {
        public int Frames = 20;
        public float Interval = 0.3f;          // game-seconds between samples
        public int CellW = 200, CellH = 356, Cols = 5;

        private readonly List<Texture2D> _shots = new List<Texture2D>();

        public static void Launch(int frames, float interval)
        {
            var go = new GameObject("RunRecorder");
            DontDestroyOnLoad(go);
            var r = go.AddComponent<RunRecorder>();
            r.Frames = Mathf.Max(4, frames);
            r.Interval = Mathf.Max(0.05f, interval);
            r.StartCoroutine(r.Record());
        }

        private IEnumerator Record()
        {
            float guard = 0f;
            while ((GameStateManager.Instance == null || GameStateManager.Instance.State != GameState.Ready) && guard < 8f)
            { guard += Time.unscaledDeltaTime; yield return null; }
            var gm = GameStateManager.Instance;
            if (gm == null) { Debug.LogError("[RunRecorder] no GameStateManager"); Destroy(gameObject); yield break; }

            string dir = System.IO.Path.Combine(Application.dataPath, "..", "Temp", "Recording");
            System.IO.Directory.CreateDirectory(dir);
            InputProvider.StartOverride = true;

            int captured = 0;
            while (captured < Frames)
            {
                float t = 0f;
                while (t < Interval)
                {
                    t += Time.deltaTime;
                    if (gm.State != GameState.Playing && gm.State != GameState.Ready) break;
                    yield return null;
                }
                if (gm.State == GameState.Playing) Steer();
                yield return new WaitForEndOfFrame();
                CaptureCell();
                if (captured % 4 == 0) CaptureFull(dir, captured);
                captured++;
                if (gm.State == GameState.Won || gm.State == GameState.Dead) break;
            }

            BuildContactSheet(dir);
            InputProvider.MoveOverride = null;
            Debug.Log("[RunRecorder] DONE — " + _shots.Count + " frames -> Temp/Recording/contact_sheet.png");
            foreach (var s in _shots) Object.DestroyImmediate(s);
            Destroy(gameObject);
        }

        private void CaptureCell()
        {
            var cam = Camera.main; if (cam == null) return;
            var rt = new RenderTexture(CellW, CellH, 24);
            var pT = cam.targetTexture; var pA = RenderTexture.active;
            cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
            var tex = new Texture2D(CellW, CellH, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, CellW, CellH), 0, 0); tex.Apply();
            cam.targetTexture = pT; RenderTexture.active = pA;
            Object.DestroyImmediate(rt);
            _shots.Add(tex);
        }

        private void CaptureFull(string dir, int idx)
        {
            var cam = Camera.main; if (cam == null) return;
            int w = 540, h = 960;
            var rt = new RenderTexture(w, h, 24);
            var pT = cam.targetTexture; var pA = RenderTexture.active;
            cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            cam.targetTexture = pT; RenderTexture.active = pA;
            Object.DestroyImmediate(rt);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "frame_" + idx.ToString("D2") + ".png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private void BuildContactSheet(string dir)
        {
            if (_shots.Count == 0) return;
            int rows = Mathf.CeilToInt(_shots.Count / (float)Cols);
            var grid = new Texture2D(Cols * CellW, rows * CellH, TextureFormat.RGB24, false);
            grid.SetPixels32(new Color32[Cols * CellW * rows * CellH]); // black fill for empty cells
            for (int k = 0; k < _shots.Count; k++)
            {
                int col = k % Cols, row = k / Cols;
                grid.SetPixels(col * CellW, (rows - 1 - row) * CellH, CellW, CellH, _shots[k].GetPixels());
            }
            grid.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "contact_sheet.png"), grid.EncodeToPNG());
            Object.DestroyImmediate(grid);
        }

        private void Steer()
        {
            var pc = PlayerController.Instance; if (pc == null) return;
            Vector3 lp = pc.transform.position; float steer = 0f, bestZ = 999f;
            foreach (var ped in Object.FindObjectsByType<GiantPedestrian>(FindObjectsSortMode.None))
            {
                Vector3 d = ped.transform.position - lp;
                if (d.z > 0.2f && d.z < 4f && Mathf.Abs(d.x) < 1.6f && d.z < bestZ) { bestZ = d.z; steer = d.x > 0f ? -1f : 1f; }
            }
            if (Mathf.Approximately(steer, 0f)) steer = Mathf.Clamp((GameConst.CorridorCenterX - lp.x) * 0.5f, -0.6f, 0.6f);
            InputProvider.MoveOverride = new Vector2(Mathf.Clamp(steer, -1f, 1f), 0f);
        }
    }
}
