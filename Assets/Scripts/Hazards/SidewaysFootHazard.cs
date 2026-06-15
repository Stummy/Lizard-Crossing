using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// The hero hazard (packet required system: SidewaysFootHazard): a pair of
    /// giant shoes striding ACROSS the corridor — always perpendicular to the
    /// lizard's route, like cross-traffic. Each footfall is a mini-stomp whose
    /// WarningMarker grows at the next landing spot while the foot is airborne,
    /// so a skilled player reads the stride rhythm and weaves between steps.
    /// </summary>
    public class SidewaysFootHazard : MonoBehaviour
    {
        private const float BaseSoleLength = 11f;
        private const float BaseSoleWidth = 4.5f;
        private const float StanceWidth = 5.5f;   // z-gap between left/right tracks
        private const float ArcHeight = 7f;

        private float _soleLength;
        private float _soleWidth;
        private float _stepLength;
        private float _stepDuration;
        private float _bothDownPause;
        private float _respawnDelay;
        private float _crossZ;
        private int _dir;             // +1 strides toward +x, -1 toward -x
        private float _startX;
        private float _endX;

        private readonly Transform[] _feet = new Transform[2];
        private readonly WarningMarker[] _markers = new WarningMarker[2];
        private readonly Vector3[] _planted = new Vector3[2];

        private enum Phase { Idle, Windup, Flight }

        private int _movingFoot;
        private float _stepTimer;
        private float _windupDuration;
        private float _idleTimer;
        private Vector3 _stepFrom, _stepTo;
        private Phase _phase = Phase.Idle;

        // While a foot winds up (planted, telegraph already glowing) before it
        // launches, the marker pre-fills to this intensity so every lane gives a
        // readable lead even when the stride is fast (see GameConst.MinWarningLead).
        private const float PreviewIntensity = 0.45f;

        public static SidewaysFootHazard Spawn(Transform parent, LaneSpec lane)
        {
            var go = new GameObject("SidewaysFootHazard");
            go.transform.SetParent(parent, false);

            var w = go.AddComponent<SidewaysFootHazard>();
            w._crossZ = lane.Z;
            w._dir = lane.Dir >= 0 ? 1 : -1;
            w._soleLength = BaseSoleLength * lane.Scale;
            w._soleWidth = BaseSoleWidth * lane.Scale;
            w._stepLength = 8.5f * lane.Scale;
            w._stepDuration = lane.StepDuration;
            w._bothDownPause = 0.12f;
            w._respawnDelay = lane.RespawnDelay;
            w._idleTimer = lane.StartDelay;

            float margin = GameConst.CorridorHalfWidth + 16f;
            w._startX = w._dir > 0 ? -margin : margin;
            w._endX = -w._startX;

            // direction self-check from the packet's testing kit
            var hint = go.AddComponent<SidewaysHazardDirectionHint>();
            hint.movementDirection = Vector3.right * w._dir;

            int variant = (Mathf.Abs(Mathf.RoundToInt(lane.Z)) * 7 + (w._dir > 0 ? 1 : 0)) % HazardParts.ShoeColors.Length;
            for (int i = 0; i < 2; i++)
            {
                w._feet[i] = HazardParts.BuildShoe(go.transform,
                    HazardParts.ShoeColors[variant],
                    HazardParts.AccentColors[variant % HazardParts.AccentColors.Length],
                    HazardParts.PantColors[variant % HazardParts.PantColors.Length],
                    w._soleLength, w._soleWidth);
                w._feet[i].rotation = w.WalkRotation(0f);
                w._markers[i] = WarningMarker.Create(go.transform, w._soleLength, w._soleWidth);
                w._markers[i].transform.rotation = Quaternion.Euler(90f, w._dir > 0 ? 90f : -90f, 0f);
            }
            w.ResetToStart();
            return w;
        }

        /// <summary>Shoes point along the walk direction (across the corridor).</summary>
        private Quaternion WalkRotation(float pitchDeg)
        {
            return Quaternion.Euler(pitchDeg * _dir, _dir > 0 ? 90f : -90f, 0f);
        }

        private void ResetToStart()
        {
            _planted[0] = new Vector3(_startX, 0f, _crossZ + StanceWidth * 0.5f);
            _planted[1] = new Vector3(_startX - _dir * _stepLength * 0.5f, 0f, _crossZ - StanceWidth * 0.5f);
            for (int i = 0; i < 2; i++)
            {
                _feet[i].position = _planted[i];
                _feet[i].GetComponent<Collider>().enabled = false; // off-corridor, irrelevant
                // hidden while idle: parked pant legs read as weird static towers
                _feet[i].gameObject.SetActive(false);
                _markers[i].Hide();
            }
            _movingFoot = 1; // trailing foot steps first
            _phase = Phase.Idle;
            _stepTimer = 0f;
        }

        private void Update()
        {
            var gm = GameStateManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;

            switch (_phase)
            {
                case Phase.Idle:
                    _idleTimer -= Time.deltaTime;
                    if (_idleTimer <= 0f) StartWindup();
                    break;

                case Phase.Windup:
                {
                    _stepTimer += Time.deltaTime;
                    float w = _windupDuration > 0f ? Mathf.Clamp01(_stepTimer / _windupDuration) : 1f;
                    // telegraph glows in place before the foot lifts; a small toe
                    // raise on the moving foot sells the wind-up
                    _markers[_movingFoot].SetIntensity(Mathf.Lerp(0f, PreviewIntensity, w));
                    _feet[_movingFoot].rotation = WalkRotation(Mathf.Lerp(0f, -10f, w));
                    if (w >= 1f) BeginFlight();
                    break;
                }

                case Phase.Flight:
                    UpdateFlight();
                    break;
            }
        }

        private void UpdateFlight()
        {
            _stepTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_stepTimer / _stepDuration);
            Transform foot = _feet[_movingFoot];

            Vector3 pos = Vector3.Lerp(_stepFrom, _stepTo, t);
            pos.y = Mathf.Sin(t * Mathf.PI) * ArcHeight;
            foot.position = pos;
            // pitch the shoe through the stride: toe-up on lift, toe-down landing
            foot.rotation = WalkRotation(Mathf.Sin(t * Mathf.PI * 2f) * -18f);

            // telegraph finishes filling from the wind-up preview to full as it lands
            _markers[_movingFoot].SetIntensity(Mathf.Lerp(PreviewIntensity, 1f, t));

            if (t < 1f) return;

            foot.position = _stepTo;
            foot.rotation = WalkRotation(0f);
            foot.GetComponent<Collider>().enabled = InCorridor(_stepTo);
            _planted[_movingFoot] = _stepTo;
            _markers[_movingFoot].Hide();

            if (Mathf.Abs(_stepTo.x) < GameConst.CorridorHalfWidth + 8f)
            {
                HazardParts.DoImpact(_stepTo, _soleWidth / BaseSoleWidth);
                HazardParts.ResolveSlam(foot, _stepTo, _soleLength, _soleWidth);
            }

            _movingFoot = 1 - _movingFoot;
            StartWindup();
        }

        /// <summary>
        /// Prepare the next stride: place its telegraph and hold the foot planted
        /// for a wind-up long enough that the total warning lead (wind-up + flight)
        /// is at least GameConst.MinWarningLead, no matter how fast the stride is.
        /// </summary>
        private void StartWindup()
        {
            // finished crossing the corridor? rest off-screen, then respawn
            if ((_dir > 0 && _planted[_movingFoot].x > _endX) ||
                (_dir < 0 && _planted[_movingFoot].x < _endX))
            {
                _idleTimer = _respawnDelay;
                ResetToStart();
                return;
            }

            if (!_feet[0].gameObject.activeSelf)
            {
                _feet[0].gameObject.SetActive(true);
                _feet[1].gameObject.SetActive(true);
            }

            _stepFrom = _planted[_movingFoot];
            // step lands ahead of the *other* foot, keeping the natural alternating stride
            float targetX = _planted[1 - _movingFoot].x + _dir * _stepLength;
            _stepTo = new Vector3(targetX, 0f,
                _crossZ + (_movingFoot == 0 ? 1f : -1f) * StanceWidth * 0.5f);

            _feet[_movingFoot].GetComponent<Collider>().enabled = false;
            _markers[_movingFoot].SetWorldPosition(_stepTo);

            _windupDuration = Mathf.Max(_bothDownPause, GameConst.MinWarningLead - _stepDuration);
            _stepTimer = 0f;
            _phase = Phase.Windup;
        }

        private void BeginFlight()
        {
            _stepTimer = 0f;
            _phase = Phase.Flight;
        }

        private bool InCorridor(Vector3 p)
        {
            return Mathf.Abs(p.x) <= GameConst.CorridorHalfWidth + _soleWidth;
        }
    }
}
