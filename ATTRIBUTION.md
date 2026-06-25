# Asset Attribution

Every imported third-party asset is listed here with its source and license, per
the project's licensing rules (CC0 preferred; CC-BY requires attribution; no
unclear/restrictive licenses).

## 3D models

_(Populated as assets are imported by the free-asset pipeline — see
`docs/ASSET_PIPELINE.md`. Each entry: asset name · category · source URL ·
author · license · what it replaces.)_

| Asset | Category | Source | Author | License | Used for |
|---|---|---|---|---|---|
| Spotted Gecko | Lizard hero | https://poly.pizza/m/65Yj7059c2K | Jeff Larson | **CC-BY 3.0** | Player lizard model (`Resources/Models/lizard.glb`) |
| Sneakers | Shoe hazard | https://poly.pizza/m/2cAXk_gG3Eh | Poly by Google | **CC-BY 3.0** | Giant sneaker hazard (`Resources/Models/sneaker.glb`) |
| Car Kit (wheels) | Wheel hazard | https://kenney.nl/assets/car-kit | Kenney | **CC0** | Rolling wheel hazard (pending import fix) |
| Fire hydrant | NYC street prop | Meshy AI (text-to-3D) | Meshy-generated (owner's plan) | **Generated — owned, commercial OK** | NYC street dressing — in-band solid (`Resources/Models/Generated/hydrant.glb`, WO-4) |
| USPS mailbox | NYC street prop | Meshy AI (text-to-3D) | Meshy-generated (owner's plan) | **Generated — owned, commercial OK** | NYC street dressing — curb edge (`Resources/Models/Generated/usps_mailbox.glb`, WO-4) |
| Newspaper vending box | NYC street prop | Meshy AI (text-to-3D) | Meshy-generated (owner's plan) | **Generated — owned, commercial OK** | NYC street dressing — curb edge (`Resources/Models/Generated/newspaper_box.glb`, WO-4) |
| Traffic cone | NYC street prop | Meshy AI (text-to-3D) | Meshy-generated (owner's plan) | **Generated — owned, commercial OK** | NYC street dressing — in-band solid (`Resources/Models/Generated/traffic_cone.glb`, WO-4) |
| Trash bags (pile) | NYC street prop | Meshy AI (text-to-3D) | Meshy-generated (owner's plan) | **Generated — owned, commercial OK** | NYC street dressing — in-band solid (`Resources/Models/Generated/trash_bags.glb`, WO-4) |
| A-frame sidewalk sign | NYC street prop | Meshy AI (text-to-3D) | Meshy-generated (owner's plan) | **Generated — owned, commercial OK** | NYC street dressing — curb edge (`Resources/Models/Generated/aframe_sign.glb`, WO-4) |
| Police barricade | NYC street prop | Meshy AI (text-to-3D) | Meshy-generated (owner's plan) | **Generated — owned, commercial OK** | NYC street dressing — curb edge (`Resources/Models/Generated/police_barricade.glb`, WO-4) |
| Cardboard boxes (stack) | NYC street prop | Meshy AI (text-to-3D) | Meshy-generated (owner's plan) | **Generated — owned, commercial OK** | NYC street dressing — curb edge (`Resources/Models/Generated/cardboard_boxes.glb`, WO-4) |

> **CC-BY 3.0 attribution (required in shipped builds):**
> - "Spotted Gecko" by **Jeff Larson** — via Poly Pizza — CC-BY 3.0
> - "Sneakers" by **Poly by Google** — via Poly Pizza — CC-BY 3.0
>
> A credits screen carrying these is planned for the launch phase; this file ships in the repo meanwhile.

## Textures

| Asset | Source | Author | License | Used for |
|---|---|---|---|---|
| _pending_ | — | — | — | — |

## HDRI / Environment lighting

| Asset | Source | Author | License | Used for |
|---|---|---|---|---|
| Qwantani (Pure Sky) — 2K HDR | https://polyhaven.com/a/qwantani_puresky | Greg Zaal (capture) · Jarod Guest (sky edits) | **CC0** | Outdoor sky HDRI for NYC theme image-based lighting + skybox (`Assets/Art/Imported/HDRI/qwantani_puresky_2k.hdr`) |

## Notes
- CC0 assets require no attribution but are listed here for provenance/traceability.
- CC-BY assets **must** keep their author + source credit in shipped builds
  (this file ships in the repo; a credits screen is planned for the launch phase).
