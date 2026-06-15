using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// One unlockable lizard: its identity, ability, costs, and the colors the
    /// procedural body (later a real model) is tinted with. Pure data — the
    /// roster below is the single source of truth (docs/DESIGN.md §6).
    ///
    /// Economy rule (packet monetization): every lizard is earnable with bugs.
    /// "Premium" only means it is ALSO buyable with gems for players who'd rather
    /// not grind — never pay-to-win, never a progression gate.
    /// </summary>
    public class LizardSpecies
    {
        public string Id;
        public string Name;
        public string Description;
        public AbilityType Ability;
        public string AbilityName;
        public int UnlockCostBugs;   // 0 = starter (owned from the start)
        public bool Premium;         // also purchasable with gems
        public int CostGems;         // gem price when Premium
        public Color BodyColor;
        public Color BellyColor;
        public Color StripeColor;
        public AbilityModifiers Modifiers;

        public bool IsStarter { get { return UnlockCostBugs <= 0 && !Premium; } }

        public static readonly LizardSpecies[] All =
        {
            new LizardSpecies
            {
                Id = "gecko", Name = "Gecko", AbilityName = "Quick Dash",
                Description = "Nimble all-rounder. The fastest dash recovery of any lizard.",
                Ability = AbilityType.Dash, UnlockCostBugs = 0,
                BodyColor = new Color(0.42f, 0.76f, 0.29f),
                BellyColor = new Color(0.65f, 0.88f, 0.5f),
                StripeColor = new Color(0.27f, 0.55f, 0.2f),
                Modifiers = new AbilityModifiers { MoveSpeedMult = 1f, DashCooldownMult = 0.85f },
            },
            new LizardSpecies
            {
                Id = "anole", Name = "Anole", AbilityName = "Double Hop",
                Description = "Springy green climber. Can hop a second time in mid-air to clear a footfall.",
                Ability = AbilityType.DoubleJump, UnlockCostBugs = 250,
                BodyColor = new Color(0.22f, 0.72f, 0.46f),
                BellyColor = new Color(0.7f, 0.92f, 0.6f),
                StripeColor = new Color(0.15f, 0.5f, 0.32f),
                Modifiers = new AbilityModifiers { MoveSpeedMult = 1f, DashCooldownMult = 1f, CanDoubleJump = true },
            },
            new LizardSpecies
            {
                Id = "chameleon", Name = "Chameleon", AbilityName = "Camouflage",
                Description = "Stand perfectly still to blend in — hazards pass right over you for a moment.",
                Ability = AbilityType.Camouflage, UnlockCostBugs = 600,
                BodyColor = new Color(0.32f, 0.62f, 0.55f),
                BellyColor = new Color(0.78f, 0.85f, 0.6f),
                StripeColor = new Color(0.55f, 0.4f, 0.65f),
                Modifiers = new AbilityModifiers { MoveSpeedMult = 0.97f, DashCooldownMult = 1f, CanCamouflage = true },
            },
            new LizardSpecies
            {
                Id = "skink", Name = "Skink", AbilityName = "Tough Hide",
                Description = "Stocky and armored. Starts every run with an extra heart.",
                Ability = AbilityType.DebrisResist, UnlockCostBugs = 1200,
                BodyColor = new Color(0.6f, 0.45f, 0.28f),
                BellyColor = new Color(0.85f, 0.74f, 0.5f),
                StripeColor = new Color(0.3f, 0.45f, 0.7f),
                Modifiers = new AbilityModifiers { MoveSpeedMult = 0.95f, DashCooldownMult = 1.05f, BonusHearts = 1 },
            },
            new LizardSpecies
            {
                Id = "gila", Name = "Gila", AbilityName = "Sprinter",
                Description = "Desert runner with a powerful sustained sprint.",
                Ability = AbilityType.Sprint, UnlockCostBugs = 2200, Premium = true, CostGems = 120,
                BodyColor = new Color(0.18f, 0.16f, 0.2f),
                BellyColor = new Color(0.9f, 0.55f, 0.3f),
                StripeColor = new Color(0.92f, 0.45f, 0.32f),
                Modifiers = new AbilityModifiers { MoveSpeedMult = 1.18f, DashCooldownMult = 1.1f },
            },
            new LizardSpecies
            {
                Id = "nightgecko", Name = "Night Gecko", AbilityName = "Night Grip",
                Description = "Sees in the dark and grips wet ground — an edge on night and rain levels.",
                Ability = AbilityType.NightGrip, UnlockCostBugs = 2600, Premium = true, CostGems = 150,
                BodyColor = new Color(0.24f, 0.3f, 0.55f),
                BellyColor = new Color(0.55f, 0.7f, 0.95f),
                StripeColor = new Color(0.4f, 0.85f, 0.9f),
                Modifiers = new AbilityModifiers { MoveSpeedMult = 1.05f, DashCooldownMult = 1f, NightAdvantage = 0.5f },
            },
        };

        public static LizardSpecies Get(string id)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].Id == id) return All[i];
            return All[0];
        }

        public static LizardSpecies Default { get { return All[0]; } }
    }
}
