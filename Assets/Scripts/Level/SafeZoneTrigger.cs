using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Win condition (packet required system: SafeZoneTrigger). Object is named
    /// "SafeZone" — the packet's tests look for a collider whose name contains
    /// "safe". Uses a position check as the source of truth (robust regardless
    /// of CharacterController trigger quirks); the trigger collider documents
    /// the volume in the editor.
    /// </summary>
    public class SafeZoneTrigger : MonoBehaviour
    {
        private float _thresholdZ;
        private bool _won;

        public static SafeZoneTrigger Create(Transform parent, float thresholdZ)
        {
            var go = new GameObject("SafeZone");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, 2f, thresholdZ + 3f);

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(GameConst.CorridorHalfWidth * 2f + 6f, 6f, 6f);

            var trigger = go.AddComponent<SafeZoneTrigger>();
            trigger._thresholdZ = thresholdZ;
            return trigger;
        }

        private void Update()
        {
            if (_won) return;
            var gm = GameStateManager.Instance;
            var player = PlayerController.Instance;
            if (gm == null || player == null || gm.State != GameState.Playing) return;

            if (player.transform.position.z >= _thresholdZ)
            {
                _won = true;
                GameAudio.Play(Sfx.Win);
                gm.WinRun();
            }
        }
    }
}
