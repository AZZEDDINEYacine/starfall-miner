# Space artwork — 3 November 2026

Generated with the built-in imagegen tool after the user's approval of the blue/cyan direction with violet accents. The supplied references informed composition, materials and lighting; their image files and lettering are not bundled. No CLI fallback, additional package or runtime service is used.

| File | Source dimensions | Use |
| --- | --- | --- |
| `nebula-blue-violet.png` | 1254 × 1254 RGB | Static cover-cropped galaxy, subdued panel/drydock texture |
| `basalt-surface.png` | 1254 × 1254 RGB | Continuous surface sampled only on surviving ordinary Default tiles |
| `ringed-world.png` | 1536 × 1024 RGBA | Cropped distant backdrop body; original alpha retained |

All three images were visually inspected as assets. The ringed-world PNG has a non-opaque alpha channel. No game rendering, Godot imports or performance checks were run by the assistant. The square texture is mapped once across the planet; no reliance on seamless tiling is required. Runtime controls use the small authored SVG nine-slice surfaces in `assets/ui/`, keeping labels native and sharp.

## Exact generation prompts

### nebula

Transparent background: false.

Use case: stylized-concept. Asset type: original production background texture for the Godot game Starfall Miner, square composition. Create a richly detailed cinematic deep-space nebula: luminous blue and cyan dust filaments and scattered fine stars, violet gas in the darker margins, near-black navy depth. Inspired by the lighting and sophistication of premium space editorial art. The bright structures form a loose diagonal around the outer thirds; the central gameplay area stays darker and spacious. Dense delicate detail, believable volumetric dust, a few fine bright stars, elegant contrast. Full-bleed background artwork only. No planets, no spacecraft, no rings, no UI, no typography, no logos, no watermark, no borders. Original artwork, not a copy of a website. Designed to be cropped responsively for portrait and wide desktop screens. Static background.

### basalt

Transparent background: false.

Use case: stylized-concept. Asset type: original square seamless albedo/detail texture for a destructible rocky mining planet in a Godot game. Orthographic top-down lunar basalt surface, photorealistic mineral texture and connected fractured bedrock, weathered rocky ridges, small crater pits and subtle dark mineral veins. Slate blue-gray monochrome with restrained tonal variation. Uniform flat diffuse illumination, material relief baked subtly from upper left, no dramatic large cast shadows. The surface fills every pixel, equal detail across the full square and edges, seamlessly repeatable on all four edges. This is a flat terrain texture to sample continuously across adjacent surviving tiles, not a rendered sphere. No space or sky, no atmosphere, no glowing veins, no cyan markings, no colored ores, no grid, no text, no objects, no UI, no frame. Original high-detail game material.

### ringed_planet

Transparent background: true.

Use case: stylized-concept. Asset type: original transparent-background distant planet cutout for Starfall Miner. One elegant realistic blue-violet ringed rocky/icy planet, completely visible with its complete tilted rings and generous transparent margin. Crisp detailed moonlike surface with craters and glacial ridges, icy cyan rim light from upper left, deep navy shadow across the lower right, subtle violet reflected light. Thin concentric rings sweep diagonally from lower left to upper right and cast a credible narrow shadow on the globe. Restrained luminous atmosphere along the lit limb. Premium cinematic space illustration, physically dimensional, intricate surface detail. Only the ringed planet and its rings, centered as one asset, genuine transparent background, no stars, no surrounding nebula, no square background, no extra moons, no UI, no text, no logos, no watermark. Keep all rings within the canvas.

