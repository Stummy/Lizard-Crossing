using System.Collections.Generic;
using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Shared-material factory so the whole procedural world batches well.
    /// URP pass (docs/DECISIONS.md D1): URP/Lit for lit surfaces (falls back to
    /// Standard if URP isn't installed yet), Sprites/Default for soft transparent
    /// quads (shadows, particles, wings — works unchanged under both pipelines).
    ///
    /// Magenta-trap note: never hardcode "Standard". LitShader resolves URP/Lit
    /// when present so runtime materials match whichever pipeline is active.
    /// Color/texture go through m.color / m.mainTexture, which Unity routes to the
    /// shader's [MainColor]/[MainTexture] (URP _BaseColor/_BaseMap or Standard
    /// _Color/_MainTex). Smoothness differs by shader, so we set both names.
    /// </summary>
    public static class MaterialCache
    {
        private static readonly Dictionary<Color, Material> Lit = new Dictionary<Color, Material>();
        private static readonly Dictionary<Material, Material> Remap = new Dictionary<Material, Material>();
        private static Material _shadowBlob;
        private static Material _warningRing;
        private static Material _softParticle;
        private static Shader _litShader;

        /// <summary>URP/Lit when the package is installed, else Standard. Cached.</summary>
        public static Shader LitShaderAsset
        {
            get
            {
                if (_litShader == null)
                    _litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                return _litShader;
            }
        }

        private static Shader _unlitShader;

        /// <summary>URP/Unlit when installed, else Built-in Unlit/Texture. Cached.</summary>
        public static Shader UnlitShaderAsset
        {
            get
            {
                if (_unlitShader == null)
                    _unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
                return _unlitShader;
            }
        }

        /// <summary>Unlit textured surface (backdrops, sky quads). Pipeline-agnostic.</summary>
        public static Material GetUnlitTextured(Texture tex)
        {
            var m = new Material(UnlitShaderAsset);
            m.mainTexture = tex; // routes to _BaseMap (URP) or _MainTex (Built-in)
            return m;
        }

        /// <summary>Set smoothness/metallic using whichever property the active shader exposes.</summary>
        private static void SetPbr(Material m, float smoothness, float metallic)
        {
            m.SetFloat("_Smoothness", smoothness); // URP/Lit
            m.SetFloat("_Glossiness", smoothness);  // Standard (ignored if absent)
            m.SetFloat("_Metallic", metallic);
        }

        public static Material GetLit(Color color)
        {
            Material m;
            if (Lit.TryGetValue(color, out m) && m != null) return m;
            m = new Material(LitShaderAsset);
            m.color = color;
            SetPbr(m, 0.12f, 0f);
            Lit[color] = m;
            return m;
        }

        public static Material GetLitTextured(Texture2D tex, Color tint)
        {
            var m = new Material(LitShaderAsset);
            m.mainTexture = tex;
            m.color = tint;
            SetPbr(m, 0.08f, 0f);
            return m;
        }

        /// <summary>
        /// PBR surface with an albedo + normal map (real depth on the ground).
        /// Falls back gracefully when the normal map is absent.
        /// </summary>
        public static Material GetTexturedNormal(Texture2D albedo, Texture2D normal, Color tint,
            float smoothness, float tileX, float tileY)
        {
            var m = new Material(LitShaderAsset);
            m.mainTexture = albedo;
            m.color = tint;
            SetPbr(m, smoothness, 0f);
            m.mainTextureScale = new Vector2(tileX, tileY);
            if (normal != null)
            {
                m.EnableKeyword("_NORMALMAP"); // same keyword + _BumpMap on URP/Lit and Standard
                m.SetTexture("_BumpMap", normal);
                m.SetTextureScale("_BumpMap", new Vector2(tileX, tileY));
                m.SetFloat("_BumpScale", 1.1f);
            }
            return m;
        }

        /// <summary>
        /// Convert an imported material (often a Standard/HDRP-shader asset that
        /// renders magenta under the active URP pipeline) into a URP/Lit equivalent,
        /// preserving its albedo texture, tint and normal map. Cached per source
        /// material so shared submeshes across a prefab stay batched. Used to make
        /// imported character prefabs render under whichever pipeline is active.
        /// </summary>
        public static Material GetUrpEquivalent(Material src)
        {
            if (src == null) return GetLit(Color.gray);
            Material m;
            if (Remap.TryGetValue(src, out m) && m != null) return m;

            m = new Material(LitShaderAsset);
            m.color = src.HasProperty("_Color") ? src.color : Color.white;
            if (src.mainTexture != null) m.mainTexture = src.mainTexture; // _MainTex → _BaseMap
            SetPbr(m, 0.18f, 0f);

            Texture bump = src.HasProperty("_BumpMap") ? src.GetTexture("_BumpMap") : null;
            if (bump != null)
            {
                m.EnableKeyword("_NORMALMAP");
                m.SetTexture("_BumpMap", bump);
                m.SetFloat("_BumpScale", 1f);
            }
            Remap[src] = m;
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
            Remap.Clear();
            _shadowBlob = null;
            _warningRing = null;
            _softParticle = null;
            _litShader = null;
        }
    }
}
