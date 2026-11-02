# Mining, fabrication, and fleet progression

Fresh games start with **one Photon lance, one ship mount, no drones, and 80 CR**. The opening should become productive quickly, then ask for increasingly expensive investments. There is no wall-clock starter boost: prices, substantial damage increments, mining research and equipment choices produce the curve. Everything remains craftable/upgradable through ordinary mining and the local credit market.

## Opening targets and measured pacing

The first Power purchase costs **25 CR**, and Fire Rate costs **20 CR**. Both fit in the starting wallet, leaving 35 CR. Every Power level now adds 60% base damage: 4× at the first rank cap (level 5) and 13× at the final current rank cap (level 20). The first three Fire Rate levels each add 10% base cadence; later levels add only 2%. Existing Power levels still add 1.2% passive cadence per level. Buying useful first gear is deliberately much cheaper than fully optimizing it.

The seeded guided automatic playthrough purchases every five simulated seconds, buys missing materials at actual market prices, equips/deploys real equipment, uses no gifted resources/forced clears and runs the production simulation. It first buys starter Power/Rate, a cannon, cannon upgrades, an armed Scout, a second ship mount/cannon, a Nova wave, then up to five Power levels per fitted weapon and the third mount. It is a fixed purchase plan, not a human playtest or optimal strategy.

| Milestone | Previous measured opening | Current opening |
| --- | --- | --- |
| First Power / Fire Rate | Affordable immediately | Affordable immediately |
| First cannon | 2 min 20 s | 40 s |
| Armed Scout | 4 min 45 s | 2 min 30 s |
| Second fitted ship weapon | 11 min 45 s | 5 min 30 s |
| Nova wave | 22 min 40 s | 9 min 15 s |
| Third hardpoint | 38 min 55 s | 18 min 10 s |
| First planet clear | About 56 min 35 s | About 29 min 15 s |

The current seeded 60-minute run ends in sector 2 with three fitted weapons and one drone. These figures include the strengthened weapon output, current impact charges and clearance recovery, and are controlled purchase-path measurements, not promises for every player. The cheap Flux beam is an alternative first craft (115 CR including market Titanium); this particular plan chooses the cannon. Later upgrades, deep materials and planet toughness retain their accelerating costs/resistance. Full campaign tuning still needs human playtesting.

Run `python3 tools/dev.py balance --label current --minutes 60` to reproduce the profile. Reports go only to ignored `builds/qa/balance-*.json`; `balance-weapon-output.json` records the current run. Historical comparison artifacts are `balance-before.json` and `balance-tuned-three-hours.json`. No player save is loaded or written.

## Manual productivity

Held manual mining gives **1.5× ship hit damage and 1.6× firing cadence**, or **2.4× nominal ship weapon output** with the same loadout. It applies to the primary and both auxiliary ship mounts, including shells, expanding waves and time-integrated continuous beams. Navigation still requires a held pointer and safe clearance; releasing does not continue mining automatically. Manual travel is faster as before.

This is actual damage/shot scheduling, not a credit multiplier. A controlled 60-second starter-loadout corridor test using real injected held-touch events mined **86 blocks / 172 CR manually**, versus **40 blocks / 80 CR on autopilot**. Actual gain depends on aim, terrain, movement, block health breakpoints and loadout. The auto-mode call to action and manual telemetry explain the benefit. Drone damage/cadence is unchanged by the selected ship mode, so toggling to manual and leaving the ship idle does not buff the fleet.

## Ore and planet difficulty

Starting radius is 54 cells; later radius is `54 + 6 × log2(sector)`, with dynamic grid extents. Existing saved worlds retain exact health, tiles and pending rewards; new geology applies only on generation. Most rocks pay credits only. Material deposits keep the underlying rock kind, collision and credit reward; rare rewards travel in collectible fragments and pay once.

| Material | Earliest normalized depth | Units | Extra health | Signature | Market buy / sell |
| --- | --- | --- | --- | --- | --- |
| Titanium | 35% | 2 | ×2.8 | Plain silver square | 80 / 28 CR |
| Iridium | 60% | 1 | ×4.5 | Violet reactor cell | 1,200 / 420 CR |
| Aether | 82% | 1; core guarantees 2 | ×7 | Amber reactor cell | 4,200 / 1,470 CR |

Depth is measured from the original radius, so manual tunnels can reach deposits before stripping the mantle. Cells use a simple color code with a shared fusion-like effect only on selected energy materials. Base ordinary health is `3.2 + 0.25 × depth in cells`, before rock-kind and material factors. Stone/gold/other base values are 2 / 8 / 4 CR. This reduces opening friction without removing hard inner deposits.

For `n = sector - 1`, toughness remains `1 + 0.38n + 0.18n^1.45`, while richness rises only `1 + 0.07n`. Planet size also grows. Resistance therefore increasingly outpaces per-rock income. The refinery adds 4% base yield per level up to 50. Sector bonuses start at 150 CR and add 75 per sector, capped after sector 501. Red charges now detonate on hit with a 32-unit radius and 12 × sector toughness center damage, falling to 45% at the edge; new charge probability is 1.5%. This replaces the faint on-destruction impulse; shared legacy damage/cadence levels retain their diminishing square-root contribution.

## Crafting

