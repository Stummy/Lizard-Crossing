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
            // ring sits a touch higher than the shadow so the two ground decals never z-fight/flicker
            // (the flicker read as noise on the low POV); it's the alarm signal, so it rides on top.
            m._ring = MakeQuad(go.transform, MaterialCache.WarningRing,
                new Vector3(footprintWidth * 2.1f, footprintLength * 1.9f, 1f), 0.06f);

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
            sc.a = Mathf.Lerp(0f, 0.66f, _intensity);
            _shadow.material.color = new Color(0f, 0f, 0f, sc.a);
            _shadow.transform.localScale = Vector3.Lerp(
                new Vector3(_baseW * 0.7f, _baseL * 0.65f, 1f),
                new Vector3(_baseW * 1.5f, _baseL * 1.4f, 1f), _intensity);

            // Ring = the alarm signal; make it POP from the low POV. A more saturated, brighter red
            // and a higher peak alpha so the foreshortened ground decal still reads; the pulse floor
            // is lifted (0.78..1.0 vs 0.7..1.0) so even between pulses it stays clearly visible, and
            // it speeds up as the foot nears the ground. Base sizes nudged up so it isn't a thin
            // sliver when the camera is near grazing-angle.
            float pulse = 0.78f + 0.22f * Mathf.Sin(Time.time * Mathf.Lerp(7f, 20f, _intensity));
            _ring.material.color = new Color(1f, 0.13f, 0.07f, Mathf.Lerp(0f, 0.95f, _intensity) * pulse);
            _ring.transform.localScale = Vector3.Lerp(
                new Vector3(_baseW * 2.9f, _baseL * 2.6f, 1f),
                new Vector3(_baseW * 2.1f, _baseL * 1.9f, 1f), _intensity); // ring tightens inward as it closes
        }

        public void SetWorldPosition(Vector3 groundPos)
        {
            transform.position = new Vector3(groundPos.x, GameConst.GroundY, groundPos.z);
        }

        public void Hide() { SetIntensity(0f); }
    }
}
