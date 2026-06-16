using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Builds and owns every crossing lane in the level (packet required system:
    /// HazardLaneManager). One cross-traffic hazard per lane, staggered by the
    /// LevelDefinition so the corridor never pulses in lockstep. Each lane is a
    /// giant walking pedestrian when the human model is imported, otherwise the
    /// procedural shoe stride (so the game still runs asset-free).
    /// </summary>
    public class HazardLaneManager : MonoBehaviour
    {
        public static HazardLaneManager Build(Transform parent, LevelDefinition level)
        {
            var go = new GameObject("HazardLanes");
            go.transform.SetParent(parent, false);
            var mgr = go.AddComponent<HazardLaneManager>();

            // Real-city street mode: traffic runs ALONG the avenue (people on the
            // sidewalk, cars on the road) instead of crossing a corridor.
            if (GameObject.Find("NYCity") != null)
            {
                StreetTraffic.Build(go.transform, level);
                return mgr;
            }

            for (int i = 0; i < level.Lanes.Length; i++)
            {
                var lane = level.Lanes[i];
                GameObject hazard;
                string label;
                switch (lane.Type)
                {
                    case LaneType.Road:
                        hazard = Car.Spawn(go.transform, lane).gameObject;
                        label = "Car";
                        break;
                    case LaneType.Alley:
                        hazard = DebrisHazard.Spawn(go.transform, lane).gameObject;
                        label = "Debris";
                        break;
                    default: // Sidewalk → pedestrians (shoe fallback if the human model is absent)
                        var ped = GiantPedestrian.Spawn(go.transform, lane);
                        hazard = ped != null ? ped.gameObject : SidewaysFootHazard.Spawn(go.transform, lane).gameObject;
                        label = ped != null ? "Pedestrian" : "Shoe";
                        break;
                }
                hazard.name = string.Format("{0}_Lane{1}_{2}", label, i, lane.Dir > 0 ? "LtoR" : "RtoL");
            }
            return mgr;
        }
    }
}
