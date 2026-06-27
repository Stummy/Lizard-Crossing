using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

namespace LizardCrossing.Testing
{
    /// <summary>
    /// Foundation-invariant regression check (World + corridor). Drives the lizard HARD into the
    /// right wall and then the left fence and asserts it physically cannot leave the sidewalk band,
    /// and samples the run-band ground height end-to-end to assert the surface stays straight
    /// sidewalk. Writes PASS/FAIL to Temp/Playtest/invariant.txt + the console.
    ///
    /// This is the gate that stops the recurring "lizard walks through the wall / sidewalk shifts /
    /// no floor at the end" cluster from silently regressing — a green "bot reached the safe zone"
    /// never catches it because the dodge-bot never tries to walk through a wall. Run it from
    /// Lizard Crossing/Bot/Invariant Check (Play mode) and as part of the verify loop.
    /// </summary>
    public class InvariantCheck : MonoBehaviour
    {
        public static void Launch()
        {
            var go = new GameObject("InvariantCheck");
            go.AddComponent<InvariantCheck>();
        }

        private IEnumerator Start()
        {
            var gm = GameStateManager.Instance;
            var player = PlayerController.Instance;
            if (gm == null || player == null)
            {
                Report(false, "no GameStateManager/PlayerController — enter Play mode first");
                yield break;
            }

            gm.StartRun();
            yield return null;

            // Tightened to the actual clamp band (owner fix 2026-06-26): the lizard CENTRE must stay
            // within the sidewalk — never past the right clamp (11.0, +0.2 slop) and never left of the
            // curb line (5.8). We now ram at DASH speed too, so this also catches collider TUNNELING
            // ("built another wall inside the wall and still went through").
            float rightLimit = GameConst.CorridorRightX + 0.2f;     // 11.2
            float leftLimit = GameConst.CorridorFenceLeftX;         // 5.8 (curb centre — never reach the road)
            var sb = new StringBuilder();
            bool pass = true;

            // 1) jam RIGHT for ~3s (auto-run carries it forward); record the furthest-right x reached
            float maxX = float.MinValue;
            for (float t = 0f; t < 3f && gm.State == GameState.Playing; t += Time.deltaTime)
            {
                InputProvider.MoveOverride = new Vector2(1f, 1f);
                InputProvider.PressDash();  // ram at DASH speed to catch tunneling through the wall
                maxX = Mathf.Max(maxX, player.KillCheckPosition.x);
                yield return null;
            }
            bool rightOk = maxX <= rightLimit;
            pass &= rightOk;
            sb.AppendLine("RIGHT wall: maxX=" + maxX.ToString("0.00") + " limit=" + rightLimit.ToString("0.00")
                + " -> " + (rightOk ? "PASS" : "FAIL"));

            // 2) jam LEFT for ~3s; record the furthest-left x reached
            float minX = float.MaxValue;
            for (float t = 0f; t < 3f && gm.State == GameState.Playing; t += Time.deltaTime)
            {
                InputProvider.MoveOverride = new Vector2(-1f, 1f);
                InputProvider.PressDash();  // ram the curb at DASH speed too (tunneling check)
                minX = Mathf.Min(minX, player.KillCheckPosition.x);
                yield return null;
            }
            bool leftOk = minX >= leftLimit;
            pass &= leftOk;
            sb.AppendLine("LEFT fence: minX=" + minX.ToString("0.00") + " limit=" + leftLimit.ToString("0.00")
                + " -> " + (leftOk ? "PASS" : "FAIL"));

            InputProvider.MoveOverride = null;

            // 3) straightness: the analytic sidewalk height must hold across the whole band length
            bool straightOk = true;
            float worstZ = -1f;
            float cx = GameConst.CorridorStripCenterX;
            for (float z = 0f; z <= 140f; z += 10f)
            {
                float h = StreetGround.HeightAt(cx, z);
                if (Mathf.Abs(h - StreetGround.SidewalkY) > 0.02f) { straightOk = false; worstZ = z; break; }
            }
            pass &= straightOk;
            sb.AppendLine("STRAIGHT band: sidewalk height holds z[0..140] -> "
                + (straightOk ? "PASS" : "FAIL at z=" + worstZ.ToString("0")));

            // FALSE-PASS GUARD (2026-06-26): if the run never entered Playing (e.g. launched right
            // after a finished run), the ram loops never execute and maxX/minX keep their sentinel
            // float.Min/MaxValue, which trivially satisfy the limit comparisons → a meaningless PASS.
            // Require that the lizard actually MOVED (the loops sampled real positions) or it's a FAIL.
            bool actuallyRan = maxX > -1e30f && minX < 1e30f;
            pass &= actuallyRan;
            sb.AppendLine("RUN SAMPLED: ram loops moved the lizard -> "
                + (actuallyRan ? "PASS" : "FAIL (run never entered Playing — invariant did NOT test; re-run from a fresh Play)"));

            Report(pass, sb.ToString());
        }

        private void Report(bool pass, string detail)
        {
            string head = pass ? "[INVARIANT] PASS" : "[INVARIANT] FAIL";
            if (pass) Debug.Log(head + "\n" + detail);
            else Debug.LogError(head + "\n" + detail);
            try
            {
                Directory.CreateDirectory("Temp/Playtest");
                File.WriteAllText("Temp/Playtest/invariant.txt", head + "\n" + detail);
            }
            catch { }
            Destroy(gameObject);
        }
    }
}
