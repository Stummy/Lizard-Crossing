# Whole-project review (Gemini) — clip run.mp4, concepts ['run', 'win', 'gameover', 'nearmiss', 'squished', 'faceplant', 'title']

## WHERE WE STAND

The project has a clear vision and a robust plan, with commendable progress on foundational mechanics and core systems like crosswalk traffic. However, the current build, as seen in the gameplay clip, remains significantly distant from the "polished, cinematic" visual quality and immersive game-feel of the concept art. While the basic auto-running and environmental confinement are functional, the world still largely reads as greybox and generic, the hero gecko lacks crucial animation fidelity, and the camera frequently obstructs the action, directly hindering the promised "speck's-eye" cinematic experience. Crucially, the "Target look locked (M1)" milestone is at risk without a focused push on core visual and game-feel elements.

## THE BIGGEST GAPS (ranked)

1.  **Missing Cinematic Visual Fidelity & NYC Character:**
    *   **What's wrong:** The current build's environment (0:00-0:04) is dominated by flat grey/blue buildings and generic surfaces. The yellow car is basic, not a stylized NYC cab. The lighting, while neutral as requested, lacks the subtle bloom, refined DoF, and cohesive palette of the concept. The ground texture appears stretched and low-res in the near-field (0:00).
    *   **Why it matters:** This is the #1 gap against "M1 — Hits the target look" and the "stylized-realistic" vision. Without rich visuals, the game fails to establish its NYC identity and cinematic ambition. It reads as a functional prototype, not a polished experience.
    *   **Concrete change:** Prioritize **greybox buildings → detailed NYC facades** (S3, `environment-artist`). Revisit DoF (R18) and bloom (Style Block) to achieve creamy bokeh and subtle highlight pop. Conduct a full material/texture pass (R19) to implement warm stone sidewalks and high-res ground textures. Source/generate a stylized yellow NYC cab model (R22).

2.  **Lizard & Pedestrian Presentation and Scale:**
    *   **What's wrong:** The hero lizard's animation is stiff and slidey (0:00) rather than a dynamic scuttle, and its low-detail model still reads as "frog-like" (R2, R3). Pedestrians (0:00, 0:01) appear as full, distant figures, not the towering, blurred legs and shoes described in the concept and desired for the "speck's-eye" POV (R10). They also appear to float/slide (R8).
    *   **Why it matters:** The hero and hazards are the core of the player's interaction. If the lizard doesn't feel alive and responsive, and pedestrians aren't imposing giants, the fundamental game-feel is broken. This directly contradicts the concept's emphasis on scale and character.
    *   **Concrete change:** Requires owner authorization for **Lizard section unlock** (S4, asset-scout/gameplay-guardian) to implement the higher-detail, properly animated walking gecko (from `gecko_walk.glb`, addressing R2/R3/R4). For pedestrians, **increase their scale/proximity** and ensure aggressive DoF blurring to achieve the "legs/shoes only" effect, and implement proper footfall animation (R10).

3.  **Obstructive Camera and Missing "Speck's-Eye" Immersion:**
    *   **What's wrong:** The camera frequently clips into props and walls, completely obscuring the lizard and the action (0:02, 0:03). While the low POV is present, the frequent clipping destroys immersion and readability. The camera's general distance also sometimes feels "high/distant" (Gemini, R11) instead of consistently ~3cm off the pavement.
    *   **Why it matters:** The camera is the player's window into the world. Obscured vision is a critical gameplay and aesthetic blocker. The "speck's-eye" POV is a core pillar of the game's identity; it must be consistently maintained and functional.
    *   **Concrete change:** Implement the queued fix for **camera de-clip/occlusion + clear spawn/in-band rubble** (S3/4, `camera-ui-juice`, `environment-artist`). Review and potentially **slightly lower the camera POV** and ensure the camera lead keeps the lizard bottom-centre without drifting (R12), especially on weaves.

4.  **Lack of Impactful Juice and Feedback:**
    *   **What's wrong:** The clip doesn't show hits, but the ledger notes "Collision / hit JUICE" (S4) and "faceplant into a side obstacle reads janky" (S4) as open items. Even the near-miss (0:03, with the yellow car) lacks speed-lines, a "!" flash, or any impactful visual/audio feedback (R27). The HUD isn't visible in the clip, but the concept shows premium, clear HUD elements including popups, which were flagged as too large (R26).
    *   **Why it matters:** Without strong visual and audio feedback, player actions (dodging, hitting obstacles) feel unsatisfying and unreadable. This is crucial for "game-feel" and player engagement (Stage 4).
    *   **Concrete change:** Prioritize **implementing full juice for all hazard interactions** (S4, `camera-ui-juice`): distinct recoil, screen shake, heart-loss animation, and specific SFX for ped stomps, car impacts, and faceplants (R5, R27). Ensure "near-miss" effects (speed-lines, "!") are punchy and visible (R27). Finalize and **refine all HUD elements** (`camera-ui-juice`), including the remaining bug icon, progress bar elements, and ensuring popups are styled and non-obstructive (R24, R26).

