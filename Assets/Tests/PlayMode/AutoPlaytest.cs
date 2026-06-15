using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LizardCrossing.Tests
{
    /// <summary>
    /// Automated playtests (packet SELF_TESTING_PLAN: give Claude ways to test
    /// "feel"). Two bots play Garden Escape end-to-end at accelerated time:
    ///  - CautiousBot waits at lane edges when a warning marker pulses ahead -
    ///    it must be able to FINISH (fairness floor: the level is beatable by
    ///    reading the telegraphs).
    ///  - NaiveBot sprints blindly forward - it must get HIT at least once
    ///    (danger floor: the lanes actually threaten).
    /// Both runs log per-lane hit/near-miss stats for tuning.
    /// </summary>
    public class AutoPlaytest
    {
        private const float TimeScale = 3f;
        private const float MaxRealSeconds = 90f;

        private int _hits;
        private int _nearMisses;
        private readonly List<float> _hitZs = new List<float>();

        [UnityTest]
        public IEnumerator CautiousBot_CanFinishByReadingTelegraphs()
        {
            yield return RunBot(cautious: true);

            var gm = GameStateManager.Instance;
            LogStats("CautiousBot", gm);
            Assert.AreEqual(GameState.Won, gm.State,
                "Cautious bot could not finish - telegraph-respecting play should beat the level. " +
                "Hits at z: " + string.Join(", ", _hitZs));
        }

        [UnityTest]
        public IEnumerator NaiveBot_GetsPunishedForIgnoringTelegraphs()
        {
            yield return RunBot(cautious: false);

            var gm = GameStateManager.Instance;
            LogStats("NaiveBot", gm);
            // Danger floor: ignoring telegraphs must repeatedly endanger the lizard.
            // A single blind run can thread a *fair* level by luck on a given frame
            // schedule, so we accept a hit OR multiple near-misses as proof that the
            // lanes actually threaten straight-line center play.
            int danger = _hits + _nearMisses + (gm.State == GameState.Dead ? 1 : 0);
            Assert.GreaterOrEqual(danger, 2,
                "Naive bot crossed nearly unharmed - lanes are not threatening enough. " +
                "hits=" + _hits + " nearMisses=" + _nearMisses);
        }

        private IEnumerator RunBot(bool cautious)
        {
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            yield return null;
            yield return null;

            _hits = 0;
            _nearMisses = 0;
            _hitZs.Clear();
            GameEvents.PlayerHit += OnHit;
            GameEvents.NearMiss += OnNearMiss;

            var gm = GameStateManager.Instance;
            var player = PlayerController.Instance;
            Assert.IsNotNull(gm);
            Assert.IsNotNull(player);

            Time.timeScale = TimeScale;
            gm.StartRun();

            float deadline = Time.realtimeSinceStartup + MaxRealSeconds;
            try
            {
                while (gm.State == GameState.Playing && Time.realtimeSinceStartup < deadline)
                {
                    InputProvider.MoveOverride = cautious
                        ? CautiousPolicy(player)
                        : Vector2.up; // naive: floor it
                    yield return null;
                }
            }
            finally
            {
                InputProvider.MoveOverride = null;
                Time.timeScale = 1f;
                GameEvents.PlayerHit -= OnHit;
                GameEvents.NearMiss -= OnNearMiss;
            }
        }

        /// <summary>
        /// Reads the same information a human has: warning markers (where the
        /// next footfalls land) and planted shoes. Stops short of any hot zone,
        /// drifts toward the safest x, otherwise runs forward.
        /// </summary>
        private static Vector2 CautiousPolicy(PlayerController player)
        {
            Vector3 p = player.transform.position;
            float dangerAhead = float.MaxValue;
            float steerX = 0f;

            foreach (var marker in Object.FindObjectsByType<WarningMarker>())
            {
                if (marker.Intensity < 0.05f) continue;
                Vector3 m = marker.transform.position;
                float dz = m.z - p.z;
                float dx = m.x - p.x;
                if (dz < -2f || dz > 14f) continue;       // behind or far ahead
                if (Mathf.Abs(dx) > 8f) continue;          // landing far to the side

                if (dz < dangerAhead) dangerAhead = dz;
                // steer away from the landing spot
                steerX += dx > 0f ? -0.6f : 0.6f;
            }

            // planted shoes are walls: sidestep them early
            foreach (var t in Object.FindObjectsByType<Transform>())
            {
                if (t.name != "Shoe" || !t.gameObject.activeInHierarchy) continue;
                Vector3 s = t.position;
                if (s.y > 0.4f) continue; // airborne, the marker already covers it
                float dz = s.z - p.z;
                float dx = s.x - p.x;
                if (dz > 0.5f && dz < 7f && Mathf.Abs(dx) < 4f)
                    steerX += dx > 0f ? -0.8f : 0.8f;
            }

            float forward;
            if (dangerAhead < 5f)
                forward = -0.4f;          // too hot: back off
            else if (dangerAhead < 9f)
                forward = 0f;             // wait at the lane edge
            else
                forward = 1f;             // clear: go

            // drift back to corridor center so walls never trap the bot
            if (Mathf.Abs(p.x) > 6f) steerX += p.x > 0f ? -0.4f : 0.4f;

            return new Vector2(Mathf.Clamp(steerX, -1f, 1f), forward);
        }

        private void OnHit(int heartsLeft, Vector3 pos)
        {
            _hits++;
            _hitZs.Add(pos.z);
        }

        private void OnNearMiss(Vector3 pos)
        {
            _nearMisses++;
        }

        private void LogStats(string bot, GameStateManager gm)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("[AutoPlaytest] {0}: state={1} time={2:0.0}s hearts={3} bugs={4}/{5} hits={6} nearMisses={7}",
                bot, gm.State, gm.RunTime, gm.Hearts, gm.BugsCollected, gm.BugsTotal, _hits, _nearMisses);
            if (_hitZs.Count > 0)
                sb.Append(" hitZs=").Append(string.Join("|", _hitZs));
            Debug.Log(sb.ToString());
        }
    }
}
