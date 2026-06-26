using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LizardCrossing.Testing
{
    /// <summary>
    /// Records a bot run as a dense CONTACT SHEET — a grid of frames sampled across the run,
    /// CAPTURED WITH THE HUD OVERLAY (ScreenCapture), so the whole playthrough incl. the damage
    /// flash / heart-shatter / pops can be reviewed in one image against the concept deck. Can
    /// force a hit mid-run so the juice actually shows up in the footage. Dev tool.
    /// </summary>
    public class RunRecorder : MonoBehaviour
    {
        public int Frames = 30;
        public float Interval = 0.12f;     // game-seconds between samples (~8 fps)
        public int HitAtFrame = -1;        // force a foot-bump on this frame to show the damage juice
        public int CellW = 200, Cols = 6;
        private int _cellH = 356;

        // ---- MP4 video mode (LaunchVideo): a real H.264 clip for upload to a video-understanding
        //      model (e.g. Gemini) so MOTION/timing/jank can be critiqued, not just stills. Uses the
        //      editor's built-in MediaEncoder — no extra package. Editor-only (dev tool). ----
        public float VideoSeconds = 0f;
        public int VideoFps = 30;

        private readonly List<Texture2D> _shots = new List<Texture2D>();

        public static void Launch(int frames, float interval, int hitAtFrame)
        {
            var go = new GameObject("RunRecorder");
            DontDestroyOnLoad(go);
            var r = go.AddComponent<RunRecorder>();
            r.Frames = Mathf.Max(4, frames);
            r.Interval = Mathf.Max(0.03f, interval);
            r.HitAtFrame = hitAtFrame;
            r.StartCoroutine(r.Record());
        }

        /// <summary>Record a real MP4 (H.264) of a bot run to Temp/Recording/run.mp4 — for
        /// uploading to a video model (Gemini etc.) to critique motion/feel, not just stills.</summary>
        public static void LaunchVideo(float seconds, int fps)
        {
            var go = new GameObject("RunRecorderVideo");
            DontDestroyOnLoad(go);
            var r = go.AddComponent<RunRecorder>();
            r.VideoSeconds = Mathf.Max(1f, seconds);
            r.VideoFps = Mathf.Clamp(fps, 12, 60);
            r.StartCoroutine(r.RecordVideo());
        }

        private IEnumerator RecordVideo()
        {
#if UNITY_EDITOR
            float guard = 0f;
            while ((GameStateManager.Instance == null || GameStateManager.Instance.State != GameState.Ready) && guard < 8f)
            { guard += Time.unscaledDeltaTime; yield return null; }
            var gm = GameStateManager.Instance;
            if (gm == null) { Debug.LogError("[RunRecorder] no GameStateManager"); Destroy(gameObject); yield break; }

            string dir = System.IO.Path.Combine(Application.dataPath, "..", "Temp", "Recording");
            System.IO.Directory.CreateDirectory(dir);
            string path = System.IO.Path.Combine(dir, "run.mp4");
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);

            InputProvider.StartOverride = true;

            // size from the first rendered frame, preserving the Game-view aspect but scaling the
            // LONGER side to ~1280 so the H.264 encode has room to stay crisp (a tiny source res
            // makes the encoder produce blocky "static"); force even dims (H.264 needs even w/h).
            yield return new WaitForEndOfFrame();
            var probe = ScreenCapture.CaptureScreenshotAsTexture();
            float aspect = (float)probe.width / Mathf.Max(1, probe.height);
            int W, H;
            if (aspect >= 1f) { W = 1280; H = Mathf.RoundToInt(1280f / aspect); }
            else { H = 1280; W = Mathf.RoundToInt(1280f * aspect); }
            W = Mathf.Clamp(W, 320, 1920); H = Mathf.Clamp(H, 320, 1920);
            if ((W & 1) == 1) W++;
            if ((H & 1) == 1) H++;
            Object.DestroyImmediate(probe);

            var attrs = new UnityEditor.Media.VideoTrackAttributes
            {
                frameRate = new UnityEditor.Media.MediaRational(VideoFps),
                width = (uint)W,
                height = (uint)H,
                includeAlpha = false,
                bitRateMode = UnityEditor.VideoBitrateMode.High // crisp encode, no compression static
            };

            int frames = 0;
            var encoder = new UnityEditor.Media.MediaEncoder(path, attrs);
            try
            {
                float frameDt = 1f / VideoFps;
                float acc = frameDt;       // capture the first frame immediately
                float elapsed = 0f;
                while (elapsed < VideoSeconds)
                {
                    yield return null;
                    float dt = Time.unscaledDeltaTime;
                    elapsed += dt; acc += dt;
                    if (gm.State == GameState.Playing) Steer();
                    if (acc < frameDt) continue;   // fixed-cadence sampling so playback timing is correct
                    acc -= frameDt;

                    yield return new WaitForEndOfFrame();
                    var full = ScreenCapture.CaptureScreenshotAsTexture(); // includes HUD overlay
                    var frame = DownscaleRGBA(full, W, H);
                    encoder.AddFrame(frame);
                    Object.DestroyImmediate(full);
                    Object.DestroyImmediate(frame);
                    frames++;

                    if (gm.State == GameState.Won || gm.State == GameState.Dead) break;
                }
            }
            finally
            {
                encoder.Dispose();
                InputProvider.MoveOverride = null;
            }
            Debug.Log("[RunRecorder] VIDEO DONE — " + frames + " frames @ " + VideoFps + "fps (" + W + "x" + H + ") -> Temp/Recording/run.mp4");
            Destroy(gameObject);
#else
            Debug.LogWarning("[RunRecorder] MP4 export is editor-only.");
            Destroy(gameObject);
            yield break;
#endif
        }

        // RGBA32 copy at a fixed size — MediaEncoder.AddFrame wants a readable, even-sized texture.
        private static Texture2D DownscaleRGBA(Texture2D src, int w, int h)
        {
            var rt = RenderTexture.GetTemporary(w, h, 0);
            var prev = RenderTexture.active;
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            var dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
            dst.ReadPixels(new Rect(0, 0, w, h), 0, 0); dst.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return dst;
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

                // force a hit so the damage flash + heart-shatter appear in the footage
                if (captured == HitAtFrame && gm.State == GameState.Playing)
                {
                    var p = PlayerController.Instance;
                    if (p != null) gm.FootBump(p.transform.position + Vector3.forward * 0.2f);
                }

                yield return new WaitForEndOfFrame();
                var full = ScreenCapture.CaptureScreenshotAsTexture(); // includes HUD overlay
                if (captured == 0)
                {
                    _cellH = Mathf.Clamp(Mathf.RoundToInt(CellW * (float)full.height / Mathf.Max(1, full.width)), 120, 520);
                }
                _shots.Add(Downscale(full, CellW, _cellH));
                if (captured % 6 == 0) System.IO.File.WriteAllBytes(
                    System.IO.Path.Combine(dir, "ui_frame_" + captured.ToString("D2") + ".png"),
                    Downscale(full, 360, Mathf.RoundToInt(360f * full.height / Mathf.Max(1, full.width))).EncodeToPNG());
                Object.DestroyImmediate(full);

                captured++;
                if (gm.State == GameState.Won || gm.State == GameState.Dead) break;
            }

            BuildContactSheet(dir);
            InputProvider.MoveOverride = null;
            Debug.Log("[RunRecorder] DONE — " + _shots.Count + " frames (UI) -> Temp/Recording/contact_sheet.png");
            foreach (var s in _shots) Object.DestroyImmediate(s);
            Destroy(gameObject);
        }

        private static Texture2D Downscale(Texture2D src, int w, int h)
        {
            var rt = RenderTexture.GetTemporary(w, h, 0);
            var prev = RenderTexture.active;
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            var dst = new Texture2D(w, h, TextureFormat.RGB24, false);
            dst.ReadPixels(new Rect(0, 0, w, h), 0, 0); dst.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return dst;
        }

        private void BuildContactSheet(string dir)
        {
            if (_shots.Count == 0) return;
            int rows = Mathf.CeilToInt(_shots.Count / (float)Cols);
            var grid = new Texture2D(Cols * CellW, rows * _cellH, TextureFormat.RGB24, false);
            grid.SetPixels32(new Color32[Cols * CellW * rows * _cellH]);
            for (int k = 0; k < _shots.Count; k++)
            {
                int col = k % Cols, row = k / Cols;
                grid.SetPixels(col * CellW, (rows - 1 - row) * _cellH, CellW, _cellH, _shots[k].GetPixels());
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

            // Also dodge cross-traffic so the bot survives the crosswalk crossings (otherwise it
            // dies at the first one and the clip never shows them to the Gemini tester). Edge away
            // from the nearest car sweeping across the lane just ahead, and DASH to punch through
            // when one is bearing down right at the crossing line.
            bool dash = false;
            foreach (var car in Object.FindObjectsByType<Car>(FindObjectsSortMode.None))
            {
                if (car == null || !car.gameObject.activeInHierarchy) continue;
                Vector3 d = car.transform.position - lp;
                if (d.z > -1.5f && d.z < 5f && Mathf.Abs(d.x) < 7f)
                {
                    if (Mathf.Approximately(steer, 0f)) steer = d.x > 0f ? -1f : 1f;   // slide to the side it isn't on
                    if (Mathf.Abs(d.x) < 2.4f && d.z > -0.5f && d.z < 2.6f) dash = true; // imminent → blast past
                }
            }
            if (dash) InputProvider.PressDash();

            if (Mathf.Approximately(steer, 0f)) steer = Mathf.Clamp((GameConst.CorridorCenterX - lp.x) * 0.5f, -0.6f, 0.6f);
            InputProvider.MoveOverride = new Vector2(Mathf.Clamp(steer, -1f, 1f), 0f);
        }
    }
}
