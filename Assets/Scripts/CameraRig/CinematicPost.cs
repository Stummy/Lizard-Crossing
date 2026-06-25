using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LizardCrossing
{
    /// <summary>
    /// URP post-processing stack for the gameplay camera (replaces the old Built-in
    /// CameraGrade, whose OnRenderImage is ignored under URP). Builds ONE global Volume
    /// in code — no .asset to wire up — so it fits the fully-procedural world.
    ///
    /// Target (docs/VISUAL_TARGET.md §2/§3): a warm, sunny, cinematic ground-level read.
    /// The two biggest levers live here:
    ///   1. Lighting + exposure + a cohesive warm grade (ACES tonemap, warm exposure,
    ///      gentle contrast/saturation, lift-gamma-gain toward the cyan-sky/warm-ground
    ///      palette, sun-kissed bloom, subtle vignette).
    ///   2. Depth of field — hero lizard tack-sharp, the close foreground hazards
    ///      strongly blurred, the far background softly blurred. Focus distance tracks
    ///      the camera->lizard distance each frame; camera-ui-juice will later drive it
    ///      from the controller for tighter framing.
    ///
    /// MOBILE BUDGET: DoF (Bokeh) + Bloom are the two most expensive post effects on a
    /// phone. We keep ONE Volume, bloom at half-res (HQ filtering off), the cheapest DoF
    /// that still separates the hero, FXAA (not SMAA), and no motion blur. The whole
    /// stack can be dropped to a "lite" path (see SetLite) for low-end devices.
    /// </summary>
    public class CinematicPost : MonoBehaviour
    {
        private DepthOfField _dof;
        private Bloom _bloom;
        private Camera _cam;
        private Transform _focusTarget;

        // Smoothed focus distance (WO-3): the raw camera->lizard distance jitters frame-to-frame
        // (follow-cam SmoothDamp lag, dash FOV kick, near-miss slow-mo, shake) which would let the
        // hero pulse in/out of focus. We ease the focus toward the measured distance so the lizard
        // stays continuously tack-sharp while the close foreground / far background stay soft.
        private float _focusDist = DofFocusFallback;
        private float _focusVel;

        // --- DoF tuning (real-world scale: 1u = 1m; TP camera sits ~1.5-2u from the
        //     lizard, the close foreground hazard ~0.3-0.8u, the background tens of u). ---
        // WO-3: the tighter framing pulls the camera to ~0.4-0.5u from the hero, so the focus
        // distance is now CLOSE — at that range a 45mm/f6.5 lens is too shallow and softens the
        // hero's own front/back. Shorter focal length + wider aperture deepens the band around
        // the hero so the WHOLE lizard stays tack-sharp.
        // S2-1 (DoF discipline): 38mm/f9 was OVER-applied — the far falloff blurred the ENTIRE
        // city + running lane into a soft wash, erasing the mid-ground / lane-to-goal read (the
        // single gating issue per the art-director re-grade, WO-6). Pulled to 26mm/f14 to deepen
        // the field a LOT: a shorter focal length (38→26) and a much higher f-number (9→14) push
        // the far-blur falloff out by tens of metres, so the mid-distance city, lane and an
        // upcoming safe-zone sign read as soft-but-LEGIBLE instead of a smear, while the hero (at
        // the ~0.2-0.5u focus distance) stays tack-sharp. The very-close foreground hazard (giant
        // leg/wheel <0.2u from the lens, well inside the near-blur zone even at f14) still blurs
        // clearly — that's the win the close-up still keeps. Tune by eye via captures, not theory.
        private const float DofFocalLength = 26f;   // mm; longer = shallower, stronger bg blur (was 38)
        private const float DofAperture = 14f;       // f-stop; higher = deeper field, mid/far stays
                                                     // legible. Close hazard still blurs (it's <0.2u,
                                                     // far inside the near falloff). (was 9)
        private const float DofFocusFallback = 1.2f; // m, used until the lizard is found

        public void Setup(Camera cam, Transform focusTarget)
        {
            _cam = cam;
            _focusTarget = focusTarget;

            // turn the camera into a post-processing camera
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing; // FXAA: cheap edge smoothing

            // a single runtime global Volume (not an asset) holding all overrides
            var volGo = new GameObject("CinematicVolume");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            vol.sharedProfile = profile;

            // --- Tonemapping: ACES for a filmic highlight roll-off (sun, sky, bright
            //     stone never clip to flat white). ACES desaturates a touch, so the grade
            //     below adds saturation back to keep the hero lizard vivid. ---
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.value = TonemappingMode.ACES;

            // --- Color Adjustments: warm, sunny, bright-but-not-blown grade ---
            var color = profile.Add<ColorAdjustments>(true);
            // WO-7 (overexposure fix): +0.35 was blowing mid/bright surfaces — the sunlit
            // pavement read hazy and near-white. Pulled to +0.15 so the asphalt and mid-tones
            // stay grounded; the scene is still warm/sunny from the grade + HDRI ambient, just
            // no longer washed out.
            color.postExposure.value = 0.15f;                    // gentle lift (was 0.35 — too hot)
            color.contrast.value = 12f;                          // gentle punch
            color.saturation.value = 18f;                        // slightly saturated (hero pops)
            color.colorFilter.value = new Color(1.05f, 1.0f, 0.92f); // faint warm wash

            // push white balance toward warm sunlight
            var wb = profile.Add<WhiteBalance>(true);
            wb.temperature.value = 14f;
            wb.tint.value = 2f;                                  // a hair of magenta kills the green cast

            // --- Lift / Gamma / Gain: the cohesive cinematic grade toward the §3 palette
            //     (warm sun in highlights, neutral-warm mids, faintly cool shadows so the
            //     cyan sky and shaded stone read cool against the warm lit planes). ---
            var lgg = profile.Add<LiftGammaGain>(true);
            lgg.lift.value = new Vector4(0.98f, 0.99f, 1.04f, 0.0f);   // cool, lifted shadows (not crushed)
            lgg.gamma.value = new Vector4(1.02f, 1.0f, 0.97f, 0.0f);   // warm mids
            lgg.gain.value = new Vector4(1.04f, 1.01f, 0.95f, 0.0f);   // warm, sun-kissed highlights

            // --- Bloom: gentle sun-kissed glow on the brightest highlights (sky, lit
            //     stone, chrome). Half-res (HQ filtering OFF) to stay cheap on mobile. ---
            // WO-7 (overexposure fix): the old 0.45 intensity / 1.10 threshold let the whole
            // sunlit street glow, hazing over mid-tones and pushing the asphalt toward white.
            // Raised the threshold so ONLY true highlights (sky, chrome, sun glints) bloom, and
            // dropped the intensity so the glow is a sun-kiss, not a haze. Mid surfaces stay solid.
            _bloom = profile.Add<Bloom>(true);
            _bloom.intensity.value = 0.30f;        // gentler glow (was 0.45 — hazy)
            _bloom.threshold.value = 1.30f;        // only genuinely bright highlights glow (was 1.10)
            _bloom.scatter.value = 0.60f;
            _bloom.highQualityFiltering.value = false; // half-res, the mobile-friendly path
            _bloom.tint.value = new Color(1f, 0.96f, 0.88f); // warm glow

            // --- Vignette: subtle edge darkening to frame the hero at bottom-center ---
            // S2-1: 0.26/0.45 was crushing the TOP of frame (the skyline) to near-black, so the
            // buildings/sky didn't read at the top. Eased to 0.16/0.30 — the falloff is lighter
            // and pulled tighter into the corners, so the skyline reads while the bottom-center
            // hero is still gently framed. Subtle, not gone.
            var vig = profile.Add<Vignette>(true);
            vig.intensity.value = 0.16f;
            vig.smoothness.value = 0.30f;

            // --- Depth Of Field (Bokeh): the signature cinematic effect. Hero sharp,
            //     close foreground hazards blurred, far background soft. Focus distance is
            //     driven per-frame in LateUpdate (fixed-ish for now; camera-ui-juice will
            //     refine the exact focus point/easing). ---
            _dof = profile.Add<DepthOfField>(true);
            _dof.mode.value = DepthOfFieldMode.Bokeh;
            _dof.focalLength.value = DofFocalLength;
            _dof.aperture.value = DofAperture;
            _dof.bladeCount.value = 5;             // fewer blades = cheaper bokeh
            _dof.focusDistance.value = DofFocusFallback;
        }

        /// <summary>
        /// Low-end "lite" path: drop the two most expensive effects (DoF + bloom) for
        /// mid/low-tier phones while keeping the grade (which is nearly free). Wire this
        /// to a quality toggle when device-tier detection lands.
        /// </summary>
        public void SetLite(bool lite)
        {
            if (_dof != null) _dof.active = !lite;
            if (_bloom != null) _bloom.active = !lite;
        }

        private void LateUpdate()
        {
            if (_dof == null || _cam == null || _focusTarget == null) return;

            // First-person POV: the lens sits AT the lizard's head, so focusing on the
            // lizard (~0.04u away) would blur the whole street. Focus a little down the
            // street so the scene reads sharp; the very near snout/feet stay softly out
            // of focus, which is fine.
            if (LizardCameraController.Instance != null && LizardCameraController.Instance.IsFirstPerson)
            {
                // snap (no easing) when in FP so toggling views doesn't ramp the focus
                _focusDist = 5f;
                _focusVel = 0f;
                _dof.focusDistance.value = _focusDist;
                return;
            }

            // Third-person: keep the lizard tack-sharp. Target the exact camera->lizard distance
            // so anything nearer (close foreground hazard) or farther (background) falls out of
            // focus — but EASE toward it (SmoothDamp) so dash FOV kicks, near-miss slow-mo, shake
            // and follow-lag can't make the hero pulse out of focus. Uses unscaled time so the
            // focus keeps tracking cleanly even while near-miss slow-mo holds Time.timeScale low.
            float target = Mathf.Max(0.1f, Vector3.Distance(_cam.transform.position, _focusTarget.position));
            float dt = Time.unscaledDeltaTime;
            _focusDist = Mathf.SmoothDamp(_focusDist, target, ref _focusVel, 0.10f, Mathf.Infinity, dt);
            _dof.focusDistance.value = _focusDist;
        }
    }
}
