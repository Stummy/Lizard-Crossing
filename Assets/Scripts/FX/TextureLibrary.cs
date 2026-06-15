using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Art seam between user-supplied Higgsfield textures and procedural
    /// fallbacks. Drop PNGs into Assets/Resources/GeneratedArt/ using the file
    /// names below (see Art/HIGGSFIELD_PROMPTS.md for the prompts + specs) and
    /// they are picked up automatically — no code changes.
    /// </summary>
    public static class TextureLibrary
    {
        public const string ResourceFolder = "GeneratedArt/";

        /// <summary>Ground paving albedo. File: pavement_stone.png/jpg (seamless).</summary>
        public static Texture2D Pavement
        {
            get { return Load("pavement_stone", ProceduralTextures.StoneTiles, repeat: true); }
        }

        /// <summary>Ground paving normal map. File: pavement_normal.png/jpg. Null-safe.</summary>
        public static Texture2D PavementNormal
        {
            get { return Load("pavement_normal", null, repeat: true); }
        }

        /// <summary>Corridor wall albedo. File: wall_stone.png/jpg (seamless). Null-safe.</summary>
        public static Texture2D Wall
        {
            get { return Load("wall_stone", null, repeat: true); }
        }

        /// <summary>Corridor wall normal map. File: wall_normal.png/jpg. Null-safe.</summary>
        public static Texture2D WallNormal
        {
            get { return Load("wall_normal", null, repeat: true); }
        }

        /// <summary>Far-end garden backdrop. File: garden_backdrop.png.</summary>
        public static Texture2D Backdrop
        {
            get { return Load("garden_backdrop", ProceduralTextures.Backdrop, repeat: false); }
        }

        /// <summary>Optional leaf decal sheet. File: leaf_decal.png (alpha). Null-safe: callers fall back to tinted quads.</summary>
        public static Texture2D LeafDecal
        {
            get { return Load("leaf_decal", null, repeat: false); }
        }

        /// <summary>Optional terracotta/pot surface. File: terracotta.png (seamless).</summary>
        public static Texture2D Terracotta
        {
            get { return Load("terracotta", null, repeat: true); }
        }

        public static bool HasGeneratedArt
        {
            get { return Resources.Load<Texture2D>(ResourceFolder + "pavement_stone") != null; }
        }

        private static Texture2D Load(string name, Texture2D fallback, bool repeat)
        {
            var tex = Resources.Load<Texture2D>(ResourceFolder + name);
            if (tex != null)
            {
                tex.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                return tex;
            }
            return fallback;
        }
    }
}
