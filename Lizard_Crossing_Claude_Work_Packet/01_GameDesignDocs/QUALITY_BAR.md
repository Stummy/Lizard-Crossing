# QUALITY BAR

This prevents the game from turning into a cheap prototype.

## Screenshot test
One screenshot must clearly show:
- tiny lizard at bottom center
- huge human-world danger
- hazards crossing sideways
- safe zone ahead
- clean HUD
- strong scale

If a screenshot does not communicate the game in 3 seconds, the build is not good enough.

## 5-second test
A viewer should understand:
"I am a tiny lizard crossing through giant moving hazards."

## Camera test
Pass:
- camera is close to the ground
- lizard is readable
- hazards feel huge
- safe zone is visible ahead

Fail:
- camera is too high
- lizard looks human-sized
- hazards do not feel giant
- scene looks like a normal runner

## Movement test
Pass:
- lizard moves forward responsively
- player can steer/dodge
- hazards cross perpendicular to lizard movement
- near-misses feel exciting

Fail:
- hazards move in same direction as lizard
- controls feel floaty
- player cannot read what is dangerous

## Hazard test
Pass:
- warning marker/shadow appears before danger
- hazard timing is fair
- collisions are reliable
- shoes/wheels feel threatening

## Visual test
Pass:
- detailed pavement
- meaningful props/debris
- lighting improves readability
- scene has a strong theme

Fail:
- empty scene
- cone-only obstacles
- default Unity look
- unclear finish

## Code quality test
Pass:
- modular scripts
- no duplicate systems
- clear responsibilities
- easy to add new levels/hazards later

Fail:
- giant messy script
- hardcoded everything
- duplicate controllers
- console errors ignored
