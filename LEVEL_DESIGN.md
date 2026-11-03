# Starfall Miner — level design branch

## Campaign artwork

The authored campaign contains 100 named planet concepts in ten themed universes. Each universe has ten distinct compositions and a shared visual motif. The complete names, difficulty tiers, reward tiers, and level-specific landmark briefs live in `scripts/LevelDesignCatalog.cs`.

| Universe | Art direction |
| --- | --- |
| The Firstlight Reach | Coastal forests, cloud meadows, luminous valleys |
| The Cinderbound March | Basalt ridges, lava channels, volcanic ruins |
| The Rimeglass Drift | Glacier gorges, auroras, frozen observatories |
| The Pelagic Choir | Ocean currents, coral shelves, abyssal landmarks |
| The Saffron Antiquity | Sand seas, lost observatories, buried cities |
| The Luminous Menagerie | Bioluminescent jungles and alien wildlife |
| The Clockwork Arc | Factories, railways, machine reefs, automatons |
| The Gloam Archive | Haunted keeps, crypts, spectral archives |
| The Mythic Orrery | Floating islands, mythic beasts, star relics |
| The Uncanny Frontier | Playful oddities with clear mining landmarks |

Each universe illustration sheet contains ten original planet illustrations in a 4×3 atlas. `PlanetRenderer` draws the selected atlas cell below the destructible ore surface. A lightly transparent common-stone layer lets the illustrations read during play, and mined tiles reveal more of the landscape; resource cells stay fully colored for readability. The selected art follows the current sector; the renderer clamps at the final authored design after level 100 while the existing endless campaign continues.

Planet size and ore generation remain owned by the current gameplay configuration. The existing radius curve makes later sectors larger. This branch adds visual content and the renderer hookup without changing menus, save data, input, rewards, or secret-universe behavior.

## Scope and review

The level catalog is authored content, not a new level-selection menu. Menu/navigation and secret-universe systems remain unchanged under the agreed level-design-only scope. The existing gameplay progression remains responsible for sector completion, rewards, and save compatibility.

Review the 100 compositions in `assets/level_catalog/universe_01.png` through `universe_10.png`. The first five standalone studies in `assets/level_art/` are retained as art-direction references; the 100-atlas set is used by gameplay rendering.

## Validation

- C# build and the Godot regression suite are required before publishing the branch.
- Confirm the 100 catalog entries are unique and every atlas contains the expected 4×3 layout.
- Inspect the rendered planet at mobile portrait size after importing the atlas assets.
- No changes in this branch alter touch controls, save schemas, or campaign economy.
