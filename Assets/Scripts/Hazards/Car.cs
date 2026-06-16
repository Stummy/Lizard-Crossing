using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Road hazard: a car drives across the corridor (±X) as cross-traffic — the
    /// classic Crossy-Road "read the gap and dash" danger. Built procedurally
    /// (no car model in the kit), sized realistically (~4.5u long) so it dwarfs the
    /// ~0.2u lizard. Lethal along its whole body while crossing; a warning strip on
    /// the entry curb lights as it approaches (buildings block sightlines by design).
    /// Walks as ambient traffic from the moment the level loads (Ready), kills only
    /// while Playing.
    /// </summary>
    public class Car : MonoBehaviour
    {
        const float Length = 4.4f;   // along travel (X)
        const float Width = 1.9f;    // across (Z)
        const float Height = 1.5f;
        const float Margin = 12f;    // how far off-corridor it starts/ends

        int _dir;
        float _crossZ;
        float _speed;
        float _startX, _endX;
        float _respawnDelay, _restTimer;
        bool _resting, _enteredCorridor;
        Vector3 _walkDir;
        Transform _holder;
        WarningMarker _marker;

        static readonly Color[] Bodies =
        {
            new Color(0.80f, 0.18f, 0.16f), new Color(0.15f, 0.32f, 0.62f),
            new Color(0.90f, 0.78f, 0.20f), new Color(0.85f, 0.85f, 0.88f),
            new Color(0.12f, 0.14f, 0.16f),
        };

        public static Car Spawn(Transform parent, LaneSpec lane)
        {
            var go = new GameObject("Car");
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<Car>();
            c._dir = lane.Dir >= 0 ? 1 : -1;
            c._crossZ = lane.Z;
            c._respawnDelay = lane.RespawnDelay;
            c._restTimer = lane.StartDelay;
            c._resting = true;
            c._walkDir = new Vector3(c._dir, 0f, 0f);
            // cars are quick; faster lanes (smaller StepDuration) are faster cars
            c._speed = 7f * lane.Scale * (0.55f / Mathf.Max(0.2f, lane.StepDuration));
            c._startX = c._dir > 0 ? -(GameConst.CorridorHalfWidth + Margin) : (GameConst.CorridorHalfWidth + Margin);
            c._endX = -c._startX;

            int variant = (Mathf.Abs(Mathf.RoundToInt(lane.Z)) * 3 + (c._dir > 0 ? 1 : 0)) % Bodies.Length;
            c.BuildCar(Bodies[variant]);
            c._marker = WarningMarker.Create(go.transform, Width * 1.4f, Length * 0.5f);
            c._marker.transform.rotation = Quaternion.Euler(90f, c._dir > 0 ? 90f : -90f, 0f);
            c.PlaceAtStart();
            return c;
        }

        void BuildCar(Color body)
        {
            _holder = new GameObject("car").transform;
            _holder.SetParent(transform, false);
            _holder.rotation = Quaternion.LookRotation(_walkDir, Vector3.up); // local +Z = travel

            var dark = new Color(0.08f, 0.08f, 0.09f);
            var glass = new Color(0.4f, 0.55f, 0.65f);
            // local frame: length along Z, width along X
            Box(new Vector3(0f, 0.42f, 0f), new Vector3(Width, 0.55f, Length), body, "Body");
            Box(new Vector3(0f, 0.85f, -0.15f), new Vector3(Width * 0.82f, 0.5f, Length * 0.5f), glass, "Cabin");
            Box(new Vector3(0f, 0.72f, 0f), new Vector3(Width * 0.9f, 0.12f, Length * 0.96f), Color.Lerp(body, Color.black, 0.25f), "Beltline");
            // wheels
            float wx = Width * 0.5f, wz = Length * 0.32f;
            foreach (var s in new[] { new Vector2(wx, wz), new Vector2(-wx, wz), new Vector2(wx, -wz), new Vector2(-wx, -wz) })
                Cyl(new Vector3(s.x, 0.18f, s.y), 0.18f, dark);
            // head/tail lights (front = +Z)
            Sphere(new Vector3(wx * 0.7f, 0.42f, Length * 0.5f), 0.12f, new Color(1f, 0.96f, 0.8f), "Head");
            Sphere(new Vector3(-wx * 0.7f, 0.42f, Length * 0.5f), 0.12f, new Color(1f, 0.96f, 0.8f), "Head");
            Sphere(new Vector3(wx * 0.7f, 0.42f, -Length * 0.5f), 0.1f, new Color(0.8f, 0.1f, 0.08f), "Tail");
            Sphere(new Vector3(-wx * 0.7f, 0.42f, -Length * 0.5f), 0.1f, new Color(0.8f, 0.1f, 0.08f), "Tail");
        }

        void PlaceAtStart()
        {
            transform.position = new Vector3(_startX, GameConst.GroundY, _crossZ);
            _enteredCorridor = false;
            if (_holder != null) _holder.gameObject.SetActive(false);
            if (_marker != null) _marker.Hide();
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
            float x = transform.position.x;

            // entry-curb warning: light the strip while the car is still off the corridor
            if (playing && !_enteredCorridor)
            {
                float edge = _dir > 0 ? -GameConst.CorridorHalfWidth : GameConst.CorridorHalfWidth;
                float distToEdge = Mathf.Abs(edge - x);
                _marker.SetWorldPosition(new Vector3(edge, 0f, _crossZ));
                _marker.SetIntensity(Mathf.Clamp01(1f - distToEdge / 7f));
            }

            bool inCorridor = Mathf.Abs(x) <= GameConst.CorridorHalfWidth + Length * 0.5f;
            if (inCorridor && !_enteredCorridor)
            {
                _enteredCorridor = true;
                _marker.Hide();
                HazardParts.DoImpact(new Vector3(x, 0f, _crossZ), 1f); // engine rumble as it arrives
            }
            if (playing && inCorridor) KillCheck();

            bool past = _dir > 0 ? x > _endX : x < _endX;
            if (past) { _resting = true; _restTimer = _respawnDelay; PlaceAtStart(); }
        }

        void KillCheck()
        {
            var player = PlayerController.Instance;
            var gm = GameStateManager.Instance;
            if (player == null || gm == null || gm.State != GameState.Playing) return;
            if (player.IsInvulnerable || player.IsAirborne) return;

            // travel is along world X, so a world-space box at the lane Z is exact
            Vector3 p = player.KillCheckPosition;
            float halfL = Length * 0.5f - GameConst.StompKillPad;
            float halfW = Width * 0.5f - GameConst.StompKillPad;
            if (Mathf.Abs(p.x - transform.position.x) <= halfL && Mathf.Abs(p.z - _crossZ) <= halfW)
                gm.HitPlayer(new Vector3(p.x, 0f, _crossZ));
        }

        // ---- primitive helpers (children of _holder, local frame) ----
        void Box(Vector3 pos, Vector3 scale, Color col, string name) { Prim(PrimitiveType.Cube, pos, scale, col, name); }
        void Sphere(Vector3 pos, float r, Color col, string name) { Prim(PrimitiveType.Sphere, pos, Vector3.one * r * 2f, col, name); }
        void Cyl(Vector3 pos, float r, Color col)
        {
            var go = Prim(PrimitiveType.Cylinder, pos, new Vector3(r * 2f, 0.12f, r * 2f), col, "Wheel");
            go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); // axle along X
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
