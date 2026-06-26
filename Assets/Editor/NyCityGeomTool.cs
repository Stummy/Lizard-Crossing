using System.Text;
using UnityEditor;
using UnityEngine;

namespace LizardCrossing.EditorTools
{
    /// <summary>
    /// Editor-only geometry probe/fixer for the imported NYCity GLB. The avenue's
    /// far end jogs right (the sidewalk "shifts" off the straight run band). This tool
    /// reports each street/building sub-mesh's WORLD bounds so we can see whether the
    /// pieces are spatial blocks (movable to straighten the run) or material groups,
    /// then surgically nudge just the offending far-end piece(s) on X and save the scene
    /// so the fix persists (the previous straighten was lost to an unsaved/Play-mode edit).
    /// Pure tooling — not shipped.
    /// </summary>
    internal static class NyCityGeomTool
    {
        const string Root = "Lizard Crossing/Debug/";
        const string StreetPath = "NYCity/root/GLTF_SceneRootNode/Plane.018_0";

        [MenuItem(Root + "Dump NYCity Street Bounds")]
        private static void DumpStreet()
        {
            DumpGroup(StreetPath, "STREET (Plane.018_0)");
            DumpGroup("NYCity/root/GLTF_SceneRootNode/CityGen Buildings_1", "BUILDINGS_1");
            DumpGroup("NYCity/root/GLTF_SceneRootNode/CityGen Buildings.001_2", "BUILDINGS_2");
        }

        private static void DumpGroup(string path, string label)
        {
            var go = GameObject.Find(path);
            if (go == null) { Debug.LogWarning("[NyCityGeom] not found: " + path); return; }

            // collect + sort by world-Z center so the run order is obvious; ONE Debug.Log per
            // renderer (the console reader collapses multi-line logs to their first line).
            var rs = go.GetComponentsInChildren<Renderer>();
            System.Array.Sort(rs, (a, b) => a.bounds.center.z.CompareTo(b.bounds.center.z));
            foreach (var r in rs)
            {
                var b = r.bounds;
                Debug.Log(
                    "[NYGEO] " + label + " " + r.name.PadRight(11) +
                    " zC " + b.center.z.ToString("0.0").PadLeft(7) +
                    " z[" + b.min.z.ToString("0.0") + ".." + b.max.z.ToString("0.0") + "]" +
                    " x[" + b.min.x.ToString("0.0") + ".." + b.max.x.ToString("0.0") + "]" +
                    " y[" + b.min.y.ToString("0.0") + ".." + b.max.y.ToString("0.0") + "]");
            }
        }
    }
}
