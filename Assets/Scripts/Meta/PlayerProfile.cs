using System;
using System.Collections.Generic;

namespace LizardCrossing
{
    /// <summary>
    /// The complete persisted player state (JsonUtility-serializable — only
    /// primitives, strings, and Lists of those or of [Serializable] structs).
    /// Owned and persisted by <see cref="MetaProgress"/>.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public int version = 2;

        // Currencies
        public int bugs;     // soft currency, earned in levels (packet: bugs are currency)
        public int gems;     // premium currency, earned slowly + optional IAP

        // Experience
        public int xp;

        // Lifetime stats
        public int totalRuns;
        public int totalWins;
        public int totalCloseCalls;
        public int totalBugsCollected;

        // Roster
        public string selectedLizardId = "gecko";
        public List<string> unlockedLizards = new List<string> { "gecko" };

        // Cosmetics
        public List<string> ownedCosmetics = new List<string>();
        public List<EquippedSlot> equipped = new List<EquippedSlot>();

        // Per-level progress
        public List<LevelRecord> levels = new List<LevelRecord>();

        // Daily challenge
        public string lastDailyDate = "";    // yyyyMMdd of last claimed daily
        public int dailyStreak;

        // ---- XP / player level curve ----
        // Level N requires 100*N cumulative-per-step XP: lvl1=0, lvl2=100, lvl3=250...
        public static int XpForLevel(int level)
        {
            if (level <= 1) return 0;
            // triangular: 50*(L-1)*L
            return 50 * (level - 1) * level;
        }

        public int PlayerLevel
        {
            get
            {
                int lvl = 1;
                while (xp >= XpForLevel(lvl + 1)) lvl++;
                return lvl;
            }
        }

        public int XpIntoLevel { get { return xp - XpForLevel(PlayerLevel); } }
        public int XpToNextLevel { get { int l = PlayerLevel; return XpForLevel(l + 1) - XpForLevel(l); } }

        public LevelRecord GetLevel(string levelId)
        {
            for (int i = 0; i < levels.Count; i++)
                if (levels[i].levelId == levelId) return levels[i];
            return null;
        }

        public int TotalStars
        {
            get
            {
                int s = 0;
                for (int i = 0; i < levels.Count; i++) s += levels[i].stars;
                return s;
            }
        }

        public string GetEquipped(CosmeticSlot slot)
        {
            for (int i = 0; i < equipped.Count; i++)
                if (equipped[i].slot == (int)slot) return equipped[i].itemId;
            return null;
        }

        public void SetEquipped(CosmeticSlot slot, string itemId)
        {
            for (int i = 0; i < equipped.Count; i++)
            {
                if (equipped[i].slot == (int)slot) { equipped[i].itemId = itemId; return; }
            }
            equipped.Add(new EquippedSlot { slot = (int)slot, itemId = itemId });
        }
    }

    [Serializable]
    public class LevelRecord
    {
        public string levelId;
        public int stars;
        public float bestTime = -1f;
        public int bestBugs;
        public bool cleared;
    }

    [Serializable]
    public class EquippedSlot
    {
        public int slot;       // (int)CosmeticSlot
        public string itemId;
    }
}
