using UnityEngine;

namespace LizardCrossing
{
    /// <summary>Result of banking one finished run, for the results UI to show.</summary>
    public struct RunReward
    {
        public int BugsEarned;
        public int XpEarned;
        public int GemsEarned;
        public bool FirstClear;
        public int StarsBefore;
        public int StarsAfter;
        public int PlayerLevelBefore;
        public int PlayerLevelAfter;
        public bool LeveledUp { get { return PlayerLevelAfter > PlayerLevelBefore; } }
        public bool NewStars { get { return StarsAfter > StarsBefore; } }
    }

    /// <summary>
    /// The meta-game service: owns the persisted <see cref="PlayerProfile"/> and
    /// every economy / unlock / cosmetic / level-record operation. Static so any
    /// system (UI, GameStateManager) can reach it without scene wiring.
    ///
    /// Monetization stance (packet): everything is earnable with bugs; gems only
    /// add convenience. No operation here gates normal progression.
    /// </summary>
    public static class MetaProgress
    {
        private const string Key = "lizard_crossing_profile_v2";
        private const string LegacyKey = "lizard_crossing_save_v1";

        private static PlayerProfile _p;

        public static PlayerProfile Profile
        {
            get { if (_p == null) Load(); return _p; }
        }

        // ---- persistence ----

        private static void Load()
        {
            string json = PlayerPrefs.GetString(Key, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                _p = JsonUtility.FromJson<PlayerProfile>(json) ?? new PlayerProfile();
            }
            else
            {
                _p = new PlayerProfile();
                TryMigrateLegacy(_p);
            }
            EnsureValid(_p);
        }

        public static void Save()
        {
            if (_p == null) return;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(_p));
            PlayerPrefs.Save();
        }

