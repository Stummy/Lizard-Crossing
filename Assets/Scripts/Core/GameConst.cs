namespace LizardCrossing
{
    /// <summary>
    /// Global tuning constants. World scale convention: 1 unit = 1 lizard body length.
    /// Level-specific numbers live in LevelDefinition; these are cross-level invariants.
    /// </summary>
    public static class GameConst
    {
        // World / layout
        public const float CorridorHalfWidth = 9f;     // full corridor half width (x) — cars/peds traverse this
        public const float SlabSize = 14f;             // sidewalk slab edge length
        public const float GroundY = 0f;

        // Straight sidewalk runner on the NYC avenue. The avenue isn't perfectly straight:
        // the curb sits at x≈3 on the first block but the street JOGS right after the second
        // intersection, so the curb moves to x≈5 on the final block. To keep the lizard on the
        // sidewalk the WHOLE way (never dropping into the road, even near the end), the run band
        // is set clear of the worst-case curb (x≈5) on every block and short of the buildings
        // (x≈12+): the lizard is clamped to x ∈ [7, 11]. This is the "invisible wall" — a dead
        // straight run that stays on the sidewalk start to finish, and is roughly centred on the
        // wide first-block sidewalk too.
        public const float CorridorCenterX = 9f;
        public const float SidewalkHalfWidth = 2f;     // used only for prop/pickup placement near the centre; the PLAYER band is the wider Z-aware one below
        public const float SidewalkVisualHalfWidth = 5f; // half width of the drawn sidewalk slab + walls

        // Player sidewalk band [minX, maxX]. The right edge hugs the building line and the left
        // edge is pinned to the curb line, so the lizard always stays on the sidewalk and can
        // never step onto the road. See PlayerController.CorridorBand. Supersedes the old fixed
        // x[7,11] clamp (and a later Z-taper whose two ends were both 6.0 — a no-op, now collapsed).
        public const float CorridorRightX = 11f;       // sidewalk right edge (building line) — safe the whole way
        public const float CorridorLeftX = 6.0f;       // sidewalk left edge — pinned to the curb line (OWNER FIX
                                                       // 2026-06-26): the band is clamped to 6.0 so the lizard can
                                                       // never slip left of the curb (5.8) onto the road.

        // --- Authored straight corridor (World section, 2026-06-26) ---
        // The imported NYC street is one continuous, collider-less mesh whose sidewalk STEPS right
        // at the end intersection, so a straight run band drifts off it (the "sidewalk shifts /
        // through the wall" bug). Instead of fighting the crooked GLB we lay our OWN straight
        // sidewalk strip + REAL collider walls down the run band (LevelBuilder.BuildStraightCorridor)
        // and let the GLB be backdrop. These define that corridor; the right wall sits just outside
        // the lizard's clamp so it physically backstops the run, the left fence keeps it off the road.
        public const float CorridorStripCenterX = 8f;   // centre of the authored sidewalk strip
        public const float CorridorStripHalfWidth = 4.6f; // strip covers x[3.4 .. 12.6] (curb buffer + band)
        public const float CorridorStripY = 0.12f;      // strip surface (== StreetGround.SidewalkY, just above the GLB)
        public const float CorridorWallRightX = 11.4f;  // solid building-facade wall (lizard grazes it at its x≈11 max)
        public const float CorridorFenceLeftX = 5.8f;   // solid left railing/curb (lizard stops ~6.1, never reaches the road)
        public const float CorridorWallHeight = 2.5f;   // a building BASE, not a canyon wall — the GLB skyline shows
                                                        // above it so the avenue reads open + deep like the run concept
        public const float CorridorFenceHeight = 0.4f;  // OWNER: a low CURB (not a "fence"/railing) on the road
                                                        // side — reads as a concrete curb edge, still solid so the
                                                        // lizard can't step off the sidewalk onto the road.
        public const float MinRunZ = -4f;               // the lizard can't be pushed back behind this Z (knockback floor)

        // Lizard — speeds scaled to the realistic ~0.15u lizard (2026-06-16).
        // Kept snappier than a true 1/12 scale so the run still feels arcade-fast.
        public const float LizardMoveSpeed = 3.6f;   // brisk scurry — a real lizard is quick
        public const float LizardTurnSpeedDeg = 1100f; // snappy banking turn so a forward→side press redirects almost at once
        public const float LizardAccel = 60f;          // speed ramp-up (u/s²) — quick off the line
        public const float LizardDecel = 14f;          // speed ramp-down — gentle, so a brief key-swap gap glides (no stall)

        // Auto-run model: the lizard always scurries FORWARD on its own; the player only steers
        // left/right to weave the crowd and thread cross-traffic, and dashes. (No manual forward.)
        public const float AutoRunSpeed = 3.6f;       // constant forward (+Z) pace once the run is live
        public const float LizardStrafeSpeed = 4.2f;  // lateral (±X) steer speed — a touch quicker than forward so dodging feels responsive
        public const float DashSpeed = 8.5f;
        public const float DashDuration = 0.25f;
        public const float DashCooldown = 2.0f;

        // Abilities (jump = Anole, camouflage = Chameleon, revive = rewarded ad)
        public const float JumpVelocity = 2.2f;
        public const float JumpGravity = 26f;
        public const float AirborneThresholdY = 0.7f; // y above which the lizard counts as airborne (jumped clear of a footfall)
        public const float CamouflageDelay = 0.4f;   // stand still this long to vanish
        public const float ReviveInvulnerableTime = 2.0f;

        // Hearts (docs: packet Phase 1 spec — HUD shows hearts)
        public const int MaxHearts = 3;
        public const float HitInvulnerableTime = 1.6f;
        public const float HitKnockback = 1.0f;

        // Tail autotomy: the first hit drops the tail (no heart lost); it regrows after
        // this long unhurt, restoring the free-hit buffer (anoles really do this).
        public const float TailRegrowDelay = 14f;

        // Camera (vertical FOV adapts so portrait keeps cross-traffic readable).
        // Scaled low + close for the realistic ~0.15u lizard POV (2026-06-16).
        // Tight over-the-shoulder: ride right behind the ~0.2u lizard so it clearly
        // fills the lower frame, camera low so the world still towers (2026-06-18).
        // WO-3 (2026-06-24, camera-ui-juice): tightened so the hero fills more of the lower
        // frame and sits bottom-center (it was reading SMALL — a speck on empty pavement).
        // Pulled the rig CLOSER (CamBack 0.34→0.22) and LOWER (CamHeight 0.14→0.105) and
        // narrowed the FOV (62→55 / hFov 58→52) so the lizard renders ~40% larger while the
        // city still towers; the look point sits lower (CamLookHeight 0.06→0.035) so the hero
        // anchors the bottom-center instead of the camera staring at dead pavement above it.
        // The central lane to the safe zone stays open via CamLookAhead (0.5→0.55). The lizard
        // is NOT resized — only the camera moved (POV/FP math untouched, see Fp* below).
        // Hero-prominence pass (2026-06-25, recorded review): the lizard still read SMALL and LOW
        // in frame vs the concept's prominent bottom-centre hero. Pulled the rig closer
        // (CamBack 0.22→0.185 = bigger hero) and dropped the look point (CamLookHeight 0.052→0.036
        // = camera pitches down so the hero rises off the bottom edge into the lower third). Still
        // a low ground-level POV; the city still towers.
        // PORTRAIT-OPEN pass (2026-06-26, owner "looks zoomed in"): the game ships PORTRAIT 9:16
        // but was tuned at landscape. At true 9:16 BaseFov()'s portrait formula WANTS ~82° vertical
        // to hit the 52° horizontal design target, but CamMaxFov=72 CLAMPED it down to only ~44°
        // horizontal — far narrower/zoomed than the open-avenue concept. Raised CamMaxFov 72→84 so
        // the portrait FOV is no longer throttled (84° vertical ≈ 52° horizontal at 9:16, well shy
        // of fisheye for a phone-portrait frame), and pulled the rig back a touch (CamBack 0.185→0.21)
        // so the avenue reads DEEP again. Hero stays prominent bottom-centre (the de-clip below keeps
        // the close low POV from burying into rubble — that was the worst "zoomed brown blob" moment).
        public const float CamBack = 0.21f;
        public const float CamHeight = 0.105f;
        public const float CamLookAhead = 0.55f;
        public const float CamLookHeight = 0.036f;
        public const float CamBaseFov = 55f;
        public const float CamMaxFov = 84f;   // portrait ceiling: lets BaseFov() reach the 52° horizontal target at 9:16 instead of clamping to a narrow ~44°
        public const float CamTargetHorizontalFov = 52f;
        public const float CamDashFovKick = 8f;
        public const float CamTraumaDecay = 1.4f;
        public const float CamMinGroundClearance = 0.1f; // camera never sinks closer than this above the ground it pans over
        public const float CamMaxLateralLead = 0f;     // OWNER 2026-06-28: LOCKED to dead-centre — the camera
                                                       // tracks the lizard's x exactly so the hero stays pinned
                                                       // bottom-CENTRE (no off-centre weave drift). Was 0.13 (a
                                                       // deliberate slide so weaving read on screen); owner
                                                       // prefers the locked framing. Bump >0 to re-allow drift.
        // De-clip: when a solid prop/wall is between the camera and the lizard, pull the rig IN
        // (toward the lizard) so the lens never ends up inside faceted rubble filling the frame.
        // A short sphere of this radius is swept from the lizard back toward the desired cam slot;
        // the first solid hit caps how far back the camera may sit (kept a hair off the surface).
        public const float CamDeClipRadius = 0.08f;     // sweep thickness — a touch under the lizard so it slips through its own gaps but not a rock
        public const float CamDeClipSkin = 0.04f;       // stop this far in front of the blocking surface so the lens stays outside it

        // First-person "lizard cam" (optional POV toggle): the camera rides at the lizard's
        // OWN snout/eye line and looks FORWARD + slightly down, so the real snout sits at the
        // bottom-centre of the frame and the two scurrying front feet splay into the bottom
        // corners — a true ground-level reptile POV down the sidewalk (matches the owner's
        // reference photo). Anchored on the measured model head, not the body centre.
        // Measured against the real model: head/eyes sit ~0.054 forward & ~0.029 up of the
        // lizard origin, snout tip ~0.075 forward. We perch the lens right at the eyes and look
        // nearly straight FORWARD (only a slight tilt down) so the sidewalk fills the frame and
        // the snout drops into the bottom-centre — NOT down at the lizard's back.
        // Offsets are expressed as FRACTIONS of the measured model head (LizardBody.ModelSnoutZ /
        // ModelEyeY) so the POV framing stays correct automatically if the lizard is rescaled.
        public const float FpForwardFrac = 0.67f;  // along the snout axis: lens sits this fraction from origin toward the snout tip → at the eyes, body behind the lens
        public const float FpUpFrac = 1.09f;       // multiple of the measured eye-line height → just barely above the eyes (calibrated to the live rig)
        public const float FpPitchDown = 24f;      // look more down the street (ahead) while the snout + front feet still ride the bottom of the frame
        public const float FpFov = 95f;            // wide, near-fisheye ground-level view (humans tower even more)
        public const float FpNearClipFrac = 0.18f; // tiny near plane (× snout distance) so the very close snout isn't clipped

        // Alley-cat predator: rubber-band chase from behind (−Z). Gap in world units.
        public const float CatStartGap = 14f;      // how far back it begins
        public const float CatCatchDistance = 0.6f;// within this, it swipes (a hit)
        public const float CatMaxGap = 22f;        // never falls further back than this
        public const float CatCloseRate = 1.1f;    // u/s it gains while the lizard dawdles
        public const float CatFallbackRate = 2.6f; // u/s it loses while the lizard sprints/dashes
        public const float CatThreatNearGap = 5f;  // gap at which the danger meter starts filling
        public const float CatHitCooldown = 1.4f;  // after a swipe, can't swipe again for this long
        public const float CatLungeDistance = 0.4f; // how far the cat visual lunges into frame on a swipe

        // Foot-bump: running head-on into a pedestrian leg/shoe. Non-damaging — it
        // staggers the lizard (lose ground) and the first bump wakes the alley cat.
        public const float FootBumpRadius = 0.22f;     // horizontal leg-column hit radius (×ped scale)
        public const float FootBumpCooldown = 0.6f;    // min gap between bumps so one pass can't chain-stagger
        public const float StumbleDuration = 0.5f;     // total stagger/roll length
        public const float StumbleControlLock = 0.3f;  // steering damped for this long at the start of a stumble

        // Faceplant: running head-on into a solid prop (trash can, hydrant, sign) or wall.
        // The lizard splats spread-eagle against it, holds, then peels off and runs on.
        public const float PropHitRadius = 0.28f;      // horizontal contact radius (×prop scale)
        // Pedestrians steer around solid props (ObstacleField) instead of ghosting through.
        public const float PedAvoidRadius = 0.5f;      // a walker's own side clearance need
        public const float PedAvoidLookahead = 3.2f;   // how far ahead a walker reacts to a prop
        public const float FaceplantDuration = 0.6f;   // total splat→recover length
        public const float FaceplantControlLock = 0.45f; // steering frozen for this long

        // Hazards / feel (scaled to the realistic lizard, 2026-06-16)
        public const float CloseCallRadius = 0.3f;
        public const float StompKillPad = 0.03f;       // margin trimmed off the sole: edges are forgiving
        public const float MinWarningLead = 0.7f;      // guaranteed telegraph lead (s) before any footfall lands
        public const float HitStopDuration = 0.07f;
        // A full-speed faceplant into a wall/prop is the hardest non-death impact — give it a
        // longer freeze-frame than a glancing stomp so the wall reads as a WALL (hit-juice pass).
        public const float FaceplantHitStop = 0.11f;
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
