using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Player movement (packet required system: PlayerController): free 2-axis
    /// control via CharacterController so planted shoes physically block the
    /// lizard. Owns dash, hit knockback + invulnerability blink, and death squash.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        public LizardBody Body { get; private set; }
        public bool IsDashing { get; private set; }
        public bool IsInvulnerable { get { return Time.time < _invulnerableUntil || _camouflaged; } }
        /// <summary>Jumped high enough to clear a footfall (kill tests ignore us).</summary>
        public bool IsAirborne { get { return transform.position.y > 0.7f; } }
        public float DashCooldownRemaining { get { return Mathf.Max(0f, _dashReadyAt - Time.time); } }
        public Vector3 Velocity { get; private set; }

        private CharacterController _cc;
        private AbilityModifiers _mods;
        private float _dashEndTime;
        private float _dashReadyAt;
        private float _invulnerableUntil;
        private Vector3 _dashDir;
        private Vector3 _knockback;
        private float _dustTimer;
        private float _vVel;        // vertical velocity (jump / gravity)
        private int _jumpsLeft;     // for double-jump lizards
        private float _stillTime;   // for camouflage
        private bool _camouflaged;

        public void Init()
        {
            Instance = this;
            gameObject.name = "Lizard";
            gameObject.tag = "Player";

            // Collider scaled to the realistic ~0.2u lizard (2026-06-16); skin width
            // and step offset shrunk to match or the tiny controller misbehaves.
            _cc = GetComponent<CharacterController>();
            _cc.radius = 0.04f;
            _cc.height = 0.08f;
            _cc.center = new Vector3(0f, 0.04f, 0f);
            _cc.skinWidth = 0.012f;
            _cc.minMoveDistance = 0f;
            _cc.slopeLimit = 60f;
            _cc.stepOffset = 0.02f;

            Body = LizardBody.Build(transform);

            _mods = MetaProgress.SelectedLizard.Modifiers;

            GameEvents.PlayerHit += OnHit;
            GameEvents.PlayerDied += OnDied;
            GameEvents.PlayerRevived += OnRevived;
        }

        private void OnDestroy()
        {
            GameEvents.PlayerHit -= OnHit;
            GameEvents.PlayerDied -= OnDied;
            GameEvents.PlayerRevived -= OnRevived;
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            var gm = GameStateManager.Instance;
            if (gm == null) return;

            if (gm.State == GameState.Ready)
            {
                Body.AnimateIdle();
                return;
            }
            if (gm.State != GameState.Playing)
                return;

            Vector2 input = InputProvider.Move;
            Vector3 wish = new Vector3(input.x, 0f, input.y);
            bool action = InputProvider.ConsumeDash();   // the single ability button
            bool jumpLizard = _mods.CanDoubleJump;       // Anole hops instead of dashing

            // ground state (jumps reset on landing)
            bool grounded = _cc.isGrounded;
            if (grounded && _vVel <= 0f)
            {
                _vVel = -2f;
                _jumpsLeft = jumpLizard ? 2 : 0;
            }

            // --- ability button: jump (Anole) or dash (everyone else) ---
            if (action)
            {
                if (jumpLizard && _jumpsLeft > 0)
                {
                    _vVel = GameConst.JumpVelocity;
                    _jumpsLeft--;
                    GameAudio.Play(Sfx.Dash);   // reuse whoosh for the hop
                    ParticleFx.DashDust(transform.position);
                }
                else if (!jumpLizard && !IsDashing && Time.time >= _dashReadyAt)
                {
                    IsDashing = true;
                    _dashEndTime = Time.time + GameConst.DashDuration;
                    _dashReadyAt = Time.time + GameConst.DashCooldown * _mods.DashCooldownMult;
                    _dashDir = wish.sqrMagnitude > 0.04f ? wish.normalized : transform.forward;
                    GameAudio.Play(Sfx.Dash);
                    ParticleFx.DashDust(transform.position);
                }
            }
            if (IsDashing && Time.time >= _dashEndTime)
                IsDashing = false;

            // --- camouflage: stand still to vanish (Chameleon) ---
            if (_mods.CanCamouflage && wish.sqrMagnitude < 0.02f && !IsDashing)
                _stillTime += Time.deltaTime;
            else
                _stillTime = 0f;
            _camouflaged = _mods.CanCamouflage && _stillTime >= GameConst.CamouflageDelay;

            // --- horizontal movement (species sprint multiplier) ---
            float moveSpeed = GameConst.LizardMoveSpeed * _mods.MoveSpeedMult;
            Vector3 move = IsDashing ? _dashDir * GameConst.DashSpeed
                                     : wish * moveSpeed;

            // hit knockback decays fast but overrides steering for a beat
            if (_knockback.sqrMagnitude > 0.05f)
            {
                move += _knockback;
                _knockback = Vector3.MoveTowards(_knockback, Vector3.zero, 30f * Time.deltaTime);
            }

            // --- vertical: off-street uses gravity/jumps; on the city street the
            //     lizard rides the analytic ground height (below) instead ---
            if (StreetGround.Active) _vVel = 0f;
            else _vVel -= GameConst.JumpGravity * Time.deltaTime;

            Vector3 displacement = move * Time.deltaTime + Vector3.up * (_vVel * Time.deltaTime);
            _cc.Move(displacement);

            // clamp to the playable corridor and ride the ground height (so the
            // lizard climbs onto raised sidewalks and drops to the road smoothly)
            Vector3 p = transform.position;
            float clampedX = Mathf.Clamp(p.x, -GameConst.CorridorHalfWidth, GameConst.CorridorHalfWidth);
            float clampedZ = Mathf.Max(p.z, -4f);
            float groundY = StreetGround.HeightAt(clampedX);
            float ridingY = StreetGround.Active
                ? Mathf.Lerp(p.y, groundY, 1f - Mathf.Exp(-14f * Time.deltaTime))
                : p.y;
            transform.position = new Vector3(clampedX, ridingY, clampedZ);

            Velocity = move;

            // facing
            Vector3 face = IsDashing ? _dashDir : wish;
            if (face.sqrMagnitude > 0.04f)
            {
                Quaternion target = Quaternion.LookRotation(face.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target,
                    GameConst.LizardTurnSpeedDeg * Time.deltaTime);
            }

            // body animation + scuttle dust while sprinting
            float speed01 = Mathf.Clamp01(move.magnitude / GameConst.DashSpeed);
            Body.AnimateRun(move.magnitude, IsDashing);
            if (move.magnitude > 1f)
            {
                _dustTimer -= Time.deltaTime;
                if (_dustTimer <= 0f)
                {
                    _dustTimer = Mathf.Lerp(0.4f, 0.12f, speed01);
                    if (IsDashing) ParticleFx.DashDust(transform.position);
                }
            }

            Body.SetBlink(IsInvulnerable);
        }

        private void OnHit(int heartsLeft, Vector3 hazardPos)
        {
            _invulnerableUntil = Time.time + GameConst.HitInvulnerableTime;

            // fling away from the hazard, never out of the corridor's far edge
            Vector3 away = transform.position - hazardPos;
            away.y = 0f;
            away = away.sqrMagnitude > 0.01f ? away.normalized : Vector3.back;
            _knockback = away * GameConst.HitKnockback;

            IsDashing = false;
            GameAudio.Play(Sfx.Hit);
        }

        private void OnDied(DeathCause cause)
        {
            Body.Squash();
            GameAudio.Play(Sfx.Death);
        }

        private void OnRevived()
        {
            _invulnerableUntil = Time.time + GameConst.ReviveInvulnerableTime;
            _knockback = Vector3.zero;
            _vVel = 0f;
            IsDashing = false;
            Body.Unsquash();
            // step back from the kill spot so we don't get instantly re-stomped
            transform.position += Vector3.back * 1.5f;
            GameAudio.Play(Sfx.Win);   // upbeat revive sting
        }

        /// <summary>World-space position used by hazard footprint kill tests.</summary>
        public Vector3 KillCheckPosition { get { return transform.position; } }
    }
}
