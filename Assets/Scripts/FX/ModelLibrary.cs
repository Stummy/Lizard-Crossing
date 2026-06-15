using System.Collections.Generic;
using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Runtime seam for swapping imported 3D models in for procedural geometry
    /// (free-asset pipeline — see docs/ASSET_PIPELINE.md and ATTRIBUTION.md).
    ///
    /// Convention: a model prefab/GLB at <c>Assets/Resources/Models/&lt;key&gt;</c>
    /// is picked up automatically. <see cref="TryBuild"/> instantiates it inside a
    /// holder, scales it so its largest horizontal dimension matches a target
    /// world size (so a swapped model lands at the same on-screen scale as the
    /// primitive it replaces — no gameplay/collision retuning), seats its base at
    /// the parent origin, strips imported colliders, and returns the holder.
    /// Returns null when no model is present, so every call site keeps its
    /// procedural fallback and the game still runs with zero imported assets.
    /// </summary>
    public static class ModelLibrary
    {
        public const string LizardKey = "lizard";
        public const string SneakerKey = "sneaker";
        public const string WheelKey = "wheel";
        public const string DrainKey = "drain";
        public const string BinKey = "trashbin";
        public const string ChairKey = "chair";
        public const string SignKey = "sign";
        public const string PlanterKey = "planter";

        private static readonly Dictionary<string, GameObject> Cache = new Dictionary<string, GameObject>();
        private static readonly HashSet<string> Missing = new HashSet<string>();

        public static bool Has(string key) { return LoadPrefab(key) != null; }

        private static GameObject LoadPrefab(string key)
        {
            if (Missing.Contains(key)) return null;
            GameObject prefab;
            if (Cache.TryGetValue(key, out prefab)) return prefab;
            prefab = Resources.Load<GameObject>("Models/" + key);
            if (prefab == null) Missing.Add(key);
            else Cache[key] = prefab;
            return prefab;
        }

        /// <summary>
        /// Instantiate the model for <paramref name="key"/> under
        /// <paramref name="parent"/>, normalized so its largest horizontal
        /// dimension equals <paramref name="targetFootprint"/> and its base sits
        /// on the ground. Returns the holder transform, or null if no model asset
        /// exists for the key.
        /// </summary>
        public static Transform TryBuild(string key, Transform parent, float targetFootprint, float yawDeg = 0f)
        {
            var prefab = LoadPrefab(key);
            if (prefab == null) return null;

            var holder = new GameObject(key + "_model").transform;
            holder.SetParent(parent, false);
            holder.localRotation = Quaternion.Euler(0f, yawDeg, 0f);

            var go = Object.Instantiate(prefab);
            go.transform.SetParent(holder, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            Bounds b;
            if (!TryGetLocalBounds(go, holder, out b))
            {
                Object.Destroy(holder.gameObject);
                return null;
            }

            float footprint = Mathf.Max(b.size.x, b.size.z);
            if (footprint < 1e-4f) footprint = Mathf.Max(b.size.y, 1e-4f);
            go.transform.localScale = Vector3.one * (targetFootprint / footprint);

            // reseat: base at y=0, centered on x/z
            if (TryGetLocalBounds(go, holder, out b))
                go.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);

            foreach (var col in holder.GetComponentsInChildren<Collider>())
                Object.Destroy(col);

            return holder;
        }

        /// <summary>Combined renderer bounds of <paramref name="go"/> expressed in
        /// <paramref name="space"/>'s local frame (corner-accurate under rotation).</summary>
        private static bool TryGetLocalBounds(GameObject go, Transform space, out Bounds bounds)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            bounds = default(Bounds);
            bool has = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Bounds wb = renderers[i].bounds;
                Vector3 c = wb.center, e = wb.extents;
                for (int sx = -1; sx <= 1; sx += 2)
                for (int sy = -1; sy <= 1; sy += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    Vector3 corner = c + new Vector3(e.x * sx, e.y * sy, e.z * sz);
                    Vector3 lp = space.InverseTransformPoint(corner);
                    if (!has) { bounds = new Bounds(lp, Vector3.zero); has = true; }
                    else bounds.Encapsulate(lp);
                }
            }
            return has;
        }

        public static void ClearCache()
        {
            Cache.Clear();
            Missing.Clear();
        }
    }
}
