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
        /// <summary>Pedestrians read this so one walk-through can't chain-stagger the lizard.</summary>
        public bool CanFootBump { get { return Time.time >= _bumpCooldownUntil; } }
        /// <summary>Mid-tumble after a foot-bump (control is briefly damped).</summary>
        public bool IsStumbling { get { return Time.time < _stumbleUntil; } }
        public float DashCooldownRemaining { get { return Mathf.Max(0f, _dashReadyAt - Time.time); } }
        public Vector3 Velocity { get; private set; }

        private CharacterController _cc;
        private AbilityModifiers _mods;
        private float _dashEndTime;
        private float _dashReadyAt;
        private float _invulnerableUntil;
        private Vector3 _dashDir;
        private Vector3 _vel;        // smoothed world-space velocity — drives both movement and facing
        private Vector3 _knockback;
        private float _dustTimer;
        private float _vVel;        // vertical velocity (jump / gravity)
        private int _jumpsLeft;     // for double-jump lizards
        private float _stillTime;   // for camouflage
        private bool _camouflaged;
        private float _stumbleUntil;       // tumbling from a foot-bump until this time
        private float _bumpCooldownUntil;  // earliest next foot-bump / prop hit
        private float _controlLockUntil;   // steering ignored until this time (start of a stumble/faceplant)

        public void Init()
        {
            Instance = this;
            gameObject.name = "Lizard";
            gameObject.tag = "Player";

            // Ride the city's surfaces analytically (StreetGround.HeightAt) instead of
            // letting the CharacterController physically collide with the road / sidewalk
            // / curb meshes. The real curb riser (~0.12u) carries a MeshCollider on the
            // CityGround layer; to the tiny 2cm-step controller it reads as a wall it
            // can't climb, so the lizard caught on every curb. On its own layer with
            // Lizard×CityGround collisions off, the lizard is never wall-blocked by the
            // street and simply scrambles up/down curbs via the height-ride below — like
            // a real lizard. Hazards live on other layers, so they still block/kill.
            int lizardLayer = LayerMask.NameToLayer("Lizard");
            if (lizardLayer >= 0)
            {
                gameObject.layer = lizardLayer;
                Physics.IgnoreLayerCollision(lizardLayer, StreetGround.GroundLayer, true);
            }

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

            GameEvents.FootBumped += OnFootBumped;
            GameEvents.Faceplanted += OnFaceplanted;
            GameEvents.PlayerHit += OnHit;
            GameEvents.PlayerTailLost += OnTailLost;
            GameEvents.PlayerTailRegrown += OnTailRegrown;
            GameEvents.PlayerDied += OnDied;
            GameEvents.PlayerRevived += OnRevived;
        }

        private void OnDestroy()
        {
            GameEvents.FootBumped -= OnFootBumped;
            GameEvents.Faceplanted -= OnFaceplanted;
            GameEvents.PlayerHit -= OnHit;
            GameEvents.PlayerTailLost -= OnTailLost;
            GameEvents.PlayerTailRegrown -= OnTailRegrown;
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
                // Sit on the ground surface during the start screen too, otherwise
                // a lizard spawned on the raised sidewalk idles sunk into the curb.
                if (StreetGround.Active)
                {
                    Vector3 rp = transform.position;
                    rp.y = StreetGround.HeightAt(rp.x, rp.z);
                    transform.position = rp;
                }
                Body.AnimateIdle();
                return;
            }
            if (gm.State != GameState.Playing)
                return;

            Vector2 input = InputProvider.Move;
            // AUTO-RUN: the lizard always scurries forward (+Z) on its own; the player only
            // STEERS left/right (input.x) to weave the crowd and thread cross-traffic. input.y
            // (manual forward) is ignored. Mid-tumble the control lock freezes the whole run.
            bool locked = Time.time < _controlLockUntil;
            float steer = locked ? 0f : Mathf.Clamp(input.x, -1f, 1f);
            // forward-biased wish drives the dash direction + facing/animation
            Vector3 wish = new Vector3(steer, 0f, 1f);
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
                    _vel = _dashDir * GameConst.DashSpeed; // dash kicks in instantly
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

            // --- AUTO-RUN velocity: a constant forward (+Z) pace, plus a lateral component the
            //     player steers (±X). The two axes are independent, so steering left/right weaves
            //     without ever slowing the forward run. Velocity eases toward the target so starts,
            //     dodges and the post-stumble resume all carry a little momentum. While the control
            //     lock is active (stumble/faceplant) the whole run brakes to a stop, then resumes. ---
            float fwd = GameConst.AutoRunSpeed * _mods.MoveSpeedMult;
            Vector3 wishVel = locked ? Vector3.zero
                                     : new Vector3(steer * GameConst.LizardStrafeSpeed, 0f, fwd);

            if (IsDashing)
            {
                _vel = _dashDir * GameConst.DashSpeed;
            }
            else
            {
                float rate = locked ? GameConst.LizardDecel : GameConst.LizardAccel;
                _vel = Vector3.MoveTowards(_vel, wishVel, rate * Time.deltaTime);
            }

            Vector3 move = _vel;

            // hit knockback decays fast but overrides steering for a beat
            if (_knockback.sqrMagnitude > 0.05f)
            {
                move += _knockback;
                _knockback = Vector3.MoveTowards(_knockback, Vector3.zero, 30f * Time.deltaTime);
            }

            // --- vertical: off-street uses gravity/jumps; on the city street the
            //     lizard rides the analytic ground height instead ---
            if (StreetGround.Active) _vVel = 0f;
            else _vVel -= GameConst.JumpGravity * Time.deltaTime;

            // Build the WHOLE frame's motion as one analytic delta and apply it with a
            // SINGLE CharacterController.Move. Two pitfalls this avoids:
            //   • Calling Move and ALSO writing transform.position in the same frame
            //     desyncs the controller's capsule from the transform — that fight turns
            //     steady input (holding W) into a stutter (the jerk owner reported).
            //   • Splitting the move across several Move calls can under-resolve and the
            //     lizard barely advances. One delta, one Move = the forward step always
            //     lands, smoothly.
            Vector3 cur = transform.position;
            float newZ = Mathf.Max(cur.z + move.z * Time.deltaTime, -4f);
            // Confined to the sidewalk band (wide for most of the run, tightening on the left
            // past the curb jog) so it stays on the pavement but has real room to weave.
            float minX, maxX;
            CorridorBand(newZ, out minX, out maxX);
            float newX = Mathf.Clamp(cur.x + move.x * Time.deltaTime, minX, maxX);

            // Ride the surface height (city) or fall under gravity (off-street). Climb a
            // curb faster (28) than the drop settles (20) so a mount reads as an eager
            // scramble — never wall-blocked, never floating.
            float newY;
            if (StreetGround.Active)
            {
                float groundY = StreetGround.HeightAt(newX, newZ);
                float rideRate = groundY > cur.y ? 28f : 20f;
                newY = Mathf.Lerp(cur.y, groundY, 1f - Mathf.Exp(-rideRate * Time.deltaTime));
            }
            else
            {
                newY = cur.y + _vVel * Time.deltaTime;
            }

            _cc.Move(new Vector3(newX - cur.x, newY - cur.y, newZ - cur.z));

            Velocity = move;

            // facing follows the smoothed velocity, so the lizard always points where it's
            // actually scurrying and banks into a weave (no snap on direction change)
            Vector3 flatVel = new Vector3(_vel.x, 0f, _vel.z);
            if (flatVel.sqrMagnitude > 1e-4f)
            {
                Quaternion target = Quaternion.LookRotation(flatVel.normalized, Vector3.up);
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

        private void OnFootBumped(Vector3 pedPos) { Stumble(pedPos); }
        private void OnFaceplanted(Vector3 propPos) { Faceplant(propPos); }

        /// <summary>Trip over a pedestrian's leg/shoe: a stagger that bleeds all forward
        /// momentum (so the woken cat gains ground) and briefly takes away control while the
        /// lizard scrambles up. The knockback + i-frames come from the paired HitPlayer.</summary>
        private void Stumble(Vector3 fromPos)
        {
            _stumbleUntil = Time.time + GameConst.StumbleDuration;
            _bumpCooldownUntil = Time.time + GameConst.FootBumpCooldown;
            _controlLockUntil = Time.time + GameConst.StumbleControlLock;
            _vel = Vector3.zero;          // lose the run
            IsDashing = false;
            if (Body != null) Body.Stumble();
            GameAudio.Play(Sfx.Hit);
        }

        /// <summary>Smack head-on into a prop/wall: the lizard splats spread-eagle against
        /// it, holds, then peels off (the HitPlayer knockback) and runs on.</summary>
        private void Faceplant(Vector3 fromPos)
        {
            _stumbleUntil = Time.time + GameConst.FaceplantDuration;
            _bumpCooldownUntil = Time.time + GameConst.FaceplantDuration;
            _controlLockUntil = Time.time + GameConst.FaceplantControlLock;
            _vel = Vector3.zero;
            IsDashing = false;
            if (Body != null) Body.Faceplant();
            GameAudio.Play(Sfx.Hit);
        }

        private void OnHit(int heartsLeft, Vector3 hazardPos)
        {
            _invulnerableUntil = Time.time + GameConst.HitInvulnerableTime;
            ApplyKnockback(hazardPos);
            IsDashing = false;
            GameAudio.Play(Sfx.Hit);
        }

        /// <summary>First hit: the lizard sheds its tail and scrambles on, no heart lost.</summary>
        private void OnTailLost(Vector3 hazardPos)
        {
            _invulnerableUntil = Time.time + GameConst.HitInvulnerableTime;
            ApplyKnockback(hazardPos);
            IsDashing = false;
            if (Body != null) Body.DropTail();
            GameAudio.Play(Sfx.Hit);
        }

        private void OnTailRegrown()
        {
            if (Body != null) Body.RegrowTail();
        }

        // fling away from the hazard, never out of the corridor's far edge
        private void ApplyKnockback(Vector3 hazardPos)
        {
            Vector3 away = transform.position - hazardPos;
            away.y = 0f;
            away = away.sqrMagnitude > 0.01f ? away.normalized : Vector3.back;
            _knockback = away * GameConst.HitKnockback;
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

        /// <summary>The playable sidewalk band [minX, maxX] at world <paramref name="z"/>. Wide
        /// on the open early/mid blocks so there's real room to weave; the LEFT edge tightens
        /// past the curb jog so the lizard never drops onto the road near the end. The right edge
        /// hugs the building line throughout.</summary>
        public static void CorridorBand(float z, out float minX, out float maxX)
        {
            maxX = GameConst.CorridorRightX;
            float t = Mathf.Clamp01((z - GameConst.CurbJogStartZ) / GameConst.CurbJogRampZ);
            minX = Mathf.Lerp(GameConst.CorridorLeftWideX, GameConst.CorridorLeftTightX, t);
        }

        /// <summary>World-space position used by hazard footprint kill tests.</summary>
        public Vector3 KillCheckPosition { get { return transform.position; } }
    }
}
