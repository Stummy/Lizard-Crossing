using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LizardCrossing
{
    /// <summary>
    /// In-run HUD + flow screens (packet required system: SimpleHUDController).
    /// Hearts, bug counter, progress bar, dash button with cooldown fill,
    /// close-call popups, and the start / death / win panels.
    /// </summary>
    public class SimpleHUDController : MonoBehaviour
    {
        private static readonly Color HeartRed = new Color(0.95f, 0.25f, 0.3f);
        private static readonly Color HeartDim = new Color(0.25f, 0.22f, 0.24f, 0.6f);
        private static readonly Color TailGreen = new Color(0.5f, 0.85f, 0.42f);
        private static readonly Color TailDim = new Color(0.25f, 0.24f, 0.22f, 0.6f);
        private static readonly Color StarGold = new Color(1f, 0.8f, 0.15f);
        private static readonly Color StarDim = new Color(0.3f, 0.3f, 0.32f, 0.8f);
        private static readonly Color PanelDim = new Color(0.05f, 0.08f, 0.05f, 0.78f);
        private static readonly Color ButtonGreen = new Color(0.35f, 0.7f, 0.3f);

        private Image[] _hearts;
        private Image _tailPip;
        private Text _bugText;
        private Image _progressFill;
        private Image _geckoMarker;
        private Text _levelText;
        private Image _dashFill;
        private Image _dangerFill;
        private GameObject _dangerGroup; // CAT meter, hidden until the cat is provoked
        private Text _dangerLabel;
        private Text _message;
        private Text _popup;
        private RectTransform _startPanel;
        private RectTransform _deathPanel;
        private DeathCause _lastDeathCause = DeathCause.Unknown;
        private RectTransform _winPanel;
        private Text _rewardText;
        private float _popupUntil;

        public static SimpleHUDController Create()
        {
            var canvas = UIFactory.CreateCanvas("HUD");
            var hud = canvas.gameObject.AddComponent<SimpleHUDController>();
            hud.Build(canvas.transform);
            return hud;
        }

        private void Build(Transform root)
        {
            // Everything that hugs a screen edge lives inside a safe-area inset so the
            // notch / rounded corners / home indicator never clip the HUD.
            var safe = UIFactory.CreateSafeArea(root);

            // ----- hearts (top-left) — count includes the lizard's heart bonus -----
            int maxHearts = GameStateManager.Instance != null
                ? GameStateManager.Instance.MaxHearts : GameConst.MaxHearts;
            _hearts = new Image[maxHearts];
            var heartSprite = ProceduralTextures.HeartSprite();
            for (int i = 0; i < _hearts.Length; i++)
            {
                // a dim "socket" behind each heart so an empty (lost) life still reads as a slot
                var socket = UIFactory.CreateImage(safe, "HeartSocket" + i, heartSprite,
                    new Color(0f, 0f, 0f, 0.32f));
                UIFactory.SetRect(socket, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                    new Vector2(72f + i * 92f, -68f), new Vector2(92f, 92f));
                socket.raycastTarget = false;

                var heart = UIFactory.CreateImage(safe, "Heart" + i, heartSprite, HeartRed);
                UIFactory.SetRect(heart, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                    new Vector2(72f + i * 92f, -68f), new Vector2(80f, 80f));
                heart.raycastTarget = false;
                // crisp dark outline so the red hearts pop over the bright world
                var ho = heart.gameObject.AddComponent<UnityEngine.UI.Outline>();
                ho.effectColor = new Color(0f, 0f, 0f, 0.55f);
                ho.effectDistance = new Vector2(2.5f, -2.5f);
                _hearts[i] = heart;
            }

            // ----- tail pip (just right of the hearts): the "free hit" buffer. Lit while
            //       the lizard still has its tail; dims when it's been dropped. -----
            _tailPip = UIFactory.CreateImage(safe, "TailPip", ProceduralTextures.CircleSprite(), TailGreen);
            UIFactory.SetRect(_tailPip, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(72f + _hearts.Length * 92f + 6f, -68f), new Vector2(46f, 46f));
            _tailPip.raycastTarget = false;
            RefreshTail(GameStateManager.Instance == null || GameStateManager.Instance.HasTail);

            // ----- bug counter (top-right): little fly icon (body + wings) + "n / total" -----
            var bugIcon = UIFactory.CreateImage(safe, "BugIcon", ProceduralTextures.CircleSprite(),
                new Color(0.32f, 0.22f, 0.14f));
            UIFactory.SetRect(bugIcon, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(-150f, -66f), new Vector2(50f, 60f));
            bugIcon.raycastTarget = false;
            var bo = bugIcon.gameObject.AddComponent<UnityEngine.UI.Outline>();
            bo.effectColor = new Color(0f, 0f, 0f, 0.45f);
            bo.effectDistance = new Vector2(2f, -2f);
            var wingL = UIFactory.CreateImage(bugIcon.transform, "WingL", ProceduralTextures.CircleSprite(),
                new Color(0.85f, 0.95f, 1f, 0.85f));
            UIFactory.SetRect(wingL, new Vector2(0.2f, 0.85f), new Vector2(0.5f, 0.5f),
                new Vector2(-8f, 6f), new Vector2(34f, 44f));
            wingL.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 30f);
            var wingR = UIFactory.CreateImage(bugIcon.transform, "WingR", ProceduralTextures.CircleSprite(),
                new Color(0.85f, 0.95f, 1f, 0.85f));
            UIFactory.SetRect(wingR, new Vector2(0.8f, 0.85f), new Vector2(0.5f, 0.5f),
                new Vector2(8f, 6f), new Vector2(34f, 44f));
            wingR.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -30f);
            _bugText = UIFactory.CreateText(safe, "BugCount", "0 / 0", 50, Color.white, TextAnchor.MiddleRight);
            UIFactory.SetRect(_bugText, new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                new Vector2(-188f, -66f), new Vector2(220f, 70f));
            AddTextOutline(_bugText);

            // ----- top-center: level title + rounded progress bar (gecko marker + goal flag) + "LEVEL n" -----
            string levelTitle = LevelTitle();
            var titleText = UIFactory.CreateText(safe, "LevelTitle", levelTitle, 46,
                new Color(1f, 0.97f, 0.86f), TextAnchor.MiddleCenter);
            UIFactory.SetRect(titleText, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -56f), new Vector2(560f, 60f));
            AddTextOutline(titleText);

            const float barW = 560f, barH = 34f;
            var barBg = UIFactory.CreateImage(safe, "ProgressBg", UIFactory.RoundedSprite(),
                new Color(0f, 0f, 0f, 0.45f));
            barBg.type = Image.Type.Sliced;
            UIFactory.SetRect(barBg, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -114f), new Vector2(barW, barH));
            var barShadow = barBg.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            barShadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            barShadow.effectDistance = new Vector2(0f, -4f);

            _progressFill = UIFactory.CreateImage(barBg.transform, "ProgressFill",
                UIFactory.RoundedSprite(), new Color(0.42f, 0.9f, 0.38f));
            _progressFill.type = Image.Type.Sliced;
            var fillRect = _progressFill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = new Vector2(4f, 0f);
            fillRect.sizeDelta = new Vector2(0f, -8f);

            // checkered goal flag pinned to the right (finish) end of the bar
            var flag = UIFactory.CreateImage(barBg.transform, "GoalFlag", ProceduralTextures.FlagSprite(),
                Color.white);
            UIFactory.SetRect(flag, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(2f, 0f), new Vector2(46f, 46f));
            flag.raycastTarget = false;
            var fo = flag.gameObject.AddComponent<UnityEngine.UI.Outline>();
            fo.effectColor = new Color(0f, 0f, 0f, 0.5f);
            fo.effectDistance = new Vector2(2f, -2f);

            // gecko marker that rides along the fill at the current progress; it perches
            // ABOVE the bar (with a little pin) so it never disappears into the green fill.
            _geckoMarker = UIFactory.CreateImage(barBg.transform, "GeckoMarker",
                ProceduralTextures.GeckoSprite(), new Color(0.62f, 1f, 0.42f));
            _geckoMarker.preserveAspect = true;
            var gm0 = _geckoMarker.rectTransform;
            gm0.anchorMin = new Vector2(0f, 0.5f);
            gm0.anchorMax = new Vector2(0f, 0.5f);
            gm0.pivot = new Vector2(0.5f, 0f); // pivot at the gecko's feet so it stands on the bar
            gm0.sizeDelta = new Vector2(66f, 66f);
            gm0.anchoredPosition = new Vector2(0f, 6f);
            _geckoMarker.raycastTarget = false;
            var go2 = _geckoMarker.gameObject.AddComponent<UnityEngine.UI.Outline>();
            go2.effectColor = new Color(0f, 0.12f, 0f, 0.85f);
            go2.effectDistance = new Vector2(2.5f, -2.5f);

            int lvl = LevelNumber();
            _levelText = UIFactory.CreateText(safe, "LevelNum", "LEVEL " + lvl, 30,
                new Color(1f, 1f, 1f, 0.92f), TextAnchor.MiddleCenter);
            UIFactory.SetRect(_levelText, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -150f), new Vector2(400f, 40f));
            AddTextOutline(_levelText);

            // ----- steer LEFT / RIGHT hold-buttons (bottom corners): the only directional
            //       control now that the lizard auto-runs forward. Hold to keep swaying. -----
            BuildSteerButton(root, "SteerLeft", "◀", -1f, true);
            BuildSteerButton(root, "SteerRight", "▶", +1f, false);

            // ----- dash button (bottom-center, between the steer buttons) -----
            var dashBtnImg = UIFactory.CreateImage(root, "DashButton", ProceduralTextures.CircleSprite(),
                new Color(1f, 1f, 1f, 0.25f));
            UIFactory.SetRect(dashBtnImg, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 70f), new Vector2(190f, 190f));
            var dashBtn = dashBtnImg.gameObject.AddComponent<Button>();
            dashBtn.targetGraphic = dashBtnImg;
            dashBtn.onClick.AddListener(InputProvider.PressDash);
            _dashFill = UIFactory.CreateImage(dashBtnImg.transform, "DashFill",
                ProceduralTextures.CircleSprite(), new Color(0.45f, 0.85f, 0.4f, 0.85f));
            var dashFillRect = _dashFill.rectTransform;
            dashFillRect.anchorMin = Vector2.zero;
            dashFillRect.anchorMax = Vector2.one;
            dashFillRect.offsetMin = new Vector2(14f, 14f);
            dashFillRect.offsetMax = new Vector2(-14f, -14f);
            _dashFill.type = Image.Type.Filled;
            _dashFill.fillMethod = Image.FillMethod.Radial360;
            var dashLabel = UIFactory.CreateText(dashBtnImg.transform, "Label", "DASH", 46,
                new Color(0.1f, 0.2f, 0.1f));
            UIFactory.SetRect(dashLabel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(200f, 60f));

            // ----- POV toggle (top-right, under the bug counter): swap nose-cam / follow cam.
            //       Moved off the bottom-left so the LEFT steer button owns that corner. -----
            var povImg = UIFactory.CreateImage(root, "PovButton", ProceduralTextures.CircleSprite(),
                new Color(1f, 1f, 1f, 0.22f));
            UIFactory.SetRect(povImg, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-90f, -150f), new Vector2(120f, 120f));
            var povBtn = povImg.gameObject.AddComponent<Button>();
            povBtn.targetGraphic = povImg;
            povBtn.onClick.AddListener(() =>
            {
                if (LizardCameraController.Instance != null) LizardCameraController.Instance.ToggleView();
            });
            var povLabel = UIFactory.CreateText(povImg.transform, "Label", "POV", 34,
                new Color(0.95f, 1f, 0.95f));
            UIFactory.SetRect(povLabel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(140f, 50f));

            // ----- predator danger meter (left edge): fills upward as the alley cat
            //       closes in from behind, reading Predator.Threat01 -----
            var dangerBg = UIFactory.CreateImage(root, "DangerBg", UIFactory.RoundedSprite(),
                new Color(0f, 0f, 0f, 0.5f));
            dangerBg.type = Image.Type.Sliced;
            UIFactory.SetRect(dangerBg, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(36f, 0f), new Vector2(26f, 360f));
            _dangerFill = UIFactory.CreateImage(dangerBg.transform, "DangerFill",
                UIFactory.RoundedSprite(), new Color(0.95f, 0.55f, 0.2f));
            _dangerFill.type = Image.Type.Sliced;
            var dangerRect = _dangerFill.rectTransform;
            dangerRect.anchorMin = new Vector2(0f, 0f);
            dangerRect.anchorMax = new Vector2(1f, 0f);
            dangerRect.pivot = new Vector2(0.5f, 0f);
            dangerRect.anchoredPosition = new Vector2(0f, 3f);
            dangerRect.sizeDelta = new Vector2(-6f, 0f);
            var dangerLabel = UIFactory.CreateText(root, "DangerLabel", "CAT", 30,
                new Color(1f, 0.72f, 0.66f));
            UIFactory.SetRect(dangerLabel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(49f, -208f), new Vector2(110f, 42f));
            // the cat doesn't exist until provoked, so the meter stays hidden until then
            _dangerGroup = dangerBg.gameObject;
            _dangerLabel = dangerLabel;
            _dangerGroup.SetActive(false);
            _dangerLabel.gameObject.SetActive(false);

            // ----- center message + popup -----
            _message = UIFactory.CreateText(root, "Message", "", 72, Color.white);
            UIFactory.SetRect(_message, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 120f));
            _popup = UIFactory.CreateText(root, "Popup", "", 64, new Color(1f, 0.55f, 0.2f));
            UIFactory.SetRect(_popup, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 100f));
            _popup.gameObject.SetActive(false);

            BuildStartPanel(root);
            _deathPanel = null;
            _winPanel = null;

            GameEvents.RunStarted += OnRunStarted;
            GameEvents.PlayerHit += OnPlayerHit;
            GameEvents.PlayerTailLost += OnTailLost;
            GameEvents.PlayerTailRegrown += OnTailRegrown;
            GameEvents.PlayerDied += OnPlayerDied;
            GameEvents.PlayerRevived += OnRevived;
            GameEvents.RunWon += OnRunWon;
            GameEvents.BugCollected += OnBugCollected;
            GameEvents.NearMiss += OnNearMiss;
            GameEvents.CatProvoked += OnCatProvoked;

            RefreshBugs(0, GameStateManager.Instance != null ? GameStateManager.Instance.BugsTotal : 0);
        }

        private void OnDestroy()
        {
            GameEvents.RunStarted -= OnRunStarted;
            GameEvents.PlayerHit -= OnPlayerHit;
            GameEvents.PlayerTailLost -= OnTailLost;
            GameEvents.PlayerTailRegrown -= OnTailRegrown;
            GameEvents.PlayerDied -= OnPlayerDied;
            GameEvents.PlayerRevived -= OnRevived;
            GameEvents.RunWon -= OnRunWon;
            GameEvents.BugCollected -= OnBugCollected;
            GameEvents.NearMiss -= OnNearMiss;
            GameEvents.CatProvoked -= OnCatProvoked;
        }

        // ---------- panels ----------

        private void BuildStartPanel(Transform root)
        {
            _startPanel = UIFactory.CreatePanel(root, "StartPanel", new Color(0f, 0f, 0f, 0.25f));

            var logoSprite = UIFactory.LoadSprite("GeneratedArt/logo");
            if (logoSprite != null)
            {
                var logo = UIFactory.CreateImage(_startPanel, "Logo", logoSprite, Color.white);
                logo.preserveAspect = true;
                UIFactory.SetRect(logo, new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(600f, 600f));
            }
            else
            {
                var title = UIFactory.CreateText(_startPanel, "Title", "LIZARD CROSSING", 110,
                    new Color(0.75f, 1f, 0.55f));
                UIFactory.SetRect(title, new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(1000f, 140f));
            }

            var sub = UIFactory.CreateText(_startPanel, "Subtitle", "Level 3 — Garden Escape", 56,
                new Color(1f, 0.95f, 0.8f));
            UIFactory.SetRect(sub, new Vector2(0.5f, 0.73f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 80f));

            var hint = UIFactory.CreateText(_startPanel, "Hint", "TAP TO GO", 80, Color.white);
            UIFactory.SetRect(hint, new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 110f));
            hint.gameObject.AddComponent<PulseText>();

            var controls = UIFactory.CreateText(_startPanel, "Controls",
                "you auto-run  ·  hold ◀ / ▶ to dodge  ·  DASH to burst", 40, new Color(1f, 1f, 1f, 0.85f));
            UIFactory.SetRect(controls, new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(950f, 70f));
        }

        private void ShowDeathPanel()
        {
            var gm = GameStateManager.Instance;
            _deathPanel = UIFactory.CreatePanel(transform, "DeathPanel", PanelDim);

            var title = UIFactory.CreateText(_deathPanel, "Title", DeathTitle(_lastDeathCause), 120,
                new Color(1f, 0.45f, 0.35f));
            UIFactory.SetRect(title, new Vector2(0.5f, 0.74f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 150f));

            var stats = UIFactory.CreateText(_deathPanel, "Stats",
                string.Format("bugs {0}/{1}", gm != null ? gm.BugsCollected : 0, gm != null ? gm.BugsTotal : 0),
                52, Color.white);
            UIFactory.SetRect(stats, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 80f));

            float y = 0.48f;
            // rewarded revive (opt-in, once per run)
            if (gm != null && gm.CanRevive)
            {
                var revive = UIFactory.CreateButton(_deathPanel, "ReviveButton", "REVIVE  ▶",
                    new Color(0.95f, 0.62f, 0.2f), Color.white, () =>
                    {
                        if (GameStateManager.Instance != null) GameStateManager.Instance.RequestRevive();
                    });
                UIFactory.SetRect(revive, new Vector2(0.5f, y), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(540f, 140f));
                var adtag = UIFactory.CreateText(_deathPanel, "AdTag", "watch a short ad", 32,
                    new Color(1f, 1f, 1f, 0.7f));
                UIFactory.SetRect(adtag, new Vector2(0.5f, y - 0.065f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(600f, 44f));
                y -= 0.17f;
            }

            var retry = UIFactory.CreateButton(_deathPanel, "RetryButton", "TRY AGAIN",
                ButtonGreen, Color.white, () =>
                {
                    if (GameStateManager.Instance != null) GameStateManager.Instance.Restart();
                });
            UIFactory.SetRect(retry, new Vector2(0.5f, y), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(460f, 130f));

            var home = UIFactory.CreateButton(_deathPanel, "HomeButton", "HOME",
                new Color(0.32f, 0.42f, 0.52f), Color.white, GoHome);
            UIFactory.SetRect(home, new Vector2(0.5f, y - 0.14f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(320f, 110f));
        }

        private void OnRevived()
        {
            if (_deathPanel != null) { Destroy(_deathPanel.gameObject); _deathPanel = null; }
            RefreshHearts(GameStateManager.Instance != null ? GameStateManager.Instance.Hearts : 1);
            ShowPopup("REVIVED!", new Color(0.5f, 1f, 0.6f));
        }

        private void GoHome()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Menu");
        }

        private void ShowWinPanel(RunResults r)
        {
            _winPanel = UIFactory.CreatePanel(transform, "WinPanel", PanelDim);

            var card = UIFactory.CreateCard(_winPanel, "ResultsCard", new Color(0.09f, 0.13f, 0.09f, 0.95f), 12f);
            UIFactory.SetRect(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -70f), new Vector2(900f, 1520f));

            var title = UIFactory.CreateText(_winPanel, "Title", "SAFE!", 130,
                new Color(0.7f, 1f, 0.5f));
            UIFactory.SetRect(title, new Vector2(0.5f, 0.74f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 160f));

            var starSprite = ProceduralTextures.StarSprite();
            for (int i = 0; i < 3; i++)
            {
                var star = UIFactory.CreateImage(_winPanel, "Star" + i, starSprite,
                    i < r.Stars ? StarGold : StarDim);
                UIFactory.SetRect(star, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f),
                    new Vector2((i - 1) * 190f, 0f), new Vector2(i == 1 ? 185f : 150f, i == 1 ? 185f : 150f));
                StartCoroutine(PopIn(star.rectTransform, 0.25f + i * 0.22f));
            }

            string stats = string.Format(
                "bugs  {0}/{1}    ·    {2:0.0}s (par {3:0.0}s)    ·    {4} close calls",
                r.BugsCollected, r.BugsTotal, r.Time, r.ParTime, r.CloseCalls);
            var statsText = UIFactory.CreateText(_winPanel, "Stats", stats, 40, new Color(1f, 1f, 1f, 0.9f));
            UIFactory.SetRect(statsText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(960f, 70f));

            var gm = GameStateManager.Instance;
            _rewardText = UIFactory.CreateText(_winPanel, "Reward", RewardLine(), 52,
                new Color(1f, 0.85f, 0.3f));
            UIFactory.SetRect(_rewardText, new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(960f, 80f));

            // rewarded double (opt-in)
            if (gm != null && gm.CanDoubleRewards)
            {
                var dbl = UIFactory.CreateButton(_winPanel, "DoubleButton", "DOUBLE  ▶",
                    new Color(0.95f, 0.62f, 0.2f), Color.white, null);
                UIFactory.SetRect(dbl, new Vector2(0.5f, 0.31f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(520f, 130f));
                dbl.onClick.AddListener(() =>
                {
                    GameAudio.Play(Sfx.UiClick);
                    var g = GameStateManager.Instance;
                    if (g == null) return;
                    g.RequestDoubleRewards(reward =>
                    {
                        if (_rewardText != null) _rewardText.text = RewardLine();
                        Destroy(dbl.gameObject);
                        ShowPopup("REWARDS DOUBLED!", new Color(1f, 0.85f, 0.3f));
                    });
                });
            }

            var again = UIFactory.CreateButton(_winPanel, "AgainButton", "RUN IT AGAIN",
                ButtonGreen, Color.white, () =>
                {
                    if (GameStateManager.Instance != null) GameStateManager.Instance.Restart();
                });
            UIFactory.SetRect(again, new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(520f, 130f));

            var home = UIFactory.CreateButton(_winPanel, "HomeButton", "HOME",
                new Color(0.32f, 0.42f, 0.52f), Color.white, GoHome);
            UIFactory.SetRect(home, new Vector2(0.5f, 0.07f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(320f, 110f));
        }

        private string RewardLine()
        {
            var gm = GameStateManager.Instance;
            if (gm == null) return "";
            var r = gm.LastReward;
            string s = "+" + r.BugsEarned + " bugs   +" + r.XpEarned + " xp";
            if (r.GemsEarned > 0) s += "   +" + r.GemsEarned + " gems";
            return s;
        }

        private IEnumerator PopIn(RectTransform rect, float delay)
        {
            Vector3 target = rect.localScale;
            rect.localScale = Vector3.zero;
            yield return new WaitForSecondsRealtime(delay);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 0.25f;
                float s = 1f + 0.3f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI); // overshoot
                rect.localScale = target * Mathf.Clamp01(t) * s;
                yield return null;
            }
            rect.localScale = target;
        }

        // ---------- event handlers ----------

        private void OnRunStarted()
        {
            if (_startPanel != null) Destroy(_startPanel.gameObject);
        }

        private void OnPlayerHit(int heartsLeft, Vector3 pos)
        {
            RefreshHearts(heartsLeft);
            ShowPopup("OUCH!", new Color(1f, 0.4f, 0.35f));
        }

        private void OnTailLost(Vector3 pos)
        {
            RefreshTail(false);
            ShowPopup("TAIL DROPPED!", new Color(0.6f, 1f, 0.5f));
        }

        private void OnTailRegrown()
        {
            RefreshTail(true);
            ShowPopup("TAIL BACK!", new Color(0.6f, 1f, 0.5f));
        }

        private void RefreshTail(bool hasTail)
        {
            if (_tailPip != null) _tailPip.color = hasTail ? TailGreen : TailDim;
        }

        private void OnPlayerDied(DeathCause cause)
        {
            _lastDeathCause = cause;
            RefreshHearts(0);
            ShowPopup(DeathCauseText(cause), new Color(1f, 0.3f, 0.2f));
            StartCoroutine(DeathPanelAfterBeat());
        }

        private static string DeathCauseText(DeathCause cause)
        {
            switch (cause)
            {
                case DeathCause.Caught: return "CAUGHT BY THE CAT!";
                case DeathCause.Squashed: return "HIT BY A CAR!";
                default: return "STEPPED ON!";
            }
        }

        // Big death-panel headline, keyed to how the run ended.
        private static string DeathTitle(DeathCause cause)
        {
            switch (cause)
            {
                case DeathCause.Caught: return "CAUGHT!";
                case DeathCause.Squashed: return "SQUISHED!";
                default: return "STOMPED!";
            }
        }

        // The alley cat wakes on the first foot-bump: reveal its danger meter and warn.
        private void OnCatProvoked()
        {
            if (_dangerGroup != null) _dangerGroup.SetActive(true);
            if (_dangerLabel != null) _dangerLabel.gameObject.SetActive(true);
            ShowPopup("THE CAT IS HUNTING!", new Color(1f, 0.3f, 0.2f));
            GameAudio.Play(Sfx.Hit);
        }

        private IEnumerator DeathPanelAfterBeat()
        {
            yield return new WaitForSecondsRealtime(1.1f); // let the squash + shake read
            ShowDeathPanel();
        }

        private void OnRunWon(RunResults r)
        {
            ShowWinPanel(r);
        }

        private void OnBugCollected(int collected, int total)
        {
            RefreshBugs(collected, total);
        }

        private void OnNearMiss(Vector3 pos)
        {
            ShowPopup("CLOSE CALL!", new Color(1f, 0.62f, 0.15f));
        }

        // ---------- refresh ----------

        private void RefreshHearts(int hearts)
        {
            for (int i = 0; i < _hearts.Length; i++)
                _hearts[i].color = i < hearts ? HeartRed : HeartDim;
        }

        private void RefreshBugs(int collected, int total)
        {
            _bugText.text = collected + " / " + total;
        }

        // ---------- top-center title helpers ----------

        // The displayed banner name. The loaded LevelDefinition is the legacy
        // "Garden Escape"; this is now the realistic NYC theme, so show a fitting
        // NYC name (falls back to the level's own Name if it ever reads NYC-ish).
        private static string LevelTitle()
        {
            var lvl = GameStateManager.Instance != null ? GameStateManager.Instance.Level : null;
            string n = lvl != null ? lvl.Name : null;
            if (!string.IsNullOrEmpty(n) && n != "Garden Escape")
                return n.ToUpperInvariant();
            return "DOWNTOWN DASH";
        }

        // No level-index field exists yet; default to 1 (single vertical-slice level).
        private static int LevelNumber()
        {
            return 1;
        }

        // Subtle dark outline so high-contrast white HUD text stays legible over the
        // bright, busy world without a heavy panel behind it.
        private static void AddTextOutline(Text t)
        {
            var o = t.gameObject.AddComponent<UnityEngine.UI.Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.6f);
            o.effectDistance = new Vector2(2f, -2f);
        }

        private void ShowPopup(string msg, Color color)
        {
            _popup.text = msg;
            _popup.color = color;
            _popup.gameObject.SetActive(true);
            _popupUntil = Time.unscaledTime + 0.9f;
        }

        private void Update()
        {
            var gm = GameStateManager.Instance;
            var player = PlayerController.Instance;
            if (gm == null) return;

            // progress — fill width + the gecko marker ride the lizard's z / level length
            if (player != null && gm.Level != null)
            {
                float t = Mathf.Clamp01(player.transform.position.z / gm.Level.Length);
                var rect = _progressFill.rectTransform;
                var parent = (RectTransform)rect.parent;
                float trackW = parent.rect.width - 8f; // matches the fill's 4px inset each side
                rect.sizeDelta = new Vector2(trackW * t, rect.sizeDelta.y);
                if (_geckoMarker != null)
                    _geckoMarker.rectTransform.anchoredPosition =
                        new Vector2(4f + trackW * t, _geckoMarker.rectTransform.anchoredPosition.y);
            }

            // dash cooldown
            if (player != null)
            {
                float cd = player.DashCooldownRemaining;
                _dashFill.fillAmount = cd <= 0f ? 1f : 1f - cd / GameConst.DashCooldown;
            }

            // predator danger meter — grows as the alley cat closes, hue ramps to red-hot
            if (_dangerFill != null)
            {
                float threat = Predator.Instance != null ? Predator.Instance.Threat01 : 0f;
                var rect = _dangerFill.rectTransform;
                var parent = (RectTransform)rect.parent;
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, (parent.rect.height - 6f) * threat);
                _dangerFill.color = Color.Lerp(new Color(0.95f, 0.55f, 0.2f),
                    new Color(1f, 0.16f, 0.12f), threat);
            }

            if (_popup.gameObject.activeSelf && Time.unscaledTime > _popupUntil)
                _popup.gameObject.SetActive(false);
        }

        /// <summary>A big translucent steer pad in a bottom corner. Held → InputProvider.ButtonSteer
        /// drives the auto-running lizard left/right; released → stops swaying.</summary>
        private void BuildSteerButton(Transform root, string name, string glyph, float value, bool leftCorner)
        {
            var img = UIFactory.CreateImage(root, name, ProceduralTextures.CircleSprite(),
                new Color(1f, 1f, 1f, 0.18f));
            Vector2 anchor = leftCorner ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
            Vector2 pos = leftCorner ? new Vector2(70f, 70f) : new Vector2(-70f, 70f);
            UIFactory.SetRect(img, anchor, anchor, pos, new Vector2(300f, 300f));
            img.gameObject.AddComponent<HoldButton>().Value = value;
            var label = UIFactory.CreateText(img.transform, "Glyph", glyph, 110,
                new Color(0.97f, 1f, 0.97f, 0.92f));
            UIFactory.SetRect(label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(220f, 220f));
        }

        /// <summary>Press-and-hold steer pad: sets <see cref="InputProvider.ButtonSteer"/> while
        /// held, clears it on release/exit. Multiple pads share the field; the latest press wins.</summary>
        private class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
        {
            public float Value;
            public void OnPointerDown(PointerEventData e) { InputProvider.ButtonSteer = Value; }
            public void OnPointerUp(PointerEventData e) { Release(); }
            public void OnPointerExit(PointerEventData e) { Release(); }
            private void OnDisable() { Release(); }
            private void Release()
            {
                // only clear if WE own the current steer (so releasing one pad doesn't cancel the other)
                if (Mathf.Approximately(InputProvider.ButtonSteer, Value)) InputProvider.ButtonSteer = 0f;
            }
        }

        /// <summary>Gentle scale pulse for the "TAP TO GO" hint.</summary>
        private class PulseText : MonoBehaviour
        {
            private void Update()
            {
                float s = 1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.06f;
                transform.localScale = new Vector3(s, s, 1f);
            }
        }
    }
}
