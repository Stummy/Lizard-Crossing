using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Builds and owns every crossing lane in the level (packet required system:
    /// HazardLaneManager). One SidewaysFootHazard per lane, staggered by the
    /// LevelDefinition so the corridor never pulses in lockstep.
    /// </summary>
    public class HazardLaneManager : MonoBehaviour
    {
        public static HazardLaneManager Build(Transform parent, LevelDefinition level)
        {
            var go = new GameObject("HazardLanes");
            go.transform.SetParent(parent, false);
            var mgr = go.AddComponent<HazardLaneManager>();

            for (int i = 0; i < level.Lanes.Length; i++)
            {
                var lane = level.Lanes[i];
                var hazard = SidewaysFootHazard.Spawn(go.transform, lane);
                hazard.gameObject.name = string.Format(
                    "SidewaysFootHazard_Lane{0}_{1}", i, lane.Dir > 0 ? "LtoR" : "RtoL");
            }
            return mgr;
        }
    }
}
