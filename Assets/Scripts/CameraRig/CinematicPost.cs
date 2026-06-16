using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LizardCrossing
{
    /// <summary>
    /// URP post-processing stack for the gameplay camera (replaces the old Built-in
    /// CameraGrade, whose OnRenderImage is ignored under URP). Builds a global Volume
    /// in code — no .asset to wire up — so it fits the fully-procedural world.
    ///
    /// The signature look from the reference (boardwalk_rush_sideways_hazards.png):
    /// shallow depth-of-field focused on the LIZARD (sharp subject, blurred scooter
    /// + background), bloomy tropical highlights, motion blur for speed, and a warm,
    /// punchy color grade. Focus distance tracks the lizard every frame.
    /// </summary>
    public class CinematicPost : MonoBehaviour
    {
        private DepthOfField _dof;
        private Camera _cam;
        private Transform _focusTarget;

        public void Setup(Camera cam, Transform focusTarget)
        {
            _cam = cam;
            _focusTarget = focusTarget;

            // turn the camera into a post-processing camera
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing; // cheap edge smoothing

            // a runtime global Volume (not an asset) holding all overrides
            var volGo = new GameObject("CinematicVolume");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            vol.sharedProfile = profile;

            // filmic tone map so HDR highlights roll off instead of clipping white
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.value = TonemappingMode.Neutral; // bright + saturated, not crushed ACES

            // warm, punchy grade (sunny tropical afternoon)
            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.value = 0.12f;
            color.contrast.value = 14f;
            color.saturation.value = 16f;
            color.colorFilter.value = new Color(1.03f, 1.0f, 0.93f); // faint warm wash

            var wb = profile.Add<WhiteBalance>(true);
            wb.temperature.value = 12f; // push toward warm

            // soft glow on bright highlights (sky, water sparkle, sand)
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.value = 0.55f;
            bloom.threshold.value = 1.05f;
            bloom.scatter.value = 0.62f;

            // gentle edge darkening to frame the lizard
            var vig = profile.Add<Vignette>(true);
            vig.intensity.value = 0.30f;
            vig.smoothness.value = 0.42f;

            // a touch of speed blur (light, so fast cross-traffic stays readable)
            var mb = profile.Add<MotionBlur>(true);
            mb.intensity.value = 0.10f;

            // depth-of-field: keep a cinematic background softness but DEEP enough
            // that the lizard and approaching traffic stay sharp at the tiny new
            // scale (lizard ~0.5u from camera). Narrow f-stop = much deeper focus.
            _dof = profile.Add<DepthOfField>(true);
            _dof.mode.value = DepthOfFieldMode.Bokeh;
            _dof.focalLength.value = 20f;
            _dof.aperture.value = 16f;     // high f-stop = subtle, deep focus
            _dof.focusDistance.value = 6f; // updated each frame to the lizard
        }

        private void LateUpdate()
        {
            if (_dof == null || _cam == null || _focusTarget == null) return;
            // keep the lizard tack-sharp; everything nearer/farther falls out of focus
            float d = Vector3.Distance(_cam.transform.position, _focusTarget.position);
            _dof.focusDistance.value = Mathf.Max(0.1f, d);
        }
    }
}
