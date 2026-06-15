# Lizard Crossing

A level-based 3D mobile arcade game: a tiny lizard crosses giant human sidewalks
from a dramatic low POV, dodging sideways-crossing foot traffic to reach a safe
zone. Vision and quality bar live in `Lizard_Crossing_Claude_Work_Packet/`.

**Engine:** Unity 6000.4.10f1 · **Target:** iOS / Android (portrait) · **Status:** Phase 1 vertical slice — *Garden Escape*

## Opening the project

1. Unity Hub → **Add** → select this `LizardCrossing` folder → open with **6000.4.10f1**.
2. Open `Assets/Scenes/Boot.unity` (if missing: menu **Lizard Crossing → Generate Boot Scene**).
3. Press **Play**, then click/tap to start.

Everything is procedurally generated at runtime — meshes, textures, audio, UI —
so the project runs with zero imported assets. Drop Higgsfield-generated
textures into `Assets/Resources/GeneratedArt/` to upgrade the visuals
(prompts + file names: [Art/HIGGSFIELD_PROMPTS.md](Art/HIGGSFIELD_PROMPTS.md)).

## Controls

| Action | Editor / Desktop | Mobile |
|---|---|---|
| Move | WASD / arrows | drag anywhere (virtual joystick) |
| Dash | Space | DASH button |
| Start / confirm | click | tap |
| Screenshot (QUALITY_BAR test) | F12 | — |

## Phase 1 contents

- Procedural gecko with code gait animation, squash & stretch, hit blink.
- Garden Escape: walled tropical stone alley, potted plants, ivy, debris,
  glowing garden safe zone with foliage arch + SAFE ZONE sign.
- 8 sideways foot-traffic lanes (both directions, ramping difficulty), each
  footfall telegraphed by a dark shadow + pulsing red ring.
- Low lizard POV camera: trauma shake, dash FOV kick, near-miss slow-mo, hit-stop.
- 3 hearts, knockback + invulnerability, death/retry, win screen with 3-star rating.
- HUD: hearts, bug counter, progress bar, dash cooldown button, close-call popups.
- Play-mode smoke tests (`Assets/Tests/PlayMode`) incl. the packet's sideways-direction test.

## Verification

- Batch: `Unity.exe -batchmode -quit -nographics -projectPath <here> -executeMethod LizardCrossing.EditorTools.ProjectSetup.BatchSetup -logFile setup.log`
- Tests: Test Runner → PlayMode → run all (or `-runTests -testPlatform PlayMode` in batch).
- In-editor: **Lizard Crossing → Validate Phase 1 Scene** while in Play Mode.

## Docs

- [docs/DESIGN.md](docs/DESIGN.md) — how this codebase implements the packet vision.
- [docs/DECISIONS.md](docs/DECISIONS.md) — every decision made on the owner's behalf.
- [docs/ROADMAP.md](docs/ROADMAP.md) — phases to a shippable game.
- [CLAUDE.md](CLAUDE.md) — permanent context for future Claude sessions.
