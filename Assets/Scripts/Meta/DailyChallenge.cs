namespace LizardCrossing
{
    public enum DailyObjective { CollectAllBugs, CloseCalls, NoHits, FinishUnderTime }

    /// <summary>
    /// A deterministic daily remix (packet: daily challenges / replayable
    /// objectives). The same calendar date always yields the same seed, modifier,
    /// and objective so every player gets the same daily and can be leaderboarded.
    /// Pure data — the level builder/HUD read <see cref="Modifier"/> and
    /// <see cref="Objective"/> when running a daily.
    /// </summary>
    public class DailyChallenge
    {
        public string Date;        // yyyyMMdd
        public int Seed;
        public DailyObjective Objective;
        public int Target;
        public string Modifier;    // "fast" | "busy" | "tiny_window" | "calm"
        public int RewardBugs;
        public int RewardGems;

        private static readonly string[] Modifiers = { "fast", "busy", "tiny_window", "calm" };

        public static DailyChallenge ForDate(string yyyymmdd)
        {
            int seed = StableHash(yyyymmdd);
            var rng = new System.Random(seed);

            var c = new DailyChallenge
            {
                Date = yyyymmdd,
                Seed = seed,
                Modifier = Modifiers[rng.Next(Modifiers.Length)],
                Objective = (DailyObjective)rng.Next(4),
                RewardBugs = 60 + rng.Next(6) * 10,
                RewardGems = rng.Next(4) == 0 ? 5 : 0,
            };

            switch (c.Objective)
            {
                case DailyObjective.CollectAllBugs: c.Target = 0; break;          // all of them
                case DailyObjective.CloseCalls: c.Target = 3 + rng.Next(4); break; // 3-6
                case DailyObjective.NoHits: c.Target = 0; break;
                case DailyObjective.FinishUnderTime: c.Target = 35 + rng.Next(15); break; // seconds
            }
            return c;
        }

        public string Describe()
        {
            switch (Objective)
            {
                case DailyObjective.CollectAllBugs: return "Collect every bug";
                case DailyObjective.CloseCalls: return "Get " + Target + " close calls";
                case DailyObjective.NoHits: return "Finish without a hit";
                case DailyObjective.FinishUnderTime: return "Finish under " + Target + "s";
            }
            return "Reach the garden";
        }

        /// <summary>True if a finished run satisfied this objective.</summary>
        public bool IsMet(RunResults r)
        {
            switch (Objective)
            {
                case DailyObjective.CollectAllBugs: return r.BugsCollected >= r.BugsTotal && r.BugsTotal > 0;
                case DailyObjective.CloseCalls: return r.CloseCalls >= Target;
                case DailyObjective.NoHits: return r.HeartsLeft >= GameConst.MaxHearts;
                case DailyObjective.FinishUnderTime: return r.Time <= Target;
            }
            return false;
        }

        private static int StableHash(string s)
        {
            unchecked
            {
                int h = 17;
                for (int i = 0; i < s.Length; i++) h = h * 31 + s[i];
                return h & 0x7fffffff;
            }
        }
    }
}
