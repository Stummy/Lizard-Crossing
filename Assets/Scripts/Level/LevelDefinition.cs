using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// One crossing lane: giant feet stride across the corridor at a fixed z,
    /// always perpendicular to the lizard's route (packet movement rule).
    /// </summary>
    /// <summary>What kind of crossing a lane is — drives its hazard + surface.</summary>
    public enum LaneType { Sidewalk, Road, Alley }

    [System.Serializable]
    public class LaneSpec
    {
        public float Z;               // lane center along the corridor
        public int Dir;               // +1 = left-to-right (+x), -1 = right-to-left
        public float StepDuration;    // seconds per stride/crossing unit (lower = faster/harder)
        public float StartDelay;      // initial offset so lanes don't sync up
        public float RespawnDelay;    // pause between crossings (lower = busier lane)
        public float Scale = 1f;      // size multiplier
        public LaneType Type = LaneType.Sidewalk; // sidewalk=people, road=cars, alley=debris
    }

    /// <summary>
    /// Pure data for one level. Phase 1 ships only GardenEscape; later phases
    /// load these from authored assets.
    /// </summary>
    public class LevelDefinition
    {
        public const string GardenEscapeId = "garden_escape";

        public string Id = GardenEscapeId;
        public string Name;
        public float Length;          // z of the safe-zone threshold
        public LaneSpec[] Lanes;
        public Vector3[] BugPositions;

        /// <summary>
        /// Phase 1 level: stone garden alley, eight lanes alternating direction,
        /// gentle pacing up front, denser and faster near the garden.
        /// Bugs alternate safe-pocket pickups and risky in-lane prizes.
        /// </summary>
        public static LevelDefinition GardenEscape()
        {
            return new LevelDefinition
            {
                Id = GardenEscapeId,
                Name = "Garden Escape",
                // The run ends INSIDE the real NYC city block (its avenue is solid, straight
                // floor to ~z=165). Length is the safe-zone threshold; keeping it at 140 lands
                // the goal + its apron comfortably within the city edge so the whole run is on
                // real street — no void, no drift off the end. (Cross-traffic lanes below are
                // only used in the non-NYC fallback; on the avenue StreetTraffic drives flow.)
                Length = 140f,
                Lanes = new[]
                {
                    // City crossing: sidewalks (pedestrians) and roads (cars) alternate,
                    // an alley (debris) breaks up the middle, busier/faster toward the end.
                    new LaneSpec { Z = 22f,  Dir = +1, StepDuration = 0.62f, StartDelay = 1.0f, RespawnDelay = 2.6f,  Type = LaneType.Sidewalk },
                    new LaneSpec { Z = 40f,  Dir = -1, StepDuration = 0.60f, StartDelay = 2.2f, RespawnDelay = 2.3f,  Type = LaneType.Road },
                    new LaneSpec { Z = 58f,  Dir = +1, StepDuration = 0.55f, StartDelay = 0.4f, RespawnDelay = 2.0f,  Type = LaneType.Sidewalk },
                    new LaneSpec { Z = 76f,  Dir = -1, StepDuration = 0.52f, StartDelay = 1.6f, RespawnDelay = 1.7f,  Type = LaneType.Road },
                    new LaneSpec { Z = 94f,  Dir = +1, StepDuration = 0.50f, StartDelay = 0.8f, RespawnDelay = 1.5f,  Type = LaneType.Alley },
                    new LaneSpec { Z = 112f, Dir = -1, StepDuration = 0.47f, StartDelay = 2.0f, RespawnDelay = 1.35f, Type = LaneType.Road },
                    new LaneSpec { Z = 128f, Dir = +1, StepDuration = 0.45f, StartDelay = 0.2f, RespawnDelay = 1.2f,  Type = LaneType.Sidewalk },
                },
                BugPositions = new[]
                {
                    // spread along the right sidewalk (SpawnBugs clamps x to the run band)
                    new Vector3( 4f, 0f, 14f),
                    new Vector3( 7f, 0f, 30f),
                    new Vector3( 5f, 0f, 46f),
                    new Vector3( 8f, 0f, 62f),
                    new Vector3( 4f, 0f, 78f),
                    new Vector3( 7f, 0f, 94f),
                    new Vector3( 5f, 0f, 110f),
                    new Vector3( 8f, 0f, 124f),
                    new Vector3( 6f, 0f, 134f),
                    // risky in-lane prizes (near a crossing track)
                    new Vector3( 6f, 0f, 70f),
                    new Vector3( 4f, 0f, 112f),
                    new Vector3( 7f, 0f, 128f),
                },
            };
        }
    }
}