        /// <summary>Port the Phase 1 v1 save (bugs/xp/best stars) forward, once.</summary>
        private static void TryMigrateLegacy(PlayerProfile p)
        {
            string json = PlayerPrefs.GetString(LegacyKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return;
            var old = JsonUtility.FromJson<LegacySave>(json);
            if (old == null) return;
            p.bugs = old.bugBank;
            p.xp = old.xp;
            p.totalRuns = old.totalRuns;
            p.totalWins = old.totalWins;
            if (old.bestStarsLevel1 > 0)
            {
                p.levels.Add(new LevelRecord
                {
                    levelId = LevelDefinition.GardenEscapeId,
                    stars = old.bestStarsLevel1,
                    bestTime = old.bestTimeLevel1,
                    cleared = true,
                });
            }
        }

        private static void EnsureValid(PlayerProfile p)
        {
            if (p.unlockedLizards == null || p.unlockedLizards.Count == 0)
                p.unlockedLizards = new System.Collections.Generic.List<string> { "gecko" };
            if (!p.unlockedLizards.Contains("gecko")) p.unlockedLizards.Add("gecko");
            if (LizardSpecies.Get(p.selectedLizardId) == null || !p.unlockedLizards.Contains(p.selectedLizardId))
                p.selectedLizardId = "gecko";
        }

        // ---- run results ----

        public static RunReward GrantRunRewards(RunResults r, string levelId)
        {
            var p = Profile;
            var reward = new RunReward
            {
                StarsBefore = p.TotalStars,
                PlayerLevelBefore = p.PlayerLevel,
            };

            var rec = p.GetLevel(levelId);
            bool firstClear = rec == null || !rec.cleared;
            reward.FirstClear = firstClear;

            // bugs: collected this run convert 1:1 to currency, plus a clear bonus
            reward.BugsEarned = r.BugsCollected + (firstClear ? 25 : 10);
            // xp: clear + per-star + close-call flair
            reward.XpEarned = 20 + r.Stars * 15 + Mathf.Min(r.CloseCalls, 10) * 2;
            // gems: a trickle, only on first 3-star clears (keeps premium scarce but earnable)
            reward.GemsEarned = (firstClear && r.Stars >= 3) ? 5 : 0;

            p.bugs += reward.BugsEarned;
            p.xp += reward.XpEarned;
            p.gems += reward.GemsEarned;
            p.totalRuns++;
            p.totalWins++;
            p.totalCloseCalls += r.CloseCalls;
            p.totalBugsCollected += r.BugsCollected;

            if (rec == null)
            {
                rec = new LevelRecord { levelId = levelId };
                p.levels.Add(rec);
            }
            rec.cleared = true;
            if (r.Stars > rec.stars) rec.stars = r.Stars;
            if (r.BugsCollected > rec.bestBugs) rec.bestBugs = r.BugsCollected;
            if (rec.bestTime < 0f || r.Time < rec.bestTime) rec.bestTime = r.Time;

            reward.StarsAfter = p.TotalStars;
            reward.PlayerLevelAfter = p.PlayerLevel;
            Save();
            return reward;
        }

        public static void RecordDeath()
        {
            Profile.totalRuns++;
            Save();
        }

        /// <summary>
        /// Grant the same run reward a second time (rewarded-ad "double rewards").
        /// Returns a reward whose earned amounts reflect the doubled total, for the UI.
        /// </summary>
        public static RunReward ApplyDoubleReward(RunReward r)
        {
            var p = Profile;
            p.bugs += r.BugsEarned;
            p.xp += r.XpEarned;
            p.gems += r.GemsEarned;
            Save();

            r.BugsEarned *= 2;
            r.XpEarned *= 2;
            r.GemsEarned *= 2;
            r.PlayerLevelAfter = p.PlayerLevel;
            return r;
        }

        /// <summary>Open a rewarded bonus chest: a bundle of bugs (+ rare gems).</summary>
        public static RunReward GrantBonusChest()
        {
            var p = Profile;
            int bugs = 75;
            int gems = 3;
            p.bugs += bugs;
            p.gems += gems;
            Save();
            return new RunReward { BugsEarned = bugs, GemsEarned = gems };
        }

        // ---- currency ----

        public static bool SpendBugs(int amount)
        {
            if (amount <= 0) return true;
            if (Profile.bugs < amount) return false;
            Profile.bugs -= amount; Save(); return true;
        }

        public static bool SpendGems(int amount)
        {
            if (amount <= 0) return true;
            if (Profile.gems < amount) return false;
            Profile.gems -= amount; Save(); return true;
        }

        public static void AddBugs(int amount) { Profile.bugs += Mathf.Max(0, amount); Save(); }
        public static void AddGems(int amount) { Profile.gems += Mathf.Max(0, amount); Save(); }

        // ---- lizards ----

        public static bool IsLizardUnlocked(string id) { return Profile.unlockedLizards.Contains(id); }

        public static LizardSpecies SelectedLizard { get { return LizardSpecies.Get(Profile.selectedLizardId); } }

        /// <summary>Buy with bugs (preferred) — returns true on success.</summary>
        public static bool TryUnlockLizardWithBugs(string id)
        {
            var s = LizardSpecies.Get(id);
            if (s == null || IsLizardUnlocked(id)) return false;
            if (!SpendBugs(s.UnlockCostBugs)) return false;
            Profile.unlockedLizards.Add(id); Save(); return true;
        }

        /// <summary>Buy a Premium lizard with gems (optional convenience path).</summary>
        public static bool TryUnlockLizardWithGems(string id)
        {
            var s = LizardSpecies.Get(id);
            if (s == null || !s.Premium || IsLizardUnlocked(id)) return false;
            if (!SpendGems(s.CostGems)) return false;
            Profile.unlockedLizards.Add(id); Save(); return true;
        }

        public static bool SelectLizard(string id)
        {
            if (!IsLizardUnlocked(id)) return false;
            Profile.selectedLizardId = id; Save(); return true;
        }

        // ---- cosmetics ----

        public static bool OwnsCosmetic(string id)
        {
            var c = CosmeticItem.Get(id);
            if (c == null) return false;
            return c.IsDefault || Profile.ownedCosmetics.Contains(id);
        }

        public static bool TryBuyCosmeticWithBugs(string id)
        {
            var c = CosmeticItem.Get(id);
            if (c == null || OwnsCosmetic(id)) return false;
            if (!SpendBugs(c.CostBugs)) return false;
            Profile.ownedCosmetics.Add(id); Save(); return true;
        }

        public static bool TryBuyCosmeticWithGems(string id)
        {
            var c = CosmeticItem.Get(id);
            if (c == null || !c.Premium || OwnsCosmetic(id)) return false;
            if (!SpendGems(c.CostGems)) return false;
            Profile.ownedCosmetics.Add(id); Save(); return true;
        }

        public static bool Equip(string id)
        {
            var c = CosmeticItem.Get(id);
            if (c == null || !OwnsCosmetic(id)) return false;
            Profile.SetEquipped(c.Slot, id); Save(); return true;
        }

        /// <summary>The equipped item for a slot, or that slot's free default.</summary>
        public static CosmeticItem EquippedItem(CosmeticSlot slot)
        {
            string id = Profile.GetEquipped(slot);
            var item = id != null ? CosmeticItem.Get(id) : null;
            return item ?? CosmeticItem.DefaultFor(slot);
        }

        // ---- level records ----

        public static LevelRecord LevelRecord(string levelId) { return Profile.GetLevel(levelId); }
        public static int TotalStars { get { return Profile.TotalStars; } }
    }

    /// <summary>Shape of the Phase 1 v1 save, for one-time migration only.</summary>
    [System.Serializable]
    internal class LegacySave
    {
        public int version;
        public int bugBank;
        public int totalRuns;
        public int totalWins;
        public int bestStarsLevel1;
        public float bestTimeLevel1 = -1f;
        public int xp;
    }
}
