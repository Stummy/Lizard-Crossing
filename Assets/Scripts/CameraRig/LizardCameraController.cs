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
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.78f, 0.88f, 0.92f); // bright tropical sky

            // warm garden haze: depth + scale
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 60f;
            RenderSettings.fogEndDistance = 260f;
            RenderSettings.fogColor = new Color(0.8f, 0.88f, 0.86f);

            GameEvents.HazardImpact += OnHazardImpact;
            GameEvents.NearMiss += OnNearMiss;
            GameEvents.PlayerHit += OnPlayerHit;
            GameEvents.PlayerDied += OnPlayerDied;

            transform.position = DesiredPosition();
            transform.rotation = Quaternion.LookRotation(LookPoint() - transform.position, Vector3.up);
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
            // ride above the lizard's ground height so the camera lifts onto the
            // sidewalk with it instead of clipping through the raised curb
            return new Vector3(anchor.x * 0.85f, anchor.y + GameConst.CamHeight, anchor.z - GameConst.CamBack);
        }

        private Vector3 LookPoint()
        {
            Vector3 anchor = _target.position;
            return new Vector3(anchor.x * 0.9f, anchor.y + GameConst.CamLookHeight, anchor.z + GameConst.CamLookAhead);
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

            Vector3 desired = DesiredPosition();
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _posVelocity, 0.12f);

            Quaternion lookRot = Quaternion.LookRotation(LookPoint() - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 1f - Mathf.Exp(-10f * Time.deltaTime));

            Vector3 shakeOffset;
            float roll;
            _shake.Evaluate(out shakeOffset, out roll);
            if (shakeOffset != Vector3.zero || roll != 0f)
            {
                transform.position += transform.TransformDirection(shakeOffset);
                transform.rotation *= Quaternion.Euler(0f, 0f, roll);
            }

            bool dashing = PlayerController.Instance != null && PlayerController.Instance.IsDashing;
            float targetFov = BaseFov() + (dashing ? GameConst.CamDashFovKick : 0f);
            _cam.fieldOfView = Mathf.SmoothDamp(_cam.fieldOfView, targetFov, ref _fovVelocity, 0.08f);
        }
    }
}
