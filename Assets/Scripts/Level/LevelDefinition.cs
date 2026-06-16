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
                Length = 205f,
                Lanes = new[]
                {
                    // City crossing: sidewalks (pedestrians) and roads (cars) alternate,
                    // an alley (debris) breaks up the middle, busier/faster toward the end.
                    new LaneSpec { Z = 26f,  Dir = +1, StepDuration = 0.62f, StartDelay = 1.0f, RespawnDelay = 2.6f,  Type = LaneType.Sidewalk },
                    new LaneSpec { Z = 48f,  Dir = -1, StepDuration = 0.60f, StartDelay = 2.2f, RespawnDelay = 2.3f,  Type = LaneType.Road },
                    new LaneSpec { Z = 70f,  Dir = +1, StepDuration = 0.55f, StartDelay = 0.4f, RespawnDelay = 2.0f,  Type = LaneType.Sidewalk },
                    new LaneSpec { Z = 92f,  Dir = -1, StepDuration = 0.52f, StartDelay = 1.6f, RespawnDelay = 1.7f,  Type = LaneType.Road },
                    new LaneSpec { Z = 115f, Dir = +1, StepDuration = 0.50f, StartDelay = 0.8f, RespawnDelay = 1.5f,  Type = LaneType.Alley },
                    new LaneSpec { Z = 138f, Dir = -1, StepDuration = 0.47f, StartDelay = 2.0f, RespawnDelay = 1.35f, Type = LaneType.Road },
                    new LaneSpec { Z = 162f, Dir = +1, StepDuration = 0.45f, StartDelay = 0.2f, RespawnDelay = 1.2f,  Type = LaneType.Sidewalk },
                    new LaneSpec { Z = 185f, Dir = -1, StepDuration = 0.42f, StartDelay = 1.2f, RespawnDelay = 1.05f, Type = LaneType.Road },
                },
                BugPositions = new[]
                {
                    // safe pockets between lanes
                    new Vector3(-4f, 0f, 16f),
                    new Vector3( 5f, 0f, 37f),
                    new Vector3(-6f, 0f, 59f),
                    new Vector3( 3f, 0f, 81f),
                    new Vector3( 6f, 0f, 104f),
                    new Vector3(-5f, 0f, 127f),
                    new Vector3( 0f, 0f, 150f),
                    new Vector3( 5f, 0f, 174f),
                    new Vector3(-4f, 0f, 196f),
                    // risky in-lane prizes (right on a crossing track)
                    new Vector3( 0f, 0f, 70f),
                    new Vector3(-2f, 0f, 138f),
                    new Vector3( 2f, 0f, 185f),
                },
            };
        }
    }
}