| Weapon | Rank | Credits | Ti / Ir / Aether | Behavior |
| --- | --- | --- | --- | --- |
| Photon lance | 1 | 80 | 1 / 0 / 0 | 1.1 damage to one block every 0.5 s |
| Nova cannon | 1 | 50 | 1 / 0 / 0 | 3.2-center-damage shell, 26-unit blast, every 1.65 s |
| Ion chain | 2 | 180 | 2 / 0 / 0 | 3.2 damage over up to three adjacent blocks, every 1.4 s |
| Nova wave | 2 | 400 | 3 / 1 / 0 | 104-unit expanding front over 1.6 s, every 4.8 s |
| Bore laser | 3 | 2,400 | 5 / 2 / 1 | 4.4 damage through two blocks; ×2.5 versus rare deposits; every 1.15 s |
| Flux beam | 1 | 35 | 1 / 0 / 0 | Continuous 6.8 base damage/s; exact held-pointer aim; first solid face stops the beam |

The first cannon costs 130 CR including one market Titanium. The Flux beam costs 115 CR including its material. Ion no longer requires Iridium, and Nova wave no longer requires Aether. Equipping a new primary releases the starting lance to arm the Scout. There are no free additional weapons. Nova wave hits each block once as its front arrives; damage falls from 5.8 at the center to 15% at its edge. Its translucent blue body has a rough edge and three trailing circular fronts. Reused drawing buffers keep the visual separate from the circular damage radius. Flux beam damage integrates elapsed time; Power/Rate/overdrive and the manual bonus apply without turning it into separated pulses. Its direction passes through the held pointer rather than the navigation obstacle center, and it stops on release/cancellation, modals or pause. Both weapons work on auxiliary and drone mounts. The beam carries unused frame energy to newly exposed rock after a destruction, preventing overkill and frame-rate losses. At Power 5/Rate 0 it clears 41 of the deepest ordinary sector-one rocks (16.70 HP each) in ten seconds; at Power 20/Rate 0 it clears 44 sector-four rocks (59.60 HP). These held-input results match at 15/30/60 fps, without shared combat upgrades or overdrive. Fire Rate can improve output further; rare deposits, later sectors and travel still constrain actual mining. One dedicated seamless PCM loop covers all active beams and stops on release/pause/mute; no per-frame one-shot audio is spawned. Effects remain bounded and pause with gameplay.

| Chassis | Rank | Credits | Ti / Ir / Aether | Power / cadence / reach |
| --- | --- | --- | --- | --- |
| Scout | 1 | 180 | 2 / 0 / 0 | 0.55× / 0.75× / 180 |
| Survey | 2 | 4,200 | 5 / 3 / 0 | 0.8× / 0.85× / 220 |
| Atlas | 3 | 12,000 | 8 / 6 / 2 | 1× / 1× / 245 |

## Research, mounts and later investment

Engineering rank uses the higher of reached sector or **lifetime rocks mined**, so giant planets do not delay every unlock until their final block. The existing saved `total_mined` counter supplies research progress; no new XP save field is required. A toast announces each unlock and open workshop recipes refresh without re-ignition.

| Rank | Sector OR total mined | Drone bays | Independent Power/Rate cap | Unlocks |
| --- | --- | --- | --- | --- |
| 1 | 1 / 0 | 1 | 5 | Photon, cannon, Flux beam, Scout; one ship mount |
| 2 | 2 / 150 | 2 | 10 | Ion, Nova wave, Survey; mount 2 costs 600 CR |
| 3 | 4 / 1,200 | 3 | 15 | Bore, Atlas; mount 3 costs 6,000 CR |
| 4 | 7 / 5,000 | 3 | 20 | Higher item ceilings |

Upgrade prices for type 0 are Power **25 / 55 / 110**, Fire Rate **20 / 45 / 95**, chassis **60 / 140 / 300** for their first three purchases. Each is multiplied by `1 + 0.75 × type` and rounded down. For current level `L >= 3`, cost is `floor(B × 1.4^(L - 3) × (L - 2)^1.15 × (1 + 0.75 × type))`, with `B = 200 / 160 / 500` for Power / Rate / chassis. Thus later purchases require more investment while Power continues adding 60% base damage per level. The full rank-4 Power bonus is 13×; separate Rate tuning is 1.64×. Invalid authored costs reject purchases without spending or partially applying them.

Hardpoints are installed in Ship; Fleet → Weapons fits auxiliaries or changes the primary. The ship always keeps one weapon, and all fitted ship weapons fire together. Each drone has exactly one unique mount. A fitted-hull schematic remains visible throughout upgrading. Drones deploy on independent routes and require recall/docking for chassis upgrades, mounted-weapon upgrades and refits. Recall movement can continue during paused servicing without mining or collecting rewards. Missions and optional rate levels persist. Chassis levels retain +5% mount power / +1% cadence and small travel-speed increases.

## Persistence and limits

All crafting, trades and upgrades validate before spending. Storage holds 24 weapons / 3 drones. Market selling pays 35% and cannot inflate offline mining income. Offline catch-up remains credits only and creates no rare resources. The v1 filename/schema, exact saved terrain, pending rewards, ownership, and saved upgrade levels remain compatible. Earlier equipment saves default new rate fields safely; older pre-crafting saves retain previously unlocked weapons. Invalid or duplicate mounts reject the full save before mutation.

Prices/rank milestones live in `resources/crafting_catalog.tres`; early/late upgrade curves and hardpoint prices in `resources/progression.tres`; geology/difficulty in `resources/planet_generation.tres`. The scripted measurements verify the revised opening and actual manual incentive; they do not establish optimal choices, full campaign balance or physical mobile performance.

Only Default and Original are supported. Rock faces, damage colors, rims and ordinary debris change hue between sectors; resource color codes remain fixed. Sector-derived color is deterministic on reload and does not modify geology or health. Older removed style IDs still fall back safely.
