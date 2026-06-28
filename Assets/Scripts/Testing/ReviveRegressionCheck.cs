using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

namespace LizardCrossing.Testing
{
    /// <summary>
    /// Regression check for [R33]: a rewarded REVIVE must NOT instantly regrow the tail.
    ///
    /// The bug only bites when MORE than the autotomy regrow delay (GameConst.TailRegrowDelay)
    /// passes between the tail-drop hit and the revive — the real-world case is the player sitting
    /// on the death screen watching the rewarded ad. Before the fix, RequestRevive left _lastHitTime
    /// stale, so the very next Playing-frame's regrow check (Time.time - _lastHitTime >= delay) was
    /// already satisfied and the tail popped straight back. The fix resets _lastHitTime on revive.
    ///
    /// This drives the lizard to die TAIL-LESS, fast-forwards (timeScale) past the regrow delay while
    /// "on the death screen", revives, and asserts the tail stays dropped. Writes PASS/FAIL to
    /// Temp/Playtest/revive.txt. Run from Lizard Crossing/Bot/Revive Check (Play mode).
    /// </summary>
    public class ReviveRegressionCheck : MonoBehaviour
    {
        public static void Launch()
        {
            var go = new GameObject("ReviveRegressionCheck");
            go.AddComponent<ReviveRegressionCheck>();
        }

        private IEnumerator Start()
        {
            var gm = GameStateManager.Instance;
            if (gm == null) { Report(false, "no GameStateManager — enter Play mode first"); yield break; }

            var sb = new StringBuilder();
            bool pass = true;
            Vector3 hazard = Vector3.zero;

            // 1) start a run and die TAIL-LESS: the first hit drops the tail (autotomy, no heart
            //    lost), then drain every heart to reach death still tail-less.
            gm.StartRun();
            yield return null;
            gm.HitPlayer(hazard);                       // drops the tail
            for (int i = 0; i < gm.MaxHearts; i++)      // drain all hearts -> death
                gm.HitPlayer(hazard);
            yield return null;

            bool diedTailless = gm.State == GameState.Dead && !gm.HasTail && gm.Hearts == 0;
            pass &= diedTailless;
            sb.AppendLine("DIED TAIL-LESS: state=" + gm.State + " hasTail=" + gm.HasTail + " hearts=" + gm.Hearts
                + " -> " + (diedTailless ? "PASS" : "FAIL"));

            // 2) sit on the death screen LONGER than the regrow delay (the rewarded-ad watch). This
            //    is the condition that made the stale-timer bug bite. Fast-forward with timeScale so
            //    the test takes ~2s of real time instead of 14s+ (Time.time still advances the delay).
            float prevScale = Time.timeScale;
            Time.timeScale = 8f;
            float waited = 0f, target = GameConst.TailRegrowDelay + 1.5f;
            while (waited < target) { waited += Time.deltaTime; yield return null; }
            Time.timeScale = prevScale;

            // 3) revive. If the ad stub somehow isn't ready, report inconclusive (never a false pass).
            if (!gm.CanRevive)
            {
                Report(false, sb + "CANNOT REVIVE (CanRevive=false: ad not ready / already used) -> test inconclusive");
                yield break;
            }
            gm.RequestRevive();
            yield return null;

            bool revived = gm.State == GameState.Playing && gm.Hearts == 1;
            pass &= revived;
            sb.AppendLine("REVIVED: state=" + gm.State + " hearts=" + gm.Hearts + " -> " + (revived ? "PASS" : "FAIL"));

            // 4) THE CHECK: the tail must still be GONE right after revive and stay gone for a beat
            //    (the stale-timer bug regrew it within one Update of re-entering Playing).
            bool tailStaysGone = !gm.HasTail;
            for (float t = 0f; t < 1.0f && tailStaysGone; t += Time.deltaTime)
            {
                if (gm.HasTail) tailStaysGone = false;
                yield return null;
            }
            pass &= tailStaysGone;
            sb.AppendLine("TAIL STAYS DROPPED after revive (no instant regrow): hasTail=" + gm.HasTail
                + " -> " + (tailStaysGone ? "PASS" : "FAIL (R33 regression — tail regrew on revive)"));

            Report(pass, sb.ToString());
        }

        private void Report(bool pass, string detail)
        {
            Time.timeScale = 1f; // safety: never leave the editor fast-forwarded if we bailed mid-wait
            string head = pass ? "[REVIVE] PASS" : "[REVIVE] FAIL";
            if (pass) Debug.Log(head + "\n" + detail);
            else Debug.LogError(head + "\n" + detail);
            try
            {
                Directory.CreateDirectory("Temp/Playtest");
                File.WriteAllText("Temp/Playtest/revive.txt", head + "\n" + detail);
            }
            catch { }
            Destroy(gameObject);
        }
    }
}
