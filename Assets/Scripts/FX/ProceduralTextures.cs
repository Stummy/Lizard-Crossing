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
        private static Texture2D _facade;
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

        private static Texture2D _gecko;
        private static Texture2D _flag;

        /// <summary>
        /// Side-on gecko/lizard silhouette for the HUD progress marker — a fat-tailed
        /// little body facing right (+x = run direction), built from a few overlapping
        /// capsules so it reads at ~40px. White; tint when used.
        /// </summary>
        public static Texture2D Gecko
        {
            get
            {
                if (_gecko == null)
                {
                    const int s = 96;
                    _gecko = NewTex(s);
                    var px = new Color[s * s];

                    // body: tapering belly capsule from tail-base (left) to snout (right)
                    Vector2 tailBase = new Vector2(0.30f, 0.46f);
                    Vector2 snout = new Vector2(0.84f, 0.52f);
                    Vector2 headC = new Vector2(0.74f, 0.55f);
                    // tail: thin curling capsule sweeping left and down
                    Vector2 tailMid = new Vector2(0.14f, 0.40f);
                    Vector2 tailTip = new Vector2(0.06f, 0.52f);

                    for (int yi = 0; yi < s; yi++)
                    for (int xi = 0; xi < s; xi++)
                    {
                        var p = new Vector2((xi + 0.5f) / s, (yi + 0.5f) / s);
                        float body = SegDist(p, tailBase, headC) - 0.11f;
                        float head = Vector2.Distance(p, headC) - 0.11f;
                        float nose = SegDist(p, headC, snout) - 0.055f;
                        float tail1 = SegDist(p, tailBase, tailMid) - 0.055f;
                        float tail2 = SegDist(p, tailMid, tailTip) - 0.032f;
                        // four little feet stubs below the belly
                        float feet = Mathf.Min(
                            SegDist(p, new Vector2(0.40f, 0.42f), new Vector2(0.38f, 0.30f)),
                            SegDist(p, new Vector2(0.62f, 0.44f), new Vector2(0.64f, 0.31f))) - 0.035f;
                        float dist = Mathf.Min(Mathf.Min(body, head, nose), Mathf.Min(tail1, tail2, feet));
                        float a = Mathf.Clamp01(-dist * s * 0.5f);
                        px[yi * s + xi] = new Color(1f, 1f, 1f, a);
                    }
                    _gecko.SetPixels(px);
                    _gecko.Apply();
                }
                return _gecko;
            }
        }

        /// <summary>Checkered race-flag for the goal end of the progress bar:
        /// a rounded square with a black/white check pattern. Opaque RGB.</summary>
        public static Texture2D Flag
        {
            get
            {
                if (_flag == null)
                {
                    const int s = 64;
                    const int cells = 4;
                    const float r = 0.16f; // rounded-corner radius (uv)
                    _flag = NewTex(s);
                    var px = new Color[s * s];
                    for (int yi = 0; yi < s; yi++)
                    for (int xi = 0; xi < s; xi++)
                    {
                        float u = (xi + 0.5f) / s, v = (yi + 0.5f) / s;
                        // rounded-rect alpha
                        float dx = Mathf.Max(r - u, u - (1f - r), 0f);
                        float dy = Mathf.Max(r - v, v - (1f - r), 0f);
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01((r - d) * s * 0.5f + 0.5f);
                        a = Mathf.Clamp01(Mathf.Max(a, (u > r && u < 1f - r) || (v > r && v < 1f - r) ? a : a));
                        int cx = Mathf.Clamp((int)(u * cells), 0, cells - 1);
                        int cy = Mathf.Clamp((int)(v * cells), 0, cells - 1);
                        bool dark = ((cx + cy) & 1) == 0;
                        Color c = dark ? new Color(0.1f, 0.1f, 0.12f) : Color.white;
                        px[yi * s + xi] = new Color(c.r, c.g, c.b, a);
                    }
                    _flag.SetPixels(px);
                    _flag.Apply();
                }
                return _flag;
            }
        }

        /// <summary>Distance from point p to segment a-b (uv space).</summary>
        private static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(Vector2.Dot(ab, ab), 1e-6f));
            return Vector2.Distance(p, a + ab * t);
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

        public static Sprite GeckoSprite()
        {
            return MakeSprite(Gecko);
        }

        public static Sprite FlagSprite()
        {
            return MakeSprite(Flag);
        }

        private static Sprite MakeSprite(Texture2D t)
        {
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Tiled concrete-and-glass building facade for the corridor's right wall —
        /// a neutral cool concrete grid of windows (most cool glass, some sky-reflecting, a few
        /// warm-lit for golden hour) so the run wall reads as an NYC building base, not brick.</summary>
        public static Texture2D BuildingFacade
        {
            get
            {
                if (_facade != null) return _facade;

                const int s = 256;
                const int cols = 6, rows = 6;
                const int cw = s / cols, ch = s / rows;
                const int margin = 7;                       // concrete frame around each pane
                var concrete = new Color(0.50f, 0.51f, 0.535f);
                var pier = new Color(0.56f, 0.565f, 0.585f);
                var glass = new Color(0.26f, 0.30f, 0.36f);
                var rng = new System.Random(4242);

                _facade = NewTex(s);
                _facade.wrapMode = TextureWrapMode.Repeat;
                var px = new Color[s * s];

                // per-window tint: mostly cool glass, some sky reflection, a few warm-lit rooms
                var winTint = new Color[cols * rows];
                for (int i = 0; i < winTint.Length; i++)
                {
                    double r = rng.NextDouble();
                    Color w = r < 0.12 ? new Color(0.86f, 0.70f, 0.44f)       // warm-lit room (golden hour)
                            : r < 0.34 ? new Color(0.50f, 0.58f, 0.66f)       // sky reflection
                                       : glass;
                    float j = ((float)rng.NextDouble() * 2f - 1f) * 0.04f;
                    winTint[i] = new Color(w.r + j, w.g + j, w.b + j);
                }

                for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    int cx = x / cw, cy = y / ch;
                    int lx = x % cw, ly = y % ch;
                    float n = ((float)rng.NextDouble() * 2f - 1f) * 0.02f;

                    bool inWindow = lx >= margin && lx < cw - margin && ly >= margin && ly < ch - margin;
                    Color c;
                    if (inWindow)
                    {
                        c = winTint[cy * cols + cx];
                        float u = (lx - margin) / (float)(cw - 2 * margin);
                        float sheen = 0.92f + 0.16f * Mathf.Sin(u * Mathf.PI); // soft glass highlight
                        c = new Color(c.r * sheen, c.g * sheen, c.b * sheen);
                        if (ly < margin + 2) c *= 1.1f;                        // window sill glint
                    }
                    else
                    {
                        c = (lx < margin) ? pier : concrete;                   // vertical piers a touch lighter
                        if (ly >= ch - 3) c = pier * 1.06f;                    // floor-slab band
                    }
                    px[y * s + x] = new Color(c.r + n, c.g + n, c.b + n, 1f);
                }
                _facade.SetPixels(px);
                _facade.Apply();
                return _facade;
            }
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
