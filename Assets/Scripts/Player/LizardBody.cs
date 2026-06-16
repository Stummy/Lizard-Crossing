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

            // imported lizard model if present; otherwise the procedural gecko.
            // Realistic scale (2026-06-16): a real ~15 cm lizard, dwarfed by the
            // human-scale city (person ~1.8 u, building ~20 u). 1 unit ~= 1 metre.
            var model = ModelLibrary.TryBuild(ModelLibrary.LizardKey, transform, 0.15f, ModelYaw);
            if (model != null)
            {
                _modelMode = true;
                _root = model;
                foreach (var r in model.GetComponentsInChildren<Renderer>()) _renderers.Add(r);
                return;
            }

            // tint to the chosen lizard
            var species = MetaProgress.SelectedLizard;
            if (species != null)
            {
                BodyGreen = species.BodyColor;
                BellyGreen = species.BellyColor;
                DarkGreen = species.StripeColor;
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

            // tapering tail
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

            ApplyCosmetics();
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

        public void AnimateRun(float speed, bool dashing)
        {
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

        /// <summary>Procedural scuttle for an imported model: bob + yaw wobble + lean.</summary>
        private void ModelAnimate(float speed, bool dashing)
        {
            if (_squashed) return;
            float s01 = Mathf.Clamp01(speed / GameConst.LizardMoveSpeed);
            float t = Time.unscaledTime;
            float rate = Mathf.Lerp(3f, dashing ? 26f : 18f, s01);
            float bob = Mathf.Abs(Mathf.Sin(t * rate)) * 0.07f * Mathf.Max(s01, 0.12f);
            float wob = Mathf.Sin(t * rate * 0.5f) * Mathf.Lerp(2.5f, 9f, s01);
            float roll = Mathf.Sin(t * rate) * 4f * s01;
            _root.localPosition = new Vector3(0f, bob, 0f);
            _root.localRotation = Quaternion.Euler(s01 * 6f, ModelYaw + wob, roll);
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
            bool visible = !blinking || Mathf.Repeat(Time.time, 0.2f) < 0.12f;
            for (int i = 0; i < _renderers.Count; i++)
                if (_renderers[i] != null) _renderers[i].enabled = visible;
        }
    }
}
