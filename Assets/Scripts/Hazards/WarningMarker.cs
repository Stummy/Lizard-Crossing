using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Danger telegraph (packet required system: WarningMarker): a soft dark
    /// shadow blob sized to the true kill footprint plus a pulsing red ring,
    /// per ART_DIRECTION.md ("red warning circles or shadows before hazards land").
    /// Gameplay-critical, so it's an explicit quad — never a realtime shadow map
    /// (docs/DECISIONS.md D6).
    /// </summary>
    public class WarningMarker : MonoBehaviour
    {
        private Renderer _shadow;
        private Renderer _ring;
        private float _baseW;
        private float _baseL;
        private float _intensity;

        /// <summary>0 = inactive, 1 = slam imminent. Read by bot playtests.</summary>
        public float Intensity { get { return _intensity; } }

        public static WarningMarker Create(Transform parent, float footprintLength, float footprintWidth)
        {
            var go = new GameObject("WarningMarker");
            go.transform.SetParent(parent, false);
            var m = go.AddComponent<WarningMarker>();
            m._baseW = footprintWidth;
            m._baseL = footprintLength;

            // blob texture falls off ~30% before its edge; oversize so the dark
            // core matches the true footprint
            m._shadow = MakeQuad(go.transform, MaterialCache.ShadowBlob,
                new Vector3(footprintWidth * 1.5f, footprintLength * 1.4f, 1f), 0.03f);
            m._ring = MakeQuad(go.transform, MaterialCache.WarningRing,
                new Vector3(footprintWidth * 2.1f, footprintLength * 1.9f, 1f), 0.04f);

            m.SetIntensity(0f);
            return m;
        }

        private static Renderer MakeQuad(Transform parent, Material baseMaterial, Vector3 scale, float y)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localPosition = new Vector3(0f, y, 0f);
            quad.transform.localScale = scale;
            var r = quad.GetComponent<Renderer>();
            r.material = new Material(baseMaterial); // instance: alpha animates per marker
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return r;
        }

        /// <summary>0 = hidden, 1 = imminent. Drives alpha, growth and ring pulse.</summary>
        public void SetIntensity(float t)
        {
            _intensity = Mathf.Clamp01(t);

            Color sc = _shadow.material.color;
            sc.a = Mathf.Lerp(0f, 0.62f, _intensity);
            _shadow.material.color = new Color(0f, 0f, 0f, sc.a);
            _shadow.transform.localScale = Vector3.Lerp(
                new Vector3(_baseW * 0.7f, _baseL * 0.65f, 1f),
                new Vector3(_baseW * 1.5f, _baseL * 1.4f, 1f), _intensity);

            // ring pulses faster as danger approaches
            float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * Mathf.Lerp(6f, 18f, _intensity));
            _ring.material.color = new Color(1f, 0.18f, 0.1f, Mathf.Lerp(0f, 0.85f, _intensity) * pulse);
            _ring.transform.localScale = Vector3.Lerp(
                new Vector3(_baseW * 2.6f, _baseL * 2.3f, 1f),
                new Vector3(_baseW * 1.9f, _baseL * 1.7f, 1f), _intensity); // ring tightens inward
        }

        public void SetWorldPosition(Vector3 groundPos)
        {
            transform.position = new Vector3(groundPos.x, GameConst.GroundY, groundPos.z);
        }

        public void Hide() { SetIntensity(0f); }
    }
}
