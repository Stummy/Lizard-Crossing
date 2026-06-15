using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LizardCrossing
{
    /// <summary>
    /// Code-built uGUI helpers (docs/DECISIONS.md D7/D11). Reference resolution
    /// 1080x1920 portrait; everything anchors so editor landscape still works.
    /// Legacy Text now; the TMP swap later only touches this file.
    /// </summary>
    public static class UIFactory
    {
        public static Font DefaultFont
        {
            get { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
        }

        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Loads a sprite from Resources by building it from the underlying
        /// Texture2D, so it works regardless of the asset's sprite import mode
        /// (a Canva/Higgsfield PNG often imports as Sprite "Multiple" with no
        /// sub-sprites, which Resources.Load&lt;Sprite&gt; can't resolve). Cached.
        /// Returns null if the texture is absent.
        /// </summary>
        public static Sprite LoadSprite(string resourcePath)
        {
            Sprite s;
            if (SpriteCache.TryGetValue(resourcePath, out s)) return s;
            var tex = Resources.Load<Texture2D>(resourcePath);
            s = tex != null
                ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f)
                : null;
            SpriteCache[resourcePath] = s;
            return s;
        }

        private static Sprite _rounded;

        /// <summary>A 9-sliced rounded-rectangle sprite: crisp rounded corners at
        /// any button/panel size. Built once and cached.</summary>
        public static Sprite RoundedSprite()
        {
            if (_rounded != null) return _rounded;
            const int s = 48;
            const float r = 16f;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = Mathf.Max(r - (x + 0.5f), (x + 0.5f) - (s - r), 0f);
                float dy = Mathf.Max(r - (y + 0.5f), (y + 0.5f) - (s - r), 0f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                px[y * s + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d + 0.5f));
            }
            tex.SetPixels(px);
            tex.Apply();
            _rounded = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            return _rounded;
        }

        /// <summary>Rounded card/panel background with a soft drop shadow.</summary>
        public static Image CreateCard(Transform parent, string name, Color color, float shadow = 6f)
        {
            var img = CreateImage(parent, name, RoundedSprite(), color);
            img.type = Image.Type.Sliced;
            if (shadow > 0f)
            {
                var sh = img.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0f, 0f, 0f, 0.3f);
                sh.effectDistance = new Vector2(0f, -shadow);
            }
            return img;
        }

        public static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
            return canvas;
        }

        public static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            var img = CreateImage(parent, name, ProceduralTextures.WhiteSprite(), color);
            var rect = img.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            return img;
        }

        public static Text CreateText(Transform parent, string name, string content, int size,
            Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = DefaultFont;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            AddShadow(go);
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label,
            Color bg, Color textColor, System.Action onClick)
        {
            var img = CreateImage(parent, name, RoundedSprite(), bg);
            img.type = Image.Type.Sliced;

            // drop shadow for depth
            var sh = img.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.35f);
            sh.effectDistance = new Vector2(0f, -6f);

            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.fadeDuration = 0.06f;
            cb.pressedColor = new Color(0.82f, 0.82f, 0.82f);
            cb.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
            btn.colors = cb;
            btn.onClick.AddListener(() =>
            {
                GameAudio.Play(Sfx.UiClick);
                if (onClick != null) onClick();
            });

            // subtle top gloss for a premium sheen
            var gloss = CreateImage(img.transform, "Gloss", RoundedSprite(), new Color(1f, 1f, 1f, 0.13f));
            gloss.type = Image.Type.Sliced;
            gloss.raycastTarget = false;
            var glr = gloss.rectTransform;
            glr.anchorMin = new Vector2(0f, 0.52f);
            glr.anchorMax = Vector2.one;
            glr.offsetMin = new Vector2(7f, 0f);
            glr.offsetMax = new Vector2(-7f, -7f);

            var text = CreateText(img.transform, "Label", label, 52, textColor);
            var tr = text.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            return btn;
        }

        public static void SetRect(Component c, Vector2 anchor, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            var rect = c.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
        }

        private static void AddShadow(GameObject go)
        {
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }
    }
}
