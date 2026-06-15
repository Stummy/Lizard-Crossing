using System.Collections.Generic;
using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Shared-material factory so the whole procedural world batches well.
    /// Built-in RP Phase 1 (docs/DECISIONS.md D1): Standard for lit surfaces,
    /// Sprites/Default for soft transparent quads (shadows, particles, wings).
    /// </summary>
    public static class MaterialCache
    {
        private static readonly Dictionary<Color, Material> Lit = new Dictionary<Color, Material>();
        private static Material _shadowBlob;
        private static Material _warningRing;
        private static Material _softParticle;

        public static Material GetLit(Color color)
        {
            Material m;
            if (Lit.TryGetValue(color, out m) && m != null) return m;
            m = new Material(Shader.Find("Standard"));
            m.color = color;
            m.SetFloat("_Glossiness", 0.12f);
            m.SetFloat("_Metallic", 0f);
            Lit[color] = m;
            return m;
        }

        public static Material GetLitTextured(Texture2D tex, Color tint)
        {
            var m = new Material(Shader.Find("Standard"));
            m.mainTexture = tex;
            m.color = tint;
            m.SetFloat("_Glossiness", 0.08f);
            m.SetFloat("_Metallic", 0f);
            return m;
        }

        /// <summary>
        /// PBR surface with an albedo + normal map (real depth on the ground).
        /// Falls back gracefully when the normal map is absent.
        /// </summary>
        public static Material GetTexturedNormal(Texture2D albedo, Texture2D normal, Color tint,
            float smoothness, float tileX, float tileY)
        {
            var m = new Material(Shader.Find("Standard"));
            m.mainTexture = albedo;
            m.color = tint;
            m.SetFloat("_Glossiness", smoothness);
            m.SetFloat("_Metallic", 0f);
            m.mainTextureScale = new Vector2(tileX, tileY);
            if (normal != null)
            {
                m.EnableKeyword("_NORMALMAP");
                m.SetTexture("_BumpMap", normal);
                m.SetTextureScale("_BumpMap", new Vector2(tileX, tileY));
                m.SetFloat("_BumpScale", 1.1f);
            }
            return m;
        }

        /// <summary>Soft round transparent blob used for warning shadows.</summary>
        public static Material ShadowBlob
        {
            get
            {
                if (_shadowBlob == null)
                {
                    _shadowBlob = new Material(Shader.Find("Sprites/Default"));
                    _shadowBlob.mainTexture = ProceduralTextures.RadialGradient;
                }
                return _shadowBlob;
            }
        }

        /// <summary>Red pulsing danger ring used by WarningMarker.</summary>
        public static Material WarningRing
        {
            get
            {
                if (_warningRing == null)
                {
                    _warningRing = new Material(Shader.Find("Sprites/Default"));
                    _warningRing.mainTexture = ProceduralTextures.Ring;
                }
                return _warningRing;
            }
        }

        public static Material SoftParticle
        {
            get
            {
                if (_softParticle == null)
                {
                    _softParticle = new Material(Shader.Find("Sprites/Default"));
                    _softParticle.mainTexture = ProceduralTextures.RadialGradient;
                }
                return _softParticle;
            }
        }

        public static void ClearRuntimeCache()
        {
            Lit.Clear();
            _shadowBlob = null;
            _warningRing = null;
            _softParticle = null;
        }
    }
}
