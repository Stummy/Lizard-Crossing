using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LizardCrossing.Tests
{
    /// <summary>
    /// CI-runnable Foundation-invariant test — the same spatial regression gate as the
    /// <c>Lizard Crossing/Bot/Invariant Check</c> menu item, expressed as a Unity Test Framework
    /// <see cref="UnityTest"/> so GameCI / the Test Runner asserts it on every push. It drives the
    /// lizard HARD into the right building wall and then the left curb at DASH speed and asserts it
    /// physically cannot leave the sidewalk band, and that the analytic run-band ground stays straight
    /// sidewalk end-to-end. A green "bot reached the safe zone" never catches this — the dodge-bot
    /// never tries to walk through a wall — so this spatial invariant must be tested explicitly.
    /// </summary>
    public class InvariantTest
    {
        [UnityTest]
        public IEnumerator Lizard_StaysConfined_AndRunBandStaysStraight()
        {
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            yield return null; // scene activation
            yield return null; // Bootstrap world build + first Update

            var gm = GameStateManager.Instance;
            var player = PlayerController.Instance;
            Assert.IsNotNull(gm, "No GameStateManager — the world did not build.");
            Assert.IsNotNull(player, "No PlayerController.");

            gm.StartRun();
            yield return null;
            Assert.AreEqual(GameState.Playing, gm.State, "Run did not enter Playing — invariant cannot be tested.");

            float rightLimit = GameConst.CorridorRightX + 0.2f;   // centre must never pass the right clamp
            float leftLimit = GameConst.CorridorFenceLeftX;       // ...nor the curb line into the road

            // 1) jam RIGHT for ~1.5s at DASH speed (also catches collider tunnelling). Auto-run carries +Z.
            float maxX = float.MinValue;
            for (float t = 0f; t < 1.5f && gm.State == GameState.Playing; t += Time.deltaTime)
            {
                InputProvider.MoveOverride = new Vector2(1f, 1f);
                InputProvider.PressDash();
                maxX = Mathf.Max(maxX, player.KillCheckPosition.x);
                yield return null;
            }
            Assert.LessOrEqual(maxX, rightLimit,
                "Lizard breached the RIGHT wall: maxX=" + maxX.ToString("0.00") + " > limit=" + rightLimit.ToString("0.00"));

            // 2) jam LEFT for ~1.5s at DASH speed
            float minX = float.MaxValue;
            for (float t = 0f; t < 1.5f && gm.State == GameState.Playing; t += Time.deltaTime)
            {
                InputProvider.MoveOverride = new Vector2(-1f, 1f);
                InputProvider.PressDash();
                minX = Mathf.Min(minX, player.KillCheckPosition.x);
                yield return null;
            }
            InputProvider.MoveOverride = null;
            Assert.GreaterOrEqual(minX, leftLimit,
                "Lizard breached the LEFT curb: minX=" + minX.ToString("0.00") + " < limit=" + leftLimit.ToString("0.00"));

            // FALSE-PASS GUARD: if the run never entered Playing the ram loops never executed and
            // maxX/minX keep their sentinels, trivially passing the comparisons above — require real motion.
            Assert.IsTrue(maxX > -1e30f && minX < 1e30f,
                "Ram loops never sampled a position — the run was not Playing, so the invariant did NOT test.");

            // 3) straightness: the analytic sidewalk height must hold across the whole band length
            float cx = GameConst.CorridorStripCenterX;
            for (float z = 0f; z <= 140f; z += 10f)
            {
                float h = StreetGround.HeightAt(cx, z);
                Assert.Less(Mathf.Abs(h - StreetGround.SidewalkY), 0.02f,
                    "Run band is not straight sidewalk at z=" + z.ToString("0") + " (height=" + h.ToString("0.00") + ").");
            }
        }
    }
}
