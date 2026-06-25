using System.Collections.Generic;
using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Registry of solid street obstacles (trash cans, hydrants, signs, …) so the world
    /// reads as physical: the lizard collides with them (faceplant, via PropObstacle) AND
    /// the pedestrian crowd STEERS AROUND them instead of ghosting through. Obstacles are
    /// registered at level build and cleared on rebuild. A flat (x,z + radius) model is
    /// enough — everything here is a ground-standing column.
    /// </summary>
    public static class ObstacleField
    {
        struct Ob { public float X, Z, Radius; }
        static readonly List<Ob> Obs = new List<Ob>();

        public static void Clear() { Obs.Clear(); }

        public static void Add(Vector3 pos, float radius)
        {
            Obs.Add(new Ob { X = pos.x, Z = pos.z, Radius = radius });
        }

        /// <summary>
        /// Lateral steer-around velocity for a walker at <paramref name="pos"/> heading
        /// <paramref name="dir"/> (normalized, horizontal). Looks a short way ahead; for any
        /// obstacle in the walker's path it returns a sideways nudge away from it, ramped up
        /// as the walker closes in, so the crowd arcs smoothly around street furniture. Zero
        /// when the path is clear. Output is a velocity (u/s) — apply × deltaTime.
        /// </summary>
        public static Vector3 Avoidance(Vector3 pos, Vector3 dir, float agentRadius, float lookahead, float maxSpeed)
        {
            if (Obs.Count == 0) return Vector3.zero;
            Vector3 right = new Vector3(dir.z, 0f, -dir.x); // 90° clockwise from dir (horizontal)

            float bestSide = 0f;
            float bestUrgency = 0f;
            for (int i = 0; i < Obs.Count; i++)
            {
                var o = Obs[i];
                float dx = o.X - pos.x, dz = o.Z - pos.z;
                float ahead = dx * dir.x + dz * dir.z;          // distance in front of the walker
                if (ahead < 0f || ahead > lookahead) continue;
                float side = dx * right.x + dz * right.z;         // signed lateral offset (right +)
                float clear = agentRadius + o.Radius;            // need this much side clearance
                if (Mathf.Abs(side) > clear) continue;           // already clear of it

                // urgency: highest for an obstacle dead-ahead and close
                float aheadFactor = 1f - Mathf.Clamp01(ahead / lookahead);
                float centerFactor = 1f - Mathf.Abs(side) / clear;
                float urgency = aheadFactor * centerFactor;
                if (urgency > bestUrgency)
                {
                    bestUrgency = urgency;
                    // veer toward whichever side the obstacle ISN'T (away from its centre);
                    // if dead-centre, pick a consistent side so the walker commits.
                    bestSide = side >= 0f ? -1f : 1f;
                }
            }

            if (bestUrgency <= 0f) return Vector3.zero;
            return right * (bestSide * maxSpeed * bestUrgency);
        }
    }
}
