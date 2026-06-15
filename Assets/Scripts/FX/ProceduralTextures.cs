using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Runtime-generated fallback textures (docs/DECISIONS.md D7). These are the
    /// zero-asset baseline; TextureLibrary swaps in Higgsfield-generated art
    /// whenever the user has dropped it into Resources/GeneratedArt.
    /// Each texture is created once and cached for the app-domain lifetime.
    /// </summary>
    public static class ProceduralTextures
    {
        private static Texture2D _stoneTiles;
        private static Texture2D _radial;
        private static Texture2D _ring;
        private static Texture2D _circle;
        private static Texture2D _heart;
        private static Texture2D _star;
        private static Texture2D _backdrop;
        private static Texture2D _white;

        /// <summary>Warm stone paving tiles with grout lines. Tiled per sidewalk slab.</summary>
        public static Texture2D StoneTiles
        {
            get
            {
                if (_stoneTiles != null) return _stoneTiles;

                const int s = 256;
                const int tiles = 4;           // 4x4 stones per texture
                const int tileSize = s / tiles;
                var baseColor = new Color(0.74f, 0.69f, 0.61f);
                var grout = new Color(0.5f, 0.46f, 0.41f);
                var rng = new System.Random(9131);

                _stoneTiles = NewTex(s);
                _stoneTiles.wrapMode = TextureWrapMode.Repeat;
                var px = new Color[s * s];

                // per-stone brightness so tiles read as individual stones
                var tileShade = new float[tiles * tiles];
                for (int i = 0; i < tileShade.Length; i++)
                    tileShade[i] = ((float)rng.NextDouble() * 2f - 1f) * 0.06f;

                for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    int tx = x / tileSize, ty = y / tileSize;
                    int lx = x % tileSize, ly = y % tileSize;
                    bool isGrout = lx < 2 || ly < 2;

                    float n = ((float)rng.NextDouble() * 2f - 1f) * 0.035f;
                    if (rng.NextDouble() < 0.012) n -= 0.1f; // grit fleck

                    Color c = isGrout ? grout : baseColor;
                    float shade = isGrout ? 0f : tileShade[ty * tiles + tx];
                    px[y * s + x] = new Color(c.r + n + shade, c.g + n + shade, c.b + n + shade, 1f);
                }
                _stoneTiles.SetPixels(px);
                _stoneTiles.Apply();
                return _stoneTiles;
            }
        }

        /// <summary>Soft radial falloff (white center, transparent edge).</summary>
        public static Texture2D RadialGradient
        {
            get
            {
                if (_radial == null)
                {
                    const int s = 128;
                    _radial = NewTex(s);
                    var px = new Color[s * s];
                    float half = (s - 1) * 0.5f;
                    for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                        float a = Mathf.Clamp01(1f - d);
                        a = a * a * (3f - 2f * a); // smoothstep
                        px[y * s + x] = new Color(1f, 1f, 1f, a);
                    }
                    _radial.SetPixels(px);
                    _radial.Apply();
                }
                return _radial;
            }
        }

        /// <summary>Annulus with soft edges — the red danger ring of WarningMarker.</summary>
        public static Texture2D Ring
        {
            get
            {
                if (_ring == null)
                {
                    const int s = 128;
                    _ring = NewTex(s);
                    var px = new Color[s * s];
                    float half = (s - 1) * 0.5f;
                    for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                        // band between 0.74 and 0.96 with smooth borders
                        float a = Mathf.Clamp01((d - 0.70f) / 0.08f) * Mathf.Clamp01((0.98f - d) / 0.06f);
                        px[y * s + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
                    }
                    _ring.SetPixels(px);
                    _ring.Apply();
                }
                return _ring;
            }
        }

        /// <summary>Hard-edged circle with light AA, for UI sprites.</summary>
        public static Texture2D Circle
        {
            get
            {
                if (_circle == null)
                {
                    const int s = 96;
                    _circle = NewTex(s);
                    var px = new Color[s * s];
                    float half = (s - 1) * 0.5f;
                    for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half));
                        float a = Mathf.Clamp01(half - d + 0.5f);
                        px[y * s + x] = new Color(1f, 1f, 1f, a);
                    }
                    _circle.SetPixels(px);
                    _circle.Apply();
                }
                return _circle;
            }
        }

        /// <summary>
        /// Heart shape for the HUD hearts: a 45°-rotated square (the point) with
        /// two circles (the lobes) — guaranteed-readable construction.
        /// </summary>
        public static Texture2D Heart
        {
            get
            {
                if (_heart == null)
                {
                    const int s = 96;
                    _heart = NewTex(s);
                    var px = new Color[s * s];

                    Vector2 lobeL = new Vector2(0.34f, 0.62f);
                    Vector2 lobeR = new Vector2(0.66f, 0.62f);
                    const float lobeRadius = 0.225f;
                    // diamond sits low: its bottom corner is the heart tip and its
                    // top corner stays below the lobes' cleft
                    Vector2 sqCenter = new Vector2(0.5f, 0.42f);
                    const float sqHalfDiag = 0.34f; // |dx|+|dy| <= this → inside rotated square

                    for (int yi = 0; yi < s; yi++)
                    for (int xi = 0; xi < s; xi++)
                    {
                        var p = new Vector2((xi + 0.5f) / s, (yi + 0.5f) / s);
                        float dist = Mathf.Min(
                            Vector2.Distance(p, lobeL) - lobeRadius,
                            Vector2.Distance(p, lobeR) - lobeRadius,
                            (Mathf.Abs(p.x - sqCenter.x) + Mathf.Abs(p.y - sqCenter.y)) - sqHalfDiag);
                        float a = Mathf.Clamp01(-dist * s * 0.5f); // ~2px soft edge
                        px[yi * s + xi] = new Color(1f, 1f, 1f, a);
                    }
                    _heart.SetPixels(px);
                    _heart.Apply();
                }
                return _heart;
            }
        }

        /// <summary>Five-pointed star for the results screen.</summary>
        public static Texture2D Star
        {
            get
            {
                if (_star == null)
                {
                    const int s = 96;
                    _star = NewTex(s);

                    // 10-vertex star polygon (alternating outer/inner radius)
                    var verts = new Vector2[10];
                    for (int i = 0; i < 10; i++)
                    {
                        float r = (i % 2 == 0) ? 0.48f : 0.20f;
                        float ang = -Mathf.PI / 2f + i * Mathf.PI / 5f;
                        verts[i] = new Vector2(0.5f + Mathf.Cos(ang) * r, 0.5f - Mathf.Sin(ang) * r);
                    }

                    var px = new Color[s * s];
                    for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        var p = new Vector2((x + 0.5f) / s, (y + 0.5f) / s);
                        px[y * s + x] = new Color(1f, 1f, 1f, PointInPolygon(p, verts) ? 1f : 0f);
                    }
                    _star.SetPixels(px);
                    _star.Apply();
                }
                return _star;
            }
        }

        private static bool PointInPolygon(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            }
            return inside;
        }

        /// <summary>
        /// Fallback far-end backdrop: vertical sky→foliage gradient with simple
        /// leaf-silhouette bumps, used when no Higgsfield backdrop is supplied.
        /// </summary>
        public static Texture2D Backdrop
        {
            get
            {
                if (_backdrop == null)
                {
                    const int s = 512;
                    _backdrop = NewTex(s);
                    var sky = new Color(0.76f, 0.88f, 0.93f);
                    var horizon = new Color(0.85f, 0.92f, 0.8f);
                    var foliageNear = new Color(0.25f, 0.5f, 0.28f);
                    var foliageFar = new Color(0.45f, 0.66f, 0.5f);
                    var rng = new System.Random(2718);

                    // two treelines (far + near) smoothed so edges roll like canopy
                    var far = RollingLine(s, rng, 0.42f, 0.58f, 0.05f);
                    var near = RollingLine(s, rng, 0.28f, 0.46f, 0.06f);

                    var px = new Color[s * s];
                    for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        float v = y / (float)(s - 1);
                        Color c = Color.Lerp(horizon, sky, Mathf.Clamp01((v - 0.4f) / 0.6f));
                        // far canopy: soft 2% edge blend
                        float farEdge = Mathf.Clamp01((far[x] - v) / 0.02f);
                        c = Color.Lerp(c, Color.Lerp(foliageFar * 0.9f, foliageFar, v / Mathf.Max(far[x], 0.01f)), farEdge);
                        float nearEdge = Mathf.Clamp01((near[x] - v) / 0.02f);
                        c = Color.Lerp(c, Color.Lerp(foliageNear * 0.75f, foliageNear, v / Mathf.Max(near[x], 0.01f)), nearEdge);
                        px[y * s + x] = c;
                    }
                    _backdrop.SetPixels(px);
                    _backdrop.Apply();
                }
                return _backdrop;
            }
        }

        /// <summary>Random-walk height line smoothed into rolling canopy bumps.</summary>
        private static float[] RollingLine(int size, System.Random rng, float min, float max, float step)
        {
            var line = new float[size];
            float h = (min + max) * 0.5f;
            for (int x = 0; x < size; x++)
            {
                h += ((float)rng.NextDouble() - 0.5f) * step;
                h = Mathf.Clamp(h, min, max);
                line[x] = h;
            }
            // box-blur passes turn the jagged walk into soft mounds
            for (int pass = 0; pass < 4; pass++)
            {
                var smooth = new float[size];
                for (int x = 0; x < size; x++)
                {
                    float sum = 0f;
                    for (int k = -6; k <= 6; k++)
                        sum += line[Mathf.Clamp(x + k, 0, size - 1)];
                    smooth[x] = sum / 13f;
                }
                line = smooth;
            }
            return line;
        }

        public static Texture2D White
        {
            get
            {
                if (_white == null)
                {
                    _white = NewTex(4);
                    var px = new Color[16];
                    for (int i = 0; i < 16; i++) px[i] = Color.white;
                    _white.SetPixels(px);
                    _white.Apply();
                }
                return _white;
            }
        }

        public static Sprite CircleSprite()
        {
            return MakeSprite(Circle);
        }

        public static Sprite WhiteSprite()
        {
            return MakeSprite(White);
        }

        public static Sprite HeartSprite()
        {
            return MakeSprite(Heart);
        }

        public static Sprite StarSprite()
        {
            return MakeSprite(Star);
        }

        private static Sprite MakeSprite(Texture2D t)
        {
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Texture2D NewTex(int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            return t;
        }
    }
}
