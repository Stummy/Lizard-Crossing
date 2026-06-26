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

            // tolerance: the lizard's body half-width past a wall's centre line is acceptable contact
            float rightLimit = GameConst.CorridorWallRightX + 0.4f;
            float leftLimit = GameConst.CorridorFenceLeftX - 0.4f;
            var sb = new StringBuilder();
            bool pass = true;

            // 1) jam RIGHT for ~3s (auto-run carries it forward); record the furthest-right x reached
            float maxX = float.MinValue;
            for (float t = 0f; t < 3f && gm.State == GameState.Playing; t += Time.deltaTime)
            {
                InputProvider.MoveOverride = new Vector2(1f, 1f);
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
