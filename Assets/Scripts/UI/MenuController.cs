using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LizardCrossing
{
    /// <summary>
    /// The meta-game front-end (main menu + lizard shop + cosmetics wardrobe +
    /// daily card). Lives in the Menu scene; PLAY loads the gameplay Boot scene.
    /// All economy actions route through <see cref="MetaProgress"/>, so the UI is
    /// a thin view over the verified backend.
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        private static readonly Color Bg = new Color(0.12f, 0.2f, 0.14f);
        private static readonly Color Card = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color CardSel = new Color(0.4f, 0.85f, 0.45f, 0.22f);
        private static readonly Color Green = new Color(0.35f, 0.72f, 0.32f);
        private static readonly Color Blue = new Color(0.3f, 0.55f, 0.9f);
        private static readonly Color Locked = new Color(0.5f, 0.5f, 0.52f, 0.5f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.2f);
        private static readonly Color Overlay = new Color(0.06f, 0.1f, 0.07f, 0.96f);

        private Transform _root;
        private Text _bugLabel;
        private Text _gemLabel;
        private Text _levelLabel;
        private Image _xpFill;
        private Text _selectedLabel;
        private RectTransform _modal;

        public static MenuController Create()
        {
            var canvas = UIFactory.CreateCanvas("MenuCanvas");
            var mc = canvas.gameObject.AddComponent<MenuController>();
            mc.Build(canvas.transform);
            return mc;
        }

        private void Build(Transform root)
        {
            _root = root;
            UIFactory.CreatePanel(root, "Bg", Bg);   // flat-color fallback

            // premium Canva tropical-garden background (full-bleed)
            var bgSprite = UIFactory.LoadSprite("GeneratedArt/menu_bg");
            if (bgSprite != null)
            {
                var bg = UIFactory.CreateImage(root, "BgImage", bgSprite, Color.white);
                bg.preserveAspect = false;
                Stretch(bg.rectTransform, Vector2.zero, Vector2.one);

                // soft full-screen dim so white text/icons stay legible over the art
                var dim = UIFactory.CreateImage(root, "Dim", ProceduralTextures.WhiteSprite(),
                    new Color(0.05f, 0.08f, 0.06f, 0.22f));
                Stretch(dim.rectTransform, Vector2.zero, Vector2.one);

                // stronger scrim over the lower button cluster for contrast
                var scrim = UIFactory.CreateImage(root, "Scrim", ProceduralTextures.WhiteSprite(),
                    new Color(0.04f, 0.07f, 0.05f, 0.42f));
                Stretch(scrim.rectTransform, Vector2.zero, new Vector2(1f, 0.46f));
            }
            else
            {
                var backdrop = UIFactory.CreateImage(root, "Backdrop", ProceduralTextures.WhiteSprite(),
                    new Color(0.16f, 0.3f, 0.2f, 0.6f));
                UIFactory.SetRect(backdrop, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, 0f), new Vector2(1200f, 700f));
            }

            var logoSprite = UIFactory.LoadSprite("GeneratedArt/logo");
            if (logoSprite != null)
            {
                var logo = UIFactory.CreateImage(root, "Logo", logoSprite, Color.white);
                logo.preserveAspect = true;
                UIFactory.SetRect(logo, new Vector2(0.5f, 0.79f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(620f, 620f));
            }
            else
            {
                var title = UIFactory.CreateText(root, "Title", "LIZARD CROSSING", 92,
                    new Color(0.8f, 1f, 0.55f));
                UIFactory.SetRect(title, new Vector2(0.5f, 0.86f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(1020f, 130f));
            }

            _selectedLabel = UIFactory.CreateText(root, "Selected", "", 46, new Color(1f, 0.95f, 0.8f));
            UIFactory.SetRect(_selectedLabel, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1000f, 70f));

            BuildCurrencyHeader(root);

            // PLAY
            var play = UIFactory.CreateButton(root, "PlayButton", "PLAY", Green, Color.white,
                () => SceneManager.LoadScene("Boot"));
            UIFactory.SetRect(play, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 40f), new Vector2(620f, 200f));
            play.GetComponentInChildren<Text>().fontSize = 84;

            // secondary buttons row
            var lizards = UIFactory.CreateButton(root, "LizardsButton", "LIZARDS", Blue, Color.white, OpenShop);
            UIFactory.SetRect(lizards, new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.5f),
                new Vector2(-330f, 0f), new Vector2(300f, 150f));

            var wardrobe = UIFactory.CreateButton(root, "WardrobeButton", "WARDROBE",
                new Color(0.7f, 0.45f, 0.85f), Color.white, OpenWardrobe);
            UIFactory.SetRect(wardrobe, new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(300f, 150f));

            var daily = UIFactory.CreateButton(root, "DailyButton", "DAILY",
                new Color(0.95f, 0.6f, 0.25f), Color.white, OpenDaily);
            UIFactory.SetRect(daily, new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.5f),
                new Vector2(330f, 0f), new Vector2(300f, 150f));

            var stars = UIFactory.CreateText(root, "Stars",
                "★ " + MetaProgress.TotalStars + "   ·   best run banked", 40,
                new Color(1f, 1f, 1f, 0.7f));
            UIFactory.SetRect(stars, new Vector2(0.5f, 0.16f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 60f));

            RefreshHeader();
        }

        // ---------- currency header ----------

        private void BuildCurrencyHeader(Transform root)
        {
            var bar = UIFactory.CreateCard(root, "CurrencyBar", new Color(0f, 0f, 0f, 0.4f), 0f);
            UIFactory.SetRect(bar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -30f), new Vector2(1000f, 96f));

            _bugLabel = UIFactory.CreateText(bar.transform, "Bugs", "0", 52, Color.white, TextAnchor.MiddleLeft);
            UIFactory.SetRect(_bugLabel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(120f, 0f), new Vector2(300f, 70f));
            var bugIcon = UIFactory.CreateImage(bar.transform, "BugIcon", ProceduralTextures.CircleSprite(),
                new Color(0.4f, 0.8f, 0.3f));
            UIFactory.SetRect(bugIcon, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(80f, 0f), new Vector2(52f, 52f));

            _gemLabel = UIFactory.CreateText(bar.transform, "Gems", "0", 52, Color.white, TextAnchor.MiddleLeft);
            UIFactory.SetRect(_gemLabel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(420f, 0f), new Vector2(220f, 70f));
            var gemIcon = UIFactory.CreateImage(bar.transform, "GemIcon", ProceduralTextures.CircleSprite(),
                new Color(0.5f, 0.7f, 1f));
            UIFactory.SetRect(gemIcon, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(380f, 0f), new Vector2(48f, 48f));

            _levelLabel = UIFactory.CreateText(bar.transform, "Level", "Lv 1", 44, Gold, TextAnchor.MiddleRight);
            UIFactory.SetRect(_levelLabel, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-40f, 18f), new Vector2(260f, 56f));
            var xpBg = UIFactory.CreateImage(bar.transform, "XpBg", ProceduralTextures.WhiteSprite(),
                new Color(0f, 0f, 0f, 0.4f));
            UIFactory.SetRect(xpBg, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-40f, -22f), new Vector2(260f, 18f));
            _xpFill = UIFactory.CreateImage(xpBg.transform, "XpFill", ProceduralTextures.WhiteSprite(), Gold);
            var xr = _xpFill.rectTransform;
            xr.anchorMin = new Vector2(0f, 0f); xr.anchorMax = new Vector2(0f, 1f);
            xr.pivot = new Vector2(0f, 0.5f); xr.anchoredPosition = Vector2.zero;
            xr.sizeDelta = new Vector2(0f, 0f);
        }

        private void RefreshHeader()
        {
            var p = MetaProgress.Profile;
            _bugLabel.text = p.bugs.ToString();
            _gemLabel.text = p.gems.ToString();
            _levelLabel.text = "Lv " + p.PlayerLevel;
            float frac = p.XpToNextLevel > 0 ? Mathf.Clamp01((float)p.XpIntoLevel / p.XpToNextLevel) : 1f;
            var bg = (RectTransform)_xpFill.rectTransform.parent;
            _xpFill.rectTransform.sizeDelta = new Vector2(bg.rect.width * frac, 0f);

            var s = MetaProgress.SelectedLizard;
            _selectedLabel.text = s.Name + "  ·  " + s.AbilityName;
        }

        // ---------- modal scaffold ----------

        private RectTransform OpenModal(string title)
        {
            CloseModal();
            _modal = UIFactory.CreatePanel(_root, "Modal", Overlay);

            var heading = UIFactory.CreateText(_modal, "Heading", title, 88, new Color(0.8f, 1f, 0.55f));
            UIFactory.SetRect(heading, new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 120f));

            var close = UIFactory.CreateButton(_modal, "Close", "✕", new Color(0.8f, 0.3f, 0.3f),
                Color.white, CloseModal);
            UIFactory.SetRect(close, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-50f, -50f), new Vector2(110f, 110f));
            return _modal;
        }

        private void CloseModal()
        {
            if (_modal != null) Destroy(_modal.gameObject);
            _modal = null;
            RefreshHeader();
        }

        // ---------- lizard shop ----------

        private void OpenShop()
        {
            var modal = OpenModal("LIZARDS");
            var all = LizardSpecies.All;
            for (int i = 0; i < all.Length; i++)
                BuildSpeciesCard(modal, all[i], GridPos(i, 2, all.Length, 470f, 360f, 40f, 0.62f));
        }

        private void BuildSpeciesCard(RectTransform parent, LizardSpecies s, Vector2 pos)
        {
            bool unlocked = MetaProgress.IsLizardUnlocked(s.Id);
            bool selected = MetaProgress.Profile.selectedLizardId == s.Id;

            var card = UIFactory.CreateCard(parent, "Card_" + s.Id, selected ? CardSel : Card, 8f);
            UIFactory.SetRect(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(470f, 360f));

            var swatch = UIFactory.CreateImage(card.transform, "Swatch", ProceduralTextures.CircleSprite(), s.BodyColor);
            UIFactory.SetRect(swatch, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -62f), new Vector2(96f, 96f));

            var name = UIFactory.CreateText(card.transform, "Name", s.Name, 48, Color.white);
            UIFactory.SetRect(name, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -130f), new Vector2(440f, 56f));
            var ability = UIFactory.CreateText(card.transform, "Ability", s.AbilityName, 34, new Color(0.7f, 0.95f, 1f));
            UIFactory.SetRect(ability, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -174f), new Vector2(440f, 46f));

            if (selected)
            {
                AddTag(card, "SELECTED", Gold);
            }
            else if (unlocked)
            {
                var b = UIFactory.CreateButton(card.transform, "Select", "SELECT", Green, Color.white, () =>
                {
                    MetaProgress.SelectLizard(s.Id); OpenShop();
                });
                UIFactory.SetRect(b, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 60f), new Vector2(340f, 96f));
            }
            else
            {
                bool affordBugs = MetaProgress.Profile.bugs >= s.UnlockCostBugs;
                float btnY = s.Premium ? 74f : 60f;
                var b = UIFactory.CreateButton(card.transform, "Buy",
                    s.UnlockCostBugs + " bugs", affordBugs ? Green : Locked, Color.white, () =>
                    {
                        if (MetaProgress.TryUnlockLizardWithBugs(s.Id)) { MetaProgress.SelectLizard(s.Id); OpenShop(); }
                    });
                UIFactory.SetRect(b, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, btnY), new Vector2(340f, 88f));
                b.GetComponentInChildren<Text>().fontSize = 40;

                if (s.Premium)
                {
                    var g = UIFactory.CreateText(card.transform, "Gem", "or " + s.CostGems + " gems", 28,
                        new Color(0.6f, 0.8f, 1f));
                    UIFactory.SetRect(g, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        new Vector2(0f, 22f), new Vector2(420f, 38f));
                }
            }
        }

        // ---------- cosmetics wardrobe ----------

        private CosmeticSlot _wardrobeSlot = CosmeticSlot.Hat;

        private void OpenWardrobe()
        {
            var modal = OpenModal("WARDROBE");

            // slot tabs
            var slots = (CosmeticSlot[])System.Enum.GetValues(typeof(CosmeticSlot));
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                bool on = slot == _wardrobeSlot;
                var tab = UIFactory.CreateButton(modal, "Tab_" + slot, SlotLabel(slot),
                    on ? Green : new Color(1f, 1f, 1f, 0.12f), Color.white, () => { _wardrobeSlot = slot; OpenWardrobe(); });
                UIFactory.SetRect(tab, new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.5f),
                    new Vector2((i - (slots.Length - 1) / 2f) * 172f, 0f), new Vector2(165f, 90f));
                tab.GetComponentInChildren<Text>().fontSize = 30;
            }

            // items for the selected slot
            var items = ItemsForSlot(_wardrobeSlot);
            for (int i = 0; i < items.Count; i++)
                BuildCosmeticCard(modal, items[i], GridPos(i, 2, items.Count, 470f, 300f, 40f, 0.56f));
        }

        private void BuildCosmeticCard(RectTransform parent, CosmeticItem c, Vector2 pos)
        {
            bool owned = MetaProgress.OwnsCosmetic(c.Id);
            bool equipped = MetaProgress.Profile.GetEquipped(c.Slot) == c.Id
                            || (MetaProgress.Profile.GetEquipped(c.Slot) == null && c.IsDefault);

            var card = UIFactory.CreateCard(parent, "Cos_" + c.Id, equipped ? CardSel : Card, 8f);
            UIFactory.SetRect(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(470f, 300f));

            var swatch = UIFactory.CreateImage(card.transform, "Swatch", ProceduralTextures.CircleSprite(),
                c.Tint.a > 0f ? c.Tint : new Color(0.6f, 0.6f, 0.6f));
            UIFactory.SetRect(swatch, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -52f), new Vector2(84f, 84f));

            var name = UIFactory.CreateText(card.transform, "Name", c.Name, 42, Color.white);
            UIFactory.SetRect(name, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -116f), new Vector2(440f, 52f));
            if (c.Rarity != Rarity.Common && !c.IsDefault)
            {
                var tag = UIFactory.CreateText(card.transform, "Tag", c.Rarity.ToString().ToUpper(), 30,
                    c.Rarity == Rarity.Epic ? new Color(0.9f, 0.5f, 1f) : new Color(0.5f, 0.8f, 1f));
                UIFactory.SetRect(tag, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -160f), new Vector2(420f, 40f));
            }

            if (equipped)
            {
                var t = UIFactory.CreateText(card.transform, "Eq", "EQUIPPED", 36, Gold);
                UIFactory.SetRect(t, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(420f, 50f));
            }
            else if (owned)
            {
                var b = UIFactory.CreateButton(card.transform, "Equip", "EQUIP", Green, Color.white, () =>
                {
                    MetaProgress.Equip(c.Id); OpenWardrobe();
                });
                UIFactory.SetRect(b, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(320f, 86f));
            }
            else
            {
                bool afford = MetaProgress.Profile.bugs >= c.CostBugs;
                var b = UIFactory.CreateButton(card.transform, "Buy", c.CostBugs + " bugs",
                    afford ? Green : Locked, Color.white, () =>
                    {
                        if (MetaProgress.TryBuyCosmeticWithBugs(c.Id)) { MetaProgress.Equip(c.Id); OpenWardrobe(); }
                    });
                UIFactory.SetRect(b, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(320f, 86f));
                b.GetComponentInChildren<Text>().fontSize = 36;
            }
        }

        // ---------- daily ----------

        private void OpenDaily()
        {
            var modal = OpenModal("DAILY CHALLENGE");
            var daily = DailyChallenge.ForDate(Today());

            var card = UIFactory.CreateCard(modal, "DailyCard", Card, 8f);
            UIFactory.SetRect(card, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 560f));

            var obj = UIFactory.CreateText(card.transform, "Obj", daily.Describe(), 60, Color.white);
            UIFactory.SetRect(obj, new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 120f));

            var mod = UIFactory.CreateText(card.transform, "Mod", "Twist: " + daily.Modifier, 44,
                new Color(0.95f, 0.7f, 0.4f));
            UIFactory.SetRect(mod, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 80f));

            var reward = UIFactory.CreateText(card.transform, "Reward",
                "Reward: " + daily.RewardBugs + " bugs" + (daily.RewardGems > 0 ? " + " + daily.RewardGems + " gems" : ""),
                46, Gold);
            UIFactory.SetRect(reward, new Vector2(0.5f, 0.46f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 80f));

            bool doneToday = MetaProgress.Profile.lastDailyDate == daily.Date;
            var play = UIFactory.CreateButton(card.transform, "PlayDaily", doneToday ? "DONE TODAY" : "PLAY DAILY",
                doneToday ? Locked : Green, Color.white, () =>
                {
                    if (!doneToday) SceneManager.LoadScene("Boot"); // daily modifiers applied next iteration
                });
            UIFactory.SetRect(play, new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 130f));

            var streak = UIFactory.CreateText(card.transform, "Streak", "Streak: " + MetaProgress.Profile.dailyStreak + " days",
                36, new Color(1f, 1f, 1f, 0.7f));
            UIFactory.SetRect(streak, new Vector2(0.5f, 0.07f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 50f));
        }

        // ---------- helpers ----------

        private void AddTag(RectTransform card, string label, Color color)
        {
            var tag = UIFactory.CreateText(card, "Tag", label, 34, color);
            UIFactory.SetRect(tag, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 55f), new Vector2(420f, 50f));
        }

        private void AddTag(Image card, string label, Color color)
        {
            AddTag(card.rectTransform, label, color);
        }

        private static string SlotLabel(CosmeticSlot s)
        {
            switch (s)
            {
                case CosmeticSlot.Hat: return "Hat";
                case CosmeticSlot.Glasses: return "Eyes";
                case CosmeticSlot.Pattern: return "Skin";
                case CosmeticSlot.TailColor: return "Tail";
                case CosmeticSlot.Backpack: return "Pack";
                case CosmeticSlot.Trail: return "Trail";
            }
            return s.ToString();
        }

        private static System.Collections.Generic.List<CosmeticItem> ItemsForSlot(CosmeticSlot slot)
        {
            var list = new System.Collections.Generic.List<CosmeticItem>();
            foreach (var c in CosmeticItem.All)
                if (c.Slot == slot) list.Add(c);
            return list;
        }

        /// <summary>Grid placement around a vertical center fraction of the screen.</summary>
        private static Vector2 GridPos(int i, int cols, int count, float cw, float ch, float gap, float centerFrac)
        {
            int col = i % cols;
            int row = i / cols;
            int rows = Mathf.CeilToInt(count / (float)cols);
            float x = (col - (cols - 1) / 2f) * (cw + gap);
            // y measured from screen center; rows stack downward from a top offset
            float topY = (rows - 1) * (ch + gap) * 0.5f;
            float y = topY - row * (ch + gap) + (centerFrac - 0.5f) * 1920f;
            return new Vector2(x, y);
        }

        private static string Today()
        {
            var n = System.DateTime.Now;
            return n.Year.ToString("D4") + n.Month.ToString("D2") + n.Day.ToString("D2");
        }

        /// <summary>Anchor-stretch a RectTransform to fill a fraction of its parent.</summary>
        private static void Stretch(RectTransform r, Vector2 min, Vector2 max)
        {
            r.anchorMin = min;
            r.anchorMax = max;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }
    }
}