5.  **Unfinished Core Content & Gameplay Loops:**
    *   **What's wrong:** While crosswalk traffic and Central Park finale are checked off, the clip shows a very basic green arch (0:00) as the safe zone placeholder, not the lush Central Park envisioned (R23). The "Alley zone + falling/scattered debris hazard" (S3) is still backlog. The game over / win screens are also backlog (S3/4).
    *   **Why it matters:** Beyond the core run, these elements define the player's journey and provide narrative structure, variety, and satisfying conclusions. A game without polished start, death, and win screens feels unfinished.
    *   **Concrete change:** Integrate the **Central Park finale into the run so it's clearly visible and impactful** as a goal (S5, `environment-artist`). Prioritize the **Alley zone + debris hazard** (S3, `environment-artist`) to add level variation. Move **polished death/win screens** (S3/4, `camera-ui-juice`) to near-term.

## ROADMAP TO CLOSE THEM

**NEAR-TERM (Next few sessions, to hit M1 target look):**

1.  **Camera & Environment De-clipping:** Resolve camera clipping into rubble/walls and clear immediate spawn rubble. (S3/4: `OWNER "why's it zoomed in" = camera BURIES into rubble props` — `camera-ui-juice` + `environment-artist`)
2.  **Cinematic Polish Pass:** Finalize DoF, subtle bloom, and film grain for a cohesive look (R18, Style Block). Address near-field ground texture warp/low-res (R19). (S2: `lighting-post-artist` + `environment-artist`)
3.  **NYC Building Facades:** Replace flat greybox buildings with detailed NYC brownstone/warm stone textures and character. (S3: `Greybox buildings` — `environment-artist`)
4.  **Pedestrian Scale & Visibility:** Increase pedestrian scale/proximity and ensure aggressive DoF to achieve "legs/shoes only" effect. (S2/S4: `gameplay-guardian` + `lighting-post-artist`)
5.  **Core HUD Implementation:** Wire up the progress bar, gecko marker, and bug icon. Finalize popup styling and positioning (R24, R26). (S4: `More HUD via Unity AI`, `HUD top-edge cleanup` — `camera-ui-juice`)

**MID-TERM (Following M1, focusing on core feel and content):**

1.  **Lizard Model & Animation:** Owner-authorize unlock of "Lizard" section. Integrate the Meshy-rigged walking gecko, ensuring it's grounded and stable at low POV. (S4: R2, R3, R4 — `asset-scout` + `gameplay-guardian`)
2.  **Full Juice & Feedback Pass:** Implement distinct impact juice for all hazards (ped, car, faceplant). Refine near-miss feedback (speed-lines, "!", recoil). (S4: `Collision / hit JUICE`, `Faceplant into a side obstacle reads janky`, `near-miss wants more` — `camera-ui-juice`)
3.  **Distinct NYC Car:** Replace the generic yellow car with a stylized NYC cab. (S3: R22 — `asset-scout` + `environment-artist`)
4.  **Alley Zone Content:** Implement lane types and the alley zone with debris hazards. (S3: `Lane TYPES`, `Alley zone` — `environment-artist`)
5.  **Audio Pass:** Add SFX, ambience, and music bed. (S3: `Audio pass` — `asset-scout`)

**BEFORE-SHIP (Stage 4+):**

1.  **Polished Screens:** Implement final start, death, and win screens with statistics and retry/continue flows. (S3/4: `Polished death/win SCREENS` — `camera-ui-juice`)
2.  **Difficulty Curve & Onboarding:** Design and implement game progression, difficulty scaling, and a soft tutorial. (S4: `Difficulty curve`, `onboarding/tutorial` — `gameplay-guardian`)
3.  **Performance Optimization & Builds:** Rigorous on-device performance testing, build pipeline setup, and asset curation for mobile. (S5: `Mobile perf budget`, `Android/iOS build pipeline` — `gameplay-guardian` + `studio-producer`)

## THE ONE THING

**Resolve the Camera Clipping and Occlusion:** The repeated camera clipping into props (0:02-0:03) is the single highest-leverage move. It breaks immersion, obscures gameplay, and directly undermines the "cinematic speck's-eye POV" core vision. Without a reliably clear view of the lizard and its immediate surroundings, no amount of polish or content can truly elevate the experience. This fix is owner-authorized and a prerequisite for effective visual review and playtesting.

## QUICK WINS

1.  **Refine Pedestrian Footfall:** Even with current models, enhance footfall clarity (R8) with brighter floor rings and a wider activation window. This is a low-cost `gameplay-guardian` polish that significantly improves readability.
2.  **Stylize Basic Props:** Quickly replace placeholder `Wet Floor` signs (0:04) and generic street cones with more NYC-themed clutter props from Megascans or CC0 assets, leveraging the existing Megascans pipeline. (S2/S3: `environment-artist`)
3.  **Re-center Lizard on Camera:** Apply the uncommitted `CamMaxLateralLead 0.13 → 0` fix to ensure the lizard stays pinned bottom-center, addressing Gemini's "slides off-center" note (R12). This is a fast `camera-ui-juice` change that improves hero prominence and camera stability. (Pending owner re-approval)
4.  **Basic Safe-Zone Beacon Polish:** Integrate the "tall amber beacon shaft + halo" for the safe zone (R23) more prominently so it's clearly visible down the lane from earlier in the run, rather than just at the entrance. (S2: `lighting-post-artist` + `environment-artist`)
