using System.Collections.Generic;
using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Procedural lizard visual (docs/DECISIONS.md D7): primitives assembled into
    /// a stylized gecko, animated in code — leg gait, tail wave, head bob,
    /// squash & stretch. Replaced by an authored skinned model in Phase 3.
    /// </summary>
    public class LizardBody : MonoBehaviour
    {
        // Tinted from the selected lizard species (MetaProgress) at build time,
        // so choosing a different lizard visibly changes the player. Defaults are
        // the Gecko's colors as a fallback.
        private Color BodyGreen = new Color(0.42f, 0.76f, 0.29f);
        private Color BellyGreen = new Color(0.65f, 0.88f, 0.5f);
        private Color DarkGreen = new Color(0.27f, 0.55f, 0.2f);

        private Transform _root;          // scaled for squash/stretch
        private Transform _head;
        private Vector3 _headRest;        // head pivot rest position (for bob)
        private Transform[] _legs;        // FL, FR, BL, BR
        private Vector3[] _legRest;
        private Transform[] _tail;
        private List<Renderer> _renderers;
        private float _gaitPhase;
        private bool _squashed;

        private bool _modelMode;          // an imported 3D model is the body
        private const float ModelYaw = 0f; // base facing offset for the imported model

        // Procedural walk for the rigged AI lizard (Tripo skeleton). We swing the four
        // upper leg bones fore/aft in a diagonal gait, synced to run speed, so the feet
        // actually step. Bones are named by the rig; phases pair the diagonals (front-left
        // + back-right, front-right + back-left).
        private static readonly string[] LegBoneNames = { "L_Clavicle", "R_Clavicle", "L_Thigh", "R_Thigh" };
        private static readonly float[] LegPhase = { 0f, Mathf.PI, Mathf.PI, 0f };
        private Transform[] _legBones;
        private Quaternion[] _legBind;
        private float _modelGait;

        // Model metrics for the first-person "lizard cam" (so the POV anchors on the real
        // snout/eye line, not the body centre). Local frame: lizard faces +Z, base at y=0.
        public bool HasModel { get { return _modelMode; } }
        public float ModelSnoutZ { get; private set; }   // local +Z to the front of the model (snout tip)
        public float ModelEyeY { get; private set; }      // local eye-line height
        public float ModelHalfWidth { get; private set; } // local half-width (x)

        public static LizardBody Build(Transform parent)
        {
            var go = new GameObject("LizardBody");
            go.transform.SetParent(parent, false);
            var body = go.AddComponent<LizardBody>();
            body.Construct();
            return body;
        }

        private void Construct()
        {
            _renderers = new List<Renderer>();

            // Species colors up front so both the imported model and the procedural
            // gecko skin themselves in the chosen lizard's palette.
            var species = MetaProgress.SelectedLizard;
            if (species != null)
            {
                BodyGreen = species.BodyColor;
                BellyGreen = species.BellyColor;
                DarkGreen = species.StripeColor;
            }

            // imported lizard model if present; otherwise the procedural gecko.
            // Realistic scale (2026-06-16): a real ~15 cm lizard, dwarfed by the
            // human-scale city (person ~1.8 u, building ~20 u). 1 unit ~= 1 metre.
            // ~0.11 u total length: a small slender anole/garden lizard, not a chunky iguana.
            var model = ModelLibrary.TryBuild(ModelLibrary.LizardKey, transform, 0.11f, ModelYaw);
            if (model != null)
            {
                _modelMode = true;
                _root = model;
                // The imported Meshy mesh arrives untextured — flat grey under URP. Skin
                // every submesh in the species' lizard green (a proper URP/Lit material,
                // never Standard → magenta) so it reads as a lizard, not a grey blob.
                var skin = MaterialCache.GetLit(BodyGreen);
                foreach (var r in model.GetComponentsInChildren<Renderer>())
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = skin;
                    r.sharedMaterials = mats;
                    _renderers.Add(r);
                }
                CacheLegBones();
                ComputeModelMetrics();
                return;
            }

            _root = new GameObject("Root").transform;
            _root.SetParent(transform, false);
            _root.localPosition = new Vector3(0f, 0.22f, 0f);

            // Realistic scale (2026-06-16): the procedural gecko is authored ~3.3u
            // long; shrink the whole visual to a real ~0.2u lizard, a speck next to
            // the human-scale city (door ~2.2u, person ~1.8u). 1 unit ~= 1 metre.
            // Scale the BODY transform — NOT _root, which squash/stretch animates.
            transform.localScale = Vector3.one * 0.06f;

            // ---- procedural gecko: designed + verified from the low game camera ----
            var black = new Color(0.05f, 0.05f, 0.06f);

            // torso + soft belly
            Part(PrimitiveType.Sphere, _root, new Vector3(0f, 0.07f, -0.05f),
                new Vector3(0.50f, 0.31f, 1.10f), BodyGreen);
            Part(PrimitiveType.Sphere, _root, new Vector3(0f, -0.02f, 0.02f),
                new Vector3(0.42f, 0.22f, 1.00f), BellyGreen);
            // smooth dorsal stripe (no lumps)
            for (int i = 0; i < 6; i++)
            {
                float dz = Mathf.Lerp(0.30f, -0.42f, i / 5f);
                Part(PrimitiveType.Sphere, _root, new Vector3(0f, 0.21f, dz),
                    new Vector3(0.14f, 0.07f, 0.32f), DarkGreen);
            }
            // neck
            Part(PrimitiveType.Sphere, _root, new Vector3(0f, 0.10f, 0.40f),
                new Vector3(0.38f, 0.29f, 0.40f), BodyGreen);

            // head pivot (empty, scale 1) so bob/sway move every feature undistorted
            _head = new GameObject("Head").transform;
            _head.SetParent(_root, false);
            _head.localPosition = new Vector3(0f, 0.17f, 0.63f);
            _headRest = _head.localPosition;
            Part(PrimitiveType.Sphere, _head, Vector3.zero, new Vector3(0.45f, 0.35f, 0.54f), BodyGreen);
            Part(PrimitiveType.Sphere, _head, new Vector3(0.17f, -0.05f, 0.03f), new Vector3(0.20f, 0.20f, 0.24f), BodyGreen);
            Part(PrimitiveType.Sphere, _head, new Vector3(-0.17f, -0.05f, 0.03f), new Vector3(0.20f, 0.20f, 0.24f), BodyGreen);
            Part(PrimitiveType.Sphere, _head, new Vector3(0f, -0.04f, 0.22f), new Vector3(0.29f, 0.23f, 0.38f), BodyGreen);    // snout
            Part(PrimitiveType.Sphere, _head, new Vector3(0f, -0.095f, 0.20f), new Vector3(0.27f, 0.14f, 0.32f), BellyGreen); // jaw
            Part(PrimitiveType.Sphere, _head, new Vector3(0.05f, -0.02f, 0.36f), new Vector3(0.044f, 0.044f, 0.06f), black);
            Part(PrimitiveType.Sphere, _head, new Vector3(-0.05f, -0.02f, 0.36f), new Vector3(0.044f, 0.044f, 0.06f), black);
            BuildEye(1);
            BuildEye(-1);

            // legs: empty pivot + thigh/foot/toes; pivot animates, children inherit no distortion
            _legs = new Transform[4];
            _legRest = new Vector3[4];
            float[] legZ = { 0.30f, 0.30f, -0.26f, -0.26f };
            float[] footZ = { 0.12f, 0.12f, 0.08f, 0.08f };
            int[] legSx = { 1, -1, 1, -1 };
            for (int i = 0; i < 4; i++)
            {
                int sx = legSx[i];
                var pivot = new GameObject("Leg" + i).transform;
                pivot.SetParent(_root, false);
                pivot.localPosition = new Vector3(0.21f * sx, -0.04f, legZ[i]);
                Part(PrimitiveType.Sphere, pivot, Vector3.zero, new Vector3(0.21f, 0.15f, 0.26f), DarkGreen);            // thigh
                float fz = footZ[i];
                Part(PrimitiveType.Sphere, pivot, new Vector3(0.10f * sx, -0.10f, fz), new Vector3(0.23f, 0.10f, 0.32f), DarkGreen); // foot
                for (int t = -1; t <= 1; t++)
                    Part(PrimitiveType.Sphere, pivot, new Vector3(0.14f * sx, -0.135f, fz + 0.13f + t * 0.055f),
                        new Vector3(0.06f, 0.044f, 0.13f), DarkGreen);                                                  // toes
                _legs[i] = pivot;
                _legRest[i] = pivot.localPosition;
            }

            BuildTail();
            ApplyCosmetics();
        }

        /// <summary>Build the tapering tail segments onto _root (also used by RegrowTail).</summary>
        private void BuildTail()
        {
            _tail = new Transform[8];
            float tz = -0.55f, ts = 0.165f;
            for (int i = 0; i < 8; i++)
            {
                var seg = Part(PrimitiveType.Sphere, _root, new Vector3(0f, 0.01f, tz),
                    new Vector3(2f * ts, 2f * ts * 0.8f, 0.44f), i < 2 ? BodyGreen : DarkGreen);
                _tail[i] = seg.transform;
                tz -= 0.215f;
                ts *= 0.83f;
            }
        }

        /// <summary>Render the equipped cosmetics (wardrobe items) on the body.</summary>
        private void ApplyCosmetics()
        {
            // tail color
            var tail = MetaProgress.EquippedItem(CosmeticSlot.TailColor);
            if (tail != null && !tail.IsDefault)
            {
                for (int i = 0; i < _tail.Length; i++)
                {
                    var r = _tail[i].GetComponent<Renderer>();
                    if (r == null) continue;
                    Color c = tail.ShapeId == "rainbow"
                        ? Color.HSVToRGB(i / (float)_tail.Length, 0.8f, 1f) : tail.Tint;
                    r.sharedMaterial = MaterialCache.GetLit(c);
                }
            }

            var hat = MetaProgress.EquippedItem(CosmeticSlot.Hat);
            if (hat != null && hat.ShapeId != "none") BuildHat(hat);

            var glasses = MetaProgress.EquippedItem(CosmeticSlot.Glasses);
            if (glasses != null && glasses.ShapeId != "none") BuildGlasses(glasses);

            var pack = MetaProgress.EquippedItem(CosmeticSlot.Backpack);
            if (pack != null && pack.ShapeId == "pack")
                Part(PrimitiveType.Cube, _root, new Vector3(0f, 0.02f, -0.34f),
                    new Vector3(0.42f, 0.42f, 0.3f), pack.Tint);
        }

        private void BuildHat(CosmeticItem hat)
        {
            var h = new GameObject("Hat").transform;
            h.SetParent(_root, false);
            h.localPosition = new Vector3(0f, 0.36f, 0.63f);
            if (hat.ShapeId == "straw")
            {
                Part(PrimitiveType.Cylinder, h, new Vector3(0f, 0f, 0f), new Vector3(0.64f, 0.03f, 0.64f), hat.Tint);
                Part(PrimitiveType.Sphere, h, new Vector3(0f, 0.09f, 0f), new Vector3(0.34f, 0.26f, 0.34f), hat.Tint);
            }
            else if (hat.ShapeId == "cone")
            {
                Part(PrimitiveType.Cylinder, h, new Vector3(0f, 0.18f, 0f), new Vector3(0.3f, 0.26f, 0.3f), hat.Tint);
                Part(PrimitiveType.Sphere, h, new Vector3(0f, 0.42f, 0f), new Vector3(0.12f, 0.12f, 0.12f), Color.white);
            }
            else if (hat.ShapeId == "crown")
            {
                Part(PrimitiveType.Cylinder, h, new Vector3(0f, 0.02f, 0f), new Vector3(0.44f, 0.12f, 0.44f), hat.Tint);
                for (int k = 0; k < 5; k++)
                {
                    float a = k / 5f * Mathf.PI * 2f;
                    Part(PrimitiveType.Cube, h, new Vector3(Mathf.Sin(a) * 0.18f, 0.14f, Mathf.Cos(a) * 0.18f),
                        new Vector3(0.08f, 0.16f, 0.08f), hat.Tint);
                }
            }
        }

        private void BuildGlasses(CosmeticItem g)
        {
            var h = new GameObject("Glasses").transform;
            h.SetParent(_root, false);
            h.localPosition = new Vector3(0f, 0.31f, 0.58f);
            Part(PrimitiveType.Sphere, h, new Vector3(0.18f, 0f, 0f), new Vector3(0.18f, 0.15f, 0.06f), g.Tint);
            Part(PrimitiveType.Sphere, h, new Vector3(-0.18f, 0f, 0f), new Vector3(0.18f, 0.15f, 0.06f), g.Tint);
            Part(PrimitiveType.Cube, h, new Vector3(0f, 0f, 0f), new Vector3(0.18f, 0.03f, 0.04f), g.Tint);
        }

        // Big charming eye (white + amber iris + pupil + catchlight + brow),
        // built into the head pivot; iris faces up/back so it reads from behind.
        private void BuildEye(int s)
        {
            Part(PrimitiveType.Sphere, _head, new Vector3(0.175f * s, 0.12f, -0.03f),
                new Vector3(0.23f, 0.23f, 0.23f), Color.white);
            Part(PrimitiveType.Sphere, _head, new Vector3(0.195f * s, 0.145f, -0.08f),
                new Vector3(0.156f, 0.156f, 0.12f), new Color(0.96f, 0.75f, 0.18f));
            Part(PrimitiveType.Sphere, _head, new Vector3(0.205f * s, 0.155f, -0.12f),
                new Vector3(0.084f, 0.084f, 0.07f), new Color(0.05f, 0.05f, 0.06f));
            Part(PrimitiveType.Sphere, _head, new Vector3(0.165f * s, 0.19f, -0.14f),
                new Vector3(0.044f, 0.044f, 0.044f), Color.white);
            Part(PrimitiveType.Sphere, _head, new Vector3(0.14f * s, 0.205f, -0.01f),
                new Vector3(0.17f, 0.10f, 0.20f), DarkGreen);
        }

        private GameObject Part(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 localScale, Color color)
        {
            var p = GameObject.CreatePrimitive(type);
            // visual-only: colliders come from the CharacterController, not the mesh parts
            var col = p.GetComponent<Collider>();
            if (col != null) Destroy(col);
            p.transform.SetParent(parent, false);
            p.transform.localPosition = localPos;
            p.transform.localScale = localScale;
            var r = p.GetComponent<Renderer>();
            r.sharedMaterial = MaterialCache.GetLit(color);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            _renderers.Add(r);
            return p;
        }

        public void AnimateIdle()
        {
            if (_modelMode) { ModelAnimate(0f, false); return; }
            float t = Time.time;
            // slow breathing + curious head sway + lazy tail
            _root.localScale = new Vector3(1f, 1f + Mathf.Sin(t * 2.2f) * 0.03f, 1f);
            _head.localPosition = _headRest;
            _head.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 0.9f) * 18f, 0f);
            WaveTail(t * 1.5f, 0.08f);
        }

        private float _stumbleT; // counts down while the lizard tumbles from a foot-bump

        /// <summary>Trip over: a quick head-over-heels tumble after running into a leg/shoe.
        /// Overrides the gait until it recovers (see <see cref="AnimateStumble"/>).</summary>
        public void Stumble()
        {
            if (_squashed) return;
            _stumbleT = GameConst.StumbleDuration;
        }

        // Drive the tumble pose while recovering; returns true if it owns the body this frame.
        private bool AnimateStumble()
        {
            if (_stumbleT <= 0f || _squashed) return false;
            _stumbleT -= Time.deltaTime;
            float k = 1f - Mathf.Clamp01(_stumbleT / GameConst.StumbleDuration); // 0..1 progress
            float pitch = Mathf.Sin(k * Mathf.PI) * 70f;       // tip forward then recover
            float roll = Mathf.Sin(k * Mathf.PI * 2f) * 25f;   // wobble sideways
            float hop = Mathf.Sin(k * Mathf.PI) * 0.05f;       // little bounce
            _root.localScale = Vector3.one;
            _root.localPosition = new Vector3(0f, hop, 0f);
            _root.localRotation = Quaternion.Euler(pitch, ModelYaw, roll);
            return true;
        }

        private float _faceplantT; // counts down while splatted against a prop/wall

        /// <summary>Splat spread-eagle against a prop/wall, hold, then peel back upright.</summary>
        public void Faceplant()
        {
            if (_squashed) return;
            _faceplantT = GameConst.FaceplantDuration;
        }

        private bool AnimateFaceplant()
        {
            if (_faceplantT <= 0f || _squashed) return false;
            _faceplantT -= Time.deltaTime;
            float p = 1f - Mathf.Clamp01(_faceplantT / GameConst.FaceplantDuration); // 0..1 progress
            float splat = p < 0.66f ? 1f : 1f - (p - 0.66f) / 0.34f;                 // held, then peel to 0
            _root.localScale = new Vector3(Mathf.Lerp(1f, 1.35f, splat), Mathf.Lerp(1f, 0.55f, splat),
                                           Mathf.Lerp(1f, 1.2f, splat));
            _root.localPosition = new Vector3(0f, 0.02f * splat, 0f);
            _root.localRotation = Quaternion.Euler(82f * splat, ModelYaw, 0f);
            // procedural lizard: splay the limbs flat against the surface
            if (!_modelMode && _legs != null)
                for (int i = 0; i < _legs.Length; i++)
                {
                    float side = (i % 2 == 0) ? -1f : 1f;
                    float fwd = (i < 2) ? 1f : -1f;
                    _legs[i].localPosition = _legRest[i] + new Vector3(side * 0.12f * splat, 0.02f * splat, fwd * 0.1f * splat);
                }
            return true;
        }

        public void AnimateRun(float speed, bool dashing)
        {
            if (AnimateFaceplant()) return;
            if (AnimateStumble()) return;
            if (_modelMode) { ModelAnimate(speed, dashing); return; }
            if (_squashed) return;
            float speed01 = Mathf.Clamp01(speed / GameConst.LizardMoveSpeed);
            _gaitPhase += Time.deltaTime * Mathf.Lerp(2f, dashing ? 34f : 22f, speed01);

            // diagonal gait: FL+BR vs FR+BL
            for (int i = 0; i < 4; i++)
            {
                float phase = _gaitPhase + ((i == 0 || i == 3) ? 0f : Mathf.PI);
                float lift = Mathf.Max(0f, Mathf.Sin(phase)) * 0.1f * speed01;
                float stride = Mathf.Cos(phase) * 0.16f * speed01;
                _legs[i].localPosition = _legRest[i] + new Vector3(0f, lift, stride);
            }

            WaveTail(_gaitPhase * 0.5f, 0.12f + 0.22f * speed01);

            // subtle stretch at speed, head bob
            float stretch = 1f + 0.12f * speed01 * (dashing ? 1.6f : 1f);
            _root.localScale = new Vector3(1f / Mathf.Sqrt(stretch), 1f / Mathf.Sqrt(stretch), stretch);
            _head.localPosition = _headRest + new Vector3(0f, Mathf.Abs(Mathf.Sin(_gaitPhase)) * 0.02f * speed01, 0f);
            _head.localRotation = Quaternion.identity;
        }

        private void WaveTail(float phase, float amplitude)
        {
            for (int i = 0; i < _tail.Length; i++)
            {
                float offset = Mathf.Sin(phase - i * 0.7f) * amplitude * (i + 1) * 0.3f;
                Vector3 p = _tail[i].localPosition;
                _tail[i].localPosition = new Vector3(offset, p.y, p.z);
            }
        }

        /// <summary>Procedural scuttle for a rig-less imported model: the legs are baked
        /// into the static mesh, so we sell the gait with the whole body — a quick step
        /// bob, a left-right waddle, a roll into each step, and a forward lean at speed.
        /// Amplitudes are tuned to the ~0.15u model so the motion clearly reads.</summary>
        private void ModelAnimate(float speed, bool dashing)
        {
            if (_squashed) return;
            float s01 = Mathf.Clamp01(speed / GameConst.LizardMoveSpeed);

            // First-person POV rides the lizard's own eyes. The third-person run pose (forward
            // lean + step bob + hip waddle) tilts the whole torso UP into that lens, so you'd see
            // the back/head dome instead of the street. In FP we keep the body dead flat & low so
            // the camera only ever sees the snout tip + the paddling front feet — only the LEGS
            // animate. (Third-person keeps the full lively body pose below.)
            if (LizardCameraController.Instance != null && LizardCameraController.Instance.IsFirstPerson)
            {
                _root.localPosition = Vector3.zero;
                _root.localRotation = Quaternion.Euler(0f, ModelYaw, 0f);
                AnimateModelLegs(s01, dashing);
                return;
            }

            float t = Time.unscaledTime;

            // step cadence ramps with speed; a slow idle shimmer keeps it alive at rest
            float rate = Mathf.Lerp(7f, dashing ? 32f : 22f, s01);
            float gait = Mathf.Max(s01, 0.08f);
            float step = t * rate;

            float bob = Mathf.Abs(Mathf.Sin(step)) * 0.02f * gait;            // up on each step
            float waddle = Mathf.Sin(step * 0.5f) * Mathf.Lerp(4f, 12f, s01); // hips sway L/R
            float roll = Mathf.Sin(step) * 6f * s01;                          // roll into the step
            float lean = s01 * (dashing ? 14f : 8f);                          // pitch forward at speed

            _root.localPosition = new Vector3(0f, bob, 0f);
            _root.localRotation = Quaternion.Euler(lean, ModelYaw + waddle, roll);

            AnimateModelLegs(s01, dashing);
        }

        /// <summary>Measure the imported model in the body's local frame (lizard faces +Z,
        /// base at y=0) so the first-person cam can ride the real snout/eye line and frame the
        /// front feet, instead of guessing from the body centre.</summary>
        private void ComputeModelMetrics()
        {
            Bounds b = default(Bounds);
            bool has = false;
            for (int n = 0; n < _renderers.Count; n++)
            {
                var r = _renderers[n];
                if (r == null) continue;
                Bounds wb = r.bounds;
                Vector3 c = wb.center, e = wb.extents;
                for (int sx = -1; sx <= 1; sx += 2)
                for (int sy = -1; sy <= 1; sy += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    Vector3 lp = transform.InverseTransformPoint(c + new Vector3(e.x * sx, e.y * sy, e.z * sz));
                    if (!has) { b = new Bounds(lp, Vector3.zero); has = true; }
                    else b.Encapsulate(lp);
                }
            }
            if (!has) return;
            ModelSnoutZ = b.max.z;                              // front tip
            ModelEyeY = Mathf.Lerp(b.center.y, b.max.y, 0.55f); // a touch below the crown
            ModelHalfWidth = b.extents.x;
        }

        /// <summary>Cache the four upper-leg bones of the rigged model + their bind pose, so we
        /// can swing them procedurally (the AI rig has no baked walk clip).</summary>
        private void CacheLegBones()
        {
            if (_root == null) return;
            var all = _root.GetComponentsInChildren<Transform>(true);
            var bones = new List<Transform>();
            var bind = new List<Quaternion>();
            for (int n = 0; n < LegBoneNames.Length; n++)
            {
                Transform found = null;
                for (int i = 0; i < all.Length; i++)
                    if (all[i].name == LegBoneNames[n]) { found = all[i]; break; }
                if (found == null) return; // rig not as expected — leave legs static
                bones.Add(found);
                bind.Add(found.localRotation);
            }
            _legBones = bones.ToArray();
            _legBind = bind.ToArray();
        }

        /// <summary>Swing the four leg bones fore/aft in a diagonal gait so the feet step.
        /// Done in world space around the lizard's left-right axis (robust to each bone's own
        /// local axis), after the body pose is set so it layers on top.</summary>
        private void AnimateModelLegs(float s01, bool dashing)
        {
            if (_legBones == null) return;
            _modelGait += Time.deltaTime * Mathf.Lerp(3f, dashing ? 26f : 15f, s01);
            Vector3 axis = transform.right;          // the lizard's sideways axis
            float amp = Mathf.Lerp(4f, 22f, s01);    // degrees of fore/aft swing
            for (int i = 0; i < _legBones.Length; i++)
            {
                var bone = _legBones[i];
                if (bone == null || bone.parent == null) continue;
                float swing = Mathf.Sin(_modelGait + LegPhase[i]) * amp;
                Quaternion worldBind = bone.parent.rotation * _legBind[i];
                bone.rotation = Quaternion.AngleAxis(swing, axis) * worldBind;
            }
        }

        public void Squash()
        {
            _squashed = true;
            if (_modelMode)
            {
                _root.localScale = new Vector3(1.5f, 0.18f, 1.5f);
                _root.localRotation = Quaternion.Euler(0f, ModelYaw, 0f);
                return;
            }
            _root.localScale = new Vector3(1.7f, 0.12f, 1.7f);
            _root.localPosition = new Vector3(0f, 0.05f, 0f);
        }

        public void Unsquash()
        {
            _squashed = false;
            _root.localScale = Vector3.one;
            if (_modelMode) { _root.localPosition = Vector3.zero; return; }
            _root.localPosition = new Vector3(0f, 0.22f, 0f);
        }

        public void SetBlink(bool blinking)
        {
            if (_bodyHidden) return; // first-person POV: stay hidden regardless of invuln blink
            bool visible = !blinking || Mathf.Repeat(Time.time, 0.2f) < 0.12f;
            for (int i = 0; i < _renderers.Count; i++)
                if (_renderers[i] != null) _renderers[i].enabled = visible;
        }

        private bool _bodyHidden;
        private bool _tailDropped;

        /// <summary>Hide/show the whole lizard body (first-person nose cam hides it so
        /// the snout doesn't fill the lens). The dropped tail FX is independent.</summary>
        public void SetBodyVisible(bool visible)
        {
            _bodyHidden = !visible;
            for (int i = 0; i < _renderers.Count; i++)
                if (_renderers[i] != null) _renderers[i].enabled = visible;
        }

        /// <summary>Autotomy: shed the tail. Procedurally we detach the real segments and
        /// let them wriggle off; on an imported model we fling a body-tinted tail proxy
        /// (we can't cut the single mesh). Either way it reads as "the tail came off".</summary>
        public void DropTail()
        {
            if (_tailDropped) return;
            _tailDropped = true;

            Vector3 basePos = _modelMode || _tail == null || _tail.Length == 0
                ? transform.position - transform.forward * 0.06f + Vector3.up * 0.03f
                : _tail[0].position;
            ParticleFx.DashDust(basePos); // little puff where it detaches

            if (!_modelMode && _tail != null && _tail.Length > 0)
            {
                var drop = new GameObject("DroppedTail").transform;
                drop.position = _tail[0].position;
                drop.rotation = _root.rotation;
                for (int i = 0; i < _tail.Length; i++)
                {
                    if (_tail[i] == null) continue;
                    _renderers.Remove(_tail[i].GetComponent<Renderer>()); // body blink/hide won't touch it
                    _tail[i].SetParent(drop, true);
                }
                drop.gameObject.AddComponent<DroppedTailFx>();
                _tail = new Transform[0]; // WaveTail now no-ops
            }
            else
            {
                SpawnTailProxy(basePos);
            }
        }

        /// <summary>Tail grows back (procedural lizard). The imported model keeps its own
        /// tail, so this is a no-op visual there — only the gameplay buffer resets.</summary>
        public void RegrowTail()
        {
            _tailDropped = false;
            if (_modelMode) return;
            if (_tail != null && _tail.Length > 0) return; // already present
            BuildTail();
            if (_bodyHidden)
                for (int i = 0; i < _tail.Length; i++)
                {
                    var r = _tail[i] != null ? _tail[i].GetComponent<Renderer>() : null;
                    if (r != null) r.enabled = false;
                }
            ApplyCosmetics(); // restore tail color cosmetic
        }

        // A small detached tail (for the imported-model case) that wriggles off and fades.
        private void SpawnTailProxy(Vector3 worldPos)
        {
            var drop = new GameObject("DroppedTail").transform;
            drop.position = worldPos;
            drop.rotation = Quaternion.LookRotation(-transform.forward, Vector3.up);
            float ts = 0.022f, z = 0f;
            for (int i = 0; i < 5; i++)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var col = p.GetComponent<Collider>(); if (col != null) Destroy(col);
                p.transform.SetParent(drop, false);
                p.transform.localPosition = new Vector3(0f, 0f, z);
                p.transform.localScale = new Vector3(ts, ts * 0.8f, ts * 2.6f);
                p.GetComponent<Renderer>().sharedMaterial = MaterialCache.GetLit(i < 1 ? BodyGreen : DarkGreen);
                z += ts * 2.0f; ts *= 0.82f;
            }
            drop.gameObject.AddComponent<DroppedTailFx>();
        }

        /// <summary>Makes a detached tail wriggle, drift back and shrink away, then vanish.</summary>
        private class DroppedTailFx : MonoBehaviour
        {
            private float _t;
            private Vector3 _baseScale;
            private void Start() { _baseScale = transform.localScale; }
            private void Update()
            {
                _t += Time.deltaTime;
                float wob = Mathf.Sin(_t * 26f) * Mathf.Lerp(14f, 2f, Mathf.Clamp01(_t / 1.6f));
                transform.rotation *= Quaternion.Euler(0f, wob * Time.deltaTime * 6f, 0f);
                transform.position += (-transform.forward * 0.04f + Vector3.down * 0.02f) * Time.deltaTime;
                if (_t > 1.3f)
                {
                    float k = Mathf.Clamp01(1f - (_t - 1.3f) / 0.6f);
                    transform.localScale = _baseScale * k;
                    if (k <= 0.001f) Destroy(gameObject);
                }
            }
        }
    }
}
