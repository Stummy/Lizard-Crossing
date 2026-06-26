using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Per-crossing pedestrian signal at a ROAD lane. Cycles between CARS-GO (vehicles sweep
    /// the crosswalk ±X) and a SAFE window (a recurring gap the auto-running lizard can thread).
    /// It GATES the crossing cars (Car's goGate) so they hold off-screen during the safe window,
    /// and drives a red/green lamp on a small post set just before the crossing so the window is
    /// telegraphed to the approaching lizard. Built by StreetTraffic at each LaneType.Road lane.
    ///
    /// The lizard auto-runs (can't stop at red), so this isn't a stop-and-wait light — it
    /// guarantees the crossing is always passable (a clean gap comes around) and tells the player
    /// whether to brace-and-dodge (red: a wave is crossing) or that it's clear (green).
    /// </summary>
    public class TrafficLight : MonoBehaviour
    {
        float _cycle, _safe, _phase;
        Renderer _red, _green;
        bool _carsGo = true, _applied;

        // Lamp colours (bright = lit, dim = off).
        static readonly Color RedOn = new Color(0.95f, 0.06f, 0.05f);
        static readonly Color RedOff = new Color(0.16f, 0.02f, 0.02f);
        static readonly Color GreenOn = new Color(0.10f, 0.95f, 0.25f);
        static readonly Color GreenOff = new Color(0.03f, 0.13f, 0.05f);

        /// <summary>True while vehicles may enter the crossing (red for the pedestrian lizard).</summary>
        public bool CarsMayGo => _carsGo;

        public static TrafficLight Create(Transform parent, float z, float cycle, float safe, float offset)
        {
            var go = new GameObject("TrafficLight_z" + Mathf.RoundToInt(z));
            go.transform.SetParent(parent, false);
            var tl = go.AddComponent<TrafficLight>();
            tl._cycle = Mathf.Max(1f, cycle);
            tl._safe = Mathf.Clamp(safe, 0.5f, cycle - 0.5f);
            tl._phase = ((offset % tl._cycle) + tl._cycle) % tl._cycle;
            tl.BuildPost(z);
            tl.Tick(0f); // set the initial lamp state
            return tl;
        }

        void Update() { Tick(Time.deltaTime); }

        void Tick(float dt)
        {
            _phase = (_phase + dt) % _cycle;
            // the last `_safe` seconds of each cycle are the SAFE window (cars held, green for ped)
            bool carsGo = _phase < (_cycle - _safe);
            if (carsGo == _carsGo && _applied) return;

            _carsGo = carsGo;
            _applied = true;
            if (_red != null) _red.sharedMaterial = MaterialCache.GetEmissive(carsGo ? RedOn : RedOff);
            if (_green != null) _green.sharedMaterial = MaterialCache.GetEmissive(carsGo ? GreenOff : GreenOn);
        }

        // A slim dark post + housing with a red (top) and green (bottom) lamp, set on the OPEN
        // LEFT road-edge a few metres BEFORE the crosswalk: out of the lizard's run band and
        // clear of both car streams, with no tall right wall to occlude it — so it's plainly in
        // view (against the open road/sky) as the lizard runs up to the crossing. The lamp head
        // cants slightly toward the oncoming lizard so the lit lamp faces the camera.
        void BuildPost(float z)
        {
            float x = GameConst.CorridorFenceLeftX - 1.0f; // ~4.8 — past the curb, on the road edge, out of band
            float pz = z - 3.4f;                            // stand a few metres before the zebra
            var dark = new Color(0.11f, 0.11f, 0.12f);

            MakeCube("Post", new Vector3(x, 1.4f, pz), new Vector3(0.14f, 2.8f, 0.14f), dark);
            MakeCube("Head", new Vector3(x, 2.75f, pz - 0.05f), new Vector3(0.34f, 0.92f, 0.26f), dark);
            _red = MakeLamp("RedLamp", new Vector3(x, 2.98f, pz - 0.20f));
            _green = MakeLamp("GreenLamp", new Vector3(x, 2.56f, pz - 0.20f));
        }

        GameObject MakeCube(string name, Vector3 pos, Vector3 scale, Color col)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = MaterialCache.GetLit(col);
            return go;
        }

        Renderer MakeLamp(string name, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * 0.28f;
            return go.GetComponent<Renderer>();
        }
    }
}
