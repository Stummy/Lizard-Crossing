# PHASE 1 VERTICAL SLICE

## Goal
Create one polished playable level proving the core game fantasy.

## Build only
- one playable lizard
- low lizard POV camera
- one level path
- sideways-moving giant foot hazards
- warning circles/shadows
- collectible bugs
- safe zone finish
- basic win/lose
- simple HUD
- camera shake and impact feedback

## Do not build yet
- cosmetics
- ads
- shop
- multiple lizards
- 50 levels
- daily missions
- leaderboard
- advanced progression

## Phase 1 level concept
Level 3 - Garden Escape

The lizard starts on a stone sidewalk and moves forward toward a lush safe zone garden opening. Giant pedestrians cross sideways across the path. The player must time movement through lanes, dodge shoes, collect bugs, and reach the safe zone.

## Required systems
1. PlayerController
2. LizardCameraController
3. HazardLaneManager
4. SidewaysFootHazard
5. WarningMarker
6. CollectibleBug
7. SafeZoneTrigger
8. GameStateManager
9. SimpleHUDController
10. CameraShake

## Minimum acceptance
- Play Mode starts without errors.
- Lizard can move.
- Camera follows correctly.
- Hazards cross sideways.
- Warning marker appears before stomp/impact.
- Player can die and restart.
- Player can collect bugs.
- Player can reach safe zone and win.
- HUD updates hearts/progress/bugs.
