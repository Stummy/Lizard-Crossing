using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Alley hazard: junk falls from the buildings above (a brick, a can, a crate)
    /// and the lizard must not be under it when it lands. A shadow telegraphs the
    /// landing spot and grows as the object drops; impact = squish if the lizard is
    /// caught. Cycles at random spots across the lane. Realistic-scale debris
    /// (~0.4u) — small to us, lethal to the 0.2u lizard.
    /// </summary>
    public class DebrisHazard : MonoBehaviour
    {
        const float FallHeight = 5f;
        const float KillRadius = 0.2f;

        enum Phase { Idle, Warn, Fall, Rest }
        Phase _phase;
        float _timer;
        float _z;                 // lane center
        float _laneDepth;
        float _respawnDelay;
        float _warnLead;
        float _fallTime;
        System.Random _rng;
        Vector3 _target;          // landing spot
        Transform _debris;
        WarningMarker _marker;

        public static DebrisHazard Spawn(Transform parent, LaneSpec lane)
        {
            var go = new GameObject("DebrisHazard");
            go.transform.SetParent(parent, false);
            var d = go.AddComponent<DebrisHazard>();
            d._z = lane.Z;
            d._laneDepth = 3f;
            d._respawnDelay = lane.RespawnDelay * 0.6f;   // alley feels busy
            d._warnLead = Mathf.Max(GameConst.MinWarningLead, lane.StepDuration * 1.4f);
            d._fallTime = 0.42f;
            d._rng = new System.Random(Mathf.RoundToInt(lane.Z) * 911 + lane.Dir);
            d._marker = WarningMarker.Create(go.transform, KillRadius * 2.4f, KillRadius * 2.4f);
            d.BuildDebris();
            d._timer = lane.StartDelay;
            d._phase = Phase.Idle;
            d.HideDebris();
            return d;
        }

        void BuildDebris()
        {
            _debris = new GameObject("junk").transform;
            _debris.SetParent(transform, false);
            // a small crate with a couple of stray bits
            Box(Vector3.zero, new Vector3(0.34f, 0.3f, 0.34f), new Color(0.55f, 0.4f, 0.24f));
            Box(new Vector3(0.05f, 0.18f, 0.02f), new Vector3(0.36f, 0.06f, 0.36f), new Color(0.4f, 0.28f, 0.16f));
        }

        void Box(Vector3 pos, Vector3 scale, Color col)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(_debris, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(8f, 24f, -6f);
            go.GetComponent<Renderer>().sharedMaterial = MaterialCache.GetLit(col);
        }

        void HideDebris() { if (_debris != null) _debris.gameObject.SetActive(false); }

        void LateUpdate()
        {
            var gm = GameStateManager.Instance;
            if (gm == null) return;
            if (gm.State != GameState.Ready && gm.State != GameState.Playing) return;
            bool playing = gm.State == GameState.Playing;
            _timer -= Time.deltaTime;

            switch (_phase)
            {
                case Phase.Idle:
                    if (_timer <= 0f) ChooseTarget();
                    break;

                case Phase.Warn:
                {
                    float t = 1f - Mathf.Clamp01(_timer / _warnLead);
                    _marker.SetIntensity(t);
                    if (_timer <= 0f) { _phase = Phase.Fall; _timer = _fallTime; _debris.gameObject.SetActive(true); }
                    break;
                }

                case Phase.Fall:
                {
                    float t = 1f - Mathf.Clamp01(_timer / _fallTime);
                    float y = FallHeight * (1f - t * t);   // accelerating drop
                    _debris.position = new Vector3(_target.x, y + 0.15f, _target.z);
                    _debris.Rotate(140f * Time.deltaTime, 90f * Time.deltaTime, 0f);
                    _marker.SetIntensity(1f);
                    if (_timer <= 0f)
                    {
                        _debris.position = new Vector3(_target.x, 0.15f, _target.z);
                        HazardParts.DoImpact(_target, 0.5f);
                        if (playing) KillCheck();
                        _marker.Hide();
                        _phase = Phase.Rest; _timer = 0.6f;
                    }
                    break;
                }

                case Phase.Rest:
                    if (_timer <= 0f) { HideDebris(); _phase = Phase.Idle; _timer = _respawnDelay; }
                    break;
            }
        }

        void ChooseTarget()
        {
            float half = GameConst.CorridorHalfWidth - 1f;
            float x = (float)(_rng.NextDouble() * 2.0 - 1.0) * half;
            float z = _z + (float)(_rng.NextDouble() * 2.0 - 1.0) * _laneDepth;
            _target = new Vector3(x, 0f, z);
            _marker.SetWorldPosition(_target);
            _marker.SetIntensity(0f);
            _phase = Phase.Warn;
            _timer = _warnLead;
        }

        void KillCheck()
        {
            var player = PlayerController.Instance;
            var gm = GameStateManager.Instance;
            if (player == null || gm == null || gm.State != GameState.Playing) return;
            if (player.IsInvulnerable || player.IsAirborne) return;

            Vector3 p = player.KillCheckPosition;
            float kill = KillRadius - GameConst.StompKillPad;
            float dist = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(_target.x, _target.z));
            if (dist <= kill) gm.HitPlayer(_target);
            else if (dist < GameConst.CloseCallRadius + KillRadius) GameEvents.RaiseNearMiss(_target);
        }
    }
}
