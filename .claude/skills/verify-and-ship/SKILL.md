---
name: verify-and-ship
description: The Lizard Crossing "done" loop — run after every completed unit of in-engine work before calling it done. Compiles clean, runs a bot playthrough to the safe zone, runs the scene validator, gates on zero console errors, captures a proof frame, then commits and pushes to GitHub. Prompts the owner to run /code-review ultra at sprint/stage gates.
---

# verify-and-ship — the standard "done" loop

Run this after EVERY completed unit of work, BEFORE calling it done. It is the
verification layer so "the owner approved it" never means "nobody checked it."
The owner judges *feel* (taste/vision); this loop checks *correctness*.

## Steps
1. **Compile clean.** `Assets/Refresh` → poll editor state until `IsCompiling=false` →
   `ReadConsole` (Errors) must be **0**. Fix any error before continuing.
2. **In-engine smoke.** Enter Play → run the bot harness (`Lizard Crossing/Bot/Start Run`
   + `InputProvider` override) → confirm it reaches the safe zone (`GameStateManager.State
   == Won`) with no NullReferenceExceptions mid-run.
3. **Validate.** Run menu `Lizard Crossing/Validate Phase 1 Scene` while in Play mode;
   read the result.
4. **Console gate.** `ReadConsole` (Errors) must be **0**. If not, fix before proceeding —
   never mark work done with console errors or a failed playthrough.
5. **Proof.** Capture a PNG via the RenderTexture trick (`Camera.main` → `EncodeToPNG` →
   `Temp/Shots/`) and review it. (`Unity_Camera_Capture` fails on the low POV cam — use
   the PNG-render path.)
6. **Restore + stop.** `Time.timeScale = 1`; Stop Play when appropriate.
7. **Ship.** `git add <scoped files>` → commit with a clear message → `git push origin
   feat/realistic-city-crossing`. Keep commits narrowly scoped — see [[commit-and-push-rule]].
8. **Gate check.** If this closes a sprint / stage / milestone (see `docs/PROJECT_PLAN.md`),
   or before any merge to `main`, **remind the owner to run `/code-review ultra`**. Claude
   CANNOT launch it — it is owner-triggered and billed — so prompt; don't attempt it.

## Notes
- The bot harness + PNG-capture trick are the reliable path — see memory
  [[lizard-bot-playtest-harness]] and [[unity-execute-code-broken]].
- Slow-mo mid-run frames: set `Time.timeScale = 0.35` before capture, restore to `1` after.
- This loop is also invokable directly by the owner as `/verify-and-ship`.
