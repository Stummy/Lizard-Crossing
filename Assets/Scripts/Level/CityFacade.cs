using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Assembles the run as a straight city street from the Downtown City kit's
    /// PRE-MADE blocks (copied into Resources/CityKit): real multi-storey buildings
    /// line both sides, a kit road with a crosswalk sits at every crossing lane, and
    /// street props (bollards, manholes, planters) dress the sidewalk. The lizard
    /// hops along the sidewalk and crosses the roads where the giants stride.
    /// Native kit scale already towers over the lizard, so no scaling. Returns false
    /// (procedural garden fallback) if the kit isn't present.
    /// </summary>
    public static class CityFacade
    {
        const float CorridorHalf = 9f;     // matches GameConst.CorridorHalfWidth
        const float BuildingInnerX = 9.8f; // inner face of the buildings sits just outside the corridor
        const string Kit = "CityKit/";

        static readonly string[] Buildings =
        {
            Kit + "Building_Medium_2_001", Kit + "Building_Small_1",
            Kit + "Building_Large_2",      Kit + "Building_Small_1",
            Kit + "Building_Medium_2_001", Kit + "Building_Large_2",
        };

        public static bool Build(Transform root, LevelDefinition level)
        {
            if (Resources.Load<GameObject>(Kit + "Building_Small_1") == null) return false;

            var parent = new GameObject("CityBlocks").transform;
            parent.SetParent(root, false);
            float length = level.Length;

            BuildRoads(parent, level);
            BuildBuildings(parent, length);
            BuildStreetProps(parent, level);

            StaticBatchingUtility.Combine(parent.gameObject);
            return true;
        }

        // ---- roads + crosswalks at the ROAD lanes only ----
        static void BuildRoads(Transform parent, LevelDefinition level)
        {
            foreach (var lane in level.Lanes)
            {
                if (lane.Type != LaneType.Road) continue; // sidewalk/alley lanes stay open pavement
                // Street_4Lane is 6 deep x 18 wide; rotate 90 so it spans the corridor (x)
                // with a 6-deep road (z) for the lizard to cross.
                Place(parent, Kit + "Street_4Lane", new Vector3(0f, 0.005f, lane.Z), 90f, AlignXZ);
                var cw = Place(parent, Kit + "Decal_Crosswalk", new Vector3(0f, 0.06f, lane.Z), 0f, AlignXZ);
                if (cw != null) cw.transform.localScale = new Vector3(2.6f, 1f, 1f); // stripe across the corridor
            }
        }

        // ---- pre-made buildings lining both sides ----
        static void BuildBuildings(Transform parent, float length)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float yaw = side > 0 ? -90f : 90f; // front faces the corridor (inward)
                int i = 0;
                for (float z = -8f; z < length + 16f; i++)
                {
                    string res = Buildings[(i + (side > 0 ? 0 : 3)) % Buildings.Length];
                    var go = Place(parent, res, new Vector3(0f, 0f, z), yaw, AlignBuildingInner, side);
                    if (go == null) { z += 14f; continue; }

                    Bounds b = WorldBounds(go);
                    // recentre on the running slot now that we know its depth, then advance
                    go.transform.position += new Vector3(0f, 0f, (z + b.size.z * 0.5f) - b.center.z);
                    z += b.size.z + 1.5f;
                }
            }
        }

        // ---- sidewalk dressing ----
        static void BuildStreetProps(Transform parent, LevelDefinition level)
        {
            float length = level.Length;

            // bollards line both curbs, skipping the road crossings
            for (float z = 4f; z < length; z += 7f)
            {
                if (OnRoad(level, z)) continue;
                Place(parent, Kit + "Prop_Bollard", new Vector3(-CorridorHalf + 0.4f, 0f, z), 0f, AlignXZ);
                Place(parent, Kit + "Prop_Bollard", new Vector3(CorridorHalf - 0.4f, 0f, z), 0f, AlignXZ);
            }

            // planters tucked against the buildings on safe stretches
            var rng = new System.Random(7);
            for (float z = 10f; z < length; z += 17f)
            {
                if (OnRoad(level, z)) continue;
                int s = rng.Next(2) == 0 ? -1 : 1;
                Place(parent, Kit + "Prop_Planter_Single", new Vector3(s * (CorridorHalf - 1.5f), 0f, z), 0f, AlignXZ);
            }

            // manholes / drains on the roads
            foreach (var lane in level.Lanes)
            {
                if (lane.Type != LaneType.Road) continue;
                Place(parent, Kit + "Prop_ManholeCover", new Vector3(-3f, 0.07f, lane.Z), 0f, AlignXZ);
                Place(parent, Kit + "Prop_Drain", new Vector3(CorridorHalf - 0.5f, 0.05f, lane.Z + 2.5f), 0f, AlignXZ);
            }
        }

        static bool OnRoad(LevelDefinition level, float z)
        {
            foreach (var lane in level.Lanes)
                if (lane.Type == LaneType.Road && Mathf.Abs(z - lane.Z) < 4f) return true;
            return false;
        }

        // ---- placement helpers ----
        enum Align { XZ, BuildingInner }
        const Align AlignXZ = Align.XZ;
        const Align AlignBuildingInner = Align.BuildingInner;

        static GameObject Place(Transform parent, string res, Vector3 basePos, float yaw, Align align, int side = 1)
        {
            var pf = Resources.Load<GameObject>(res);
            if (pf == null) return null;
            var go = Object.Instantiate(pf, parent);
            go.transform.localScale = Vector3.one;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            Bounds b = WorldBounds(go);
            Vector3 delta;
            if (align == Align.BuildingInner)
            {
                float innerEdge = side > 0 ? b.min.x : b.max.x;          // edge nearest the corridor
                float targetInner = side * BuildingInnerX;
                delta = new Vector3(targetInner - innerEdge, -b.min.y, basePos.z - b.center.z);
            }
            else
            {
                delta = new Vector3(basePos.x - b.center.x, basePos.y - b.min.y, basePos.z - b.center.z);
            }
            go.transform.position += delta;

            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            return go;
        }

        static Bounds WorldBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = new Bounds(go.transform.position, Vector3.zero);
            bool has = false;
            foreach (var r in rs) { if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds); }
            return b;
        }
    }
}
