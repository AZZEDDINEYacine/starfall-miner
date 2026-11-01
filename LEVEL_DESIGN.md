# Planet expeditions — level design review

The first thirteen sectors form a named campaign, followed by seeded deep-space expeditions. Each world uses the existing five block types and three weapons. Its shape, crust strength, and ore placement create a different mining decision. The presentation stays flat: square blocks, a small ship, and little visual clutter.

## Intended player experience

- A planet is larger than the auto-mode mining viewport. The camera follows the ship toward the active surface, so the player sees the ship and only part of the planet. Manual flight also follows the ship. Changing modes does not recreate the planet.
- Earth introduces mining and collection. Subsequent layouts add craters, weak seams, volatile pockets, an ore canyon, separated asteroids, layered bands, rings, and angled veins. Rare worlds combine the learned patterns.
- A sector completes only after its remaining blocks are mined, as before. Completion grants credits, iron, and energy. The amount increases with sector number and difficulty; rare worlds pay a premium.
- Mars, Uranus, and the Helios Foundry grant permanent Nova, Ion, and Photon weapon modules respectively. All three original base weapons remain selectable from the beginning. A module increases only its weapon's damage by 12% per tier.
- Iron is a visible, saved completion resource. Spending it is intentionally a later economy/UI decision for the team; it does not change existing credit purchases.

## Campaign

| Sector | World / level name | Layout and decision | Difficulty | Clear reward (credits / iron / energy) | Weapon module |
| --- | --- | --- | ---: | ---: | --- |
| 1 | Earth — Verdant Echo | Soft round crust; follow the gold seam. | 1 | 150 / 1 / 7 | — |
| 2 | Moon — Pale Scar | Crater openings expose new sides. | 1 | 190 / 1 / 7 | — |
| 3 | Mercury — Cinderwake | Three weak seams cut a hard shell. | 2 | 302 / 4 / 9 | — |
| 4 | Venus — Gilded Veil | Separate volatile pockets; choose a blast. | 2 | 384 / 4 / 9 | — |
| 5 | Mars — Rustfall Rift | Ore-rich diagonal canyon leads inward. | 3 | 585 / 7 / 11 | Nova MK II |
| 6 | Asteroid Belt — Shard Choir | Five islands require retargeting. | 3 | 743 / 8 / 11 | — |
| 7 | Jupiter — Tempest Crown | Hard and rich bands favor weapon switching. | 4 | 1,101 / 12 / 13 | — |
| 8 | Saturn — Halo Requiem | Destructible ring plus inner planet. | 4 | 1,398 / 12 / 13 | — |
| 9 | Uranus — Axial Ghost | Diagonal energy veins reward shot alignment. | 4 | 1,776 / 13 / 13 | Ion MK II |
| 10 | Neptune — Abyssal Blue | Four softer entrances into a dense vault. | 5 | 2,578 / 18 / 15 | — |
| 11 | Rare — Nox Seraph | Obsidian arms and volatile fault lines. | 5 | 4,420 / 28 / 15 | — |
| 12 | Rare — Aurelia-IX | Golden outer lattice around a harder heart. | 5 | 5,614 / 29 / 15 | — |
| 13 | Solar forge — Helios Foundry | Four arms combine pierce, blast, and chain routes. | 5 | 8,186 / 29 / 15 | Photon MK II |

The Sun is represented by a fictional nearby forge, not a mineable natural planet. The two rare worlds are original settings. After sector 13, Nox and Aurelia layouts alternate with new seeds and sector-scaled rewards.

## Balance and technical boundaries

The radius grows from 16.1 to 20 cells; the previous small planets had a radius near 9 cells. This roughly triples to quadruples the mining surface. Per-block toughness therefore rises by 16% per sector rather than the old 27%, before local hard/soft zones. Rewards grow 27% per sector and also scale with difficulty and rarity. These are design values, awaiting a played timing pass; no completion-time or device-performance claim is made here.

The generator uses a deterministic sector seed. The save still writes `user://starfall_save_v1.json`, `version: 1`, and the existing tile fields. Optional `iron` and `weapon_tiers` fields default to zero in older saves. Saved, partly mined legacy planets keep their exact tiles and health. Wider tile coordinates are validated up to 23 cells. The frozen legacy generator remains available only to compare the original generation fixture.

## Acceptance checks for review

1. At phone portrait and landscape sizes, the ship and a useful mining surface are visible; the whole planet does not fit. Steering, fire, and menu touch targets stay usable. A mode switch keeps every live block, projectile, and pending fragment.
2. Each campaign sector generates deterministically, has a core and all intended ore types, and looks different enough to identify from its layout and stone color.
3. Finish a planet once and receive the listed rewards once. Relooking at a cleared save advances once, and reloading never duplicates iron, weapon tiers, or pending debris.
4. Legacy 0.1 and 0.2 saves retain their wallet, blocks, health, weapon, and mode. New saves restore iron and weapon modules.
5. Completion remains reachable on autopilot and manual flight. The longer sectors should feel challenging through route and weapon choice rather than simply high block health.

This branch does not change touch ownership. Input regressions and rendered checks are still required before merging. The user requested no compilation during this pass, so those checks remain pending.
