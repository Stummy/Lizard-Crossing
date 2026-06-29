# Capture recipe — getting a frame/clip out of the running game

**Single source of truth.** This recipe was duplicated across five studio agents
(`art-director`, `lighting-post-artist`, `environment-artist`, `camera-ui-juice`,
`gameplay-guardian`). It now lives here so there is ONE authoritative copy to keep
correct; those agents should reference this file instead of carrying their own copy.

There are two ways to get pixels out of the game, and **which one you use depends on
what you're judging.** Getting this wrong has cost us real time (see the ⚠️ below).

---

## A) Judging the LOOK (lighting / grade / colour / "does it match the concept") → REAL frames only

Use the **real recorded MP4**, never the RenderTexture capture below.

1. Portrait the Game view: menu **`Lizard Crossing/Bot/Set Game View 9:16`**.
2. Record: menu **`Lizard Crossing/Bot/Record MP4 (14s)`** → writes `Temp/Recording/run.mp4`.
   **The recorder renders the game CAMERA to a RenderTexture each frame** (`RunRecorder.RecordVideo`,
   fixed 2026-06-29), so it works **even when the editor is backgrounded / the Game-view tab isn't
   repainting** — the old `ScreenCapture` path produced a single frozen frame in that case. Trade-offs:
   the **Screen-Space-Overlay HUD is NOT in the clip** (the camera doesn't render it — you see the
   world), and the RT path renders a **touch BRIGHTER than the device** (tone warning below).
3. Wait for the file size to stabilize (a real run is **MBs / hundreds of frames**; a ~20 KB / 1-frame
   file means something went wrong), then: `python Tools/gemini_review.py --state run` (or `win`/…).
4. Read its **BUGS / CONCEPT-GAP / PUNCH-LIST** and the **`## REGRESSION CHECKLIST`** section.

> ⚠️ **TONE is no longer trustworthy on the recorded clip.** Since the recorder now camera-renders,
> the MP4 *is* the brighter-than-device RT path — it has fooled whole passes into thinking the
> lighting was fine (read warm/concept-close on the RT while the real run read dark/night, peds
> glowing white — commit `62ccfff` + memory `capture-real-gameview-for-look`). The clip is accurate
> for **framing / motion / composition / regression items**; for true **TONE**, judge a **foreground
> Game-view ScreenCapture or the real device**. The owner's stated grade preference supersedes either.

> ⚠️ The Gemini reviewer is **unreliable on lighting** (it has hallucinated "streetlights /
> night" that don't exist). Weight its concrete BUG reports + measured frames over a vague
> vibe label, and treat the **owner's stated preference as superseding the spec**.

---

## B) Checking GEOMETRY / composition / "is the prop there / is it magenta" → RT capture is fine

`Unity_Camera_Capture` FAILS on the low-POV game camera ("Failed to render scene preview").
Instead render `Camera.main` to a RenderTexture and write a PNG via `Unity_RunCommand`, then `Read` it.
**Fine for spotting gross issues (a missing/clipping prop, a magenta material, framing); NOT for tone.**

```csharp
// Unity_RunCommand body. Class MUST be `internal class CommandScript : IRunCommand`.
// Sandbox can't use System.Reflection (and avoid HashSet/ISet). System.IO is OK.
var cam = Camera.main; int w = 540, h = 960;
var rt = new RenderTexture(w, h, 24); var pt = cam.targetTexture; var pa = RenderTexture.active;
cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
cam.targetTexture = pt; RenderTexture.active = pa;
System.IO.File.WriteAllBytes("C:/Users/snpvi/Lizard-Crossing/Temp/Shots/shot.png", tex.EncodeToPNG());
```

Then `Read` the PNG to view it.

### Driving the run while you capture
- Menu seams: **`Lizard Crossing/Bot/Start Run`** → `Move Forward` (and `Toggle POV`,
  `Move Fwd+Left`/`Move Fwd+Right`, `Dash`, `Release Override`).
- For clean mid-run stills, set `Time.timeScale = 0.25f` before the run and restore `1f` after.
- Use a **fresh Play session** to reset run state.
- Always check **`Unity_ReadConsole`** for errors/exceptions after a capture.

---

## Machine gates (assert, don't eyeball)
These write PASS/FAIL files you read instead of looking at a frame:
- **`Lizard Crossing/Bot/Invariant Check`** → `Temp/Playtest/invariant.txt` (R28 confinement + straight band).
- **`Lizard Crossing/Bot/Revive Check`** → `Temp/Playtest/revive.txt` (R33 — revive doesn't regrow the tail).
- **`Lizard Crossing/Bot/Auto-Playtest (8 runs)`** → `Temp/Playtest/report.txt` (reaches the safe zone; console-error count).

> See `CO-OP.md` for how these slot into the per-change loop (specialist gate → Gemini gate →
> machine gate → commit).
