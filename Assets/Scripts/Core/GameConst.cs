namespace LizardCrossing
{
    /// <summary>
    /// Global tuning constants. World scale convention: 1 unit = 1 lizard body length.
    /// Level-specific numbers live in LevelDefinition; these are cross-level invariants.
    /// </summary>
    public static class GameConst
    {
        // World / layout
        public const float CorridorHalfWidth = 9f;     // playable half width (x)
        public const float SlabSize = 14f;             // sidewalk slab edge length
        public const float GroundY = 0f;

        // Lizard — speeds scaled to the realistic ~0.15u lizard (2026-06-16).
        // Kept snappier than a true 1/12 scale so the run still feels arcade-fast.
        public const float LizardMoveSpeed = 2.6f;
        public const float LizardTurnSpeedDeg = 720f;
        public const float DashSpeed = 6f;
        public const float DashDuration = 0.25f;
        public const float DashCooldown = 2.0f;

        // Abilities (jump = Anole, camouflage = Chameleon, revive = rewarded ad)
        public const float JumpVelocity = 2.2f;
        public const float JumpGravity = 26f;
        public const float CamouflageDelay = 0.4f;   // stand still this long to vanish
        public const float ReviveInvulnerableTime = 2.0f;

        // Hearts (docs: packet Phase 1 spec — HUD shows hearts)
        public const int MaxHearts = 3;
        public const float HitInvulnerableTime = 1.6f;
        public const float HitKnockback = 1.0f;

        // Camera (vertical FOV adapts so portrait keeps cross-traffic readable).
        // Scaled low + close for the realistic ~0.15u lizard POV (2026-06-16).
        public const float CamBack = 0.5f;
        public const float CamHeight = 0.16f;
        public const float CamLookAhead = 1.2f;
        public const float CamLookHeight = 0.12f;
        public const float CamBaseFov = 62f;
        public const float CamMaxFov = 76f;
        public const float CamTargetHorizontalFov = 58f;
        public const float CamDashFovKick = 8f;
        public const float CamTraumaDecay = 1.4f;

        // Hazards / feel (scaled to the realistic lizard, 2026-06-16)
        public const float CloseCallRadius = 0.3f;
        public const float StompKillPad = 0.03f;       // margin trimmed off the sole: edges are forgiving
        public const float MinWarningLead = 0.7f;      // guaranteed telegraph lead (s) before any footfall lands
        public const float HitStopDuration = 0.07f;
        public const float NearMissSlowScale = 0.45f;
        public const float NearMissSlowDuration = 0.35f;

        // Economy / scoring
        public const int StarTwoBugPercent = 75;

        public static float ParTime(float levelLength)
        {
            return levelLength / (0.78f * LizardMoveSpeed); // see docs/DECISIONS.md D14
        }
    }
}
