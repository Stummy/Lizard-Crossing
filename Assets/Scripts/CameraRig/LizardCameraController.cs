using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// The hero system (packet: CAMERA_AND_SCALE.md): locked-low third-person
    /// follow that keeps the lizard bottom-center and the world towering above.
    /// Smooth lag follow, dash FOV kick, portrait-aware FOV so sideways
    /// cross-traffic stays readable, shake via CameraShake, near-miss slow-mo.
    /// </summary>
    public class LizardCameraController : MonoBehaviour
    {
        public static LizardCameraController Instance { get; private set; }

        private Camera _cam;
        private CameraShake _shake;
        private Transform _target;
        private float _fovVelocity;
        private Vector3 _posVelocity;
        private Vector3 _lastTargetPos;
        private bool _firstPerson;

        // Follow smoothing time. Against the constant +Z auto-run this SmoothDamp would settle to
        // a steady following-lag of ~(forwardSpeed * FollowSmooth) ≈ 0.2u, which roughly doubles
        // the camera→lizard distance and HALVES the on-screen hero. We cancel that with Z velocity
        // feed-forward in UpdateThirdPerson, so the smoothing only ever damps dodges/shake.
        private const float FollowSmooth = 0.06f;

        /// <summary>Toggle the optional first-person "lizard cam" POV. The body stays visible —
        /// in FP we ride just above/behind the lizard's own head so its real snout, side-eyes
        /// and front legs frame the view.</summary>
        public void ToggleView()
        {
            _firstPerson = !_firstPerson;
            GameAudio.Play(Sfx.UiClick);
        }

        public bool IsFirstPerson { get { return _firstPerson; } }

        public static LizardCameraController Create(Transform target)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            var rig = go.AddComponent<LizardCameraController>();
            rig._shake = go.AddComponent<CameraShake>();
            rig.Setup(cam, target);
            go.AddComponent<CinematicPost>().Setup(cam, target); // URP post: DoF, bloom, grade
            return rig;
        }

        private void Setup(Camera cam, Transform target)
        {
            Instance = this;
            _cam = cam;
            _target = target;

            _cam.fieldOfView = BaseFov();
            _cam.nearClipPlane = 0.05f;
            _cam.farClipPlane = 400f;
            // render the HDRI skybox behind the world (WO-1) instead of a flat fill colour,
            // so the sky reads as a real sunny sky and lights the scene image-based.
            _cam.clearFlags = CameraClearFlags.Skybox;
            _cam.backgroundColor = new Color(0.78f, 0.88f, 0.92f); // fallback only (if skybox unset)

            // subtle warm atmospheric haze, pushed out so it only softens the far city
            // backdrop into the sky band — it must NOT grey-out the readable mid lane.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 110f;
            RenderSettings.fogEndDistance = 320f;
            RenderSettings.fogColor = new Color(0.83f, 0.86f, 0.84f);

            GameEvents.HazardImpact += OnHazardImpact;
            GameEvents.NearMiss += OnNearMiss;
            GameEvents.PlayerHit += OnPlayerHit;
            GameEvents.PlayerDied += OnPlayerDied;

            transform.position = DesiredPosition();
            transform.rotation = Quaternion.LookRotation(LookPoint() - transform.position, Vector3.up);
            _lastTargetPos = _target.position;
        }

        private void OnDestroy()
        {
            GameEvents.HazardImpact -= OnHazardImpact;
            GameEvents.NearMiss -= OnNearMiss;
            GameEvents.PlayerHit -= OnPlayerHit;
            GameEvents.PlayerDied -= OnPlayerDied;
            if (Instance == this) Instance = null;
        }

        private void OnHazardImpact(Vector3 pos, float severity)
        {
            _shake.AddTrauma(Mathf.Lerp(0.15f, 0.65f, severity));
        }

        private void OnNearMiss(Vector3 pos)
        {
            _shake.AddTrauma(0.35f);
            if (TimeEffects.Instance != null) TimeEffects.Instance.SlowMo();
            GameAudio.Play(Sfx.CloseCall);
        }

        private void OnPlayerHit(int heartsLeft, Vector3 hazardPos)
        {
            _shake.AddTrauma(0.75f);
            if (TimeEffects.Instance != null) TimeEffects.Instance.HitStop();
        }

        private void OnPlayerDied(DeathCause cause)
        {
            _shake.AddTrauma(1f);
            if (TimeEffects.Instance != null) TimeEffects.Instance.HitStop();
        }

        /// <summary>
        /// Camera stays world-axis aligned behind the lizard's travel corridor
        /// (+Z) rather than its facing, so quick dodges don't whip the horizon —
        /// steadiness sells the scale, and sideways traffic reads clearly.
        /// </summary>
        private Vector3 DesiredPosition()
        {
            Vector3 anchor = _target.position;
            // Track the lizard's X directly so it stays centred in frame. (The old
            // 0.85 pull toward world-centre assumed the lizard plays near x=0; on the
            // city street it starts at x≈6 on the sidewalk, which pushed it clean off
            // the right edge of the screen.) Steadiness comes from the SmoothDamp lag
            // and the world-axis +Z look direction, not from re-centring X.
            float camX = anchor.x;
            float camY = anchor.y + GameConst.CamHeight;
            // Hard floor: never let the camera dip into the ground it's over.
            float groundUnderCam = StreetGround.HeightAt(camX, anchor.z);
            camY = Mathf.Max(camY, groundUnderCam + GameConst.CamMinGroundClearance);
            return new Vector3(camX, camY, anchor.z - GameConst.CamBack);
        }

        private Vector3 LookPoint()
        {
            Vector3 anchor = _target.position;
            return new Vector3(anchor.x, anchor.y + GameConst.CamLookHeight, anchor.z + GameConst.CamLookAhead);
        }

        /// <summary>Portrait phones need a taller FOV to keep cross-traffic visible.</summary>
        private float BaseFov()
        {
            float aspect = _cam != null ? _cam.aspect : 1.78f;
            if (aspect >= 1f) return GameConst.CamBaseFov;
            float halfHRad = GameConst.CamTargetHorizontalFov * 0.5f * Mathf.Deg2Rad;
            float vFov = 2f * Mathf.Atan(Mathf.Tan(halfHRad) / aspect) * Mathf.Rad2Deg;
            return Mathf.Clamp(vFov, GameConst.CamBaseFov, GameConst.CamMaxFov);
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            if (Input.GetKeyDown(KeyCode.V)) ToggleView(); // desktop POV toggle (HUD button on touch)

            if (_firstPerson) UpdateFirstPerson();
            else UpdateThirdPerson();

            Vector3 shakeOffset;
            float roll;
            _shake.Evaluate(out shakeOffset, out roll);
            if (shakeOffset != Vector3.zero || roll != 0f)
            {
                transform.position += transform.TransformDirection(shakeOffset);
                transform.rotation *= Quaternion.Euler(0f, 0f, roll);
            }

            bool dashing = PlayerController.Instance != null && PlayerController.Instance.IsDashing;
            float baseFov = _firstPerson ? GameConst.FpFov : BaseFov();
            float targetFov = baseFov + (dashing ? GameConst.CamDashFovKick : 0f);
            _cam.fieldOfView = Mathf.SmoothDamp(_cam.fieldOfView, targetFov, ref _fovVelocity, 0.08f);
        }

        private void UpdateThirdPerson()
        {
            if (_cam.nearClipPlane != 0.05f) _cam.nearClipPlane = 0.05f; // restore from the FP close-up

            Vector3 desired = DesiredPosition();
            // Cancel the auto-run following-lag: feed-forward the target's FORWARD (+Z) velocity
            // by FollowSmooth so SmoothDamp's steady-state error is removed and the hero keeps a
            // constant, close, BIG framing at any run/dash speed. Z only — the lateral lag is kept
            // deliberately (below) so side-weaving still reads on screen.
            float dt = Time.deltaTime;
            float targetVelZ = dt > 0f ? (_target.position.z - _lastTargetPos.z) / dt : 0f;
            _lastTargetPos = _target.position;
            desired.z += targetVelZ * FollowSmooth;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _posVelocity, FollowSmooth);

            // Let the lizard visibly LEAD the camera sideways so a weave reads on screen, but
            // leash the camera to it so it can never slide out of frame. Without this the camera
            // tracks the lizard's x exactly and it sits dead-centre — making side movement invisible.
            float lizX = _target.position.x;
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, lizX - GameConst.CamMaxLateralLead, lizX + GameConst.CamMaxLateralLead);
            transform.position = p;

            Quaternion lookRot = Quaternion.LookRotation(LookPoint() - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 1f - Mathf.Exp(-10f * Time.deltaTime));
        }

        /// <summary>Lizard cam: ride at the lizard's OWN snout/eye line and look FORWARD +
        /// slightly down, so the real snout sits at the bottom-centre and the two scurrying
        /// front feet splay into the bottom corners — a true ground-level reptile POV down the
        /// sidewalk (the actual model — no primitive viewmodel). Aim is blended slightly toward
        /// +Z so quick dodges don't whip the view; from ground level the humans and the cat
        /// tower over the speck of a lizard. Anchored on the measured model head so the snout
        /// frames the lower view instead of the camera staring down at the lizard's back.</summary>
        private void UpdateFirstPerson()
        {
            // Measured head geometry (scales with the model, so the framing survives a resize).
            var body = PlayerController.Instance != null ? PlayerController.Instance.Body : null;
            float snoutZ = body != null && body.HasModel ? body.ModelSnoutZ : 0.055f;
            float eyeY = body != null && body.HasModel ? body.ModelEyeY : 0.023f;

            _cam.nearClipPlane = snoutZ * GameConst.FpNearClipFrac; // the snout is millimetres away — don't clip it

            Vector3 fwd = _target.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 flat = Vector3.Slerp(fwd, Vector3.forward, 0.35f).normalized;

            // Perch the lens right at the lizard's eyes (forward of the body, just above the
            // eye line) and look forward, so the snout drops into the bottom-centre and the
            // sidewalk recedes ahead — not the camera staring down at the lizard's back.
            Vector3 eye = _target.position
                          + flat * (snoutZ * GameConst.FpForwardFrac)
                          + Vector3.up * (eyeY * GameConst.FpUpFrac);
            float g = StreetGround.HeightAt(eye.x, eye.z);
            eye.y = Mathf.Max(eye.y, g + eyeY * 0.4f); // never sink below the pavement

            // a gentle downward tilt so the snout + scurrying front feet sit in the lower frame
            Vector3 right = Vector3.Cross(Vector3.up, flat);
            Vector3 aim = Quaternion.AngleAxis(GameConst.FpPitchDown, right) * flat;

            transform.position = eye;
            transform.rotation = Quaternion.LookRotation(aim, Vector3.up);
        }
    }
}
