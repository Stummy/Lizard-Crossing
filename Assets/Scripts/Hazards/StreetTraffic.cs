using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Populates the NYC avenue with traffic that runs ALONG the street (±Z), the
    /// way a real street works: pedestrians walking the sidewalk (whose shoes squish
    /// the lizard) and cars driving the road. The lizard starts on the sidewalk and
    /// runs forward, dodging shoes; stray onto the road and dodge cars.
    ///
    /// Lane X positions come from the avenue cross-section probe (road ≈ x[-8..+3],
    /// right sidewalk ≈ x[+3..+9]). Each lane pre-distributes its actors evenly so
    /// the street is busy from the first frame.
    /// </summary>
    public static class StreetTraffic
    {
        public static void Build(Transform parent, LevelDefinition level)
        {
            float zLow = -25f, zHigh = level.Length + 30f;

            // --- cars on the road (mostly oncoming so the player reads the gaps) ---
            CarLane(parent, -6.0f, -1, 6.5f, 3, zLow, zHigh);
            CarLane(parent, -2.5f, -1, 7.5f, 3, zLow, zHigh);
            CarLane(parent,  0.8f, +1, 5.5f, 2, zLow, zHigh);

            // --- pedestrians on the right sidewalk ---
            PedLane(parent, 4.2f, -1, 0.50f, 5, zLow, zHigh);
            PedLane(parent, 6.0f, +1, 0.55f, 4, zLow, zHigh);
            PedLane(parent, 7.8f, -1, 0.50f, 5, zLow, zHigh);
        }

        static void CarLane(Transform parent, float x, int dir, float speed, int count, float zLow, float zHigh)
        {
            Vector3 start = dir > 0 ? new Vector3(x, 0f, zLow) : new Vector3(x, 0f, zHigh);
            Vector3 end   = dir > 0 ? new Vector3(x, 0f, zHigh) : new Vector3(x, 0f, zLow);
            for (int i = 0; i < count; i++)
                Car.SpawnTrack(parent, start, end, speed, 0f, 0f, (i + 0.5f) / count);
        }

        static void PedLane(Transform parent, float x, int dir, float stepDuration, int count, float zLow, float zHigh)
        {
            Vector3 start = dir > 0 ? new Vector3(x, 0f, zLow) : new Vector3(x, 0f, zHigh);
            Vector3 end   = dir > 0 ? new Vector3(x, 0f, zHigh) : new Vector3(x, 0f, zLow);
            for (int i = 0; i < count; i++)
                GiantPedestrian.SpawnTrack(parent, start, end, stepDuration, 0f, 0f, 1f, (i + 0.5f) / count);
        }
    }
}
