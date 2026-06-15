using UnityEngine;
using UnityEngine.SceneManagement;

namespace LizardCrossing
{
    public enum GameState { Ready, Playing, Dead, Won }

    /// <summary>
    /// Run-scoped state machine (packet required system: GameStateManager).
    /// Owns the run clock, hearts, bug count, close calls, and star scoring.
    /// Constructed by Bootstrap.
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Ready;
        public float RunTime { get; private set; }
        public int Hearts { get; private set; } = GameConst.MaxHearts;
        public int MaxHearts { get; private set; } = GameConst.MaxHearts;
        public int BugsCollected { get; private set; }
        public int BugsTotal { get; set; }
        public int CloseCalls { get; private set; }
        public LevelDefinition Level { get; set; }

        /// <summary>Rewards banked from the last win, for the results screen.</summary>
        public RunReward LastReward { get; private set; }

        public bool ReviveUsed { get; private set; }
        public bool RewardsDoubled { get; private set; }

        private IAdService _ads;

        public void Init(IAdService ads = null)
        {
            Instance = this;
            _ads = ads ?? new StubAdService();
            Application.targetFrameRate = 60;
            // starting hearts include the selected lizard's ability bonus (e.g. Skink)
            MaxHearts = GameConst.MaxHearts + Mathf.Max(0, MetaProgress.SelectedLizard.Modifiers.BonusHearts);
            Hearts = MaxHearts;
            GameEvents.NearMiss += OnNearMiss;
        }

        private void OnDestroy()
        {
            GameEvents.NearMiss -= OnNearMiss;
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (State == GameState.Ready && InputProvider.AnyStartPressed())
                StartRun();

            if (State == GameState.Playing)
                RunTime += Time.deltaTime;
        }

        public void StartRun()
        {
            if (State != GameState.Ready) return;
            State = GameState.Playing;
            GameEvents.RaiseRunStarted();
        }

        public void CollectBug()
        {
            if (State != GameState.Playing) return;
            BugsCollected++;
            GameEvents.RaiseBugCollected(BugsCollected, BugsTotal);
        }

        private void OnNearMiss(Vector3 pos)
        {
            CloseCalls++;
        }

        /// <summary>A hazard connected with the lizard. Costs a heart; 0 hearts = death.</summary>
        public void HitPlayer(Vector3 hazardPos)
        {
            if (State != GameState.Playing) return;

            Hearts--;
            if (Hearts > 0)
            {
                GameEvents.RaisePlayerHit(Hearts, hazardPos);
            }
            else
            {
                Hearts = 0;
                State = GameState.Dead;
                MetaProgress.RecordDeath();
                GameEvents.RaisePlayerDied(DeathCause.Stomped);
            }
        }

        public void WinRun()
        {
            if (State != GameState.Playing) return;
            State = GameState.Won;

            var results = new RunResults
            {
                BugsCollected = BugsCollected,
                BugsTotal = BugsTotal,
                Time = RunTime,
                ParTime = GameConst.ParTime(Level != null ? Level.Length : 220f),
                CloseCalls = CloseCalls,
                HeartsLeft = Hearts,
            };
            results.Stars = ScoreStars(results);

            string levelId = Level != null ? Level.Id : LevelDefinition.GardenEscapeId;
            LastReward = MetaProgress.GrantRunRewards(results, levelId);
            GameEvents.RaiseRunWon(results);
        }

        private static int ScoreStars(RunResults r)
        {
            int stars = 1; // finishing is the first star
            if (r.BugsTotal > 0 && r.BugsCollected * 100 >= r.BugsTotal * GameConst.StarTwoBugPercent)
                stars = 2;
            if (r.BugsCollected == r.BugsTotal && r.Time <= r.ParTime)
                stars = 3;
            return stars;
        }

        // ---- rewarded-ad flows (opt-in; never forced) ----

        public bool CanRevive
        {
            get { return State == GameState.Dead && !ReviveUsed && _ads != null && _ads.IsRewardedReady; }
        }

        /// <summary>One rewarded revive per run: come back with a single heart.</summary>
        public void RequestRevive()
        {
            if (!CanRevive) return;
            _ads.ShowRewarded(AdPlacement.Revive, success =>
            {
                if (success && State == GameState.Dead)
                {
                    ReviveUsed = true;
                    Hearts = 1;
                    State = GameState.Playing;
                    GameEvents.RaisePlayerRevived();
                }
            });
        }

        public bool CanDoubleRewards
        {
            get { return State == GameState.Won && !RewardsDoubled && _ads != null && _ads.IsRewardedReady; }
        }

        /// <summary>Watch an ad to double this run's banked rewards.</summary>
        public void RequestDoubleRewards(System.Action<RunReward> onDone)
        {
            if (!CanDoubleRewards) { if (onDone != null) onDone(LastReward); return; }
            _ads.ShowRewarded(AdPlacement.DoubleRewards, success =>
            {
                if (success)
                {
                    RewardsDoubled = true;
                    LastReward = MetaProgress.ApplyDoubleReward(LastReward);
                }
                if (onDone != null) onDone(LastReward);
            });
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
