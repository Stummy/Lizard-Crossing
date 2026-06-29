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

            // Fixed portrait 9:16 capture (the Game view is set to 9:16). We render the GAME CAMERA
            // explicitly to this RenderTexture each frame (in the loop below) rather than grabbing the
            // screen backbuffer — see the capture note there for why.
            int W = 720, H = 1280;
            var cam = Camera.main;
            if (cam == null) { Debug.LogError("[RunRecorder] no Camera.main to record"); Destroy(gameObject); yield break; }
            var capRt = new RenderTexture(W, H, 24);

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

                    // Render the game camera straight to capRt. This forces a REAL render every frame
                    // regardless of whether the Game-view tab is repainting or the editor is foreground.
                    // (ScreenCapture only grabs the screen backbuffer, which goes stale when the editor
                    // is backgrounded — that's why earlier clips were a single frozen frame. No more
                    // WaitForEndOfFrame either: it can stall when nothing is presenting.) The HUD is a
                    // Screen-Space-Overlay canvas, so it isn't camera-rendered — the clip shows the WORLD
                    // without HUD, which is what we want for judging framing/look/motion. The RT path
                    // renders a touch brighter than the device (docs/CAPTURE_RECIPE.md) — weight tone loosely.
                    var prevTarget = cam.targetTexture; var prevActive = RenderTexture.active;
                    cam.targetTexture = capRt; cam.Render();
                    RenderTexture.active = capRt;
                    var frame = new Texture2D(W, H, TextureFormat.RGBA32, false);
                    frame.ReadPixels(new Rect(0, 0, W, H), 0, 0); frame.Apply();
                    cam.targetTexture = prevTarget; RenderTexture.active = prevActive;
                    encoder.AddFrame(frame);
                    Object.DestroyImmediate(frame);
                    frames++;

                    if (gm.State == GameState.Won || gm.State == GameState.Dead) break;
                }
            }
            finally
            {
                encoder.Dispose();
                InputProvider.MoveOverride = null;
                RenderTexture.active = null;
                if (capRt != null) Object.DestroyImmediate(capRt);
            }
            Debug.Log("[RunRecorder] VIDEO DONE — " + frames + " frames @ " + VideoFps + "fps (" + W + "x" + H + ") -> Temp/Recording/run.mp4");
            Destroy(gameObject);
#else
            Debug.LogWarning("[RunRecorder] MP4 export is editor-only.");
            Destroy(gameObject);
            yield break;
#endif
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
