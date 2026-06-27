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

        // (Retired 2026-06-27: the Bokeh focus-distance fields + lens constants — _focusDist/_focusVel,
        //  DofFocalLength/DofAperture/FocusForwardBias/DofFocusFallback — are gone. Far-only Gaussian DoF
        //  below uses a fixed world-space blur band, so there is no per-frame focus distance to chase.)
        //
        // GAUSSIAN far-only DoF (2026-06-27, research-backed): Unity URP Gaussian mode blurs the FAR
        // field ONLY — everything nearer than gaussianStart is fully sharp — so the hero + near/mid
        // street (all < ~14m) stay tack-sharp (respects R31) while the deep avenue + skyline recede
        // into soft focus (the concept's depth, R18). Cheapest DoF mode; no per-frame focus chase.
        private const float DofGaussStart = 14f;     // m: sharp out to here (hero + readable mid-street)
        private const float DofGaussEnd = 42f;       // m: full blur by here (deep avenue / skyline only)

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

            // --- Tonemapping: NEUTRAL (2026-06-27, research-backed). ACES is destructive to
            //     CHROMA — it desaturates/washes exactly the bright greens & cyans in our frame
            //     (the EMERALD hero + the cyan sky), which read as the "desaturated/washed" look.
            //     Unity's Neutral tonemapper does range-remapping with minimal hue/saturation loss
            //     (Khronos/Unity guidance: the right base for a vivid hero on neutral daylight). With
            //     postExposure already low (0.05) nothing clips, so we don't need ACES' hard roll-off. ---
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.value = TonemappingMode.Neutral;

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
            color.postExposure.value = 0.05f;                    // 0.15->0.05 (lighting-post 2026-06-27): +0.15 was clipping the road/sidewalk to white on the real MP4
            color.contrast.value = 6f;                           // 10->6: compress the blown-road-vs-black-building range (R16) into a legible midtone
            color.saturation.value = 10f;                        // 22->10 (2026-06-27): Neutral tonemap no longer desaturates, so the +22 (which compensated for ACES) would over-juice the surfaces toward "yellow"; the emerald hero now pops from its own albedo + Neutral preserving chroma
            color.colorFilter.value = Color.white;               // neutral: no tint from the filter

            // White balance: a HAIR cool to neutralise the residual warm cast the owner reads as "yellow"
            // (warm brick/asphalt albedo + ACES warming the highlights). -6 nudges the whole frame toward a
            // clean midday-daylight white without going blue. Tint stays neutral.
            var wb = profile.Add<WhiteBalance>(true);
            wb.temperature.value = 0f;                           // -2->0 (lighting-post 2026-06-27): -2 was nudging the already-cool/overcast frame cooler; sit neutral and let the sun key carry warmth
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

            // --- Depth Of Field (GAUSSIAN, far-only): the concept's creamy deep-avenue depth (R18)
            //     WITHOUT the regression that got Bokeh disabled. Bokeh blurred the NEAR foreground
            //     (the pavement filling the bottom of the portrait frame) and never delivered far
            //     bokeh, so it only SUBTRACTED sharpness ("looks worse when playing", R31). Gaussian
            //     mode blurs the FAR field ONLY: the hero + near/mid street (< gaussianStart) are
            //     fully sharp, only the deep avenue/skyline (> gaussianStart, max at gaussianEnd)
            //     softens. So depth is added with ZERO near-hero softening, and it's the cheapest DoF
            //     mode (mobile-correct). No per-frame focus chase — the blur band is fixed in world m. ---
            _dof = profile.Add<DepthOfField>(true);
            _dof.mode.value = DepthOfFieldMode.Gaussian;
            _dof.gaussianStart.value = DofGaussStart;   // sharp out to here (hero + near/mid street)
            _dof.gaussianEnd.value = DofGaussEnd;        // full blur by here (deep avenue / skyline)
            _dof.gaussianMaxRadius.value = 1.0f;         // gentle recede, not a smeary wash
            _dof.highQualitySampling.value = false;      // mobile-cheap
            _dof.active = true;                          // re-enabled: far-only can't soften the near hero (R31-safe)
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

        // Gaussian DoF uses a FIXED world-space near/far blur band (gaussianStart/End in Setup), not a
        // per-frame focus distance — so there is nothing to chase here. (The old Bokeh focus-distance
        // easing lived here; it's retired with the switch to far-only Gaussian.) Kept as a no-op hook in
        // case a future mode needs per-frame focus again.
        private void LateUpdate() { }
    }
}
