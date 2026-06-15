# SELF TESTING PLAN

Claude cannot magically know if the game feels good unless the project gives it ways to inspect and test.

Create these testing layers:

## 1. Unity Play Mode smoke test
Checks if the game can start, spawn player, find camera, find game manager, and run without immediate errors.

## 2. Scene validation test
Checks the Phase 1 scene has:
- Player
- Main Camera
- GameStateManager
- SafeZone
- at least one hazard lane
- at least one collectible
- HUD canvas

## 3. Directionality test
Checks hazards move sideways across the player route.
If player route is Z-forward, hazards should mostly move along X.

## 4. Console error check
Claude should review and fix:
- compile errors
- missing references
- null references
- duplicate class names
- broken prefabs

## 5. Screenshot review
Claude should capture or request a screenshot and compare against QUALITY_BAR.md:
- tiny lizard bottom center
- giant hazards
- sideways crossing
- safe zone ahead
- readable HUD

## 6. Manual play checklist
Even if automated tests pass, human review still matters:
- does it feel fun?
- does it look polished?
- does it feel like lizard POV?
- are hazards fair?
