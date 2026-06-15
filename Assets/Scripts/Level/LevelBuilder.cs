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

            BuildGround(root, level.Length);
            BuildSlabJointsAndCracks(root, level.Length, rng);
            BuildCitySurfaces(root, level);
            BuildGardenWalls(root, level.Length, rng);
            BuildScatterProps(root, level.Length, rng);
            BuildSafeZoneGarden(root, level.Length, rng);
            BuildBackdrop(root, level.Length);
            SpawnBugs(root, level);

            SafeZoneTrigger.Create(root, level.Length);
            return root;
        }

        // ---------- ground ----------

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
                var mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(1f, 0.95f, 0.5f);
                mat.EnableKeyword("_EMISSION");
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
            var mat = new Material(Shader.Find("Unlit/Texture"));
            mat.mainTexture = TextureLibrary.Backdrop;
            quad.GetComponent<Renderer>().sharedMaterial = mat;
        }

        // ---------- collectibles ----------

        private static void SpawnBugs(Transform root, LevelDefinition level)
        {
            var bugs = new GameObject("Bugs").transform;
            bugs.SetParent(root, false);
            for (int i = 0; i < level.BugPositions.Length; i++)
                CollectibleBug.Spawn(bugs, level.BugPositions[i], "Bug_" + i);

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
