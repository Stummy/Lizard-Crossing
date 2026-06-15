using System.Collections;
using UnityEngine;
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
        private static readonly Color StarGold = new Color(1f, 0.8f, 0.15f);
        private static readonly Color StarDim = new Color(0.3f, 0.3f, 0.32f, 0.8f);
        private static readonly Color PanelDim = new Color(0.05f, 0.08f, 0.05f, 0.78f);
        private static readonly Color ButtonGreen = new Color(0.35f, 0.7f, 0.3f);

        private Image[] _hearts;
        private Text _bugText;
        private Image _progressFill;
        private Image _dashFill;
        private Text _message;
        private Text _popup;
        private RectTransform _startPanel;
        private RectTransform _deathPanel;
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
            // ----- hearts (top-left) — count includes the lizard's heart bonus -----
            int maxHearts = GameStateManager.Instance != null
                ? GameStateManager.Instance.MaxHearts : GameConst.MaxHearts;
            _hearts = new Image[maxHearts];
            var heartSprite = ProceduralTextures.HeartSprite();
            for (int i = 0; i < _hearts.Length; i++)
            {
                var heart = UIFactory.CreateImage(root, "Heart" + i, heartSprite, HeartRed);
                UIFactory.SetRect(heart, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(40f + i * 95f, -40f), new Vector2(85f, 85f));
                _hearts[i] = heart;
            }

            // ----- bug counter (top-right): tiny fly icon (body + wing dots) -----
            var bugIcon = UIFactory.CreateImage(root, "BugIcon", ProceduralTextures.CircleSprite(),
                new Color(0.25f, 0.18f, 0.12f));
            UIFactory.SetRect(bugIcon, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-180f, -48f), new Vector2(52f, 64f));
            var wingL = UIFactory.CreateImage(bugIcon.transform, "WingL", ProceduralTextures.CircleSprite(),
                new Color(0.85f, 0.95f, 1f, 0.8f));
            UIFactory.SetRect(wingL, new Vector2(0.2f, 0.85f), new Vector2(0.5f, 0.5f),
                new Vector2(-8f, 6f), new Vector2(34f, 44f));
            wingL.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 30f);
            var wingR = UIFactory.CreateImage(bugIcon.transform, "WingR", ProceduralTextures.CircleSprite(),
                new Color(0.85f, 0.95f, 1f, 0.8f));
            UIFactory.SetRect(wingR, new Vector2(0.8f, 0.85f), new Vector2(0.5f, 0.5f),
                new Vector2(8f, 6f), new Vector2(34f, 44f));
            wingR.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -30f);
            _bugText = UIFactory.CreateText(root, "BugCount", "0/0", 58, Color.white, TextAnchor.MiddleLeft);
            UIFactory.SetRect(_bugText, new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(-160f, -42f), new Vector2(150f, 70f));

            // ----- progress bar (top-center) -----
            var barBg = UIFactory.CreateImage(root, "ProgressBg", UIFactory.RoundedSprite(),
                new Color(0f, 0f, 0f, 0.5f));
            barBg.type = Image.Type.Sliced;
            UIFactory.SetRect(barBg, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -150f), new Vector2(620f, 26f));
            _progressFill = UIFactory.CreateImage(barBg.transform, "ProgressFill",
                UIFactory.RoundedSprite(), new Color(0.45f, 0.92f, 0.4f));
            _progressFill.type = Image.Type.Sliced;
            var fillRect = _progressFill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = new Vector2(3f, 0f);
            fillRect.sizeDelta = new Vector2(0f, -6f);
            var goalDot = UIFactory.CreateImage(barBg.transform, "GoalDot", ProceduralTextures.CircleSprite(),
                new Color(0.95f, 1f, 0.6f));
            UIFactory.SetRect(goalDot, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(44f, 44f));

            // ----- dash button (bottom-right) -----
            var dashBtnImg = UIFactory.CreateImage(root, "DashButton", ProceduralTextures.CircleSprite(),
                new Color(1f, 1f, 1f, 0.25f));
            UIFactory.SetRect(dashBtnImg, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-60f, 60f), new Vector2(220f, 220f));
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
            GameEvents.PlayerDied += OnPlayerDied;
            GameEvents.PlayerRevived += OnRevived;
            GameEvents.RunWon += OnRunWon;
            GameEvents.BugCollected += OnBugCollected;
            GameEvents.NearMiss += OnNearMiss;

            RefreshBugs(0, GameStateManager.Instance != null ? GameStateManager.Instance.BugsTotal : 0);
        }

        private void OnDestroy()
        {
            GameEvents.RunStarted -= OnRunStarted;
            GameEvents.PlayerHit -= OnPlayerHit;
            GameEvents.PlayerDied -= OnPlayerDied;
            GameEvents.PlayerRevived -= OnRevived;
            GameEvents.RunWon -= OnRunWon;
            GameEvents.BugCollected -= OnBugCollected;
            GameEvents.NearMiss -= OnNearMiss;
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
                "drag to scurry  ·  DASH to burst through gaps", 40, new Color(1f, 1f, 1f, 0.85f));
            UIFactory.SetRect(controls, new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(950f, 70f));
        }

        private void ShowDeathPanel()
        {
            var gm = GameStateManager.Instance;
            _deathPanel = UIFactory.CreatePanel(transform, "DeathPanel", PanelDim);

            var title = UIFactory.CreateText(_deathPanel, "Title", "SQUISHED!", 120,
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

        private void OnPlayerDied(DeathCause cause)
        {
            RefreshHearts(0);
            StartCoroutine(DeathPanelAfterBeat());
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
            _bugText.text = collected + "/" + total;
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

            // progress
            if (player != null && gm.Level != null)
            {
                float t = Mathf.Clamp01(player.transform.position.z / gm.Level.Length);
                var rect = _progressFill.rectTransform;
                var parent = (RectTransform)rect.parent;
                rect.sizeDelta = new Vector2((parent.rect.width - 6f) * t, rect.sizeDelta.y);
            }

            // dash cooldown
            if (player != null)
            {
                float cd = player.DashCooldownRemaining;
                _dashFill.fillAmount = cd <= 0f ? 1f : 1f - cd / GameConst.DashCooldown;
            }

            if (_popup.gameObject.activeSelf && Time.unscaledTime > _popupUntil)
                _popup.gameObject.SetActive(false);
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
