namespace LizardCrossing
{
    /// <summary>
    /// Analytic ground height for the NYC avenue cross-section so the lizard rides
    /// up onto the raised sidewalks and back down to the road smoothly (the real
    /// city meshes have no colliders). Boundaries come from the cross-section probe:
    /// road ≈ x[-8..+3], sidewalks beyond. Only active on the city street level.
    /// </summary>
    public static class StreetGround
    {
        public static bool Active;          // set by LevelBuilder for the NYC level
        public const float SidewalkY = 0.12f;
        public const float RightCurbX = 3f; // right sidewalk starts here (x >)
        public const float LeftCurbX = -8f; // left sidewalk starts here (x <)

        public static float HeightAt(float x)
        {
            if (!Active) return 0f;
            return (x > RightCurbX || x < LeftCurbX) ? SidewalkY : 0f;
        }
    }
}
