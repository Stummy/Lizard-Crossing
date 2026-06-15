# Lizard Crossing — Claude Context

**Permanent project context lives in `Lizard_Crossing_Claude_Work_Packet/`.
Read `Lizard_Crossing_Claude_Work_Packet/00_READ_ME_FIRST/CLAUDE_INSTRUCTIONS.md`
before doing any work.** Do not ask the owner to re-explain the game idea.

## Non-negotiable rules (from the packet)
- The lizard moves **forward (+Z)** toward the safe zone; primary hazards move
  **sideways (±X)** across its path, like cross-traffic. Never parallel walkers.
- The camera is the most important feature: very low third-person lizard POV
  (camera y < 3), lizard bottom-center, hazards must feel giant.
- Build in phases. No cosmetics/shop/ads/multiple lizards/level select/daily
  challenges until the Phase 1 vertical slice feels good.
- Review every change against `Lizard_Crossing_Claude_Work_Packet/01_GameDesignDocs/QUALITY_BAR.md`
  and update `Lizard_Crossing_Claude_Work_Packet/04_ReviewChecklists/BUG_AND_GAP_LOG.md`.

## Project facts
- Unity **6000.4.10f1**, Built-in RP (URP planned for the art pass), portrait mobile.
- The Boot scene (`Assets/Scenes/Boot.unity`) contains only a `Bootstrap` object;
  the entire world is constructed at runtime from code. Regenerate the scene via
  menu **Lizard Crossing → Generate Boot Scene** or batch
  `-executeMethod LizardCrossing.EditorTools.ProjectSetup.BatchSetup`.
- 3-hearts lives model. Free continuous movement + dash. Bugs are the currency.
- Textures: user-generated Higgsfield art goes in `Assets/Resources/GeneratedArt/`
  (prompts + filenames in `Art/HIGGSFIELD_PROMPTS.md`); procedural fallbacks
  in `ProceduralTextures` keep the game runnable with zero assets.
- Tests: play-mode smoke tests in `Assets/Tests/PlayMode` (adapted from the
  packet); editor validator menu **Lizard Crossing → Validate Phase 1 Scene**
  (run while in Play Mode — the world is runtime-built).
- Decision log: `docs/DECISIONS.md`. Design: `docs/DESIGN.md`. Phases: `docs/ROADMAP.md`.

## Batch verification
```
& "C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe" -batchmode -quit -nographics `
  -projectPath "C:\Users\Family\New Game\LizardCrossing" `
  -executeMethod LizardCrossing.EditorTools.ProjectSetup.BatchSetup -logFile setup.log
```
