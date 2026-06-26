using System.Collections.Generic;
using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// A car, built procedurally (no car model in the kit), sized realistically
    /// (~4.4u long) so it dwarfs the ~0.2u lizard. Drives from a start point to an
    /// end point along any horizontal direction, then recycles — used for traffic
    /// driving DOWN the avenue (±Z) on the road. Lethal along its whole body while
    /// the run is Playing. Drives as ambient traffic from level load (Ready too).
    /// </summary>
    public class Car : MonoBehaviour
    {
        const float Length = 4.4f;   // along travel (local +Z)
        const float Width = 1.9f;    // across (local X)
        const float Height = 1.5f;
        const float Margin = 12f;    // how far off-corridor a crossing car starts/ends

        float _speed;
        Vector3 _startPos, _endPos, _walkDir;
        float _respawnDelay, _restTimer;
        bool _resting, _rumbled, _nearMissed;
        Transform _holder;
        System.Func<bool> _goGate;   // optional: car holds at the start line until this returns true (traffic light)

        // Real Kenney "Car Kit" GLBs (CC0, ~2k tris, shared palette atlas) replacing the old
        // procedural box-car. Biased toward the yellow taxi for the NYC read; the rest add body
        // variety. The KillCheck/lane/speed logic below is UNCHANGED — this is a visual swap only.
        const string VehFolder = "Models/CityKit/Vehicles/";
        // Weighted bag: taxi appears 3× so roughly half the traffic is the yellow cab.
        static readonly string[] VehBag =
        {
            "taxi", "taxi", "taxi", "sedan", "suv", "suv_luxury", "van",
        };
        static readonly Dictionary<string, GameObject> _vehCache = new Dictionary<string, GameObject>();

        /// <summary>Legacy crossing-lane spawn (kit-city fallback): drives across ±X.</summary>
        public static Car Spawn(Transform parent, LaneSpec lane)
        {
            int dir = lane.Dir >= 0 ? 1 : -1;
            float margin = GameConst.CorridorHalfWidth + Margin;
            Vector3 start = new Vector3(dir > 0 ? -margin : margin, GameConst.GroundY, lane.Z);
            Vector3 end = new Vector3(-start.x, GameConst.GroundY, lane.Z);
            float speed = 7f * lane.Scale * (0.55f / Mathf.Max(0.2f, lane.StepDuration));
            return SpawnTrack(parent, start, end, speed, lane.StartDelay, lane.RespawnDelay);
        }

        /// <summary>Drive from start to end along any direction (e.g. down the road, ±Z).
        /// <paramref name="goGate"/> (optional) holds the car at its start line until it returns
        /// true — used by a crossing's traffic light so cars only enter on its "cars-go" phase.</summary>
        public static Car SpawnTrack(Transform parent, Vector3 start, Vector3 end,
            float speed, float startDelay, float respawnDelay, float startProgress = 0f,
            System.Func<bool> goGate = null)
        {
            var go = new GameObject("Car");
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<Car>();
            c._startPos = start;
            c._endPos = end;
            c._walkDir = (end - start).normalized;
            c._speed = speed;
            c._respawnDelay = respawnDelay;
            c._restTimer = startDelay;
            c._resting = true;
            c._goGate = goGate;

            int variant = (Mathf.Abs(Mathf.RoundToInt(start.x + start.z * 1.7f))) % VehBag.Length;
            c.BuildCar(VehBag[variant]);
            c.PlaceAtStart();
            if (startProgress > 0f) // pre-distribute along the road so traffic is flowing at once
            {
                c.transform.position = Vector3.Lerp(start, end, startProgress);
                c._resting = false;
                if (c._holder != null) c._holder.gameObject.SetActive(true);
            }
            return c;
        }

        void BuildCar(string kind)
        {
            _holder = new GameObject("car").transform;
            _holder.SetParent(transform, false);
            _holder.rotation = Quaternion.LookRotation(_walkDir, Vector3.up); // local +Z = travel

            var src = LoadVeh(kind);
            if (src == null) { BuildBoxCar(); return; }   // graceful fallback if the GLB is missing
            CityKitSkin.SkinVehicle(src);                  // bind the palette atlas (taxi reads yellow)

            var model = Object.Instantiate(src, _holder, false);
            model.name = kind;
            foreach (var col in model.GetComponentsInChildren<Collider>()) Object.Destroy(col); // visual only
            // The Kenney GLB's local +Z is its length (taxi native 2.75u long, faces +Z = travel),
            // matching the holder frame. Re-zero the importer's baked root transform so the car
            // points straight down the holder's +Z.
            model.transform.localRotation = Quaternion.identity;
            model.transform.localPosition = Vector3.zero;
            model.transform.localScale = Vector3.one;

            // Measure the model's LOCAL (holder-frame) bounds so the holder's travel rotation
            // doesn't skew the length read, then scale uniformly so the length (local z) = the
            // gameplay box Length (~4.4u) and sit the wheels on the ground centred on the holder.
            Bounds lb = LocalBounds(model, _holder);
            float modelLen = Mathf.Max(0.01f, lb.size.z);
            float s = Length / modelLen;
            model.transform.localScale = Vector3.one * s;
            lb = LocalBounds(model, _holder);              // re-measure in scaled local space
            model.transform.localPosition += new Vector3(-lb.center.x, -lb.min.y, -lb.center.z);
        }

        /// <summary>AABB of all renderers expressed in <paramref name="frame"/>'s local space.</summary>
        static Bounds LocalBounds(GameObject go, Transform frame)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            bool first = true; Bounds b = new Bounds();
            foreach (var r in rends)
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var mb = mf.sharedMesh.bounds;
                // 8 corners of the mesh-local AABB → frame-local
                Vector3 c = mb.center, e = mb.extents;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = c + new Vector3(
                        ((i & 1) == 0 ? -e.x : e.x),
                        ((i & 2) == 0 ? -e.y : e.y),
                        ((i & 4) == 0 ? -e.z : e.z));
                    Vector3 p = frame.InverseTransformPoint(r.transform.TransformPoint(corner));
                    if (first) { b = new Bounds(p, Vector3.zero); first = false; }
                    else b.Encapsulate(p);
                }
            }
            return b;
        }

        GameObject LoadVeh(string kind)
        {
            if (_vehCache.TryGetValue(kind, out var g)) return g;
            g = Resources.Load<GameObject>(VehFolder + kind);
            _vehCache[kind] = g;
            return g;
        }

        /// <summary>Primitive box-car fallback (only used if a vehicle GLB fails to load).</summary>
        void BuildBoxCar()
        {
            var body = new Color(0.90f, 0.78f, 0.20f);
            var dark = new Color(0.08f, 0.08f, 0.09f);
            var glass = new Color(0.4f, 0.55f, 0.65f);
            Box(new Vector3(0f, 0.42f, 0f), new Vector3(Width, 0.55f, Length), body, "Body");
            Box(new Vector3(0f, 0.85f, -0.15f), new Vector3(Width * 0.82f, 0.5f, Length * 0.5f), glass, "Cabin");
            float wx = Width * 0.5f, wz = Length * 0.32f;
            foreach (var s in new[] { new Vector2(wx, wz), new Vector2(-wx, wz), new Vector2(wx, -wz), new Vector2(-wx, -wz) })
                Cyl(new Vector3(s.x, 0.18f, s.y), 0.18f, dark);
        }

        void PlaceAtStart()
        {
            transform.position = _startPos;
            _rumbled = false;
            _nearMissed = false;
            if (_holder != null) _holder.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            var gm = GameStateManager.Instance;
            if (gm == null) return;
            if (gm.State != GameState.Ready && gm.State != GameState.Playing) return;
            bool playing = gm.State == GameState.Playing;

            if (_resting)
            {
                _restTimer -= Time.deltaTime;
                // Wait out the rest timer AND (if gated) the traffic light's cars-go phase, so
                // gated crossing cars hold off-screen during the lizard's safe window.
                bool ready = _restTimer <= 0f && (_goGate == null || _goGate());
                if (ready) { _resting = false; if (_holder != null) _holder.gameObject.SetActive(true); }
                else return;
            }

            transform.position += _walkDir * (_speed * Time.deltaTime);

            if (playing) KillCheck();

            if (Vector3.Dot(transform.position - _endPos, _walkDir) > 0f)
            { _resting = true; _restTimer = _respawnDelay; PlaceAtStart(); }
        }

        void KillCheck()
        {
            var player = PlayerController.Instance;
            var gm = GameStateManager.Instance;
            if (player == null || gm == null || gm.State != GameState.Playing) return;
            if (player.IsInvulnerable || player.IsAirborne) return;

            // oriented box: the holder's local frame has +Z along travel (length), X across (width)
            Vector3 local = _holder.InverseTransformPoint(player.KillCheckPosition);
            float halfL = Length * 0.5f - GameConst.StompKillPad;
            float halfW = Width * 0.5f - GameConst.StompKillPad;
            if (Mathf.Abs(local.z) <= halfL && Mathf.Abs(local.x) <= halfW)
            {
                gm.HitPlayer(player.KillCheckPosition, DeathCause.Squashed);
                return;
            }

            // Near-miss: this car swept PAST the lizard without crushing it — a giant taxi
            // whiffing across its nose (concept frame #4). Fire once per pass (the holder's
            // local box grown by CloseCallRadius) so the blue whoosh + slow-mo trigger.
            if (!_nearMissed)
            {
                float pad = GameConst.CloseCallRadius;
                if (Mathf.Abs(local.z) <= halfL + pad && Mathf.Abs(local.x) <= halfW + pad)
                {
                    _nearMissed = true;
                    GameEvents.RaiseNearMiss(player.KillCheckPosition);
                }
            }
        }

        void Box(Vector3 pos, Vector3 scale, Color col, string name) { Prim(PrimitiveType.Cube, pos, scale, col, name); }
        void Cyl(Vector3 pos, float r, Color col)
        {
            var go = Prim(PrimitiveType.Cylinder, pos, new Vector3(r * 2f, 0.12f, r * 2f), col, "Wheel");
            go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }
        GameObject Prim(PrimitiveType t, Vector3 pos, Vector3 scale, Color col, string name)
        {
            var go = GameObject.CreatePrimitive(t);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(_holder, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = MaterialCache.GetLit(col);
            return go;
        }
    }
}
