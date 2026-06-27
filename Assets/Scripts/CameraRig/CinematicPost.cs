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
        // REALISM PASS 2026-06-26: 26mm/f14 focused at the 0.2m hero distance smeared the WHOLE avenue
        // (the real MP4 was a uniform blur wash that erased every surface). Deepen the field hard so only
        // the very-near hazard + far skyline go soft and the mid-ground street/peds/facades read sharp:
        // shorter focal length (26->18) and a much higher f-number (14->22) push both the near and far
        // blur falloffs far out. Combined with FocusForwardBias (focus just past the hero), the lizard and
        // the street around it sit inside the deep in-focus zone. Tuned by eye on the MP4, not by theory.
        private const float DofFocalLength = 18f;   // mm; shorter = deeper field (was 26)
        private const float DofAperture = 22f;       // f-stop; higher = deeper field, mid-ground stays sharp (was 14)
        private const float FocusForwardBias = 0.7f; // m; focus this far PAST the hero so hero+near-street are sharp
        private const float DofFocusFallback = 1.5f; // m, used until the lizard is found

        public void Setup(Camera cam, Transform focusTarget)
        {
            _cam = cam;
            _focusTarget = focusTarget;

            // turn the camera into a post-processing camera
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            // SMAA (sharper) over FXAA (which BLURRED the whole frame — a big part of the "soft/cheap"
            // look the owner flagged). Paired with 4x MSAA on the URP asset (geometric edges), the city
            // + hero edges read crisp instead of fuzzy. SetLite drops post-AA for low-tier.
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;

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
            // GOLDEN-HOUR pass (Gemini #1 gap: the run read DARK + cold "night/evening"). The prior
            // anti-glare passes had crushed postExposure to +0.06 which, with the cool fill/ambient,
            // left the avenue dark and flat. The real fix for the bright-peds wasn't to crush the
            // whole frame's exposure (which killed the golden hour) — it's a sane bloom threshold +
            // a harder ped-albedo cap (GiantPedestrian.CalmMaterial). So LIFT exposure back up to pull
            // the run out of the dark, and let the grade carry the warmth. With ambient lowered to
            // 0.85 the brights no longer clip, so this lift evens the start-vs-avenue exposure instead
            // of blowing the start.
            // GOLDEN-HOUR pass v2 (Gemini re-review: v1 over-shot into an "intense desaturated
            // yellow/orange monochrome wash" — the cyan sky + asphalt grey vanished and peds glowed
            // yellow). The fix: keep the warm key/grade but RESTORE the blue channel so the frame is
            // warm-AND-cohesive (warm lit planes vs a living cyan sky), not a uniform yellow tint, and
            // pull exposure back so nothing overblows. Warmth comes from the WARM SUN now, not a heavy
            // global filter — the grade just nudges.
            // EXPOSURE FIX (real ScreenCapture truth): the sun/ambient drop above tames the white
            // clip, so postExposure goes to 0 — any positive lift here re-blew the peds/hero. Push
            // SATURATION harder so the now-unclipped mid-tones read as vivid golden + a clearly
            // EMERALD hero (the wash had desaturated both toward white).
            // REALISM PASS: with flat ambient pulled way down, the frame got more contrast on its own, so
            // ease the grade's added contrast (16->10) — the lighting now provides the punch, and lower
            // grade-contrast keeps shadows from crushing to black at the low POV. Saturation eased 30->22:
            // 30 was over-juicing the warm brick/asphalt toward the "yellow" the owner flagged; 22 keeps
            // tones vivid while letting the emerald hero stay the MOST saturated thing (its albedo, not
            // the grade, makes it pop).
            color.postExposure.value = 0.15f;                    // 0->0.15 (lighting-post 2026-06-26): gentle global lift out of the dim register
            color.contrast.value = 10f;                          // 16->10: lighting now carries the punch; protect shadow detail
            color.saturation.value = 22f;                        // 30->22: de-juice the warm surfaces (kills "yellow"); hero still pops
            color.colorFilter.value = Color.white;               // neutral: no tint from the filter

            // White balance: a HAIR cool to neutralise the residual warm cast the owner reads as "yellow"
            // (warm brick/asphalt albedo + ACES warming the highlights). -6 nudges the whole frame toward a
            // clean midday-daylight white without going blue. Tint stays neutral.
            var wb = profile.Add<WhiteBalance>(true);
            wb.temperature.value = -2f;                          // -6->-2 (lighting-post 2026-06-26): the warm cast is gone post-override; -6 now reads cold, so sit near-neutral
            wb.tint.value = 0f;                                  // neutral

            // --- Lift / Gamma / Gain: the cohesive cinematic grade toward the §3 palette
            //     (warm sun in highlights, neutral-warm mids, faintly cool shadows so the
            //     cyan sky and shaded stone read cool against the warm lit planes). ---
            // GOLDEN-HOUR pass: the old lift pushed shadows COOL/blue (1.04 blue), which — stacked on
            // the cool fill + cool ambient — was a big part of the "cold evening" read. Warm the
            // shadows to near-neutral (just a whisper of cool kept in the deepest shadow so the cyan
            // SKY still reads cool by contrast) and push the mids/highlights firmly warm so the lit
            // pavement and the sun-kissed building tops glow golden like the concept.
            // v2: softened the LGG warmth (v1 pushed highlights blue down to 0.90 which, stacked with
            // the warm filter + WB, monochromed the bright sky into orange). Keep warm mids for the
            // golden pavement, but let the highlights stay near-neutral so the bright sky reads cyan,
            // and keep a faint cool whisper in the deepest shadow for warm/cool separation.
            // EXPOSURE-EVENNESS FIX: lift the shadows a touch more (the global lift .01->.04) so the
            // occluded "pure black" giant-ped/building moments come up off black — squeezing the dark
            // end up to meet the (now-tamed) bright end for an even read across the run.
            // OWNER COLOR OVERRIDE 2026-06-26: neutralized the warm LGG bias back toward neutral grey so
            // the grade reads as clean daylight, NOT golden pavement / warm highlights. The gentle global
            // shadow lift (.02) is KEPT for the even-exposure read; only the per-channel warm tilt is removed.
            var lgg = profile.Add<LiftGammaGain>(true);
            lgg.lift.value = new Vector4(1.0f, 1.0f, 1.0f, 0.02f);  // neutral shadows + gentle lift (was warm-cool tilt)
            lgg.gamma.value = new Vector4(1.0f, 1.0f, 1.0f, 0.0f);  // neutral mids (was warm golden pavement)
            lgg.gain.value = new Vector4(1.0f, 1.0f, 1.0f, 0.0f);   // neutral highlights (was lightly warm)

            // --- Bloom: gentle sun-kissed glow on the brightest highlights (sky, lit
            //     stone, chrome). Half-res (HQ filtering OFF) to stay cheap on mobile. ---
            // WO-7 (overexposure fix): the old 0.45 intensity / 1.10 threshold let the whole
            // sunlit street glow, hazing over mid-tones and pushing the asphalt toward white.
            // Raised the threshold so ONLY true highlights (sky, chrome, sun glints) bloom, and
            // dropped the intensity so the glow is a sun-kiss, not a haze. Mid surfaces stay solid.
            _bloom = profile.Add<Bloom>(true);
            // Readability pass (owner + Gemini video review): the backlit golden-hour rim on the
            // pedestrians was blooming into a glowing HALO that obscured their form ("glowing
            // effects around them"). Pulled intensity down and the threshold up so only true
            // highlights (sun/sky/chrome) glow — the pedestrian rim no longer halos, so the
            // figures read as solid. Pairs with GiantPedestrian.CalmMaterial (albedo cap).
            // Forward-glare pass (Gemini re-review + eyes-on frame: the lizard auto-runs INTO a low
            // golden-hour sun, so the bright HDRI sky at the horizon + bloom bleed merged pedestrians
            // ahead into "blinding blobs of light" with no contrast). Cut the bloom hard so the bright
            // sky stops bleeding OVER the pedestrian silhouettes — the figures regain defined edges
            // against the background. Only a whisper of glow remains on the literal sun/chrome.
            // GOLDEN-HOUR pass: bloom had been crushed to ~off (0.06/2.2) to fight the glowing-white
            // peds — but that also killed the warm sun-kiss the golden hour needs, and was a wrong
            // tool: the peds glow because of their bright albedo + high ambient, not a low threshold.
            // Now that ambient is 0.85 and CalmMaterial caps the ped albedo harder, restore a tasteful
            // sun-kiss: a moderate threshold so ONLY genuine highlights (sky strip, sun, chrome, the
            // golden building tops) bloom, at a gentle intensity. Peds sit below the threshold so they
            // no longer halo. Warm golden tint on the glow.
            // v2: cut bloom back (v1's 0.20/1.25 contributed to the overblown wash, esp. on the bright
            // sky strip up the avenue). A gentle kiss on only the genuine hotspots, near-neutral tint
            // so the glow doesn't paint the sky yellow.
            _bloom.intensity.value = 0.0f;         // RT-LIE FIX: bloom OFF. On the real MP4 render the
                                                   // bright distance (avenue end / sun-side peds) bloomed
                                                   // into blown-WHITE BLOBS that read as "night w/ lights"
                                                   // and buried the city. The 0.11 looked fine on the
                                                   // brighter RT capture but blew on the real render.
            _bloom.threshold.value = 2.20f;        // and only true HDR hotspots if ever re-enabled
            _bloom.scatter.value = 0.60f;          // KEEP
            _bloom.highQualityFiltering.value = false; // KEEP: half-res, the mobile-friendly path
            _bloom.tint.value = Color.white;       // OWNER OVERRIDE: 1,0.96,0.90 (warm) -> white (neutral glow, no golden tint)

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
        /// REALISM PASS: the lite path also pulls the (now soft, 4-cascade) shadows back to a
        /// cheaper budget — shorter shadow distance trims the cascade work, which is the main
        /// cost the realism pass added on the GPU. The directional sun shadow stays ON (it's the
        /// grounding lever) but tighter, so even low tier keeps things sitting on the ground.
        /// </summary>
        public void SetLite(bool lite)
        {
            if (_dof != null) _dof.active = !lite;
            if (_bloom != null) _bloom.active = !lite;
            // Cheaper shadows on low tier: pull the distance in (less cascade fill) without
            // killing the contact shadow entirely. Full tier keeps the 42m realism distance.
            QualitySettings.shadowDistance = lite ? 24f : 42f;
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

            // Third-person REALISM PASS: the camera sits only ~0.2m behind the tiny lizard, so focusing
            // EXACTLY on it gave a razor-thin in-focus slab — the entire avenue (pavement, facades, peds)
            // smeared to a heavy wash on the real MP4, erasing all the surface detail (the new normal maps,
            // the facade windows) and reading as flat/low-detail. The signature DoF should blur only the
            // VERY-near foreground hazard (giant leg/wheel at the lens) and the FAR background — the
            // mid-ground street the player reads should be reasonably sharp. So push the focus a bit BEYOND
            // the lizard (FocusForwardBias) so both the hero AND the near street fall inside the acceptable
            // -sharp zone; the deeper lens (set in Setup) then keeps the mid-ground legible while the far
            // skyline and the close hazard still go soft. EASE toward it (SmoothDamp, unscaled) so dash FOV
            // kicks / near-miss slow-mo / shake can't pulse the hero out of focus.
            float dist = Mathf.Max(0.1f, Vector3.Distance(_cam.transform.position, _focusTarget.position));
            float target = dist + FocusForwardBias;   // focus just past the hero, not on it
            float dt = Time.unscaledDeltaTime;
            _focusDist = Mathf.SmoothDamp(_focusDist, target, ref _focusVel, 0.10f, Mathf.Infinity, dt);
            _dof.focusDistance.value = _focusDist;
        }
    }
}
