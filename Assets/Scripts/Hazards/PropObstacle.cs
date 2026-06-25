using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// A solid street prop on the sidewalk (trash can, hydrant, sign). Running head-on
    /// into it faceplants the lizard and costs damage, the same way a pedestrian shoe
    /// does — a cheap horizontal proximity test (mirroring
    /// <see cref="GiantPedestrian.CheckFootBump"/>), no physics collider needed. The
    /// player owns the re-hit cooldown / i-frames, so one prop can't chain-hit.
    /// </summary>
    public sealed class PropObstacle : MonoBehaviour
    {
        public float Radius = 0.28f;

        private void Update()
        {
            var gm = GameStateManager.Instance;
            var player = PlayerController.Instance;
            if (gm == null || player == null || gm.State != GameState.Playing) return;
            if (player.IsAirborne || player.IsInvulnerable || !player.CanFootBump) return;

            Vector3 p = player.KillCheckPosition;
            float dx = transform.position.x - p.x;
            float dz = transform.position.z - p.z;
            if (dx * dx + dz * dz <= Radius * Radius)
                gm.PropBump(transform.position);
        }
    }
}
