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
            BuildStreetProps(root, level);   // generated NYC dressing (hydrant/mailbox/cone/...)
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

        /// <summary>
        /// Real street furniture (imported prefabs) framing the corridor as giant POV landmarks:
        /// street lamps line the curb (left edge), a bus stop / phone booth / bench sit against
        /// the building (right edge). All placed OUTSIDE the lizard's reachable X-band, so they
        /// are scenery — colliders stripped (the lizard can never reach them) — but registered in
        /// <see cref="ObstacleField"/> so the sidewalk crowd flows around the building-side ones.
        /// Each is sat on the ground via its own bounds and the bench (an un-normalized OBJ) is
        /// scaled to a real ~0.85 m height.
        /// </summary>
        private static void BuildEdgeFurniture(Transform parent, LevelDefinition level)
        {
            var holder = new GameObject("EdgeFurniture").transform;
            holder.SetParent(parent, false);
            float L = level.Length;

            var lamp = Resources.Load<GameObject>(FurnFolder + "streetlamp");
            for (float z = 18f; z < L - 6f; z += 22f)               // lamps line the curb
                PlaceFurniture(holder, lamp, new Vector3(4.0f, 0f, z), 0f, 4.5f);

            var bus = Resources.Load<GameObject>(FurnFolder + "busstop");
            var booth = Resources.Load<GameObject>(FurnFolder + "phonebooth");
            var bench = Resources.Load<GameObject>(FurnFolder + "bench/bench");
            const float bx = 11.4f;                                  // building side, just past the band
            PlaceFurniture(holder, bus,   new Vector3(bx, 0f, 38f),  -90f, 2.4f);
            PlaceFurniture(holder, bench, new Vector3(bx, 0f, 70f),  -90f, 0.85f);
            PlaceFurniture(holder, booth, new Vector3(bx, 0f, 100f), -90f, 2.2f);
            if (L > 130f) PlaceFurniture(holder, bench, new Vector3(bx, 0f, 128f), -90f, 0.85f);
            if (L > 150f) PlaceFurniture(holder, bus,   new Vector3(bx, 0f, 158f), -90f, 2.4f);
        }

        private static void PlaceFurniture(Transform parent, GameObject src, Vector3 at, float yaw, float targetHeight)
        {
            if (src == null) return;
            var inst = Object.Instantiate(src, parent);
            // compose a Y-yaw with the prefab's baked orientation (don't clobber the
            // importer's Z-up→Y-up correction, which tips the model on its side)
            inst.transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * inst.transform.rotation;
            foreach (var col in inst.GetComponentsInChildren<Collider>()) Object.Destroy(col); // scenery

            var rends = inst.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) { inst.transform.position = at; return; }
            Bounds b = CombinedBounds(rends);
            if (targetHeight > 0f && b.size.y > 0.001f)             // normalize to a real-world height
            {
                inst.transform.localScale *= targetHeight / b.size.y;
                b = CombinedBounds(inst.GetComponentsInChildren<Renderer>());
            }
            float ground = StreetGround.HeightAt(at.x, at.z);
            inst.transform.position += new Vector3(at.x - b.center.x, ground - b.min.y, at.z - b.center.z);
            ObstacleField.Add(new Vector3(at.x, 0f, at.z), 0.6f);
        }

        private static Bounds CombinedBounds(Renderer[] rends)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b;
        }

        // ---------- generated NYC street dressing (Meshy props) ----------

        const string GenFolder = "Models/Generated/";
        static readonly Dictionary<string, GameObject> _genCache = new Dictionary<string, GameObject>();

        static GameObject LoadGen(string name)
        {
            if (_genCache.TryGetValue(name, out var g)) return g;
            g = Resources.Load<GameObject>(GenFolder + name);
            _genCache[name] = g;
            return g;
        }

        /// <summary>
        /// Scatters the generated NYC props (hydrant, mailbox, newspaper box, traffic cone,
        /// trash bags, A-frame sign, police barricade, cardboard boxes) along the run as set
        /// dressing. SPARSE + tasteful so the playable lane stays fair and oncoming hazards
        /// stay readable:
        ///   • Small SOLID props (hydrant / cone / trash bags) sit OCCASIONALLY just inside the
        ///     run band — the lizard faceplants them (<see cref="PropObstacle"/>) and the crowd
        ///     steers around them (<see cref="ObstacleField"/>), exactly like the rubble piles.
        ///   • Larger props (mailbox / newspaper box / A-frame / barricade / boxes) are pure
        ///     EDGE dressing at the curb (x≈4) or building line (x≈11.4), OUTSIDE the band, with
        ///     colliders STRIPPED (scenery the lizard can never reach) but registered in
        ///     ObstacleField so the sidewalk crowd flows around the building-side ones.
        /// Each is re-normalized to a real-world height via combined bounds and its baked
        /// Z-up→Y-up orientation is preserved (yaw composed, never overwritten). Road bands are
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

            // --- SOLID in-band props (small, occasional). Real-world heights per WO. ---
            // band is the prop-placement band around the centre (kept clear of the curb jog).
            float cx = GameConst.CorridorCenterX;
            float band = GameConst.SidewalkHalfWidth - 0.7f; // ±1.3u around x=9 -> well inside [6,11]
            string[] solidKinds = { "hydrant", "traffic_cone", "trash_bags" };
            float[] solidHeights = { 0.6f, 0.7f, 0.7f };
            float[] solidRadius = { 0.30f, 0.22f, 0.34f }; // contact radius (real-world metres)
            // start at z=20, large stride so they're rare; skip the rubble cadence by phase offset
            for (float z = 20f; z < L - 8f; z += 30f + (float)rng.NextDouble() * 16f)
            {
                if (NearRoad(z)) continue;
                int k = rng.Next(solidKinds.Length);
                float x = cx + ((float)rng.NextDouble() * 2f - 1f) * band;
                float yaw = (float)rng.NextDouble() * 360f;
                PlaceGeneratedProp(holder, LoadGen(solidKinds[k]), new Vector3(x, 0f, z), yaw,
                    solidHeights[k], solidRadius[k], true);
            }

            // --- EDGE dressing (larger props, outside the band, colliders stripped). ---
            // The building facade juts toward the run at ground level and SWALLOWS short props on
            // that side, so ground-level edge dressing lives on the CURB side (x≈4.0), where there
            // is open visible sidewalk before the road. They sit clear of the band's left edge
            // (which is x≥4.5 early, tightening to x≥6 past the curb jog) and face the run (+X).
            const float cxr = 4.0f;  // curb side (lamp side) — open sidewalk
            PlaceGeneratedProp(holder, LoadGen("usps_mailbox"),     new Vector3(cxr, 0f, 30f),  90f, 1.2f, 0.6f, false);
            PlaceGeneratedProp(holder, LoadGen("police_barricade"), new Vector3(cxr, 0f, 44f),  90f, 1.0f, 0.7f, false);
            PlaceGeneratedProp(holder, LoadGen("newspaper_box"),    new Vector3(cxr, 0f, 64f),  90f, 1.2f, 0.6f, false);
            PlaceGeneratedProp(holder, LoadGen("aframe_sign"),      new Vector3(cxr, 0f, 84f),  90f, 0.9f, 0.5f, false);
            PlaceGeneratedProp(holder, LoadGen("cardboard_boxes"),  new Vector3(cxr, 0f, 108f), 90f, 0.8f, 0.7f, false);
            // a couple past the curb jog (z>95): the curb shifts to x≈5, so nudge to x≈4.6 to stay
            // on the narrowed sidewalk edge, still clear of the tightened band (x≥6).
            PlaceGeneratedProp(holder, LoadGen("usps_mailbox"),     new Vector3(4.6f, 0f, 124f), 90f, 1.2f, 0.6f, false);
            if (L > 134f)
                PlaceGeneratedProp(holder, LoadGen("newspaper_box"), new Vector3(4.6f, 0f, 138f), 90f, 1.2f, 0.6f, false);
        }

        static readonly Dictionary<GameObject, Material> _genMatCache = new Dictionary<GameObject, Material>();

        /// <summary>
        /// Instantiates one generated GLB prop: keeps the GLB's own baked glTF materials (the
        /// shader-agnostic glTF-pbrMetallicRoughness, NOT a raw Standard), re-normalizes to
        /// <paramref name="targetHeight"/> via combined bounds, composes <paramref name="yaw"/>
        /// onto the importer's baked Z-up→Y-up rotation (never overwrites it), and sits it on the
        /// ground. <paramref name="solid"/> props get a <see cref="PropObstacle"/>; edge props
        /// have colliders stripped. Both register in <see cref="ObstacleField"/> so the crowd
        /// routes around them.
        /// </summary>
        private static void PlaceGeneratedProp(Transform parent, GameObject src, Vector3 at, float yaw,
            float targetHeight, float radius, bool solid)
        {
            if (src == null) return;
            var inst = Object.Instantiate(src, parent);
            inst.name = src.name;
            // compose yaw with the baked orientation (don't clobber the Z-up→Y-up correction)
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

            if (solid)
            {
                // PropObstacle tests against ITS transform.position, but the GLB root pivot is
                // offset from the visual centre (we centred the bounds, not the pivot). Carry the
                // hit test on a child placed exactly at the footprint centre so the faceplant
                // lines up with the visible prop.
                var hit = new GameObject("PropHit");
                hit.transform.SetParent(inst.transform, false);
                hit.transform.position = new Vector3(at.x, ground, at.z);
                var prop = hit.AddComponent<PropObstacle>();
                prop.Radius = radius;
            }
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

            // grass apron the lizard escapes onto
            Box(garden, new Vector3(0f, 0.025f, length + 9f), new Vector3(30f, 0.05f, 20f), GrassGreen, "Grass");

            // grass tufts
            for (int i = 0; i < 26; i++)
            {
                float x = -12f + (float)rng.NextDouble() * 24f;
                float z = length + 1.5f + (float)rng.NextDouble() * 16f;
                Sphere(garden, new Vector3(x, 0.3f, z),
                    new Vector3(0.5f, 0.55f + (float)rng.NextDouble() * 0.5f, 0.5f),
                    LeafGreens[rng.Next(LeafGreens.Length)], "GrassTuft");
            }

            // foliage arch framing the goal — the readable "go here"
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 5.5f;
                for (float h = 0.8f; h <= 7.5f; h += 1.1f)
                {
                    float bulge = 1.5f + Mathf.Sin(h / 7.5f * Mathf.PI) * 0.7f;
                    Sphere(garden, new Vector3(x + side * 0.3f * Mathf.Sin(h), h, length + 2f),
                        new Vector3(bulge, 1.3f, bulge),
                        LeafGreens[(int)(h * 3f) % LeafGreens.Length], "ArchFoliage");
                }
            }
            for (float x = -4.5f; x <= 4.5f; x += 1.4f)
            {
                float y = 7.8f + Mathf.Cos(x / 5.5f * Mathf.PI * 0.5f) * 1.1f;
                Sphere(garden, new Vector3(x, y, length + 2f),
                    new Vector3(1.8f, 1.4f, 1.8f), LeafGreens[Mathf.Abs((int)(x * 2f)) % LeafGreens.Length], "ArchFoliage");
            }
            // flowers dotted on the arch
            for (int i = 0; i < 10; i++)
            {
                float x = -5.5f + (float)rng.NextDouble() * 11f;
                float y = 1f + (float)rng.NextDouble() * 7.5f;
                if (Mathf.Abs(x) < 4f && y < 7f) continue; // keep the opening clear
                Sphere(garden, new Vector3(x, y, length + 1.6f), Vector3.one * 0.4f,
                    FlowerColors[rng.Next(FlowerColors.Length)], "ArchFlower");
            }

            // welcoming glow on the ground through the arch
            var glow = FlatQuad(garden, new Vector3(0f, 0.03f, length + 2f),
                new Vector3(9f, 6f, 1f), 0f, new Color(0.6f, 1f, 0.5f, 0.35f), "SafeGlow");
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

            // "SAFE ZONE" sign mounted at the arch crown (bright readable goal,
            // per ART_DIRECTION; the opening below stays clear for the run-through)
            BuildSafeSign(garden, new Vector3(0f, 7.4f, length + 1.8f));
        }

        private static void BuildSafeSign(Transform parent, Vector3 pos)
        {
            Box(parent, pos, new Vector3(7.5f, 1.9f, 0.3f), new Color(0.16f, 0.42f, 0.2f), "SafeSignBoard");
            Box(parent, pos + new Vector3(0f, 0f, 0.02f), new Vector3(7.9f, 2.3f, 0.25f),
                new Color(0.55f, 0.38f, 0.22f), "SafeSignFrame");

            // canvas parented to the (unscaled) garden root — NOT the scaled board,
            // which would multiply the text size by the board's scale
            var canvasGo = new GameObject("SafeSignText");
            canvasGo.transform.SetParent(parent, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = canvasGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(750f, 190f);
            rect.localScale = Vector3.one * 0.01f;
            rect.localPosition = pos + new Vector3(0f, 0f, -0.18f);
            rect.localRotation = Quaternion.identity;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(canvasGo.transform, false);
            var text = textGo.AddComponent<UnityEngine.UI.Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = "SAFE ZONE";
            text.fontSize = 110;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.95f, 1f, 0.85f);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
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
