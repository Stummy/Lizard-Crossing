using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Ground-height service shared by the lizard, camera and pedestrians on the NYC
    /// avenue. The real city meshes have no colliders, so height comes from the avenue
    /// CROSS-SECTION analytically: a flush road between the curbs (x[-8..+3]) and raised
    /// sidewalks beyond, with a short curb ramp so the lizard scrambles up/down smoothly.
    /// Because it's a pure function of X (not Z), a straight forward run holds a constant
    /// sidewalk height the whole way — the run never "drifts" off the sidewalk. Only
    /// active on the city street level (set by <see cref="LevelBuilder"/>).
    /// </summary>
    public static class StreetGround
    {
        public static bool Active;                 // set by LevelBuilder for the NYC avenue level
        public const int GroundLayer = 8;          // "CityGround" (kept for the lizard's layer setup)

        public const float SidewalkY = 0.12f;      // raised sidewalk surface height
        public const float RoadY = 0f;             // flush road height
        public const float RightCurbX = 3f;        // right sidewalk starts here (x > this + ramp)
        public const float LeftCurbX = -8f;        // left sidewalk starts here (x < this - ramp)

        const float RampHalf = 0.6f;               // curb-ramp width on each side

        /// <summary>No-op: the avenue profile is purely X-based, so there's nothing to
        /// learn from the lane Z list. Kept for call-site compatibility.</summary>
        public static void Configure(LevelDefinition level) { }

        /// <summary>Surface height under world (x, z): raised sidewalk beyond either curb,
        /// flush road between, ramped across the curb lip. Z is ignored (avenue runs +Z).
        /// 0 when inactive.</summary>
        public static float HeightAt(float x, float z)
        {
            if (!Active) return 0f;

            if (x >= RightCurbX + RampHalf || x <= LeftCurbX - RampHalf) return SidewalkY;
            if (x > RightCurbX) return Mathf.Lerp(RoadY, SidewalkY, (x - RightCurbX) / RampHalf);
            if (x < LeftCurbX) return Mathf.Lerp(RoadY, SidewalkY, (LeftCurbX - x) / RampHalf);
            return RoadY;
        }
    }
}
