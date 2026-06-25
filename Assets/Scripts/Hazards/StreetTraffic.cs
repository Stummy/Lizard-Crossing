using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Populates the NYC avenue with traffic that runs ALONG the street (±Z): busy
    /// crowds of pedestrians walking up AND down both sidewalks (whose shoes squish
    /// the lizard), and cars driving the road. The lizard starts on the sidewalk and
    /// runs forward, dodging shoes; stray onto the road and dodge cars.
    ///
    /// Lane X positions come from the avenue cross-section probe (road ≈ x[-8..+3],
    /// right sidewalk ≈ x[+3..+9], thin left sidewalk near x[-9..-8]). Each lane
    /// pre-distributes its actors evenly so the street is busy from the first frame.
    /// </summary>
    public static class StreetTraffic
    {
        // Player-relative window the sidewalk crowd lives in (units of Z ahead of the
        // lizard). NOBODY spawns behind it: forward strollers walk away and oncoming
        // walkers come toward it, but both are recycled to a fresh spot ahead once
        // overtaken — so no pedestrian ever streams up from behind to surprise the
        // player. Tuned so the crowd stays dense all the way to the end of the run.
        const float AheadMin = 5f;     // nearest a recycled pedestrian re-enters
        const float AheadMax = 60f;    // furthest ahead it re-enters (≈ view depth)
        const float BehindCull = 12f;  // how far behind the lizard before recycling

        public static void Build(Transform parent, LevelDefinition level)
        {
            float zLow = -25f, zHigh = level.Length + 30f;
            float sw = GameConst.GroundY; // the street is a flat plane — pedestrians stand on y=0, not a raised curb

            // --- cars on the road (mostly oncoming so the player reads the gaps) ---
            CarLane(parent, -6.2f, -1, 6.5f, 3, zLow, zHigh);
            CarLane(parent, -2.6f, -1, 7.5f, 3, zLow, zHigh);
            CarLane(parent,  0.8f, +1, 5.5f, 2, zLow, zHigh);

            // --- busy two-way crowds on the right sidewalk, straddling the lizard's run
            //     band (x∈[7,11], centred on 9) so it actually weaves THROUGH the flow of
            //     people walking forward (away) AND toward it, at mixed walk and run paces.
            //     Four interleaved lanes packed enough to make weaving a real challenge,
            //     without being a solid wall of bodies. ---
            SidewalkStream(parent, 7.3f, sw, 5, 0.80f);
            SidewalkStream(parent, 8.4f, sw, 5, 0.82f);
            SidewalkStream(parent, 9.6f, sw, 5, 0.80f);
            SidewalkStream(parent, 10.7f, sw, 5, 0.82f);

            // --- a thinner two-way stream on the narrow left sidewalk ---
            SidewalkStream(parent, -8.3f, sw, 3, 0.80f);
            SidewalkStream(parent, -8.8f, sw, 3, 0.82f);

            // --- jaywalkers crossing the road, staggered down the avenue, so the
            //     lizard can't treat the roadway as a safe gap between car waves ---
            CrossLane(parent, level.Length, 5);
        }

        static void CarLane(Transform parent, float x, int dir, float speed, int count, float zLow, float zHigh)
        {
            Vector3 start = dir > 0 ? new Vector3(x, 0f, zLow) : new Vector3(x, 0f, zHigh);
            Vector3 end   = dir > 0 ? new Vector3(x, 0f, zHigh) : new Vector3(x, 0f, zLow);
            for (int i = 0; i < count; i++)
                Car.SpawnTrack(parent, start, end, speed, 0f, 0f, (i + 0.5f) / count);
        }

        // One sidewalk lane: a crowd of pedestrians filling the player-relative window,
        // half walking forward (away) and half oncoming, at a wide spread of paces.
        static void SidewalkStream(Transform parent, float x, float y, int count, float stepDuration)
        {
            for (int i = 0; i < count; i++)
            {
                // Alternate facing so each lane is genuine two-way foot traffic:
                // even = oncoming (toward the lizard), odd = forward (away from it).
                int dir = (i % 2 == 0) ? -1 : +1;

                // A real mix of paces so the crowd never reads as a uniform march:
                // slow strollers, brisk walkers, joggers, and the occasional fast
                // sprinter blowing past. The gait tier sets both move speed and clip
                // speed in SpawnTrack/BuildHuman. ~12% sprint, ~28% run, the rest walk.
                float roll = Random.value;
                bool sprint = roll < 0.12f;
                bool run = !sprint && roll < 0.40f;

                // Spread the initial spawn across the whole ahead-window so the street
                // is crowded from the first frame — and never behind the start line, so
                // nobody is ever approaching from behind, even on frame one.
                float z0 = Mathf.Lerp(1f, AheadMax, (i + 0.5f) / count);
                float laneX = x + Random.Range(-0.35f, 0.35f); // jitter so lanes don't read as rails
                GiantPedestrian.SpawnSidewalk(parent, laneX, y, dir, stepDuration, run,
                    AheadMin, AheadMax, BehindCull, z0, sprint);
            }
        }

        // A line of pedestrians crossing the roadway (±X) at staggered Z down the
        // avenue. Each starts off-corridor, walks across, then recycles after a
        // random rest so the crossings stay irregular and unpredictable.
        static void CrossLane(Transform parent, float length, int count)
        {
            float margin = GameConst.CorridorHalfWidth + 16f;
            for (int i = 0; i < count; i++)
            {
                float z = Mathf.Lerp(8f, Mathf.Max(8f, length - 8f), (i + 0.5f) / count);
                int dir = (i % 2 == 0) ? 1 : -1;
                Vector3 start = new Vector3(dir > 0 ? -margin : margin, 0f, z);
                Vector3 end   = new Vector3(-start.x, 0f, z);
                GiantPedestrian.SpawnTrack(parent, start, end,
                    Random.Range(0.46f, 0.60f),   // step duration (walk pace)
                    Random.Range(0f, 4f),         // start delay (stagger first crossing)
                    Random.Range(2.5f, 6f),       // rest before re-crossing
                    1f);
            }
        }
    }
}
