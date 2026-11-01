# Mining, fabrication, and fleet progression

Fresh games start with **one Photon lance, one ship mount, no drones, and 80 CR**. Extra weapons are owned instances. The loop is common-credit mining → targeted deep excavation or costly market materials → specialized weapons → independently deployed drones → additional mothership mounts → harder planets. The workshop keeps a live view of the ship and all fitted hardpoints visible while purchasing, fitting, or upgrading.

## Ore and difficulty

Most rocks pay credits only. A deposit retains the underlying rock kind, credit reward and collision; the rare reward travels in a collectible fragment and is awarded once.

| Material | Earliest normalized depth | Deposit units | Additional health | Visual signature | Market buy / sell |
| --- | --- | --- | --- | --- | --- |
| Titanium | 35% | 2 | ×2.8 | Three notched metal bars | 480 / 168 CR |
| Iridium | 60% | 1 | ×4.5 | Nested square lattice | 1,800 / 630 CR |
| Aether | 82% | 1; core guarantees 2 | ×7 | Split orbital brackets and nucleus | 6,400 / 2,240 CR |

Depth is measured from the original generated radius, so tunnels can reach deposits before the whole mantle is stripped. Symbols have dark cut-outs and distinct geometry; they do not rely on color. Pickups retain the corresponding silhouettes. Seeded deep placement and rare-bearing pending rewards survive saving.

Ordinary block health starts at `4.6 + 0.48 × depth in cells`, before rock-kind multipliers. For `n = sector - 1`, planet toughness is `1 + 0.38n + 0.18n^1.45`. Approximate multipliers are 1× / 1.56× / 3.03× / 5.70× / 21.1× in sectors 1 / 2 / 4 / 7 / 20. It continues increasing beyond sector 501. Rare resistance multiplies this health rather than replacing it. Volatile cascades retain their chain behavior but receive a substantially smaller free damage impulse.

Credit richness rises slowly: `1 + 0.07n`. Base stone/gold/other values are 1.5 / 6 / 3 CR; the refinery adds 4% base yield per level up to 50. Sector bonuses start at 150 CR and add 75 per sector, capped after sector 501. The starting lance no longer pierces multiple layers, and old exponential shared damage/cadence bonuses now contribute through diminishing square-root curves. Existing upgrade levels are preserved.

## Weapons

Base damage excludes equipment levels, legacy shared bonuses, manual cadence and overdrive.

| Weapon | Rank | Credits | Ti / Ir / Aether | Base behavior |
| --- | --- | --- | --- | --- |
| Photon lance | 1 | 450 | 2 / 0 / 0 | 1.1 damage to one block every 0.5 s |
| Nova cannon | 1 | 600 | 4 / 0 / 0 | 3.2-center-damage shell, 26-unit blast, every 1.65 s |
| Ion chain | 2 | 4,800 | 3 / 3 / 1 | 1.6 damage across up to three adjacent blocks, every 1.4 s |
| Nova wave | 2 | 6,400 | 8 / 4 / 2 | 104-unit expanding front over 1.6 s, every 4.8 s |
| Bore laser | 3 | 9,000 | 10 / 6 / 3 | 2.6 damage through two blocks every 1.15 s; 2.5× versus rare deposits |

Nova wave strikes each block **once as the front reaches it**. Damage falls from 5.8 at its center to 15% of that at the outer edge. It is not an instantaneous explosion hidden behind an animated ring. Waves are bounded to 16 simultaneous instances, reuse the planet's damage buffer, and pause with gameplay.

Weapon levels add 8% base power and 1.2% cadence per level. A level-20 item therefore has 2.6× damage and 1.24× cadence, without a second exponential multiplier. Weapon upgrade cost is `floor((160 + type × 200) × (level + 1)^1.9)`. Manual play retains a 25% ship cadence advantage. Extra ship mounts use the same cadence modifier and fire alongside the primary when a valid target is in range.

## Ship and engineering milestones

| Rank | Reached sector | Drone bays | Item level cap | Unlocks |
| --- | --- | --- | --- | --- |
| 1 | 1 | 1 | 5 | Photon, cannon, Scout; one ship mount |
| 2 | 2 | 2 | 10 | Ion, Nova wave, Survey; buy mount 2 for 2,400 CR |
| 3 | 4 | 3 | 15 | Bore laser, Atlas; buy mount 3 for 9,500 CR |
| 4 | 7 | 3 | 20 | Higher per-item ceiling |

Hardpoints are installed in **Ship**; reserve weapons are fitted with **Fleet → Weapons → Fit Ship**. Set Primary changes the main selected weapon; Unfit Aux returns an auxiliary to reserve. Swapping an auxiliary into the primary slot preserves the old primary in that auxiliary slot. The ship always keeps a primary. Only the mothership supports simultaneous weapons; a drone has exactly one mount. No weapon instance can occupy two mounts, though duplicate crafted instances of the same type can be used.

## Drones and servicing

| Chassis | Rank | Credits | Ti / Ir / Aether | Power / cadence / reach |
| --- | --- | --- | --- | --- |
| Scout | 1 | 900 | 4 / 0 / 0 | 0.55× / 0.75× / 180 |
| Survey | 2 | 4,200 | 5 / 3 / 0 | 0.8× / 0.85× / 220 |
| Atlas | 3 | 12,000 | 8 / 6 / 2 | 1× / 1× / 245 |

Crafted drones begin unarmed and docked. Fit a reserve weapon and press **Deploy**. Drones choose separated mining routes around the planet rather than following the ship; they intercept the first rock along their weapon path and use the actual shared implementation of all five weapons. A blocked flight route falls back to terrain-aware autopilot movement. Unarmed drones return without mining.

Press **Recall**, or **Ship → Recall All**, before changing a drone's weapon, upgrading its chassis, upgrading its mounted weapon or transferring that weapon to the ship. Deployed/returning drones reject those transactions without spending. Returning drones perform an orbital transfer (no collision damage and no mining during recall), then dock within 12 units of the mothership. Return movement continues during a paused workshop so servicing does not deadlock; mining, projectiles and reward collection stay paused. Docking updates the live schematic and controls. Deploy again after servicing. The deployed/recall order persists across loading; flight restarts near the ship.

Drone levels add 5% mount power, 1% cadence and a small travel-speed increase. Chassis upgrade cost is `floor((380 + type × 450) × (level + 1)^1.9)`. There is no health or destruction mechanic.

## Economy, saves and tuning

Crafting, upgrades, mount transfers and market transactions validate the whole action before spending. Storage holds 24 weapons and three drones; Fleet and Fabricator are paged. The local in-game market buys batches of 1/5 and sells 1 at 35% of purchase value. Market sales do not inflate observed mining income. Offline income remains credits only; it does not consume terrain or create rare materials.

The v1 filename and schema remain compatible. Old equipment saves without auxiliary fields default to one ship mount. Older pre-crafting saves keep their three previously unlocked weapons. Existing tile health and pending rewards remain exact; **new health, rarity and resistance rules apply to newly generated planets**, so old worlds are not silently rewritten. Equipment levels remain intact, with the revised shared-bonus formulas preventing runaway legacy multipliers. Duplicate mounts and malformed payloads reject the entire save before changing state.

These are revised initial tuning values, not a claim of fully playtested campaign pacing. The checks establish weaker early output, bounded upgrades, accelerating hardness, unique mounts, independent missions and correct servicing. Long-session clear times, time to first crafted weapon/drone, and physical mobile performance still need playtesting. Costs/ranks/colors live in `resources/crafting_catalog.tres`; geology and difficulty in `resources/planet_generation.tres`.
