using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LizardCrossing.Testing
{
    /// <summary>
    /// Autonomous playtest bot. Plays N full runs end-to-end with a simple dodge AI and logs
    /// telemetry — outcome, death cause, distance reached, time, bugs, hearts left, near-misses,
    /// frame time, and any console errors/exceptions — then writes a report to Temp/Playtest/.
    ///
    /// Dev tool only (launched from the Bot menu). Survives the scene reload between runs via
    /// DontDestroyOnLoad and re-acquires the singletons each run; GameEvents are cleared on
    /// reload, so it re-subscribes per run. Frame-time here is an editor signal, not a substitute
    /// for the on-device pass (S2-6) — runs are sped up via Time.timeScale.
    /// </summary>
    public class AutoPlaytest : MonoBehaviour
    {
        public int TargetRuns = 8;
        public float SpeedUp = 5f;

        private struct RunRecord
        {
            public bool won; public string cause; public float dist; public float goal; public float time;
            public int bugs; public int bugsTotal; public int heartsLeft; public int closeCalls;
            public int errors; public float frameMaxMs; public float frameAvgMs;
        }

        private readonly List<RunRecord> _runs = new List<RunRecord>();
        private int _errorsThisRun;
        private string _lastCause = "—";

        public static void Launch(int runs)
        {
            var go = new GameObject("AutoPlaytest");
            DontDestroyOnLoad(go);
            var ap = go.AddComponent<AutoPlaytest>();
            ap.TargetRuns = Mathf.Max(1, runs);
            ap.StartCoroutine(ap.Drive());
        }

        private void OnEnable() { Application.logMessageReceived += OnLog; }
        private void OnDisable() { Application.logMessageReceived -= OnLog; GameEvents.PlayerDied -= OnDied; }
        private void OnLog(string c, string s, LogType t) { if (t == LogType.Error || t == LogType.Exception) _errorsThisRun++; }
        private void OnDied(DeathCause cause) { _lastCause = cause.ToString(); }

        private IEnumerator Drive()
        {
            Time.timeScale = Mathf.Max(1f, SpeedUp);
            for (int i = 0; i < TargetRuns; i++)
                yield return RunOne();
            Time.timeScale = 1f;
            InputProvider.MoveOverride = null;
            WriteReport();
            Debug.Log("[AutoPlaytest] DONE — " + _runs.Count + " runs. Report: Temp/Playtest/report.txt");
            Destroy(gameObject);
        }

        private IEnumerator RunOne()
        {
            // wait for a fresh world in Ready state (after the previous run's scene reload)
            float guard = 0f;
            while ((GameStateManager.Instance == null || GameStateManager.Instance.State != GameState.Ready) && guard < 8f)
            { guard += Time.unscaledDeltaTime; yield return null; }
            var gm = GameStateManager.Instance;
            if (gm == null) yield break;

            // GameEvents are nulled on scene reload — re-subscribe for the death cause this run.
            GameEvents.PlayerDied -= OnDied; GameEvents.PlayerDied += OnDied;
            _errorsThisRun = 0; _lastCause = "—";
            float frameSum = 0f, frameMax = 0f; int frameCount = 0, maxZi = 0; float maxZ = 0f;

            InputProvider.StartOverride = true; // fire the start gate

            guard = 0f;
            while (gm.State == GameState.Ready || gm.State == GameState.Playing)
            {
                if (gm.State == GameState.Playing)
                {
                    Steer();
                    var pc = PlayerController.Instance;
                    if (pc != null) maxZ = Mathf.Max(maxZ, pc.transform.position.z);
                    float ms = Time.unscaledDeltaTime * 1000f;
                    frameSum += ms; frameCount++; if (ms > frameMax) frameMax = ms; maxZi++;
                }
                guard += Time.unscaledDeltaTime;
                if (guard > 90f) break; // safety: never hang on a stuck run
                yield return null;
            }

            InputProvider.MoveOverride = null;
            _runs.Add(new RunRecord
            {
                won = gm.State == GameState.Won,
                cause = gm.State == GameState.Won ? "WON" : _lastCause,
                dist = maxZ, goal = gm.Level != null ? gm.Level.Length : 0f,
                time = gm.RunTime, bugs = gm.BugsCollected, bugsTotal = gm.BugsTotal,
                heartsLeft = gm.Hearts, closeCalls = gm.CloseCalls, errors = _errorsThisRun,
                frameMaxMs = frameMax, frameAvgMs = frameCount > 0 ? frameSum / frameCount : 0f,
            });

            if (_runs.Count < TargetRuns) gm.Restart(); // reload for the next run
            yield return null;
        }

        /// <summary>Minimal survival AI: steer away from the nearest hazard in the lizard's path,
        /// else drift back toward the corridor centre; panic-dash through a very tight gap.</summary>
        private void Steer()
        {
            var pc = PlayerController.Instance;
            if (pc == null) return;
            Vector3 lp = pc.transform.position;
            float steer = 0f, bestZ = 999f;

            foreach (var ped in Object.FindObjectsByType<GiantPedestrian>(FindObjectsSortMode.None))
            {
                Vector3 d = ped.transform.position - lp;
                if (d.z > 0.2f && d.z < 4f && Mathf.Abs(d.x) < 1.6f && d.z < bestZ)
                { bestZ = d.z; steer = d.x > 0f ? -1f : 1f; }
            }
            foreach (var car in Object.FindObjectsByType<Car>(FindObjectsSortMode.None))
            {
                Vector3 d = car.transform.position - lp;
                if (d.z > 0.2f && d.z < 5f && Mathf.Abs(d.x) < 2.0f && d.z < bestZ)
                { bestZ = d.z; steer = d.x > 0f ? -1f : 1f; }
            }
            if (Mathf.Approximately(steer, 0f)) // clear — recentre on the run band
                steer = Mathf.Clamp((GameConst.CorridorCenterX - lp.x) * 0.5f, -0.6f, 0.6f);

            InputProvider.MoveOverride = new Vector2(Mathf.Clamp(steer, -1f, 1f), 0f);
            if (bestZ < 1.5f) InputProvider.PressDash();
        }

        private void WriteReport()
        {
            int wins = 0, totErr = 0; float fMax = 0f, fAvgSum = 0f, distFracSum = 0f;
            var causes = new Dictionary<string, int>();
            foreach (var r in _runs)
            {
                if (r.won) wins++;
                totErr += r.errors;
                if (r.frameMaxMs > fMax) fMax = r.frameMaxMs;
                fAvgSum += r.frameAvgMs;
                distFracSum += r.goal > 0f ? r.dist / r.goal : 0f;
                causes[r.cause] = causes.TryGetValue(r.cause, out var n) ? n + 1 : 1;
            }
            int N = Mathf.Max(1, _runs.Count);
            var sb = new StringBuilder();
            sb.AppendLine("LIZARD CROSSING - AUTO-PLAYTEST REPORT");
            sb.AppendLine("runs " + _runs.Count + " | wins " + wins + " (" + (100f * wins / N).ToString("F0") + "%) | console errors " + totErr);
            sb.AppendLine("avg distance reached: " + (100f * distFracSum / N).ToString("F0") + "% of goal");
            sb.AppendLine("frame time (editor, sped " + SpeedUp.ToString("F0") + "x): avg " + (fAvgSum / N).ToString("F1") + " ms, worst " + fMax.ToString("F1") + " ms");
            sb.AppendLine("outcomes:");
            foreach (var kv in causes) sb.AppendLine("  " + kv.Key + " x" + kv.Value);
            sb.AppendLine("--- per run ---");
            for (int i = 0; i < _runs.Count; i++)
            {
                var r = _runs[i];
                sb.AppendLine(string.Format(
                    "#{0,2} {1,-14} dist {2,5:F0}/{3:F0} ({4,3:F0}%)  t {5,4:F1}s  bugs {6}/{7}  hearts {8}  close {9}  err {10}  f(avg {11:F1} max {12:F1})ms",
                    i + 1, r.cause, r.dist, r.goal, r.goal > 0 ? 100f * r.dist / r.goal : 0f,
                    r.time, r.bugs, r.bugsTotal, r.heartsLeft, r.closeCalls, r.errors, r.frameAvgMs, r.frameMaxMs));
            }
            string dir = System.IO.Path.Combine(Application.dataPath, "..", "Temp", "Playtest");
            System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "report.txt"), sb.ToString());
        }
    }
}
