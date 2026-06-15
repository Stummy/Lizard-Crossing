# CLAUDE INSTRUCTIONS - READ BEFORE WORKING

You are helping build a fresh Unity mobile game called Lizard Crossing.

## Permanent game vision

Lizard Crossing is a level-based 3D mobile game where the player controls a tiny lizard crossing sidewalks, curbs, patios, and boardwalks from a dramatic low lizard POV.

This is NOT open world.

Each level is a short crossing challenge with:
- start point
- forward route
- sideways-moving hazards
- collectibles
- finish safe zone
- win/lose state

## Core fantasy

The player is tiny. The world is huge.

Human feet, sneakers, sandals, boots, scooter wheels, stroller wheels, cars, bikes, puddles, sidewalk cracks, leaves, gum, crumbs, curbs, drains, dogs, birds, and shadows should feel massive compared to the lizard.

The game should feel like a completely revamped, high-graphics, lizard POV version of Crossy Road, but level-based and more cinematic.

## Camera rule

The camera is the most important feature.

The camera must be:
- very low to the ground
- third-person behind the lizard
- close enough that the lizard is readable near the bottom center
- wide enough to show sideways traffic crossing the lizard's route
- dramatic enough that shoes/wheels feel huge
- not a normal human-height camera
- not an open-world exploration camera

## Movement rule

The lizard moves forward toward the safe zone.

Hazards generally move sideways across the lizard's path, left-to-right or right-to-left.

Do not make the primary pedestrian hazards walk in the same direction as the lizard. The game should read like lane-based crossing with perpendicular danger.

## Development rule

Build in phases.

Do not build:
- cosmetics
- shop
- ads
- 50 levels
- daily challenges
- multiple lizards
- level select
until Phase 1 feels good.

## Before coding

Always:
1. Read the relevant docs.
2. Summarize the current task.
3. List files you will create/modify.
4. Explain the short plan.
5. Then implement only the approved scope.

## After coding

Always:
1. List changed files.
2. Explain how to test in Unity Play Mode.
3. Run or describe relevant tests.
4. Check for console errors.
5. Review against QUALITY_BAR.md.
6. List what is still weak.
