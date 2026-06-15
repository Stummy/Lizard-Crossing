using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LizardCrossing.Tests
{
    /// <summary>
    /// The work packet's Phase 1 smoke tests (03_SelfTesting), adapted to play
    /// mode because the whole world is runtime-built by Bootstrap: each test
    /// loads the Boot scene, waits for construction, then runs the packet's
    /// original assertions (plus the packet's directionality test).
    /// </summary>
    public class Phase1SmokeTests
    {
        private static IEnumerator LoadBoot()
        {
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            yield return null; // scene activation
            yield return null; // Bootstrap.Awake world build + first Update
        }

        [UnityTest]
        public IEnumerator Phase1Scene_HasCoreObjects()
        {
            yield return LoadBoot();

            var player = GameObject.FindGameObjectWithTag("Player");
            Assert.IsNotNull(player, "Missing Player object with tag 'Player'.");

            var camera = Camera.main;
            Assert.IsNotNull(camera, "Missing Main Camera tagged as MainCamera.");

            Assert.IsNotNull(Object.FindAnyObjectByType<GameStateManager>(),
                "Missing GameStateManager.");

            bool safeFound = false;
            foreach (var c in Object.FindObjectsByType<Collider>())
                if (c.gameObject.name.ToLower().Contains("safe")) { safeFound = true; break; }
            Assert.IsTrue(safeFound, "Missing Safe Zone collider/object. Name should include 'Safe'.");
        }

        [UnityTest]
        public IEnumerator Phase1Scene_HasHazardsAndCollectibles()
        {
            yield return LoadBoot();

            bool hazardFound = false, bugFound = false;
            foreach (var t in Object.FindObjectsByType<Transform>())
            {
                string n = t.name.ToLower();
                if (n.Contains("hazard") || n.Contains("shoe") || n.Contains("foot")) hazardFound = true;
                if (n.Contains("bug") || n.Contains("collectible")) bugFound = true;
                if (hazardFound && bugFound) break;
            }
            Assert.IsTrue(hazardFound, "Missing hazard objects. Add sideways-moving shoes/feet.");
            Assert.IsTrue(bugFound, "Missing bug/collectible objects.");
        }

        [UnityTest]
        public IEnumerator MainCamera_IsLowEnoughForLizardPOV()
        {
            yield return LoadBoot();

            var camera = Camera.main;
            Assert.IsNotNull(camera, "Missing Main Camera.");
            Assert.Less(camera.transform.position.y, 3.0f,
                "Camera is probably too high for low lizard POV.");
        }

        [UnityTest]
        public IEnumerator Hazards_MoveSidewaysAcrossPlayerRoute()
        {
            yield return LoadBoot();

            var hints = Object.FindObjectsByType<SidewaysHazardDirectionHint>();
            Assert.Greater(hints.Length, 0, "No hazards carry SidewaysHazardDirectionHint.");

            foreach (var hint in hints)
            {
                Vector3 move = hint.movementDirection; move.y = 0f;
                Vector3 expected = hint.expectedSidewaysAxis; expected.y = 0f;
                float alignment = Mathf.Abs(Vector3.Dot(move.normalized, expected.normalized));
                Assert.GreaterOrEqual(alignment, 0.7f,
                    hint.name + ": hazard is not moving sideways across the lizard's path.");
            }
        }

        [UnityTest]
        public IEnumerator Hud_CanvasExists()
        {
            yield return LoadBoot();
            Assert.IsNotNull(Object.FindAnyObjectByType<Canvas>(), "Missing HUD canvas.");
        }
    }
}
