using System.Collections.Generic;
using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Re-skins the imported "NYCity" GLB's baked materials with the high-res Fab/Megascans
    /// surface scans dropped into <c>Resources/GeneratedArt</c>. The city ships its walkable
    /// surfaces (sidewalk, road, grass, facades) as low-res textures baked onto the GLB's own
    /// materials — the <see cref="TextureLibrary"/> slots never touch them — so we swap the
    /// base (and normal) maps in place at level build, KEEPING the GLB's own UVs so the scans
    /// sit at realistic scale with no retiling.
    ///
    /// Visual only: no colliders, gameplay, scale, camera, or hazard logic is touched. Runs at
    /// runtime (called from <see cref="LevelBuilder"/> when the GLB is present), so the GLB
    /// asset on disk is never modified — the swap re-applies each play. See
    /// Lizard_Crossing_Claude_Work_Packet/05_AssetPlan/ASSET_INTEGRATION_PLAN.md.
    /// </summary>
    public static class CityReskin
    {
        // A Skin can swap the base/normal maps AND/OR force a flat tint. When Base is null
        // the material keeps its own texture and only the Tint (if set) is applied — used to
        // recolor the crosswalk-stripe geometry to clean painted white. Tint with a<0 = leave.
        struct Skin { public string Base; public string Normal; public Color Tint;
                      public Skin(string b, string n) { Base = b; Normal = n; Tint = new Color(0,0,0,-1); }
                      public Skin(string b, string n, Color t) { Base = b; Normal = n; Tint = t; } }

        // NYCity GLB material name -> GeneratedArt texture base/normal names.
        // A null Normal leaves the material's existing normal map untouched.
        static readonly Dictionary<string, Skin> Map = new Dictionary<string, Skin>
        {
            { "CityGenside_walks",   new Skin("pavement_stone", "pavement_normal") }, // sidewalk (was 512px)
            { "CityGen_Streets",     new Skin("asphalt",        null) },              // road (was 512px)
            { "CityGen_Grass",       new Skin("grass",          null) },              // grass pockets
            // BUILDINGS: real CC0 facade textures WITH WINDOWS (ambientCG Facade018/019/020/006,
            // public domain) replacing the windowless brick/stone that read as featureless boxes.
            // A mix of brick / concrete / glass across the city materials for real variety.
            // (Color only this pass — normals are the next polish step; null keeps the prior normal.)
            { "CityGen_LR_Facades",  new Skin("facade_brick",    null) },   // brick mid-rise + window rows
            { "Building_Facade.001", new Skin("facade_concrete", null) },   // grey concrete + window bands
            { "CityGenyellow_stone", new Skin("facade_brick2",   null) },   // brown-brick tower (variety)
            { "CityGensimple_concrete_1", new Skin("facade_concrete", null) }, // concrete facade w/ windows
            { "CityGenconcrete.001",      new Skin("facade_concrete", null) },

            // --- WO-4: road-zone barrier walls + crosswalk (were flat grey / flat orange) ---
            // The grey concrete barrier curbs flanking the road read as flat un-skinned grey.
            // Skin them with the granite scan so they match the city's stone, no flat grey.
            { "CityGen_Curb",        new Skin("granite",        null) },              // road-side concrete barrier/curb
            // Crosswalk = white-stripe geometry sitting on an orange-brown band. Skin the band
            // to dark asphalt and force the stripes to clean painted white so it reads as a real
            // crosswalk (bright stripes on dark road) instead of a flat orange-red rectangle.
            { "CityGen_lanes_secondary_color", new Skin("asphalt", null) },          // between-stripe road band -> asphalt
            { "CityGen_lanes_white",           new Skin(null, null, new Color(0.93f, 0.93f, 0.90f)) }, // painted stripes -> bright white
            // A flat ground-decal layer covering the whole street that read as flat magenta-pink
            // (its baked albedo). Skin it to asphalt so it blends into the road instead of
            // splotching the crossing pink.
            { "Street_Assets.001",             new Skin("asphalt", null) },
            // The RED twin (owner playtest: the "red fences/panels"): a second Street_Assets
            // material baked PURE RED (1,0,0) on two map-spanning street-furniture meshes
            // (Object_15/Object_16). Same placeholder class as .001 — never skinned, so it read
            // as flat-red eyesores flanking the run under the warm grade. Skin to asphalt so it
            // blends into the street instead of glowing red. (Making these solid colliders is a
            // separate, spatial pass — see BUG_AND_GAP_LOG fences entry.)
            { "Street_Assets",                 new Skin("asphalt", null) },

            // --- S2-3/S2-4: kill the giant flat PLACEHOLDER albedos on the imported GLB ---
            // A scene audit found several huge (180–280u) un-skinned GLB materials rendering as
            // pure primary placeholder colours — a magenta building face and green/blue/purple
            // "metal" structures — which dominated the mid-ground and shattered the palette (the
            // big "red wall" on approach was the magenta one read under the warm grade).
            { "material_0",                          new Skin("facade_glass", null) },                        // magenta (1,0,.5) building face -> glass office tower
            { "CityGenbasic_metal.001",              new Skin(null, null, new Color(0.24f, 0.25f, 0.26f)) },  // pure green -> dark weathered metal
            { "CityGenbasic_rough_metal_dark",       new Skin(null, null, new Color(0.20f, 0.21f, 0.23f)) },  // pure blue -> dark metal
            { "CityGenbasic_rough_metal_dark_.001",  new Skin(null, null, new Color(0.20f, 0.21f, 0.23f)) },  // pure purple -> dark metal
            { "Material.002",                        new Skin(null, null, new Color(0.56f, 0.52f, 0.45f)) },  // electric yellow blob -> muted warm stone
        };

        const string Folder = "GeneratedArt/";

        /// <summary>Swap base/normal maps on the matching materials under <paramref name="cityRoot"/>.</summary>
        public static void Apply(GameObject cityRoot)
        {
            if (cityRoot == null) return;
            var texCache = new Dictionary<string, Texture2D>();
            var done = new HashSet<Material>();

            foreach (var r in cityRoot.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null || !done.Add(m)) continue;          // each shared material once
                    if (!Map.TryGetValue(m.name, out var skin)) continue;

                    var baseTex = Load(skin.Base, texCache);
                    if (baseTex != null)
                    {
                        // The imported GLB uses Shader Graphs/glTF-pbrMetallicRoughness
                        // (baseColorTexture/baseColorFactor); procedural/URP fallback uses
                        // _BaseMap/_MainTex. Set whichever this material actually exposes.
                        SetFirstTexture(m, baseTex, "baseColorTexture", "_BaseMap", "_MainTex");
                        // clear any baking tint so the scan reads at its true albedo (unless an
                        // explicit Tint is provided below)
                        SetFirstColor(m, Color.white, "baseColorFactor", "_BaseColor", "_Color");
                    }

                    // Optional flat tint (alpha >= 0). Used to recolor stripe geometry that keeps
                    // its own texture (Base == null) to clean painted white.
                    if (skin.Tint.a >= 0f)
                        SetFirstColor(m, new Color(skin.Tint.r, skin.Tint.g, skin.Tint.b, 1f),
                            "baseColorFactor", "_BaseColor", "_Color");

                    var normalTex = Load(skin.Normal, texCache);
                    if (normalTex != null)
                    {
                        if (m.HasProperty("_BumpMap")) m.EnableKeyword("_NORMALMAP");
                        SetFirstTexture(m, normalTex, "normalTexture", "_BumpMap");
                        if (m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", 1f); // realistic, not exaggerated
                    }
                }
            }
        }

        /// <summary>Assign <paramref name="tex"/> to the first of <paramref name="props"/> the material exposes.</summary>
        static void SetFirstTexture(Material m, Texture tex, params string[] props)
        {
            foreach (var p in props)
                if (m.HasProperty(p)) { m.SetTexture(p, tex); return; }
        }

        static void SetFirstColor(Material m, Color c, params string[] props)
        {
            foreach (var p in props)
                if (m.HasProperty(p)) { m.SetColor(p, c); return; }
        }

        static Texture2D Load(string name, Dictionary<string, Texture2D> cache)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (cache.TryGetValue(name, out var t)) return t;
            t = Resources.Load<Texture2D>(Folder + name);
            cache[name] = t;
            return t;
        }
    }
}
