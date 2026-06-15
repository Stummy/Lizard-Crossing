# Imported model drop-zone

Place imported 3D model **prefabs / GLBs** here, named by key, and the game picks
them up automatically (via `ModelLibrary`, with procedural fallback when absent):

| File (any of `.glb` / `.prefab`) | Replaces |
|---|---|
| `lizard`    | the procedural gecko player body |
| `sneaker`   | the procedural shoe hazard |
| `wheel`     | rolling wheel/tire hazard |
| `drain`, `trashbin`, `chair`, `sign`, `planter` | corridor props |

Rules:
- Keep the file name exactly the key (e.g. `sneaker.glb`).
- Models import with their own scale; `ModelLibrary` normalizes each to the game's
  target footprint, so you don't need to pre-scale.
- Every imported asset's source + license must be recorded in the project-root
  `ATTRIBUTION.md`.
