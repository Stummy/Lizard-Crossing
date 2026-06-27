using System.Collections.Generic;
using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Procedurally assembles the Garden Escape environment (packet ART_DIRECTION:
    /// colorful tropical alley, detailed pavement, cracks/leaves/pebbles/gum,
    /// bright readable safe zone). Geometry is code-built this phase; textures
    /// come from TextureLibrary (Higgsfield art with procedural fallbacks).
    /// </summary>
    public static class LevelBuilder
    {
        private static readonly Color WallSandstone = new Color(0.82f, 0.72f, 0.58f);
        private static readonly Color Terracotta = new Color(0.76f, 0.42f, 0.28f);
        private static readonly Color GrassGreen = new Color(0.38f, 0.66f, 0.3f);
        // city surface palette — varied terrain so the run feels like a real city
        // block (sidewalk stone + asphalt streets + concrete curbs + grass) instead
        // of one flat floor. Colors are first-pass; each surface gets a matched
        // Higgsfield texture later.
        private static readonly Color Asphalt = new Color(0.17f, 0.17f, 0.19f);
        private static readonly Color RoadLine = new Color(0.93f, 0.86f, 0.45f);
        private static readonly Color CurbConcrete = new Color(0.72f, 0.69f, 0.64f);
        private static readonly Color[] LeafGreens =
        {
            new Color(0.3f, 0.55f, 0.25f),
            new Color(0.42f, 0.68f, 0.3f),
            new Color(0.24f, 0.48f, 0.26f),
        };
        private static readonly Color[] FlowerColors =
        {
            new Color(0.95f, 0.4f, 0.55f),  // pink
            new Color(1f, 0.75f, 0.25f),    // marigold
            new Color(0.95f, 0.45f, 0.2f),  // orange
            new Color(0.8f, 0.35f, 0.75f),  // violet
        };

        public static Transform Build(LevelDefinition level)
        {
            var root = new GameObject("Level_" + level.Name.Replace(" ", "")).transform;
            var rng = new System.Random(level.Name.GetHashCode());

            // THE REAL NYC CITY (the imported "NYCity" GLB in the scene) is the environment
            // when present — a real avenue with buildings, side streets and cars passing by.
            // The lizard runs forward along the WIDE RIGHT SIDEWALK, confined to it by the
            // corridor X-clamp (its "invisible wall" at the curb), riding the avenue's
            // analytic cross-section (StreetGround). We add only an invisible ground + our
            // colliding props, and skip every procedural scenery piece + the backdrop so the
            // real city is what shows. StreetTraffic (via HazardLaneManager) fills the avenue
            // with the crowd + cars.
            bool nyc = GameObject.Find("NYCity") != null;
            StreetGround.Active = nyc;
            if (nyc)
            {
                BuildInvisibleGround(root, level.Length);
                CityReskin.Apply(GameObject.Find("NYCity")); // re-skin the GLB's baked surfaces with Megascans scans
                BuildStraightCorridor(root, level);          // our OWN straight, walled run surface over the crooked GLB
                BuildStreetCrossings(root, level);           // painted crosswalk + asphalt band at each ROAD lane (the crossings)
            }
            else
            {
                BuildGround(root, level.Length);       // textured pavement walking surface
                if (!CityFacade.Build(root, level))    // kit buildings / roads / crosswalks / props
                {
                    // procedural fallback if neither the GLB nor the kit is present
                    BuildSlabJointsAndCracks(root, level.Length, rng);
                    BuildCitySurfaces(root, level);
                    BuildGardenWalls(root, level.Length, rng);
                    BuildScatterProps(root, level.Length, rng);
                }
            }
            // Solid street furniture: the lizard faceplants them (PropObstacle) AND the crowd
            // steers around them (registered in ObstacleField) — so nothing is walked through.
            BuildSidewalkProps(root, level);
            BuildEdgeFurniture(root, level);
            BuildStreetProps(root, level);   // occasional in-band CityKit solids (cone/hydrant/trash)
            BuildPlants(root, level);
            BuildSafeZoneGarden(root, level.Length, rng);
            if (!nyc) BuildBackdrop(root, level.Length);
            SpawnBugs(root, level);

            SafeZoneTrigger.Create(root, level.Length);
            return root;
        }

        // ---------- ground ----------

        /// <summary>Flat invisible collider at y=0 so the lizard runs on a clean
        /// plane over the real city's streets (whose own meshes have no colliders).</summary>
        private static void BuildInvisibleGround(Transform root, float length)
        {
            var go = new GameObject("GroundCollider");
            go.transform.SetParent(root, false);
            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, -0.1f, length * 0.5f); // top face at y=0 (road level)
            col.size = new Vector3(40f, 0.2f, length + 80f);
            go.layer = StreetGround.GroundLayer; // flat road-level fallback where the city mesh doesn't reach
        }

        // ---------- authored straight corridor (World section, 2026-06-26) ----------
        /// <summary>
        /// The imported NYC street is ONE continuous, collider-less mesh whose sidewalk STEPS right
        /// at the end intersection, so the lizard's straight run band drifts off it (the "sidewalk
        /// shifts / lizard walks through the wall / no floor at the end" cluster). Rather than fight
        /// the crooked GLB, we lay our OWN straight sidewalk strip + REAL collider walls down the run
        /// band and let the GLB be the backdrop behind them. This is the single source of truth for
        /// the run surface; the Foundation-invariant validator asserts the lizard can't leave it.
        /// </summary>
        private static void BuildStraightCorridor(Transform root, LevelDefinition level)
        {
            var corr = new GameObject("StraightCorridor").transform;
            corr.SetParent(root, false);

            float z0 = -6f;
            float z1 = level.Length + 8f;        // run the strip past the goal so the safe-zone approach has floor
            float zc = (z0 + z1) * 0.5f;
            float zlen = z1 - z0;
            float cx = GameConst.CorridorStripCenterX;
            float halfW = GameConst.CorridorStripHalfWidth;

            // 1) STRAIGHT sidewalk surface: a textured plane just above the GLB sidewalk, covering the
            //    band the whole way (it bridges the cross-streets so the run never loses its floor).
            //    No collider — the lizard rides the analytic StreetGround height, which is flat here.
            var strip = GameObject.CreatePrimitive(PrimitiveType.Plane);
            strip.name = "CorridorSidewalk";
            strip.transform.SetParent(corr, false);
            Object.Destroy(strip.GetComponent<Collider>());
            strip.transform.localPosition = new Vector3(cx, GameConst.CorridorStripY, zc);
            strip.transform.localScale = new Vector3((halfW * 2f) / 10f, 1f, zlen / 10f); // Unity plane = 10u at scale 1
            const float stoneTile = 26f;
            // mid-grey concrete tint (NOT near-white) so the sunlit sidewalk doesn't blow out to a
            // white highlight under the golden-hour sun — keep it readable like the concept pavement.
            strip.GetComponent<Renderer>().sharedMaterial = MaterialCache.GetTexturedNormal(
                TextureLibrary.Pavement, TextureLibrary.PavementNormal, new Color(0.70f, 0.68f, 0.64f),
                0.08f, (halfW * 2f) / stoneTile, zlen / stoneTile);

            // 2) RIGHT building-facade wall — a SOLID collider the CharacterController lizard physically
            //    can't pass, opaque + tall so it frames the run and occludes the drifting GLB buildings.
            //    Skinned with the procedural concrete/glass facade (owner: neutral building base, not brick).
            BuildCorridorWall(corr, GameConst.CorridorWallRightX, z0, z1,
                GameConst.CorridorWallHeight, 0.5f,
                ProceduralTextures.BuildingFacade, null, new Color(0.95f, 0.95f, 0.96f), "CorridorWallRight");

            // 3) LEFT railing/curb — a lower SOLID collider so the lizard never drifts onto the road.
            BuildCorridorWall(corr, GameConst.CorridorFenceLeftX, z0, z1,
                GameConst.CorridorFenceHeight, 0.3f,
                null, null, new Color(0.55f, 0.55f, 0.52f), "CorridorCurbLeft");
        }

        /// <summary>One long solid wall/curb box along the run (kept collider → blocks the lizard).
        /// Optional texture tiles down its length; null tex = flat lit colour.</summary>
        private static void BuildCorridorWall(Transform parent, float x, float z0, float z1,
            float height, float thick, Texture2D tex, Texture2D normal, Color color, string name)
        {
            float zc = (z0 + z1) * 0.5f;
            float zlen = z1 - z0;
            var w = Box(parent, new Vector3(x, height * 0.5f, zc), new Vector3(thick, height, zlen), color, name);
            if (tex != null)
            {
                // ~8m of facade per texture tile along the run, one tile up the wall height, so the
                // window grid reads at building scale (windows ~1.3m wide, ~0.7m tall). Slight sheen.
                w.GetComponent<Renderer>().sharedMaterial = MaterialCache.GetTexturedNormal(
                    tex, normal, color, 0.18f, zlen / 8f, Mathf.Max(1f, height / 4f));
            }
        }

        // ---------- Stage 3: crosswalk crossings ----------
        /// <summary>
        /// At each ROAD lane the continuous sidewalk strip is over-painted with a dark asphalt
        /// cross-street band + a white zebra crosswalk, so the ±X car cross-traffic
        /// (StreetTraffic.CarCrossLane) reads as a real intersection the lizard crosses — not
        /// cars driving over the sidewalk. It's a FLAT overlay a few mm above the strip (no
        /// height change, colliders stripped), so the run band stays dead straight and the
        /// Foundation invariant (sidewalk height holds end-to-end) still passes.
        /// </summary>
        private static void BuildStreetCrossings(Transform root, LevelDefinition level)
        {
            if (level.Lanes == null) return;
            var cross = new GameObject("StreetCrossings").transform;
            cross.SetParent(root, false);

            float cx = GameConst.CorridorStripCenterX;            // 8 — strip centre
            float bandW = GameConst.CorridorStripHalfWidth * 2f;  // ~9.2 — span the full strip width
            float y = GameConst.CorridorStripY + 0.004f;          // a hair above the sidewalk strip
            const float roadDepth = 6.5f;                         // z-depth of the cross-street band

            var asphalt = new Color(0.16f, 0.16f, 0.17f);
            var paint = new Color(0.92f, 0.92f, 0.88f);

            foreach (var lane in level.Lanes)
            {
                if (lane.Type != LaneType.Road) continue;
                float z = lane.Z;

                // 1) dark asphalt cross-street band over-printing the sidewalk strip
                Decal(cross, new Vector3(cx, y, z), new Vector3(bandW, 0.02f, roadDepth), asphalt, "Crossing_Asphalt");

                // 2) white zebra: bars spanning the lizard's crossing lane (x), repeated along
                //    +Z so it walks OVER the stripes as it crosses. Bars sit just above the asphalt.
                const int bars = 7;
                float barW = bandW * 0.78f;     // a touch inset from the band edges
                float span = roadDepth - 1.0f;  // leave a front/back margin
                for (int i = 0; i < bars; i++)
                {
                    float t = i / (float)(bars - 1);
                    float bz = z - span * 0.5f + t * span;
                    Decal(cross, new Vector3(cx, y + 0.004f, bz), new Vector3(barW, 0.02f, 0.34f), paint, "Crossing_Stripe");
                }
            }
        }

        /// <summary>A flat painted overlay quad (Box with its collider stripped) — used for the
        /// crosswalk asphalt/stripes so the lizard runs over them without tripping.</summary>
        private static GameObject Decal(Transform parent, Vector3 pos, Vector3 scale, Color color, string name)
        {
            var go = Box(parent, pos, scale, color, name);
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            return go;
        }

        private static void BuildGround(Transform root, float length)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Pavement";
            ground.transform.SetParent(root, false);
            float totalLen = length + 45f;
            ground.transform.position = new Vector3(0f, 0f, totalLen * 0.5f - 15f);
            ground.transform.localScale = new Vector3(3.0f, 1f, totalLen / 10f);

            // real CC0 cobblestone (Poly Haven) + normal map; one texture tile ≈ 26
            // world units so individual stones read several lizard-lengths across.
            const float stoneTile = 26f;
            var mat = MaterialCache.GetTexturedNormal(
                TextureLibrary.Pavement, TextureLibrary.PavementNormal, new Color(0.92f, 0.9f, 0.86f),
                0.12f, 30f / stoneTile, totalLen / stoneTile);
            ground.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void BuildSlabJointsAndCracks(Transform root, float length, System.Random rng)
        {
            var jointColor = new Color(0.35f, 0.32f, 0.28f, 0.85f);
            for (float z = 0f; z < length; z += GameConst.SlabSize)
                FlatQuad(root, new Vector3(0f, 0.012f, z), new Vector3(30f, 0.35f, 1f), 0f, jointColor, "SlabJoint");

            // cracks: trench-like dark seams (CAMERA_AND_SCALE: cracks feel like trenches)
            for (int i = 0; i < 16; i++)
            {
                float z = 8f + (float)rng.NextDouble() * (length - 16f);
                float x = -9f + (float)rng.NextDouble() * 18f;
                float yaw = (float)rng.NextDouble() * 180f;
                float len = 2.5f + (float)rng.NextDouble() * 5f;
                FlatQuad(root, new Vector3(x, 0.011f, z), new Vector3(0.22f, len, 1f), yaw,
                    new Color(0.3f, 0.27f, 0.24f, 0.9f), "Crack");
            }
        }

        // ---------- city surface variety (sidewalk / curb / street / grass) ----------

        /// <summary>
        /// Overlays varied city terrain on the base pavement so the run reads like a
        /// real city block rather than one flat floor: an asphalt STREET (with a
        /// dashed center line and concrete curbs) at every crossing lane — that's the
        /// dangerous road the traffic crosses — and GRASS strips in a few safe pockets
        /// between them. All cosmetic / collider-free so the lizard glides across.
        /// </summary>
        private static void BuildCitySurfaces(Transform root, LevelDefinition level)
        {
            const float corridorWidth = 30f;
            var asphaltTex = TextureLibrary.Asphalt;   // null until art is dropped in
            var grassTex = TextureLibrary.Grass;

            foreach (var lane in level.Lanes)
            {
                float z = lane.Z;
                const float streetHalf = 4f; // street depth along the run (~8 units)

                // asphalt road where the traffic crosses (textured when art present)
                var street = FlatBox(root, new Vector3(0f, 0.02f, z),
                    new Vector3(corridorWidth, 0.04f, streetHalf * 2f), Asphalt, "Street");
                if (asphaltTex != null)
                    street.GetComponent<Renderer>().sharedMaterial = MaterialCache.GetTexturedNormal(
                        asphaltTex, null, Color.white, 0.05f, corridorWidth / 7f, (streetHalf * 2f) / 7f);

                // dashed center line running across the corridor (along x)
                for (float x = -13f; x <= 13f; x += 4.2f)
                    FlatBox(root, new Vector3(x, 0.045f, z),
                        new Vector3(1.8f, 0.04f, 0.45f), RoadLine, "RoadLine");

                // low concrete curbs lipping each edge of the street
                FlatBox(root, new Vector3(0f, 0.08f, z - streetHalf),
                    new Vector3(corridorWidth, 0.16f, 0.6f), CurbConcrete, "Curb");
                FlatBox(root, new Vector3(0f, 0.08f, z + streetHalf),
                    new Vector3(corridorWidth, 0.16f, 0.6f), CurbConcrete, "Curb");
            }

            // grass strips in safe pockets between streets (breaks up the stone)
            float[] grassZ = { 37f, 104f, 150f };
            foreach (float gz in grassZ)
            {
                if (gz > level.Length) continue;
                var grass = FlatBox(root, new Vector3(0f, 0.018f, gz),
                    new Vector3(corridorWidth, 0.05f, 6f), GrassGreen, "GrassStrip");
                if (grassTex != null)
                    grass.GetComponent<Renderer>().sharedMaterial = MaterialCache.GetTexturedNormal(
                        grassTex, null, Color.white, 0.05f, corridorWidth / 6f, 1f);
            }
        }

        /// <summary>Thin, collider-free flat slab used for cosmetic ground surfaces.</summary>
        private static GameObject FlatBox(Transform parent, Vector3 pos, Vector3 scale, Color color, string name)
        {
            var go = Box(parent, pos, scale, color, name);
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        // ---------- colliding sidewalk props (faceplant obstacles) ----------

        // Trash cans placed on the flat pavement inside the lizard's reachable band, off
        // the road bands. Running head-on into one faceplants the lizard (PropObstacle)
        // for the shared tail→heart damage. Roads are read straight from the lane data.
        private static void BuildSidewalkProps(Transform parent, LevelDefinition level)
        {
            var props = new GameObject("SidewalkProps").transform;
            props.SetParent(parent, false);
            ObstacleField.Clear(); // fresh registry each build (avoids stale obstacles across rebuilds)

            var roads = new List<float>();
            foreach (var lane in level.Lanes)
                if (lane.Type == LaneType.Road) roads.Add(lane.Z);

            const float roadHalf = 4f;
            var rng = new System.Random(91);
            float cx = GameConst.CorridorCenterX;
            float band = GameConst.SidewalkHalfWidth - 0.6f;
            for (float z = 14f; z < level.Length - 6f; z += 16f + (float)rng.NextDouble() * 9f)
            {
                bool inRoad = false;
                foreach (float rz in roads) if (Mathf.Abs(z - rz) < roadHalf + 2f) { inRoad = true; break; }
                if (inRoad) continue;
                float x = cx + ((float)rng.NextDouble() * 2f - 1f) * band;
                BuildRubblePile(props, new Vector3(x, 0f, z), 1.0f + (float)rng.NextDouble() * 0.7f, rng);
            }
        }

        const string FurnFolder = "Models/Furniture/";
        const string KitFurnFolder = "Models/CityKit/Furniture/";
        static readonly Dictionary<string, GameObject> _kitCache = new Dictionary<string, GameObject>();

        static GameObject LoadKit(string name)
        {
            if (_kitCache.TryGetValue(name, out var g)) return g;
            g = Resources.Load<GameObject>(KitFurnFolder + name);
            _kitCache[name] = g;
            return g;
        }

        /// <summary>
        /// Dense, cohesive NYC street furniture from the CC0 KayKit "City Builder Bits" set (one
        /// shared atlas, 18–636 tris each) lining BOTH sidewalk edges so the block reads BUSY and
        /// believable — street lamps, traffic lights, hydrants, trash cans, dumpsters, benches,
        /// bushes. The visible OPEN sidewalk is the CURB side (left of the low curb at x=5.8): the
        /// camera looks down the run and sees that foreground, so the dense dressing lives there
        /// (x≈4.0–5.3). The RIGHT side is the tall opaque facade wall (x=11.4); we sit only LOW
        /// items (hydrant/trash/bench) tight against its base (x≈11.3) so they read in front of
        /// the wall without poking over it.
        ///
        /// All edge furniture is OUTSIDE the lizard's run band (x∈[6,11]) → colliders stripped
        /// (pure scenery) but registered in <see cref="ObstacleField"/> so the sidewalk crowd
        /// routes around the building-side ones. Each is normalized to a real-world height via
        /// combined bounds, its baked Z-up→Y-up orientation preserved (yaw composed), sat on the
        /// ground, and skinned with the road/city palette so nothing renders flat-white. Road
        /// bands are skipped so nothing spawns mid-crossing.
        /// </summary>
        private static void BuildEdgeFurniture(Transform parent, LevelDefinition level)
        {
            var holder = new GameObject("EdgeFurniture").transform;
            holder.SetParent(parent, false);
            float L = level.Length;
            var rng = new System.Random(4242);

            var roads = new List<float>();
            foreach (var lane in level.Lanes)
                if (lane.Type == LaneType.Road) roads.Add(lane.Z);
            bool NearRoad(float z) { foreach (float rz in roads) if (Mathf.Abs(z - rz) < 6f) return true; return false; }

            // CURB (left) side — the open visible foreground. A repeating, varied rhythm so the
            // sidewalk reads continuously furnished without two identical props side by side.
            // Tall poles (lamp/light) hug the curb at x≈3.4 (thin footprint, fine near the band);
            // wide ground items (bench/dumpster/trash) sit further left at x≈3.0 so their FULL
            // footprint stays clear of the run-band left edge (x=6) and the curb collider (5.8).
            // (kind, targetHeightMetres, baseX) — baseX tuned per footprint width.
            (string kind, float h, float x)[] curbCycle =
            {
                ("streetlight",    4.6f, 3.6f),
                ("firehydrant",    0.62f, 4.4f),
                ("trash_A",        0.95f, 3.4f),
                ("bench",          0.85f, 2.9f),
                ("trafficlight_A", 3.2f, 3.6f),
                ("bush",           0.7f, 4.4f),
                ("dumpster",       1.35f, 2.6f),
                ("trash_B",        0.8f, 4.6f),
            };
            int ci = 0;
            for (float z = 12f; z < L - 6f; z += 6.5f + (float)rng.NextDouble() * 3.5f)
            {
                if (NearRoad(z)) continue;
                var item = curbCycle[ci % curbCycle.Length];
                ci++;
                float x = item.x + (float)(rng.NextDouble() * 0.5f - 0.25f); // small jitter
                float yaw = 90f + (float)(rng.NextDouble() * 30f - 15f);      // roughly facing the run (+X)
                PlaceCityKit(holder, LoadKit(item.kind), new Vector3(x, 0f, z), yaw, item.h, true);
            }

            // BUILDING (right) side — only NARROW items (hydrant / small trash), set BEHIND the
            // facade-wall line (x≈11.9) so their footprint stays out of the run band (right edge
            // x=11). The wall is opaque, so these are subtle accents glimpsed at gaps; sparse.
            (string kind, float h)[] wallCycle = { ("firehydrant", 0.62f), ("trash_B", 0.8f), ("firehydrant", 0.62f), ("bush", 0.7f) };
            int wi = 0;
            for (float z = 26f; z < L - 8f; z += 20f + (float)rng.NextDouble() * 10f)
            {
                if (NearRoad(z)) continue;
                var item = wallCycle[wi % wallCycle.Length];
                wi++;
                float yaw = -90f + (float)(rng.NextDouble() * 20f - 10f);  // facing the run (−X)
                PlaceCityKit(holder, LoadKit(item.kind), new Vector3(11.9f, 0f, z), yaw, item.h, true);
            }
        }

        /// <summary>
        /// Instantiates one CityKit GLB prop as EDGE scenery: skins it with the cohesive palette
        /// atlas (so it isn't flat-white), strips colliders, re-normalizes to <paramref name="targetHeight"/>
        /// via combined bounds, composes <paramref name="yaw"/> onto the importer's baked Z-up→Y-up
        /// rotation (never overwrites it), sits it on the ground, and (when <paramref name="register"/>)
        /// registers a footprint in <see cref="ObstacleField"/> so the crowd routes around it. Pure
        /// scenery — no <see cref="PropObstacle"/> — because it lives outside the lizard's run band.
        /// </summary>
        private static void PlaceCityKit(Transform parent, GameObject src, Vector3 at, float yaw,
            float targetHeight, bool register)
        {
            if (src == null) return;
            // NOTE: KayKit furniture already imports WITH its citybits atlas bound (verified
            // in-editor) — do NOT re-skin it. Only the Kenney cars/signage need atlas binding.
            var inst = Object.Instantiate(src, parent);
            inst.name = src.name;
            inst.transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * inst.transform.rotation;
            foreach (var col in inst.GetComponentsInChildren<Collider>()) Object.Destroy(col);

            var rends = inst.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) { inst.transform.position = at; return; }
            Bounds b = CombinedBounds(rends);
            if (targetHeight > 0f && b.size.y > 0.001f)
            {
                inst.transform.localScale *= targetHeight / b.size.y;
                b = CombinedBounds(inst.GetComponentsInChildren<Renderer>());
            }
            float ground = StreetGround.HeightAt(at.x, at.z);
            inst.transform.position += new Vector3(at.x - b.center.x, ground - b.min.y, at.z - b.center.z);
            if (register) ObstacleField.Add(new Vector3(at.x, 0f, at.z), 0.5f);
        }

        private static Bounds CombinedBounds(Renderer[] rends)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b;
        }

        // ---------- in-band solid CityKit props (faceplant obstacles) ----------

        /// <summary>
        /// Occasional SOLID in-band CityKit props (orange traffic cone / hydrant / trash can) just
        /// inside the run band: the lizard faceplants them (<see cref="PropObstacle"/>) and the
        /// crowd steers around them (<see cref="ObstacleField"/>), exactly like the rubble piles.
        /// Rare + near the band centre so the lane stays fair and oncoming hazards stay readable.
        /// The dense EDGE dressing lives in <see cref="BuildEdgeFurniture"/>. Road bands are
        /// avoided so nothing spawns mid-crossing.
        /// </summary>
        private static void BuildStreetProps(Transform parent, LevelDefinition level)
        {
            var holder = new GameObject("StreetProps").transform;
            holder.SetParent(parent, false);
            float L = level.Length;
            var rng = new System.Random(307);

            var roads = new List<float>();
            foreach (var lane in level.Lanes)
                if (lane.Type == LaneType.Road) roads.Add(lane.Z);
            bool NearRoad(float z) { foreach (float rz in roads) if (Mathf.Abs(z - rz) < 6f) return true; return false; }

            // --- SOLID in-band props (small, occasional). Cohesive CC0 CityKit pieces: an orange
            // Kenney traffic cone (skinned with the road atlas) and a KayKit hydrant/trash. They
            // sit OCCASIONALLY just inside the run band — the lizard faceplants them
            // (<see cref="PropObstacle"/>) and the crowd steers around them (<see cref="ObstacleField"/>),
            // exactly like the rubble piles. Kept rare + near the band centre so the lane stays fair.
            float cx = GameConst.CorridorCenterX;
            float band = GameConst.SidewalkHalfWidth - 0.7f; // ±1.3u around x=9 -> well inside [6,11]
            // Narrow footprints only, so an in-band prop is a dodgeable obstacle, not a wall that
            // chokes the lane: an orange cone, a fire hydrant, and the SMALL trash can (trash_B).
            // (loader, name, height, contactRadius)
            (System.Func<string, GameObject> load, string name, float h, float r)[] solids =
            {
                (LoadKitSig, "traffic_cone", 0.55f, 0.22f),
                (LoadKit,    "firehydrant",  0.6f,  0.26f),
                (LoadKit,    "trash_B",      0.7f,  0.24f),
            };
            for (float z = 24f; z < L - 8f; z += 30f + (float)rng.NextDouble() * 16f)
            {
                if (NearRoad(z)) continue;
                var s = solids[rng.Next(solids.Length)];
                float x = cx + ((float)rng.NextDouble() * 2f - 1f) * band;
                float yaw = (float)rng.NextDouble() * 360f;
                PlaceKitSolid(holder, s.load(s.name), s.name == "traffic_cone",
                    new Vector3(x, 0f, z), yaw, s.h, s.r);
            }
        }

        static GameObject LoadKitSig(string name)
        {
            if (_kitCache.TryGetValue("sig/" + name, out var g)) return g;
            g = Resources.Load<GameObject>("Models/CityKit/Signage/" + name);
            _kitCache["sig/" + name] = g;
            return g;
        }

        /// <summary>
        /// Instantiates one CityKit GLB as a SOLID in-band prop: skins Kenney signage with the
        /// road atlas (KayKit furniture is already textured), normalizes to <paramref name="targetHeight"/>,
        /// composes <paramref name="yaw"/> onto the baked rotation, sits it on the ground, gives it
        /// a <see cref="PropObstacle"/> hit at the footprint centre, and registers it in
        /// <see cref="ObstacleField"/> so the crowd routes around it.
        /// </summary>
        private static void PlaceKitSolid(Transform parent, GameObject src, bool isSignage, Vector3 at,
            float yaw, float targetHeight, float radius)
        {
            if (src == null) return;
            if (isSignage) CityKitSkin.SkinSignage(src); // cone reads orange
            var inst = Object.Instantiate(src, parent);
            inst.name = src.name;
            inst.transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * inst.transform.rotation;
            foreach (var col in inst.GetComponentsInChildren<Collider>()) Object.Destroy(col);

            var rends = inst.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) { inst.transform.position = at; return; }
            Bounds b = CombinedBounds(rends);
            if (targetHeight > 0f && b.size.y > 0.001f)
            {
                inst.transform.localScale *= targetHeight / b.size.y;
                b = CombinedBounds(inst.GetComponentsInChildren<Renderer>());
            }
            float ground = StreetGround.HeightAt(at.x, at.z);
            inst.transform.position += new Vector3(at.x - b.center.x, ground - b.min.y, at.z - b.center.z);

            // carry the hit test on a child placed exactly at the footprint centre (the GLB root
            // pivot is offset from the centred visual), matching the generated-prop pattern.
            var hit = new GameObject("PropHit");
            hit.transform.SetParent(inst.transform, false);
            hit.transform.position = new Vector3(at.x, ground, at.z);
            var prop = hit.AddComponent<PropObstacle>();
            prop.Radius = radius;
            ObstacleField.Add(new Vector3(at.x, 0f, at.z), radius);
        }

        /// <summary>
        /// Scatters real photoscanned raspberry bushes (Megascans, alpha-cutout leaf material)
        /// as decorative greenery along the sidewalk edges (between the furniture) and clustered
        /// in the safe-zone apron at the end of the run. Decorative only — no colliders, not in
        /// ObstacleField — and sat on the ground, normalized to a real ~0.5 m bush height.
        /// </summary>
        private static void BuildPlants(Transform parent, LevelDefinition level)
        {
            var src = Resources.Load<GameObject>("Models/Furniture/raspberry/raspberry");
            if (src == null) return;
            var mat = Resources.Load<Material>("Models/Furniture/raspberry/raspberry_leaf");
            var holder = new GameObject("Plants").transform;
            holder.SetParent(parent, false);
            var rng = new System.Random(57);
            float L = level.Length;

            for (float z = 26f; z < L - 4f; z += 17f + (float)rng.NextDouble() * 8f) // edge greenery
            {
                float x = (rng.NextDouble() < 0.5) ? 4.4f : 11.2f;
                PlacePlant(holder, src, mat, new Vector3(x, 0f, z + (float)rng.NextDouble() * 4f),
                    rng, 0.45f + (float)rng.NextDouble() * 0.3f);
            }
            for (int i = 0; i < 8; i++)                                              // safe-zone cluster
                PlacePlant(holder, src, mat,
                    new Vector3(5f + (float)rng.NextDouble() * 6f, 0f, L - 2f + (float)rng.NextDouble() * 6f),
                    rng, 0.5f + (float)rng.NextDouble() * 0.4f);
        }

        private static void PlacePlant(Transform parent, GameObject src, Material mat, Vector3 at,
            System.Random rng, float targetHeight)
        {
            var inst = Object.Instantiate(src, parent);
            inst.name = "Raspberry";
            inst.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            foreach (var col in inst.GetComponentsInChildren<Collider>()) Object.Destroy(col);
            var rends = inst.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) { inst.transform.position = at; return; }
            if (mat != null) foreach (var r in rends) r.sharedMaterial = mat;
            Bounds b = CombinedBounds(rends);
            if (b.size.y > 0.001f)
            {
                inst.transform.localScale *= targetHeight / b.size.y;
                b = CombinedBounds(inst.GetComponentsInChildren<Renderer>());
            }
            float ground = StreetGround.HeightAt(at.x, at.z);
            inst.transform.position += new Vector3(at.x - b.center.x, ground - b.min.y, at.z - b.center.z);
        }

        static GameObject _rubbleSrc;
        static Material _rubbleMat;

        /// <summary>
        /// A real photoscanned concrete-rubble chunk (Megascans) as a solid sidewalk obstacle:
        /// the lizard faceplants it (<see cref="PropObstacle"/>) and it is registered in
        /// <see cref="ObstacleField"/> so the crowd can steer around it. One random chunk from
        /// the 6-piece pack is kept per spawn (~9k tris) to stay mobile-light; the rest are
        /// discarded. Centered on <paramref name="basePos"/> and sat on the ground. Replaces the
        /// old primitive-cylinder trash can (kept as a graceful fallback if the asset is absent).
        /// </summary>
        private static void BuildRubblePile(Transform parent, Vector3 basePos, float scale, System.Random rng)
        {
            if (_rubbleSrc == null) _rubbleSrc = Resources.Load<GameObject>("Models/rubble");
            if (_rubbleSrc == null) { BuildTrashCan(parent, basePos, scale); return; }
            if (_rubbleMat == null)
                _rubbleMat = MaterialCache.GetTexturedNormal(
                    Resources.Load<Texture2D>("GeneratedArt/rubble"),
                    Resources.Load<Texture2D>("GeneratedArt/rubble_normal"),
                    Color.white, 0.2f, 1f, 1f);

            var inst = Object.Instantiate(_rubbleSrc, parent);
            inst.name = "RubblePile";
            inst.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            inst.transform.localScale = Vector3.one * scale;

            // keep a single random chunk; discard the rest
            var mfs = inst.GetComponentsInChildren<MeshFilter>();
            Renderer keptR = null;
            if (mfs.Length > 0)
            {
                int keep = rng.Next(mfs.Length);
                keptR = mfs[keep].GetComponent<Renderer>();
                for (int i = 0; i < mfs.Length; i++)
                    if (i != keep) Object.Destroy(mfs[i].gameObject);
            }
            if (keptR != null) keptR.sharedMaterial = _rubbleMat;

            // center the kept chunk on basePos and sit it on the ground (its own bounds stay valid)
            if (keptR != null)
            {
                Vector3 c = keptR.bounds.center;
                inst.transform.position += new Vector3(basePos.x - c.x, basePos.y - keptR.bounds.min.y, basePos.z - c.z);
            }
            else inst.transform.position = basePos;

            // physical: lizard faceplants it; crowd steers around it
            var prop = inst.AddComponent<PropObstacle>();
            prop.Radius = GameConst.PropHitRadius * scale * 1.2f;
            ObstacleField.Add(new Vector3(basePos.x, 0f, basePos.z), prop.Radius);
        }

        private static void BuildTrashCan(Transform parent, Vector3 basePos, float scale)
        {
            var go = new GameObject("TrashCan");
            go.transform.SetParent(parent, false);
            go.transform.position = basePos;

            float h = 0.95f * scale; // ~6× the 0.15u lizard: a real wall to run into
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(body.GetComponent<Collider>());
            body.name = "Can";
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
            body.transform.localScale = new Vector3(0.5f * scale, h * 0.5f, 0.5f * scale);
            body.GetComponent<Renderer>().sharedMaterial = MaterialCache.GetLit(new Color(0.32f, 0.36f, 0.34f));

            var lid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(lid.GetComponent<Collider>());
            lid.name = "Lid";
            lid.transform.SetParent(go.transform, false);
            lid.transform.localPosition = new Vector3(0f, h + 0.02f, 0f);
            lid.transform.localScale = new Vector3(0.58f * scale, 0.05f, 0.58f * scale);
            lid.GetComponent<Renderer>().sharedMaterial = MaterialCache.GetLit(new Color(0.22f, 0.25f, 0.24f));

            var prop = go.AddComponent<PropObstacle>();
            prop.Radius = GameConst.PropHitRadius * scale;
        }

        // ---------- flanking gardens ----------

        private static void BuildGardenWalls(Transform root, float length, System.Random rng)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float wallX = side * 12.5f;
                var wall = Box(root, new Vector3(wallX, 2.2f, length * 0.5f - 5f),
                    new Vector3(2.5f, 4.4f, length + 30f), WallSandstone, "GardenWall");
                // real CC0 plaster (Poly Haven) when present; tiles along the run
                if (TextureLibrary.Wall != null)
                {
                    var wallMat = MaterialCache.GetTexturedNormal(
                        TextureLibrary.Wall, TextureLibrary.WallNormal, WallSandstone,
                        0.1f, (length + 30f) / 9f, 0.9f);
                    wall.GetComponent<Renderer>().sharedMaterial = wallMat;
                }
                // cap stones
                Box(root, new Vector3(wallX, 4.55f, length * 0.5f - 5f),
                    new Vector3(2.9f, 0.35f, length + 30f), Color.Lerp(WallSandstone, Color.white, 0.25f), "WallCap");

                // ivy patches climbing the wall
                int ivyCount = (int)(length / 18f);
                for (int i = 0; i < ivyCount; i++)
                {
                    float z = (float)rng.NextDouble() * length;
                    float h = 1f + (float)rng.NextDouble() * 3f;
                    Sphere(root, new Vector3(wallX - side * 1.3f, h, z),
                        new Vector3(0.6f, 1.6f + (float)rng.NextDouble(), 2.2f),
                        LeafGreens[i % LeafGreens.Length], "Ivy");
                }

                // potted tropical plants every ~16 units, just inside the wall
                for (float z = 6f; z < length - 6f; z += 14f + (float)rng.NextDouble() * 6f)
                {
                    BuildPottedPlant(root, new Vector3(side * 10.6f, 0f, z), rng);
                }
            }
        }

        private static void BuildPottedPlant(Transform root, Vector3 pos, System.Random rng)
        {
            var plant = new GameObject("PottedPlant").transform;
            plant.SetParent(root, false);
            plant.position = pos;

            // terracotta pot (towers over the lizard)
            float potH = 2.6f + (float)rng.NextDouble() * 1.2f;
            var pot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(pot.GetComponent<Collider>());
            pot.name = "Pot";
            pot.transform.SetParent(plant, false);
            pot.transform.localPosition = new Vector3(0f, potH * 0.5f, 0f);
            pot.transform.localScale = new Vector3(2.4f, potH * 0.5f, 2.4f);
            var potTex = TextureLibrary.Terracotta;
            pot.GetComponent<Renderer>().sharedMaterial = potTex != null
                ? MaterialCache.GetLitTextured(potTex, Color.white)
                : MaterialCache.GetLit(Terracotta);
            // rim
            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(rim.GetComponent<Collider>());
            rim.transform.SetParent(plant, false);
            rim.transform.localPosition = new Vector3(0f, potH, 0f);
            rim.transform.localScale = new Vector3(2.7f, 0.25f, 2.7f);
            rim.GetComponent<Renderer>().sharedMaterial = MaterialCache.GetLit(Color.Lerp(Terracotta, Color.white, 0.2f));

            // fan of giant leaves
            int leaves = 5 + rng.Next(4);
            for (int i = 0; i < leaves; i++)
            {
                float yaw = i * (360f / leaves) + (float)rng.NextDouble() * 20f;
                float tilt = 35f + (float)rng.NextDouble() * 25f;
                var leaf = Sphere(plant, Vector3.zero,
                    new Vector3(1.1f, 0.12f, 3.4f + (float)rng.NextDouble() * 1.5f),
                    LeafGreens[rng.Next(LeafGreens.Length)], "Leaf");
                leaf.transform.localRotation = Quaternion.Euler(-tilt, yaw, 0f);
                leaf.transform.localPosition = new Vector3(0f, potH + 0.6f, 0f)
                    + leaf.transform.forward * 1.6f;
            }

            // a few bright flowers on stems
            int flowers = 1 + rng.Next(3);
            for (int i = 0; i < flowers; i++)
            {
                float yaw = (float)rng.NextDouble() * 360f;
                float r = 0.5f + (float)rng.NextDouble() * 0.7f;
                Vector3 offset = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * r;
                float stemH = 1.4f + (float)rng.NextDouble() * 0.9f;
                var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.Destroy(stem.GetComponent<Collider>());
                stem.transform.SetParent(plant, false);
                stem.transform.localPosition = new Vector3(offset.x, potH + stemH * 0.5f, offset.z);
                stem.transform.localScale = new Vector3(0.08f, stemH * 0.5f, 0.08f);
                stem.GetComponent<Renderer>().sharedMaterial = MaterialCache.GetLit(LeafGreens[2]);
                Sphere(plant, new Vector3(offset.x, potH + stemH + 0.15f, offset.z),
                    Vector3.one * (0.45f + (float)rng.NextDouble() * 0.2f),
                    FlowerColors[rng.Next(FlowerColors.Length)], "Flower");
            }
        }

        // ---------- debris / readability props ----------

        private static void BuildScatterProps(Transform root, float length, System.Random rng)
        {
            // fallen leaves
            var leafTex = TextureLibrary.LeafDecal;
            for (int i = 0; i < 40; i++)
            {
                float z = (float)rng.NextDouble() * (length + 10f) - 5f;
                float x = -10f + (float)rng.NextDouble() * 20f;
                Color c = Color.Lerp(LeafGreens[rng.Next(LeafGreens.Length)],
                    new Color(0.85f, 0.6f, 0.2f), (float)rng.NextDouble() * 0.7f);
                if (leafTex != null)
                {
                    FlatTexturedQuad(root, new Vector3(x, 0.015f, z),
                        new Vector3(0.9f, 1.1f, 1f), (float)rng.NextDouble() * 360f, leafTex, c, "FallenLeaf");
                }
                else
                {
                    var leaf = Sphere(root, new Vector3(x, 0.04f, z),
                        new Vector3(0.55f, 0.05f, 0.8f), c, "FallenLeaf");
                    leaf.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                }
            }

            // pebbles
            for (int i = 0; i < 28; i++)
            {
                float z = (float)rng.NextDouble() * length;
                float x = -10.5f + (float)rng.NextDouble() * 21f;
                float s = 0.12f + (float)rng.NextDouble() * 0.3f;
                Sphere(root, new Vector3(x, s * 0.4f, z), new Vector3(s, s * 0.7f, s),
                    new Color(0.6f, 0.58f, 0.55f), "Pebble");
            }

            // old gum spots
            for (int i = 0; i < 9; i++)
            {
                float z = (float)rng.NextDouble() * length;
                float x = -9f + (float)rng.NextDouble() * 18f;
                var gum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.Destroy(gum.GetComponent<Collider>());
                gum.name = "Gum";
                gum.transform.SetParent(root, false);
                gum.transform.localPosition = new Vector3(x, 0.02f, z);
                float s = 0.7f + (float)rng.NextDouble() * 0.8f;
                gum.transform.localScale = new Vector3(s, 0.02f, s);
                gum.GetComponent<Renderer>().sharedMaterial =
                    MaterialCache.GetLit(new Color(0.45f, 0.42f, 0.4f));
            }

            // crumbs near the later lanes (patio vibes)
            for (int i = 0; i < 14; i++)
            {
                float z = length * 0.5f + (float)rng.NextDouble() * length * 0.45f;
                float x = -8f + (float)rng.NextDouble() * 16f;
                float s = 0.08f + (float)rng.NextDouble() * 0.14f;
                Sphere(root, new Vector3(x, s * 0.5f, z), Vector3.one * s,
                    new Color(0.85f, 0.72f, 0.45f), "Crumb");
            }
        }

        // ---------- safe zone ----------

        private static void BuildSafeZoneGarden(Transform root, float length, System.Random rng)
        {
            var garden = new GameObject("SafeZoneGarden").transform;
            garden.SetParent(root, false);
            // Centre the whole goal on the run line (the lizard runs the NYC sidewalk band at
            // x=CorridorCenterX≈9, NOT x=0) so the gate/sign/arch sit DEAD AHEAD as a beacon
            // rather than off to the left. All children below are authored relative to this root.
            garden.localPosition = new Vector3(GameConst.CorridorCenterX, 0f, 0f);

            // --- CENTRAL PARK arrival (Stage 5): a big grass expanse the lizard escapes ONTO,
            //     planted with real CC0 (Kenney) trees + bushes so it reads as a lush park the
            //     run bursts into — replacing the old primitive flower-arch. The central path
            //     corridor (|x|<~4) stays clear so the glowing goal reads dead-ahead. ---
            var parkGround = Box(garden, new Vector3(0f, 0.025f, length + 22f), new Vector3(64f, 0.05f, 54f), GrassGreen, "ParkGrass");
            if (TextureLibrary.Grass != null) // real tiled turf, not a flat green plane (~4m tiles across the park)
                parkGround.GetComponent<Renderer>().sharedMaterial = MaterialCache.GetTexturedNormal(
                    TextureLibrary.Grass, null, new Color(0.80f, 0.86f, 0.72f), 0.05f, 16f, 13f);

            // a pale dirt path leading straight in through the gate (the "enter here" lane)
            FlatQuad(garden, new Vector3(0f, 0.052f, length + 16f), new Vector3(6f, 30f, 1f), 0f,
                new Color(0.62f, 0.54f, 0.40f, 1f), "ParkPath");

            // a parkland: a back tree-line for depth + clumps flanking the path, varied species/size.
            string[] parkTrees = { "tree_oak", "tree_default", "tree_detailed", "tree_fat", "tree_oak", "tree_pineRoundA" };
            for (int i = 0; i < 26; i++)
            {
                float x = -30f + (float)rng.NextDouble() * 60f;
                float z = length + 6f + (float)rng.NextDouble() * 40f;
                if (Mathf.Abs(x) < 4.5f && z < length + 13f) continue; // keep the gate mouth clear
                float h = 4.5f + (float)rng.NextDouble() * 4.5f;
                PlaceNature(garden, parkTrees[rng.Next(parkTrees.Length)],
                    new Vector3(x, 0.05f, z), h, (float)rng.NextDouble() * 360f);
            }
            // two statement trees framing the entrance corners
            PlaceNature(garden, "tree_oak", new Vector3(-7.5f, 0.05f, length + 4f), 7.5f, 40f);
            PlaceNature(garden, "tree_default", new Vector3(7.5f, 0.05f, length + 4f), 7f, 205f);

            // a DENSE back tree-line so the park reads deep + lush and hides the GLB building edge
            for (float x = -30f; x <= 30f; x += 3.0f)
            {
                float h = 6.5f + (float)rng.NextDouble() * 4f;
                PlaceNature(garden, parkTrees[rng.Next(parkTrees.Length)],
                    new Vector3(x + (float)(rng.NextDouble() * 1.8 - 0.9), 0.05f, length + 42f + (float)rng.NextDouble() * 4f),
                    h, (float)rng.NextDouble() * 360f);
            }

            // bushes as undergrowth along the front edge + under the trees
            string[] parkBushes = { "plant_bushLarge", "plant_bushDetailed" };
            for (int i = 0; i < 18; i++)
            {
                float x = -28f + (float)rng.NextDouble() * 56f;
                float z = length + 4f + (float)rng.NextDouble() * 40f;
                if (Mathf.Abs(x) < 3.5f && z < length + 13f) continue;
                PlaceNature(garden, parkBushes[rng.Next(parkBushes.Length)],
                    new Vector3(x, 0.05f, z), 0.6f + (float)rng.NextDouble() * 0.7f, (float)rng.NextDouble() * 360f);
            }
            // real flower clusters (CC0 Kenney) for colour at ground level
            string[] parkFlowers = { "flower_redA", "flower_yellowA", "flower_purpleA" };
            for (int i = 0; i < 22; i++)
            {
                float x = -26f + (float)rng.NextDouble() * 52f;
                float z = length + 5f + (float)rng.NextDouble() * 38f;
                if (Mathf.Abs(x) < 3f) continue;
                PlaceNature(garden, parkFlowers[rng.Next(parkFlowers.Length)],
                    new Vector3(x, 0.05f, z), 0.35f + (float)rng.NextDouble() * 0.25f, (float)rng.NextDouble() * 360f);
            }
            // tufts of real grass for lush ground cover
            string[] parkGrass = { "grass", "grass_large" };
            for (int i = 0; i < 30; i++)
            {
                float x = -28f + (float)rng.NextDouble() * 56f;
                float z = length + 4f + (float)rng.NextDouble() * 40f;
                if (Mathf.Abs(x) < 2.8f) continue;
                PlaceNature(garden, parkGrass[rng.Next(parkGrass.Length)],
                    new Vector3(x, 0.05f, z), 0.3f + (float)rng.NextDouble() * 0.3f, (float)rng.NextDouble() * 360f);
            }

            // welcoming glow on the ground through the arch — warm amber/gold so the gate reads as
            // the glowing GOLD beacon of the run concept (the green is the park foliage above).
            var glow = FlatQuad(garden, new Vector3(0f, 0.03f, length + 2f),
                new Vector3(9f, 6f, 1f), 0f, new Color(1f, 0.82f, 0.46f, 0.42f), "SafeGlow");
            glow.GetComponent<Renderer>().material.mainTexture = ProceduralTextures.RadialGradient;

            // fireflies drifting in the goal mouth
            for (int i = 0; i < 7; i++)
            {
                var fly = Sphere(garden, new Vector3(-4f + (float)rng.NextDouble() * 8f,
                    1f + (float)rng.NextDouble() * 5f, length + 1f + (float)rng.NextDouble() * 4f),
                    Vector3.one * 0.16f, new Color(1f, 0.95f, 0.5f), "Firefly");
                var mat = new Material(MaterialCache.LitShaderAsset); // URP/Lit or Standard
                mat.color = new Color(1f, 0.95f, 0.5f);
                mat.EnableKeyword("_EMISSION"); // _EMISSION + _EmissionColor exist on both
                mat.SetColor("_EmissionColor", new Color(1f, 0.9f, 0.3f) * 1.6f);
                fly.GetComponent<Renderer>().material = mat;
                var bob = fly.AddComponent<Bobber>();
                bob.Amplitude = 0.4f + (float)rng.NextDouble() * 0.5f;
                bob.Speed = 0.6f + (float)rng.NextDouble() * 0.8f;
            }

            // The readable "GOAL HERE" gate: emissive finish-gate spanning the corridor with a
            // glowing SAFE ZONE sign crowned where the LOW game camera frames it, plus a checkered
            // ground finish line — so the goal reads as a bright beacon from far down the run
            // (S2-2; the old single dark sign at y7.4 sat off the top of frame and didn't read).
            BuildGoalGate(garden, length);
        }

        /// <summary>Instantiate a CC0 Kenney nature GLB (Resources/Models/Nature/&lt;model&gt;),
        /// uniformly scaled so its height ≈ <paramref name="targetHeight"/> metres, sat on the
        /// ground at <paramref name="pos"/>, yawed, colliders stripped. No-op (null) if missing.</summary>
        private static GameObject PlaceNature(Transform parent, string model, Vector3 pos, float targetHeight, float yawDeg)
        {
            var src = Resources.Load<GameObject>("Models/Nature/" + model);
            if (src == null) return null;
            var go = Object.Instantiate(src, parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yawDeg, 0f);
            go.transform.localScale = Vector3.one;

            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                float s = targetHeight / Mathf.Max(0.01f, b.size.y);
                go.transform.localScale = Vector3.one * s;
                // re-measure after scaling and sit the base on the ground (Kenney pivots vary)
                b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                go.transform.localPosition += new Vector3(0f, pos.y - b.min.y, 0f);
            }
            foreach (var col in go.GetComponentsInChildren<Collider>()) Object.Destroy(col);
            return go;
        }

        /// <summary>
        /// Emissive finish gate at the safe zone (S2-2). Two glowing posts + crossbar frame the
        /// corridor; a bright emissive SAFE ZONE sign is crowned at ~y4.7 (centred in the low POV
        /// frame, not clipped off the top); a checkered band marks the finish line on the ground.
        /// Everything emits so it blooms and punches through the haze/DoF as a distant beacon.
        /// </summary>
        private static void BuildGoalGate(Transform parent, float length)
        {
            float z = length + 1.8f;
            Color amber = new Color(1f, 0.84f, 0.5f);     // warm glowing gate metal
            Color signGlow = new Color(1f, 0.8f, 0.42f);  // warm gold halo (was green) — match the run concept's amber beacon

            // glowing gate posts flanking the corridor, with brighter lamp caps
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 6f;
                EmissiveBox(parent, new Vector3(x, 3.25f, z), new Vector3(0.5f, 6.5f, 0.5f), amber, 2.2f, "GatePost");
                var cap = Sphere(parent, new Vector3(x, 6.7f, z), Vector3.one * 0.9f, amber, "GateCap");
                cap.GetComponent<Renderer>().sharedMaterial = EmissiveMat(amber, 3.0f);
            }
            // crossbar joining the posts above the sign
            EmissiveBox(parent, new Vector3(0f, 6.4f, z), new Vector3(12.5f, 0.5f, 0.5f), amber, 2.0f, "GateBar");

            // emissive SAFE ZONE sign, crowned where the low camera frames it. A glowing green halo
            // behind a dark board so the bright text reads with contrast (board itself is NOT emissive).
            Vector3 signPos = new Vector3(0f, 4.7f, z);
            EmissiveBox(parent, signPos + new Vector3(0f, 0f, 0.06f), new Vector3(9.2f, 2.7f, 0.2f),
                signGlow, 2.2f, "SignGlow");
            var board = Box(parent, signPos, new Vector3(8.5f, 2.1f, 0.3f), new Color(0.12f, 0.11f, 0.13f), "SafeSignBoard");
            Object.Destroy(board.GetComponent<Collider>());
            BuildSignText(parent, signPos + new Vector3(0f, 0f, -0.2f), "SAFE ZONE");

            // checkered finish line across the corridor at the goal Z — classic "you made it" read
            const int cells = 14;
            const float bandW = 18f;
            float cellW = bandW / cells;
            for (int i = 0; i < cells; i++)
                for (int row = 0; row < 2; row++)
                {
                    bool white = ((i + row) & 1) == 0;
                    float x = -bandW * 0.5f + (i + 0.5f) * cellW;
                    float zz = length + (row == 0 ? -0.4f : 0.4f);
                    FlatQuad(parent, new Vector3(x, 0.04f, zz), new Vector3(cellW, 0.8f, 1f), 0f,
                        white ? new Color(0.95f, 0.95f, 0.95f) : new Color(0.07f, 0.07f, 0.07f), "FinishCell");
                }
        }

        private static void BuildSignText(Transform parent, Vector3 pos, string msg)
        {
            // canvas parented to the (unscaled) garden root — NOT a scaled board,
            // which would multiply the text size by the board's scale
            var canvasGo = new GameObject("SafeSignText");
            canvasGo.transform.SetParent(parent, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = canvasGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(820f, 210f);
            rect.localScale = Vector3.one * 0.01f;
            rect.localPosition = pos;
            rect.localRotation = Quaternion.identity;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(canvasGo.transform, false);
            var text = textGo.AddComponent<UnityEngine.UI.Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = msg;
            text.fontSize = 120;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.96f, 1f, 0.86f);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        /// <summary>An unlit-bright emissive material on the project's Lit shader (URP/Lit or
        /// Standard fallback) — bright enough (intensity > bloom threshold) to glow as a beacon.</summary>
        private static Material EmissiveMat(Color c, float intensity)
        {
            var m = new Material(MaterialCache.LitShaderAsset);
            m.color = c;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * intensity);
            return m;
        }

        private static GameObject EmissiveBox(Transform parent, Vector3 pos, Vector3 scale, Color c, float intensity, string name)
        {
            var go = Box(parent, pos, scale, c, name);
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = EmissiveMat(c, intensity);
            return go;
        }

        // ---------- backdrop ----------

        private static void BuildBackdrop(Transform root, float length)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(quad.GetComponent<Collider>());
            quad.name = "Backdrop";
            quad.transform.SetParent(root, false);
            quad.transform.position = new Vector3(0f, 26f, length + 38f);
            quad.transform.localScale = new Vector3(170f, 80f, 1f);
            var mat = MaterialCache.GetUnlitTextured(TextureLibrary.Backdrop);
            quad.GetComponent<Renderer>().sharedMaterial = mat;
        }

        // ---------- collectibles ----------

        private static void SpawnBugs(Transform root, LevelDefinition level)
        {
            var bugs = new GameObject("Bugs").transform;
            bugs.SetParent(root, false);
            float band = GameConst.SidewalkHalfWidth - 0.3f; // keep pickups inside the run band
            for (int i = 0; i < level.BugPositions.Length; i++)
            {
                Vector3 bp = level.BugPositions[i];
                bp.x = Mathf.Clamp(bp.x, GameConst.CorridorCenterX - band, GameConst.CorridorCenterX + band);
                bp.y = StreetGround.HeightAt(bp.x, bp.z);
                CollectibleBug.Spawn(bugs, bp, "Bug_" + i);
            }

            if (GameStateManager.Instance != null)
                GameStateManager.Instance.BugsTotal = level.BugPositions.Length;
        }

        // ---------- primitive helpers ----------

        private static GameObject Box(Transform parent, Vector3 pos, Vector3 scale, Color color, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = MaterialCache.GetLit(color);
            return go;
        }

        private static GameObject Sphere(Transform parent, Vector3 pos, Vector3 scale, Color color, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = MaterialCache.GetLit(color);
            return go;
        }

        /// <summary>Ground-decal quad lying flat; scale = (width, length, 1).</summary>
        private static GameObject FlatQuad(Transform parent, Vector3 pos, Vector3 scale, float yawDeg, Color color, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(90f, yawDeg, 0f);
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Sprites/Default"));
            r.material.color = color;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        private static GameObject FlatTexturedQuad(Transform parent, Vector3 pos, Vector3 scale, float yawDeg,
            Texture2D tex, Color tint, string name)
        {
            var go = FlatQuad(parent, pos, scale, yawDeg, tint, name);
            go.GetComponent<Renderer>().material.mainTexture = tex;
            return go;
        }

        /// <summary>Tiny helper for idle floating motion (fireflies).</summary>
        public class Bobber : MonoBehaviour
        {
            public float Amplitude = 0.4f;
            public float Speed = 1f;
            private Vector3 _origin;
            private float _phase;

            private void Start()
            {
                _origin = transform.position;
                _phase = Random.value * 10f;
            }

            private void Update()
            {
                float t = Time.time * Speed + _phase;
                transform.position = _origin + new Vector3(
                    Mathf.Sin(t * 0.7f) * Amplitude,
                    Mathf.Sin(t) * Amplitude * 0.6f,
                    Mathf.Cos(t * 0.5f) * Amplitude * 0.5f);
            }
        }
    }
}
