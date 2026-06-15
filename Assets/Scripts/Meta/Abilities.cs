namespace LizardCrossing
{
    /// <summary>
    /// Unique lizard abilities (packet: "dash, jump, camouflage, faster sprint,
    /// debris resistance, night/rain advantages"). Each species maps to one.
    /// </summary>
    public enum AbilityType
    {
        Dash,        // baseline burst (Gecko) — every lizard can dash; Gecko's is cheapest cooldown
        DoubleJump,  // Anole: a second airborne hop to clear a footfall
        Camouflage,  // Chameleon: brief hazard immunity while standing still
        Sprint,      // Gila/others: higher sustained run speed
        DebrisResist,// Skink: starts with an extra heart
        NightGrip,   // Night Gecko: edge in night/rain levels (future themes)
    }

    /// <summary>
    /// Numeric gameplay modifiers an ability contributes. Read by PlayerController
    /// and GameStateManager so the roster is data-driven, not branchy code.
    /// </summary>
    public struct AbilityModifiers
    {
        public float MoveSpeedMult;    // 1 = baseline
        public float DashCooldownMult; // 1 = baseline (lower = dash more often)
        public int BonusHearts;        // extra starting hearts
        public bool CanDoubleJump;
        public bool CanCamouflage;     // invulnerable after standing still briefly
        public float NightAdvantage;   // 0..1 perk strength in night/rain levels (future)

        public static AbilityModifiers None()
        {
            return new AbilityModifiers
            {
                MoveSpeedMult = 1f,
                DashCooldownMult = 1f,
                BonusHearts = 0,
                CanDoubleJump = false,
                CanCamouflage = false,
                NightAdvantage = 0f,
            };
        }
    }
}
