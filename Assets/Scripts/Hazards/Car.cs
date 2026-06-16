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
        bool _resting, _rumbled;
        Transform _holder;

        static readonly Color[] Bodies =
        {
            new Color(0.80f, 0.18f, 0.16f), new Color(0.15f, 0.32f, 0.62f),
            new Color(0.90f, 0.78f, 0.20f), new Color(0.85f, 0.85f, 0.88f),
            new Color(0.12f, 0.14f, 0.16f),
        };

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

        /// <summary>Drive from start to end along any direction (e.g. down the road, ±Z).</summary>
        public static Car SpawnTrack(Transform parent, Vector3 start, Vector3 end,
            float speed, float startDelay, float respawnDelay, float startProgress = 0f)
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

            int variant = (Mathf.Abs(Mathf.RoundToInt(start.x + start.z))) % Bodies.Length;
            c.BuildCar(Bodies[variant]);
            c.PlaceAtStart();
            if (startProgress > 0f) // pre-distribute along the road so traffic is flowing at once
            {
                c.transform.position = Vector3.Lerp(start, end, startProgress);
                c._resting = false;
                if (c._holder != null) c._holder.gameObject.SetActive(true);
            }
            return c;
        }

        void BuildCar(Color body)
        {
            _holder = new GameObject("car").transform;
            _holder.SetParent(transform, false);
            _holder.rotation = Quaternion.LookRotation(_walkDir, Vector3.up); // local +Z = travel

            var dark = new Color(0.08f, 0.08f, 0.09f);
            var glass = new Color(0.4f, 0.55f, 0.65f);
            Box(new Vector3(0f, 0.42f, 0f), new Vector3(Width, 0.55f, Length), body, "Body");
            Box(new Vector3(0f, 0.85f, -0.15f), new Vector3(Width * 0.82f, 0.5f, Length * 0.5f), glass, "Cabin");
            Box(new Vector3(0f, 0.72f, 0f), new Vector3(Width * 0.9f, 0.12f, Length * 0.96f), Color.Lerp(body, Color.black, 0.25f), "Beltline");
            float wx = Width * 0.5f, wz = Length * 0.32f;
            foreach (var s in new[] { new Vector2(wx, wz), new Vector2(-wx, wz), new Vector2(wx, -wz), new Vector2(-wx, -wz) })
                Cyl(new Vector3(s.x, 0.18f, s.y), 0.18f, dark);
            Sphere(new Vector3(wx * 0.7f, 0.42f, Length * 0.5f), 0.12f, new Color(1f, 0.96f, 0.8f), "Head");
            Sphere(new Vector3(-wx * 0.7f, 0.42f, Length * 0.5f), 0.12f, new Color(1f, 0.96f, 0.8f), "Head");
            Sphere(new Vector3(wx * 0.7f, 0.42f, -Length * 0.5f), 0.1f, new Color(0.8f, 0.1f, 0.08f), "Tail");
            Sphere(new Vector3(-wx * 0.7f, 0.42f, -Length * 0.5f), 0.1f, new Color(0.8f, 0.1f, 0.08f), "Tail");
        }

        void PlaceAtStart()
        {
            transform.position = _startPos;
            _rumbled = false;
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
                if (_restTimer <= 0f) { _resting = false; if (_holder != null) _holder.gameObject.SetActive(true); }
                else return;
            }

            transform.position += _walkDir * (_speed * Time.deltaTime);

            // one engine rumble as it nears the lizard's row
            if (playing && !_rumbled)
            {
                var pl = PlayerController.Instance;
                if (pl != null && Vector3.Distance(transform.position, pl.transform.position) < 10f)
                { _rumbled = true; HazardParts.DoImpact(transform.position, 1f); }
            }

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
                gm.HitPlayer(player.KillCheckPosition);
        }

        void Box(Vector3 pos, Vector3 scale, Color col, string name) { Prim(PrimitiveType.Cube, pos, scale, col, name); }
        void Sphere(Vector3 pos, float r, Color col, string name) { Prim(PrimitiveType.Sphere, pos, Vector3.one * r * 2f, col, name); }
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
