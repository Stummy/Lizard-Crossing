using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// A soft contact shadow that grounds the hero lizard: a flat dark radial decal that tracks
    /// the lizard's ground position each frame, so the lizard always reads as sitting ON the
    /// pavement (and pops off it) instead of floating — matching the concept hero. The lizard's
    /// real cast shadow rakes off to the side under the low golden-hour sun, so this guarantees a
    /// grounding shadow directly beneath it regardless of sun angle. Visual only.
    /// </summary>
    public class GroundShadow : MonoBehaviour
    {
        private Transform _target;

        public static GroundShadow Create(Transform target)
        {
            var go = new GameObject("GroundShadow");
            var gs = go.AddComponent<GroundShadow>();
            gs._target = target;

            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var col = q.GetComponent<Collider>(); if (col != null) Destroy(col);
            q.name = "ShadowQuad";
            q.transform.SetParent(go.transform, false);
            q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // lie flat on the ground
            q.transform.localScale = new Vector3(0.22f, 0.30f, 1f);     // oval, ~lizard footprint, elongated along run (+Z)

            var r = q.GetComponent<Renderer>();
            var m = new Material(Shader.Find("Sprites/Default"));
            m.mainTexture = ProceduralTextures.RadialGradient; // soft alpha falloff -> dark centre, transparent edge
            m.color = new Color(0f, 0f, 0f, 0.45f);
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return gs;
        }

        private void LateUpdate()
        {
            if (_target == null) { return; }
            Vector3 p = _target.position;
            float g = StreetGround.HeightAt(p.x, p.z);
            transform.position = new Vector3(p.x, g + 0.012f, p.z); // hug the pavement, just above z-fight range
        }
    }
}
