using System.Collections.Generic;
using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Binds the Kenney palette-atlas (<c>colormap</c>) to the imported CityKit GLB materials at
    /// runtime. The Kenney car / signage GLBs ship a single shared <c>colormap</c> material whose
    /// submesh UVs index a 512² color palette — but Unity's glTF importer leaves
    /// <c>baseColorTexture</c> UNBOUND (the atlas isn't embedded), so the cars/cones import as flat
    /// WHITE. This skinner re-binds the matching atlas (cars vs. roads) so the taxi reads yellow,
    /// the cone orange, etc.
    ///
    /// Shader-agnostic + runtime-only (the GLB on disk is never modified): it sets whichever base
    /// texture/color the material exposes (glTF <c>baseColorTexture</c>/<c>baseColorFactor</c> or
    /// the URP <c>_BaseMap</c>/<c>_BaseColor</c> fallback), mirroring <see cref="CityReskin"/>.
    /// Each source prefab's shared materials are skinned ONCE (cached) so every instantiated car
    /// stays batched and we don't touch the same material twice.
    /// </summary>
    public static class CityKitSkin
    {
        const string CarAtlas  = "Models/CityKit/Vehicles/colormap_cars";
        const string RoadAtlas = "Models/CityKit/Signage/colormap_roads";

        static Texture2D _carTex, _roadTex;
        // Source prefabs already skinned (the shared materials are the prefab's own, so once is enough).
        static readonly HashSet<GameObject> _doneCars  = new HashSet<GameObject>();
        static readonly HashSet<GameObject> _doneRoads = new HashSet<GameObject>();

        /// <summary>Skin a vehicle prefab (taxi/sedan/...) with the car palette atlas. Idempotent.</summary>
        public static void SkinVehicle(GameObject src)
        {
            if (src == null || !_doneCars.Add(src)) return;
            if (_carTex == null) _carTex = Resources.Load<Texture2D>(CarAtlas);
            Bind(src, _carTex);
        }

        /// <summary>Skin a signage prefab (cone/barrier/sign) with the road palette atlas. Idempotent.</summary>
        public static void SkinSignage(GameObject src)
        {
            if (src == null || !_doneRoads.Add(src)) return;
            if (_roadTex == null) _roadTex = Resources.Load<Texture2D>(RoadAtlas);
            Bind(src, _roadTex);
        }

        static void Bind(GameObject root, Texture2D atlas)
        {
            if (atlas == null) return;
            var done = new HashSet<Material>();
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null || !done.Add(m)) continue;
                    SetFirstTexture(m, atlas, "baseColorTexture", "_BaseMap", "_MainTex");
                    // clear any baking tint so the palette reads at its true colors
                    SetFirstColor(m, Color.white, "baseColorFactor", "_BaseColor", "_Color");
                }
        }

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
    }
}
