# Starfall Miner — substantial Power progression and sustained laser audio

Power now adds **60% of base damage per level** across all six weapons: 4× at level 5 and 13× at level 20, replacing the old 1.68×/2.28× multipliers. Rank caps, saved item levels, separate Rate tuning, docking rules and prices remain intact. Flux beam base output doubles to 6.8 damage/s; Ion hits rise from 1.6 to 3.2 and Bore from 2.6 to 4.4, preserving chain/rare-armor behavior. Fresh Photon remains 1.1 damage; cannon and Nova retain their area damage bases. Values are authored in `resources/progression.tres` and workshop Power readouts use the same resource-backed multiplier.

Laser damage remaining after a destruction now continues onto the newly exposed first-hit face within the same frame, with a 32-iteration cap. It cannot damage through surviving terrain, and aim remains precisely aligned with the held pointer. This prevents overkill and frame-rate limits from erasing high-level output.

Measured with real held touch and production movement/damage, no Rate levels, shared combat upgrades or overdrive:

| Power / reference world | Ordinary block HP | Destroyed in 10 s at 15 / 30 / 60 fps |
| --- | --- | --- |
| Level 5 / sector 1, deepest ordinary stone | 16.70 | 41 / 41 / 41 |
| Level 20 / sector 4, deepest ordinary stone | 59.60 | 44 / 44 / 44 |

These are manual-mining throughput floors on reference ordinary rock, not four guaranteed rare/deep late-sector deposits per second. Fire Rate upgrades can increase output further. Remaining geology and growing sector resistance are unchanged; exact saved health is not rewritten.

A single dedicated audio voice now sustains a generated 0.2-second, 22.05-kHz PCM loop (about 8.8 KB). Harmonic carrier/shimmer frequencies have integer loop periods; one voice covers all active primary/auxiliary/drone beams without restarting every frame. Release/control cancellation, pause and mute stop it immediately; unmuting resumes a still-held beam. Muting also stops currently playing one-shot voices.

Validation:

- `python3 tools/dev.py test-rendered`: **366 checks, zero failures**, compiler zero warnings/errors. Log: `builds/qa/weapon-output-regression.log`. Includes the six measured throughput cases, real per-weapon 13× output through all six firing behaviors, equal frame-rate-integrated beam damage, pointer precision/first-face occlusion, sustained-loop single start, release/mute/unmute/pause stops, and all prior save/crafting/docking/input/pilot regressions. The old aiming fixture's destination was outside the viewport at full manual zoom; it now keeps the held point inside the phone window while still testing off-center navigation snapping.
- `python3 tools/rendered_check.py`: **108 rendered input checks passed**, log `builds/qa/weapon-output-live.log`. The live Power-20/Rate-0 fixture mines at least 40 deep sector-four rocks in ten seconds, stays on exactly one audio loop start, and stops audio on release. Existing scaled engine-mouse, touch/drag, menu, recall, exact pickup and terrain-handoff checks pass. Live runtime logs have no errors/warnings. Malformed-save warnings in the regression suite are intentional rejection tests.
- `python3 tools/dev.py balance --label weapon-output --minutes 60`: completed. Guided automatic plan reaches cannon **0:40**, armed Scout **2:30**, second ship weapon **5:30**, Nova **9:15**, third hardpoint **18:10**, sector 2 around **29:15**. It ends with three fitted weapons, one drone and 316 sector-two blocks remaining. Artifacts: `builds/qa/balance-weapon-output.json` and corresponding log. This is a fixed purchase plan with real transactions, not optimal play/full campaign proof.
- Inspected `builds/qa/laser-max-power-mining.png` and `onboarding-starter-upgrades.png`: exact straight beam/contact, ongoing held mining and readable +60% Power feedback remain visible. Python syntax and `git diff --check` pass.
- All persistence/inspection stayed in isolated test state. No player save was accessed; no APK/AAB or native package was built. Audio validation checks the actual engine loop state and restart count; speaker/headphone listening quality and physical Android audio/performance have not been evaluated. Rendered checks are desktop/injected input, not physical phone testing.

---

# Historical: pause-menu test funds

Pause now includes **TEST / +1B CR**, with the current wallet shown on the button. Each activation adds exactly 1,000,000,000 credits and uses the normal save path in ordinary play. Grants do not change mining income, lifetime earnings, research, materials or terrain. The pause card is taller to keep the new action separate from Resume and reset controls.

- Local C# build passed with zero warnings/errors: `builds/qa/test-credit-button-build.log`.
- Six targeted rendered checks passed: phone-sized control separation, canceled touch grants nothing, one-billion single-touch grant despite synthesized mouse events, retained pause/terrain/research, repeat grants, and isolated test storage. Log: `builds/qa/test-credit-button-live.log`. No new automated regression suite was added for this small UI action.
- Inspected `builds/qa/test-credit-button.png`: the action and updated 2.0B wallet fit at 390×844. Runtime log is clean; `git diff --check` passes. This was injected desktop touch, not physical Android testing. No player save was accessed and no APK/AAB was built.

---

# Historical: pocket clearance, precise continuous laser and earlier weapons

The local autopilot now has a clearance watchdog. If the preferred mining stand-off cannot be reached and positional progress stays below 12 units for 1.1 seconds, it cuts exposed forward first-hit faces from the available position. The normal retreat still runs first. This widens a tight pocket rather than waiting indefinitely; it does not use a planet/global exit map, hidden ore or the minimap. Swept collision, speed bounds, occlusion and heading alignment stay enforced, including inherited overlap recovery.

Flux beam is added as weapon type 5 without changing existing IDs. It crafts at rank one for 35 CR + one Titanium (115 CR with market material), and uses time-integrated 3.4 base damage/s. In manual mode the direction passes precisely through the held pointer independently of navigation's obstacle-center aim and the ship's turn. Exact grid traversal ends the beam on the first actual tile face. It stays continuous on every held frame and extinguishes immediately on release/cancellation, modal opening or control reset. Power, Rate, overdrive and the 2.4× manual ship bonus apply; auxiliary and independently mining drone mounts use the same continuous behavior. No beam state is persisted, and older v1 saves retain their IDs/defaults.

Cannon is now 50 CR + one Titanium; Ion 180 CR + two Titanium; Nova wave 400 CR + three Titanium + one Iridium; Bore 2,400 CR + five Titanium + two Iridium + one Aether. Nova no longer waits on deep Aether. Research thresholds become 0/150/1,200/5,000 mined rocks or sectors 1/2/4/7. Later upgrade curves and planet toughness still rise. In the fixed guided automatic 60-minute profile, cannon arrives at **0:45**, armed Scout at **2:55**, second fitted weapon at **7:20** and Nova wave at **10:55**; prior measurements were 2:20, 4:45, 11:45 and 22:40. Sector two begins around 42:20. These are current seeded production-simulation measurements with actual purchases, not a guarantee for every play style or an isolated attribution to prices alone.

Nova wave now uses translucent blue fill, a rough luminous boundary and three trailing outward circles. Two reused drawing buffers serve all active waves; the circular expanding damage front, falloff and one-hit-per-block behavior remain. Both terrain designs change rock/damage-rim/common-debris hue between sectors using a deterministic sector-derived shift. Material, resource and red-charge color identities stay fixed; style resources and geology are not mutated.

Validation:

- `python3 tools/dev.py test-rendered`: **349 checks, zero failures**, build zero warnings/errors. `builds/qa/beam-clearance-regression.log`. The tight chamber with a one-cell exit reproduces zero mining for 12 seconds when clearance recovery is disabled. Enabled recovery cuts 17–18 cells and moves over 37 units in 40 simulated seconds at 15/30/60 fps, with no hull overlap or speed-bound violations. Existing local pilot, sparse remnants and manually carved handoff checks still pass.
- Beam checks use real Viewport-held touch/drag: exact pointer alignment despite navigation snapping, first-face clipping with an untouched block behind it, empty-space aim, uninterrupted stationary-thumb frames, equal 30/60-fps damage, immediate release/cancellation/modal stop, real auxiliary/drone mining, pause isolation, atomic crafting and v1 round-trip. Existing older save fixtures and transaction rejection checks pass; malformed-save warnings are intentional.
- `python3 tools/rendered_check.py`: **105 rendered input checks passed**, `builds/qa/beam-clearance-live.log`. Includes the trapped-pocket reproduction, exact beam touch/drag, canceled release, engine mouse hold/release at scaled desktop size, three distinct sector hues, and all previous workshop, drone, pickup and handoff checks. Live/runtime logs contain no errors or warnings.
- `python3 tools/dev.py balance --label faster-weapons --minutes 60`: completed with production simulation and isolated no-save state. Report: `builds/qa/balance-faster-weapons.json`; log: `balance-faster-weapons.log`. The plan ends in sector 2 with three fitted ship weapons and one drone. It selects the cannon rather than the alternative cheap Flux beam. Campaign balance still needs human testing.
- Inspected `trapped-pocket-before.png`, `trapped-pocket-widened.png`, `flux-beam-held.png`, `flux-beam-drag.png`, `nova-expanding-wave.png`, `minimap-growing-sector-2.png` and `minimap-growing-sector-4.png` under `builds/qa/`. The pocket visibly widens, the beam is a straight persistent line with a first-face hot spot, Nova shows blue translucency/rough edge/concentric fronts, and the planets show blue versus amber rock with unchanged ore colors.
- Python syntax and `git diff --check` pass. No player save was accessed. Rendered checks use desktop windows and injected touch/mouse, not physical Android hardware. No APK/AAB or native package was built. Physical-phone performance and exhaustive escape behavior for all concavities are not established by this suite.

---

# Historical: color-coded cells and visible impact charges

Only Clean Frontier (Default) and Original (Classic Grid) remain selectable. The other 15 presets and their rendering branches have been removed; a saved removed style ID falls back to Default without changing terrain, health, wallet or pending rewards.

Resources replace their full grid square with a colored face and thin perimeter. Identifying glyphs, stripes and inset rock badges are gone. Titanium and gold remain plain; Iridium, Aether, crystal, core and charge cells share a simplified hot core with two circulating plasma particles. Color distinguishes materials. Animation pauses with the world, culls offscreen cells and caps orbiting particles at 128 without per-cell nodes.

Red charges now detonate on any positive damage, regardless of their remaining health. A dashed target preview shows the actual 32-world-unit (two-cell) radius; a brighter shockwave reaches that radius quickly and fades over 0.65 seconds. Falloff damage breaks nearby shallow rock, chips harder deposits and triggers other charges. New planets use fewer charges (1.5%); legacy rare-bearing volatile cells remain resistant minerals rather than concealed explosives. Chain processing is iterative, with at most 64 visual rings and exact-once rewards.

Validation:

- `python3 tools/dev.py test-rendered`: **329 checks, zero failures**; build has zero warnings/errors. Log: `builds/qa/resource-cells-regression.log`. Added impact ignition, radius/falloff, invalid-damage rejection, a 1,024-charge chain, exact-once rare pickup persistence, single planet-clear notification, removed-style save fallback, paused animation and dense-field particle-budget checks. The lower total reflects removal of the obsolete style comparisons.
- `python3 tools/rendered_check.py`: **98 rendered input checks passed**. Log: `builds/qa/resource-cells-live.log`. Real held input detonates two high-health charges, destroys adjacent rock/mineral cells, preserves a distant block, and collects exactly 16 CR and two Titanium units after fragment travel. Existing injected touch/drag, scaled engine mouse, workshop, drone service and manual-to-auto handoff checks pass. Final visual-only shockwave timing was rebuilt and exercised in this walkthrough.
- Inspected `resource-cells-default-frame-1.png`, `technical-deposits-classic.png`, `world-design-picker.png`, `impact-charge-before.png` and `impact-charge-detonation.png` under `builds/qa/`. Both designs show full color cells with a shared small reactor effect. The picker contains only two designs. The charge preview surrounds the actual affected area; the impact frame shows overlapping expanding rings and cleared cells.
- Python syntax and `git diff --check` pass. Test saves remain isolated; no player save was accessed. These are rendered desktop checks, including phone-sized windows, not physical Android testing. No APK/AAB or native package was built. The earlier pacing measurements below predate the expanded charge damage; campaign balance and physical-device performance remain unmeasured for this change.

---

# Historical: faster opening, later investment and active manual mining

The starting 80 CR now buys Power (25 CR) and Fire Rate (20 CR), leaving 35 CR before mining income. The first three purchases are discounted: Power 25/55/110, Rate 20/45/95 and chassis 60/140/300 for type 0, scaled by type. Initial Power/Rate increments are 20% damage / 10% cadence each; after level three they fall to 4% / 2%. Later prices use a rising 1.4 exponential factor and level power term, independent of wall-clock time. The per-item Power cadence contribution remains. `resources/progression.tres` centralizes costs and ship expansion prices. Invalid authored prices reject transactions atomically.

First cannon is 100 CR + 2 Titanium, Scout 180 CR + 2 Titanium, and Titanium market price is 80 CR. Deeper materials/advanced chassis remain substantial investments. Newly generated ordinary ore starts at 3.2 HP plus 0.25 per depth cell, with 2/8/4 CR base stone/gold/other values. Rare resistance, depth gates, growing planets and the accelerating sector health curve remain. Research uses sector OR existing saved lifetime-mined thresholds 0/250/2,000/8,000, so a giant first planet does not block all equipment expansion. Open recipes refresh when research unlocks and do not re-ignite the workshop. Existing tiles/health/pickups and save version are preserved.

Held manual ship mining now provides 1.5× hit damage and 1.6× cadence (2.4× nominal output) on primary and auxiliary mounts. The benefit is real firing/damage rather than multiplied pickup credit. The same-equipment 60-second corridor check uses actual injected held touch and records **86 blocks / 172 CR manual versus 40 blocks / 80 CR automatic**. Independent drone damage/cadence is unchanged by the ship mode. Automatic-mode call-to-action text and manual mining telemetry show the benefit.

The stronger mining exposed a drone routing issue: a direct approach could repeatedly re-enter a jagged lip immediately after backing away. Drones now commit to a short outside transfer, climbing to clearance before retrying the approach. Recall/docking, single mounts, saved orders, weapon behavior and mode-independent output remain intact. The rendered regression measures an excursion over 1,800 units, mining, clear hull and an idle mothership. Live checks sample the complete excursion rather than requiring a particular separation at exactly the last frame.

Measured pacing (production C# simulation, fixed guided automatic purchase plan, ordinary resource purchases, no gifted resources/forced clears):

| Milestone | Before | Final tuning |
| --- | --- | --- |
| First Power | 5:40 | Affordable at start |
| First Rate | 0:55 | Affordable at start |
| Cannon | 40:05 | 2:20 |
| Armed Scout | Not reached in 60 minutes | 4:45 |
| Second fitted ship weapon | Not reached | 11:45 |
| Nova wave | Not reached | 22:40 |
| Third hardpoint | Not reached | 38:55 |
| Sector 2 | Not reached | 56:35 |

At 60 simulated minutes the final plan has three ship weapons, one drone, sector 2 and about 447 CR/min over the last five minutes, compared with one fitted weapon, no drones, sector 1 and 39 CR/min previously. The final plan was run through 180 minutes and reached sector 3. It buys the first five Power levels per fitted item, not an optimal/unbounded upgrade strategy. These are controlled purchase-path measurements, not promises for every player. Full human campaign/phone balancing remains open.

Validation:

- `python3 tools/dev.py balance --label before --minutes 60` recorded the old curve; `python3 tools/dev.py balance --label tuned-three-hours --minutes 180` records the final first hour and extended run. Output: ignored `builds/qa/balance-before.json`, `balance-tuned-three-hours.json`, and corresponding logs. The opt-in `tests/balance.tscn` requires isolated `--test` state and never invokes player persistence. The original sandboxed baseline had the macOS headless display notice but completed successfully; final extended profile completed without runtime errors.
- `python3 tools/dev.py test-rendered`: **357 checks, zero failures**, compiler zero warnings/errors. `builds/qa/onboarding-rendered-regression.log`. New coverage checks immediate purchases, front-loaded/diminishing increments, accelerating prices, invalid authored-cost rejection, affordable recipes/rare material differentiation, mining research and save persistence, live recipe unlock without ignition replay, rising resistance/income ratio, held-input manual credits/blocks/damage, mode-independent drone output and the tuned cannon drone's jagged-lip transfer. All previous touch/mouse, workshop, crafting, saves, styles, manual handoff and minimap checks remain passing.
- `python3 tools/rendered_check.py`: **124 rendered input checks passed**, `builds/qa/onboarding-live.log`. Added actual starting-wallet purchases, visible upgrade feedback and updated real transaction prices. The legacy unlocked input fixture explicitly restores its original 80-CR wallet after the separate fresh-game purchase check. Drone excursion reaches about 1,851 units with continued mining; recall/service/redeployment still pass. Runtime logs have no errors or warnings.
- Inspected `onboarding-starter-upgrades.png`, `manual-clearing-path.png` and the existing phone-sized workshop walkthrough. Power +20% / Rate +10%, remaining wallet, next costs, fitted hull and manual 2.4× output guidance are visible and contained. Desktop rendering and injected touch at 390×844/540×960 are not physical Android testing.
- Python syntax and `git diff --check` pass. No player save was read/written; no APK/AAB or native package was built.

`PROGRESSION.md` records recipes, milestone rules, cost formulas, measurement methodology and pacing limitations. `tools/dev.py balance --label current --minutes 60` reproduces an isolated profile for further tuning.

---

# Historical: local reactive pilot, survey minimap and growing planets

The mothership's automatic flight no longer uses the planet center, global nearest-ore selection or a prescribed orbit. It scans a forward first-hit cone, holds a visible target, approaches/brakes to an 88-unit mining distance, aligns before firing, separates from nearby hull hazards, and uses two-unit swept movement with local slide/backoff. Slight inherited corner overlaps use continuous penetration-reducing recovery. If forward contact is lost it turns to look around, then surveys expanding legs using odometry. Small changing lateral corrections keep mining locally rather than circling. Only visible fronts can be selected/fired upon; hidden ore and the minimap do not supply destinations. Drone outside-transfer routing remains separate. Authored rules are in `resources/autopilot_flight.tres`.

Starting planet radius is 54 cells, twice the previous diameter and approximately four times the block area (over 9,000 blocks). Radius increases through `54 + 6 × log2(sector)`, with generation extents derived from the authored radius and v1 validation supporting expanded coordinates. Sectors 1/2/4/16 have radii 54/60/66/78 cells. Existing saved terrain and health remain exact; the enlarged geology applies on generation. Damage regions gather only nearby cells and rendering looks up visible cells, avoiding whole-planet scans per blast or wave tick.

Both flight modes include a read-only Survey minimap with remaining terrain, ship/heading, deployed/returning drone markers and camera footprint. A distant ship remains marked at the edge. The terrain texture is cached and refreshed at most five Hz when terrain changes; camera-only redraws do not invalidate its revision. Markers stay fluid. Map touches cannot start steering. The map joins the staggered HUD ignition while workshop tabs retain the earlier no-replay fix.

Validation:

- `python3 tools/dev.py test-rendered`: **341 checks, zero failures**, compiler zero warnings/errors. `builds/qa/local-pilot-rendered-regression.log`. Includes first-hit occlusion, no influence from hidden rear ore, intentional look-around to reacquire rearward ore, approach/range-gated firing, local mining hover, bays/ledges/L-shaped cuts at 15/30/60 fps, corner-overlap recovery, actual held manual excavation followed by safe auto handoff, speed bounds, old sparse-remnant cases, expanded grid generation/growth and v1 round-trip, minimap bounds/input isolation/cache invalidation/edge markers, and all prior crafting/rate/upgrades/touch/legacy regressions. Intentional malformed-save warnings are expected.
- A prior headless run passed **337 checks** before adding the final handoff and look-around checks. `builds/qa/local-pilot-regression.log`. Its existing shutdown ObjectDB leak notice is not a physical-device performance result.
- `python3 tools/rendered_check.py`: **122 rendered input checks passed**. `builds/qa/local-pilot-live.log`. Uses injected Viewport touches/drags and the scaled engine desktop mouse path. The added live fixture performs manual mining in a carved bay, switches through the actual mode button without changing terrain/position, then simulates twenty seconds of local autopilot with **zero hull-overlap frames** and continued mining/movement. It also generates sectors 1/2/4 and confirms larger real grids plus the visible minimap. Sparse search timing allows the deliberate look-around before approach/aim/fire. Runtime logs have no errors or warnings.
- Inspected `local-pilot-manual-excavation.png`, `local-pilot-after-handoff.png`, `minimap-growing-sector-1.png`, `minimap-growing-sector-4.png`, and `manual-phone-size.png`. The map fits above heading telemetry, the ship stays visible and the camera follows the excavated bay. QA screenshots now wait for deferred CanvasItem redraw commands before forcing an occluded desktop frame, avoiding stale ship positions after generating a fixture.
- `git diff --check` and Python syntax validation pass. Test saves stay in `.godot/tests`; no player save is read or written. These are rendered desktop checks, including 390×844 phone-sized windows, not physical Android testing. No APK/AAB or native package was built.

This establishes the tested local steering/recovery behavior and actual geometry growth. It is not exhaustive proof for every possible concavity or a full campaign/mobile performance playtest. Larger planets increase excavation time and rare-layer travel distance; `PROGRESSION.md` records the revised geometry and existing balance limitations.

---

# Historical: workshop ignition frequency and separate fire-rate tuning

Workshop ignition is now an explicit entry event. Opening Ship Systems (or the paused workshop) plays it once; switching/reselecting Ship/Craft/Fleet/Market tabs, changing categories/pages, purchasing, or resizing an open workshop leaves it settled. Clicking the selected tab is a no-op that preserves its existing channel rather than restarting its animation. Closing and reopening begins a new workshop visit. Other startup/mode-change neon behavior remains as authored.

Fleet → Weapons now offers separate **Power** and **Fire Rate** controls with displayed credit costs and levels. Fire-rate tuning adds a `1 + 0.04 × rate level` multiplier, independently rank-capped, with cost `floor((120 + type × 140) × (rate level + 1)^1.9)` CR. It reduces the actual primary, auxiliary and drone-mounted weapon cooldowns. Purchases remain atomic; drone-mounted tuning requires docking. Optional saved `rate_level` defaults to zero for older equipment saves and is validated before applying a save. Existing Power levels/bonuses remain intact.

Weapon pages now contain two items with separate fit/primary and Power/Fire Rate rows; all inventory pages remain accessible. The fitted ship stays visible, and its callouts show Power/Rate levels.

Validation:

- `python3 tools/dev.py test-rendered`: **315 checks, zero failures**, build zero warnings/errors. `builds/qa/workshop-rate-rendered-regression.log`. New checks inject real touchscreen events for first-entry ignition, selected-tab identity/age preservation, all workshop tabs staying settled, phone-sized resizing without replay, a rate purchase with duplicate mouse emulation charging once, independent levels, actual shorter primary/auxiliary/drone cooldowns, insufficient-funds/rank/service rejection, rate persistence, malformed-rate save rejection and older saves without the new field. Existing navigation, touch ownership, rendering, crafting, progression and legacy-save regressions pass.
- `python3 tools/rendered_check.py`: **115 rendered input checks passed**. `builds/qa/workshop-rate-live.log`. Added live checks cover entry/reselection/tab behavior and independent fire-rate purchasing. The full route continues through phone-sized desktop inputs, all world styles, crafting, mount fitting, drone mining/recall/service and Nova effects. Runtime logs contain no errors or warnings.
- Inspected `builds/qa/crafting-weapon-upgrades.png` at a 390×844 desktop window: separate Power and Fire Rate controls, level/cost feedback, ship schematic and transaction text remain contained. Inspected `drydock-ship-mounts.png` to verify the second weapon page and fitted hull remain usable.
- `git diff --check` and Python syntax validation pass. Test saves remain isolated in `.godot/tests`; no player save is read or written. Rendering inspection and input injection are desktop verification, not physical Android testing. No APK/AAB or other native package was built.

---

# Historical: technical designs, neon ignition, ship mounts and independent drones

Current revision addresses the visual/progression follow-up. Clean Frontier is retained; the original flat-square design was not deleted and is now explicitly titled **Original**, placed beside Clean Frontier on the first picker page. Eight additional restrained treatments are registered: Contour, Lithograph, Carbon, Faultline, Monolith, Spectral, Circuit and Radar. The seven earlier decorative presets remain in later pages for saved-ID compatibility. All 17 choices are paged and preserve terrain, partial health, wallet, pending rewards and ship state.

Titanium, Iridium and Aether now use distinct dark-backed ingot, nested-lattice and split-orbit silhouettes, including their pickups. Generated deposits multiply block health by 2.8/4.5/7 on top of depth, kind and sector resistance. The starter hits a single block for 1.1 every 0.5 seconds; individual damage/cadence growth is restrained, rank caps are 5/10/15/20, and saved shared upgrade levels contribute through diminishing formulas. Planet toughness accelerates continuously with sector while credit richness grows slowly. Exact existing terrain is preserved; new geology applies to generated planets.

Ship upgrading displays a live fitted-hull schematic across Ship, Fabricator, Fleet and Market. Rank/credit gates purchase second/third mothership mounts; fitted weapons fire together. Each drone still has one unique mount. Nova wave expands over 1.6 seconds with distance-dependent damage and one hit per block. Bore laser pierces two blocks and has a rare-deposit damage specialization. Drones leave the mothership to mine independent routes. Remote upgrades/refits are rejected until recall and docking; recall transfers work during paused workshop servicing without mining, collection or projectile impacts. Deploy orders persist on loading.

HUD ignition retains its staggered three-second budget but replaces smooth pulse ramps with abrupt failed strikes, blackouts and residual contact chatter. Stepped jitter has a 2.4 logical-pixel horizontal budget with smaller vertical motion, reaches its exact resting position and stops processing. Rendering uses the existing bounded vector halos, with no new blur pass or viewport.

Validation:

- `python3 tools/dev.py test-rendered`: **294 checks, zero failures**, compiler **zero warnings/errors**. Log: `builds/qa/fleet-evolution-rendered-regression.log`. Includes all prior touch/mouse, sparse autopilot, held steering, styles and legacy-save checks; deposit depth/resistance; starter single-block output; rank gates and bounded scaling; simultaneous mounts; wave-front timing/falloff/one-hit behavior; Bore armor specialization; independent drone travel/mining; remote-service rejection; paused recall/docking; mission and hardpoint persistence; old crafting saves without new fields; actual touch purchases and visible ship preview. Four intentional invalid-save fixtures emit rejection warnings. Warp integration uses sparse worlds rather than claiming a full campaign pacing test.
- `python3 tools/rendered_check.py`: **107 rendered input checks passed**, using real `InputEventScreenTouch`/drag injection and the scaled desktop `Input.ParseInputEvent` path. Log: `builds/qa/fleet-evolution-live.log`. Checks all 17 appearances, crafting/market/upgrades, visible fitted hull, an auxiliary Nova wave, independent drone mining far from an idle ship, denied remote upgrade, recall during paused servicing, successful docked upgrade and redeployment. Phone-sized macOS windows include 390×844; desktop regression also resizes to 540×960. Runtime/render logs contain no errors or warnings.
- Inspected `ship-systems.png`, `world-design-picker.png`, `drydock-new-weapons.png`, `drydock-ship-mounts.png`, `drydock-returned-drone.png`, `nova-expanding-wave.png`, and the ten `technical-deposits-*.png` views (Original, Clean Frontier and all eight new treatments). Rare glyphs remain distinct against each palette. Drydock screenshots show the fitted hull/mount changes, service status, and contained text. Rendered ignition frames and regression samples verify overshoot, abrupt luminance breaks and visible bounded displacement; this is desktop inspection, not physical phone inspection.
- `git diff --check` and Python syntax validation pass. All data-changing QA fixtures require `--qa` in an isolated scene; all persistence uses `.godot/tests`, never the player's save.

`PROGRESSION.md` contains the revised recipes, upgrade formulas, material resistance and campaign curve. These checks establish implementation behavior, not fully playtested long-session balance or physical mobile performance. No APK/AAB or other native package was built.

---

# Historical: crafting, armed drones, and equipment progression

Fresh games now start with one Photon lance. Most rocks pay only credits; seeded Titanium/Iridium/Aether deposits appear at 35%/60%/82% normalized depth, with guaranteed Aether in the core. Deposits have visible inlays, and collectible fragments carry rare rewards into inventory. New-sector health/value scaling is linear rather than exponential. Existing worlds retain their exact tiles and health.

Ship Systems and Pause expose four connected screens: Ship/refinery, Fabricator, Fleet, and Intergalactic Market. Six authored recipes build three weapon types and Scout/Survey/Atlas drone chassis. Weapons are owned instances, can occupy only one mount, and are upgraded individually with credits. Drones require spare weapons, follow the ship, and autonomously use the actual Photon/Nova/Ion implementations to mine. Chassis upgrades amplify mount performance. Rank milestones at sectors 1/2/4/7 unlock blueprints, bays (up to three), and upgrade ceilings. Ship Systems shows the next objective. The local in-game market buys/sells rare materials at fixed credit prices.

All purchases validate before spending; insufficient funds/materials, rank locks, capacity limits, and invalid mounts do not partially apply. The ship cannot be left unarmed. Inventory changes rebuild menu content without replaying the three-second ignition. Feedback is contained/wrapped inside the workshop card. Pages/categories keep the full fleet and recipes accessible without touch-scroll conflicts.

Persistence retains the v1 path/schema with optional equipment and rare-reward fields. Validation rejects duplicate mounts before applying any state. Legacy saves preserve their unlocked weapon types, shared combat bonuses, exact terrain, and pending credit/energy rewards. Pending rare materials and inventory round-trip together and cannot be collected twice. Market sales do not inflate offline mining income; offline catch-up remains credits only. No player save is used by tests.

Validation:

- `python3 tools/dev.py test-rendered`: **242 checks, zero failures**, zero compiler warnings/errors. `builds/qa/crafting-rendered-regression.log`. Covers deep-only deposit placement, predominantly credit-only rocks, crafting failure/success accounting, market spread and income exclusion, unique mounts, individual upgrades, all three weapons on drones, unarmed/idle behavior, pending-material save restoration exactly once, equipment/level/mount persistence, invalid-save rejection, bay/rank gates, real touchscreen crafting with mouse emulation, readable feedback bounds, and existing navigation/styles/progression/legacy regressions.
- A prior headless pass completed **236 checks, zero failures** before the additional drone-mount and feedback checks. `builds/qa/crafting-regression.log`.
- `python3 tools/rendered_check.py`: **80 rendered input checks passed**. `builds/qa/crafting-rendered.log`. The end-to-end fixture crafts a Nova and Scout through actual touches, fits the spare mount, upgrades both, buys Iridium, then observes autonomous drone mining while the ship is idle. The original input tests explicitly use a legacy unlocked loadout; a separate check verifies the fresh starter-only state.
- Inspected `crafting-fabricator.png`, `crafting-drone-blueprints.png`, `crafting-fleet.png`, `crafting-weapon-upgrades.png`, `crafting-market.png`, and `crafting-drone-mining.png`. Review caught overflowing status text; label bounds and wrapping were corrected and a regression was added.
- `git diff --check` and Python syntax validation pass. All data-changing inspection fixtures require the isolated `--qa` scene and are excluded from exports.

Recipes, prices, material colors, and milestones are editable in `resources/crafting_catalog.tres`; depth distributions and sector curves in `resources/planet_generation.tres`. `PROGRESSION.md` records the initial numbers and rationale. These checks establish functionality, not a fully tuned late-game economy or physical Android performance. No APK/AAB or other native package was built.

---

# Historical: world-design comparison

Pause → Compare World Designs now previews eight new visual designs plus Classic Grid on the same frozen world. The chooser sits at the bottom with a light scrim so terrain remains visible. Selection updates immediately without rebuilding the menu or replaying its ignition; Back returns to pause, and Resume continues with the selected design.

Clean Frontier prioritizes the supplied reference's simplicity: plain pixel terrain, a clear stepped rim, restrained background grid, and small collectible motes. Its initial turquoise palette is independently editable. Fractured, Prism, Holo Survey, Solar Forge, Abyssal, Relic, and Chromatic explore chipped plates, facets, wireframes, molten seams, rounded nodules, runes, and graphic shading. Classic Grid preserves the original baseline. Presets also style background and collectible shapes; the shared holographic HUD is retained.

Eight authored presets live under `resources/world_styles/`, with the baseline at `resources/planet_visual_style.tres`. `Main.VisualStyles` registers their order. Rendering reuses polygon buffers and culls invisible cells; terrain remains one data grid with one renderer. The optional v1 `visual_style` ID remembers the preference, with a default for legacy/missing/unknown IDs. No generation, collision, mining, health, wallet, or reward rules were changed.

Validation:

- `python3 tools/dev.py test-rendered`: **209 checks, zero failures**, zero compiler warnings/errors. `builds/qa/world-designs-rendered-regression.log`. New checks use real pause-menu touches to select every design and verify the same live planet, exact tiles/partial health, generation settings, ship, wallet, pending rewards, damage brightness for all minerals, preference round-trip, and legacy-save fallback.
- `python3 tools/rendered_check.py`: **70 rendered input checks passed**. `builds/qa/world-designs-rendered.log`. Compares all nine presets on identical frozen terrain, captures each design after selection through the actual pause menu, and retains prior touch/mouse/navigation/ignition/autopilot checks.
- Inspected every `world-design-*.png` comparison and `world-design-picker.png` under `builds/qa/`. The first capture sequence followed flight too far into empty space; captures now occur before that flight so terrain is clearly visible for comparison. Inspection also caught an unnecessary stroke-width argument on filled collectible rectangles; that rendering warning was removed.
- `git diff --check` and Python syntax validation passed. `world-designs-build.log` records the final local compile after the rendering-warning fix.
- The final 70-check live inspection run has no runtime warnings or errors in `builds/qa/runtime.log`.

No APK/AAB or other native package was exported. Rendering was inspected on standalone macOS; physical Android performance and touch comfort remain unprofiled. More detailed presets issue more drawing commands than Classic Grid/Clean Frontier, though they add no per-tile nodes or texture downloads.

---

# Historical: sparse autopilot fix and longer HUD activation

Autopilot could circle the planet's empty center indefinitely after only an outer-edge block or a few separated remnants survived. Its surface lookahead often missed those remnants, so the computed orbit never entered the 245-unit firing range. Navigation now checks the nearest surviving ore: when the center-based route would lose mining contact, it approaches and orbits that ore locally with a range buffer. The 155-unit/s movement cap and collision handling remain in force.

The new regression reproduces the old stall with isolated blocks on all four sides and with a cluster plus a distant island. All 15 weapon/scenario completion checks failed before the fix; they pass afterward. Each case also verifies bounded movement, hull clearance, and preservation of the live planet. Clearing a remnant exercises the real weapon, damage, and sector-completion paths rather than merely checking a destination flag.

The user's subsequent timing adjustment sets holographic activation to **3 seconds** by default, with an Inspector range up to 6 seconds. The same stagger, overshoot, diminishing fluctuations, and responsive input remain. Timing checks and screenshot sampling now follow the authored duration instead of assuming 400 ms.

Validation:

- `python3 tools/dev.py test`: **176 checks, zero failures** for the autopilot fix before the timing adjustment. `builds/qa/sparse-autopilot-regression.log`. The original reproduction log is `sparse-autopilot-before.log` (15 expected failures).
- `python3 tools/dev.py test-rendered`: **176 checks, zero failures**, zero compiler warnings/errors, including the final three-second timing. `builds/qa/sparse-autopilot-rendered-regression.log`.
- `python3 tools/rendered_check.py`: **51 rendered input checks passed**. `builds/qa/sparse-autopilot-rendered.log`. The isolated QA fixture verifies approach from beyond range, destruction of the final block, and progression into the next generated sector. Inspected `autopilot-last-block-approach.png` and `autopilot-last-block-cleared.png`; the live runtime log contains no warnings/errors. The runner also samples ignition relative to the authored three-second duration.
- `git diff --check` and Python syntax validation passed. Physical Android behavior was not tested.

No APK/AAB or other native package was exported. Inspection and regressions use isolated test data and do not access the player's save.

---

# Historical: holographic terminal HUD (400 ms timing superseded)

The HUD now uses open cyan/amber ship telemetry rather than rectangular statistic cards. Manual mode adds peripheral navigation arcs and live velocity/heading, unboxed capital/reactor readouts, reactor segments, angular weapon controls, and session recovery text. A low-resolution gradient keeps readouts legible over terrain. The large mode announcement is replaced with a compact link-status line.

Startup and mode changes energize modules in staggered order within **400 ms**. The envelope has a luminous overshoot, three irregular diminishing peaks, and rapidly damped jitter bounded to 0.65 logical pixels. Before its delay a module is fully unlit; after ignition only luminance fluctuates, with constant alpha. It settles to normal brightness and zero jitter, then disables its animation processing. Modal cards ignite independently; closing them does not restart the main HUD. The former entrance slide/fade and ongoing status pulse are removed.

Timing, stagger, peak intensity, and jitter are editable in `resources/mode_transition.tres`; inks and halo strength live in `resources/hologram_style.tres`; the reusable module is `scenes/ui/hologram_channel.tscn`. Soft bloom is approximated with local antialiased halo strokes and font outlines. The atmosphere is a 1 × 128 gradient. No HDR renderer change, blur pass, extra viewport, runtime glow texture, or image asset was added.

Validation:

- `python3 tools/dev.py test-rendered`: **146 checks, zero failures**, zero compiler warnings/errors. `builds/qa/holographic-rendered-regression.log`. Includes stagger/overshoot, three diminishing peaks, completion and animation shutdown at 400 ms, bounded jitter, held steering during ignition, modal behavior, all existing mining/navigation/progression/save regressions.
- `python3 tools/rendered_check.py`: **47 rendered input checks passed**. `builds/qa/holographic-rendered.log`. Includes timed ignition states, full-opacity settled modules, actual rendering, portrait phone-sized windows, and touch/engine-mouse steering. `runtime.log` contains no runtime warnings/errors.
- Inspected `hologram-020ms.png`, `hologram-085ms.png`, `hologram-180ms.png`, `hologram-400ms.png`, and settled manual/phone views under `builds/qa/`. The early-frame inspection caught black silhouettes in not-yet-energized modules; binary pre-ignition visibility now removes them. The settled text and line work remain crisp.
- `git diff --check` and Python syntax validation passed. A prior headless pass completed 137 pre-existing gameplay checks before adding the ignition-specific assertions.

No APK/AAB or other native package was exported. These checks use standalone macOS rendering and engine-injected inputs. Physical Android frame time, thermal behavior, and touch comfort remain unprofiled, so no mobile FPS claim is made.

---

# Historical: held steering and safe mining (source only)

The user's clarification replaces released-tap waypoints with continuous one-thumb steering. Pressing the play area begins steering toward the finger, dragging changes direction, and lifting/canceling stops movement. A stationary held thumb continues guiding the ship as the camera follows; there is no persistent destination after release. Desktop hold-left-mouse and motion use the same behavior. Both modes remain portrait.

The ship slows before blocking ore, leaves an additional 40 world units beyond its hull collision check, fires the selected weapon while holding clearance, and continues as rocks clear. `resources/manual_flight.tres` exposes speed, extra stand-off, approach response, and dead-zone size. The camera/UI timing remains in `resources/mode_transition.tres`. No health system was added.

The earlier tests passed because they encoded the assistant's incorrect tap-to-waypoint interpretation. They did not validate the user's intended held steering. The replacement regressions explicitly keep a finger down during motion, drag that same owner, check travel beyond the original map point, and verify that release leaves the ship stopped. The rendered runner additionally routes desktop mouse events through `Input.ParseInputEvent` at a scaled window size, then observes movement in the normal running process loop; these are engine-injected events, not physical mouse hardware tests.

Verification:

- `python3 tools/dev.py test`: **137 checks, zero failures**. `builds/qa/held-steering-regression.log`.
- `python3 tools/dev.py test-rendered`: **137 checks, zero failures**, zero compiler warnings/errors. `builds/qa/held-steering-rendered-regression.log`. Includes durable-rock safety distance and slowdown, all three weapons resuming movement, touch/mouse drag and release behavior, modal/lifecycle cancellation, bounded autopilot, progression, and legacy saves.
- `python3 tools/rendered_check.py`: **44 rendered input checks passed**. `builds/qa/held-steering-rendered.log`. Includes a stationary held thumb traveling beyond its initial world point, same-finger redirection, release remaining stopped, live automatic path mining, portrait layouts, and mouse press/drag/release through the engine's scaled window-input path. `builds/qa/runtime.log` contains no runtime errors or warnings.
- Inspected `manual-steering.png` and `manual-clearing-path.png` in `builds/qa/`: steering instructions and release hint are readable, the ship follows the held point, and the mining approach leaves visible room before the rock face. Python syntax validation and `git diff --check` pass.

No APK/AAB or other native package was exported. Physical phone handling and other operating systems remain unverified; a PC-specific runtime failure was not established by the user's clarified report.

---

# Historical: portrait destination navigation (superseded)

Updated 1 November 2026 on `gameplay/basic-controls`. The incoming scene/resource, procedural planet, bounded autopilot, and animated HUD refactors were preserved. Both modes now remain portrait. Mode switches no longer request OS rotation, desktop window resizing, or an orientation wait. The authored zoom, HUD slides, and neon status presentation remain configurable in `resources/mode_transition.tres`.

Manual mode replaces both sticks with a completed world tap. The ship travels toward that fixed world destination, automatically aims/fires at ore obstructing its hull corridor, continues when clear, and stops on arrival. Another tap redirects it; tapping the ship stops early. It does not mine unrelated ore while idle or traveling through empty space. All three weapons and the existing 25% manual cadence advantage remain. Canceled/swiped/outside gestures and synthesized mouse input cannot create destinations. Weapon selection retains travel; pause, focus loss, resizing, modal opening, mode change, sector change, and successful save loading cancel it.

Validation:

- `python3 tools/dev.py test-rendered`: **132 checks, zero failures**, zero compiler warnings/errors. `builds/qa/portrait-navigation-rendered-regression.log`. Covers real touch and desktop clicks, all weapons mining/resuming, diagonal and hull-edge clearance, world-fixed destinations, stop taps during camera movement, independent menu fingers, lifecycle cancellation, mode reversals, large planets, original saves, exact pending rewards, and two-sector automatic progression.
- `python3 tools/rendered_check.py`: **34 live rendered touchscreen checks passed**. `builds/qa/portrait-navigation-rendered.log`. Includes automatic mining along an inward route, travel and arrival, unchanged native window dimensions, canceled taps, lifecycle/modal controls, and phone-sized portrait layouts.
- Inspected `auto-portrait.png`, `manual-navigating.png`, `manual-clearing-path.png`, and `manual-phone-size.png` under `builds/qa/`. Added an opaque background to the redirect/stop hint after terrain made the text hard to read. `portrait-navigation-build.log` records the final local build; `runtime.log` records the live inspection run.
- An earlier headless pass completed **130 checks, zero failures** before the load-cancellation and moving-stop assertions were added. `builds/qa/portrait-navigation-regression.log`. It emitted an eight-instance Godot shutdown leak warning; the rendered regression did not. Two malformed-save warnings in the regression logs are intentional rejection tests.
- `git diff --check` and Python syntax validation passed. The incoming refactor was verified through actual local builds and runs, rather than relying on its supplied description.

No APK/AAB or macOS package was exported. The existing APK is an earlier build and contains neither the incoming refactors nor this portrait navigation. Android packaging still requires an explicit user request under `AGENTS.md`.

Limitations: desktop rendering, phone-sized desktop windows, and injected Viewport touch events do not establish physical Android behavior or one-thumb comfort on a phone. Embedded macOS editor play was not revalidated. Manual productivity is a tuning goal, not a measured guarantee.

The disk initially rejected source writes with `ENOSPC`. Only ignored, reproducible Android export binaries and already-extracted toolchain installer archives were removed. Source, saved games, installed toolchains, signing keys, and the existing APK were retained.

---

# Historical: Starfall Miner 0.3.1 — basic controls

Branch: `gameplay/basic-controls`, created from fetched `origin/main`. The separate workflow commit remains on `docs/team-workflow`; the existing untracked `TEAM_PROMPT.md` was preserved.

## Transition fix at that checkpoint

- Android requests `WindowManager.LayoutParams.ROTATION_ANIMATION_JUMPCUT` on the current activity window, then requests portrait/landscape in the same Android UI-thread callback. This uses Godot's built-in AndroidRuntime and JavaClassWrapper, including public reflection for the public rotation-animation field. It needs no custom plugin, Gradle build, new permission, or global device animation change. A bridge failure logs a warning and retains an orientation fallback.
- The HUD retains its previous mode until the requested viewport geometry arrives, then switches at full opacity. Resize events no longer restart its fade. A one-second timeout exposes flight controls if the device refuses rotation.
- Camera zoom and centering ease together. Resizing preserves the existing offset from viewport center, avoiding an additional camera sweep or a premature zoom against the old portrait viewport.
- Completing the handoff no longer clamps/teleports the live ship into the new visible bounds. Further outward steering is blocked, and the pilot can steer back in continuously. Planet, ship, projectiles, debris, and rewards remain live.
- Old touch ownership is cleared; fresh controls respond once the viewport is ready while camera easing continues. The recovered-resource caption appears after settling so the shrinking planet does not cross its text.

These changes cross gameplay input, camera/orientation, and HUD presentation; UI review is appropriate before merging.

## Verification at that checkpoint

- `python3 tools/dev.py test`: **103 checks, zero failures**, compiler warnings/errors zero. Log: `builds/qa/basic-controls-regression.log`.
- `python3 tools/dev.py test-rendered`: **103 checks, zero failures** with rendering. Log: `builds/qa/basic-controls-rendered-regression.log`.
- `python3 tools/rendered_check.py`: **30 rendered touchscreen checks passed**. Log: `builds/qa/basic-controls-rendered.log`.
- Visually inspected `manual-transition-start.png`, `manual-transition-mid.png`, and the settled manual view under `builds/qa/`. The start/midpoint HUD stays opaque and camera framing contracts continuously. The automated pass also covers portrait, both manual window sizes, panels, independent fingers, and return to autopilot.
- `python3 tools/dev.py export-android`: **passed**, followed by automatic verification of identity/version, signatures, CRC, excluded QA code, and 16 KB ZIP/ELF alignment. The APK includes AndroidRuntimePlugin and createRunnableFromGodotCallable in its DEX payload.
- `git diff --check` and Python tool syntax checks passed.

Current APK: `builds/Starfall-Miner-Android.apk`, **0.3.1 / code 4**, `com.starfall.miner.demo`, ARM64, API 24 minimum / API 36 target. Size: 105,196,386 bytes. SHA-256: `06c6f50111e1eb436f155ed0929882e68e737a93dc6f0c675f9b2e1d2ffad9cd`. Signing certificate matches the prior local 0.3.0 C# build; it has not been matched to the original GDScript demo. The APK verifier now reads version metadata from export presets so subsequent version bumps do not leave stale expected values.

**Android OS animation is not physically verified:** `adb devices -l` reports no connected devices. Tests use injected touch events and desktop window resizing. Android only honors the per-window rotation-animation policy under its documented fullscreen/window conditions; it can override the request. Desktop checks and an APK build cannot prove suppression on a particular phone. The macOS source was rendered and tested; this task produced an updated Android package, without rebuilding the earlier packaged macOS app.

References: [Android rotation-animation conditions](https://developer.android.com/reference/android/view/WindowManager.LayoutParams#rotationAnimation), [Godot's built-in Android API bridge](https://docs.godotengine.org/en/stable/tutorials/platform/android/javaclasswrapper_and_androidruntimeplugin.html).

---

# Historical 0.3.0 migration verification

The remainder records the earlier migration baseline. Its test counts and artifact checksums describe that earlier build. Current source behavior is described at the top of this report.

## Planet-expedition branch status

The level-design branch adds thirteen larger, named layouts, camera following, difficulty-scaled completion rewards, iron, and weapon modules. On this Windows PC, the matching Godot 4.7.2 .NET editor and .NET SDK 10.0.403 were found. The user later authorized a local build. The branch was built and run in a visible Godot debug window on 1 November 2026. The active Downloads checkout remains separate and untouched; the OneDrive branch retains editor-generated, uncommitted changes. The historical macOS checks below belong to the earlier 0.3 migration commit and are separate from this Windows pass.

## Level-design branch Windows validation — 1 November 2026

The first build exposed a C# local-name collision in `tests/Smoke.cs`; the test variable was renamed without changing behavior. A second `dotnet build StarfallMiner.csproj -p:NuGetAudit=false -p:RestoreConfigFile=NuGet.Windows.Config` succeeded with **0 warnings and 0 errors**, producing `.godot/mono/temp/bin/Debug/StarfallMiner.dll`. The initial sandboxed build could not load Windows NuGet settings, so the successful build used the normal Windows process context and the cached Godot SDK. This is a build-environment issue, not a project compiler error.

Godot headless and rendered smoke runs both reported **120 checks, 0 failures**. The rendered suite exercised `InputEventScreenTouch` paths, including simultaneous steering and firing, second-finger weapon selection, independent releases, and modal barriers. The two malformed-save warnings were expected rejection cases. The rendered game was opened in a visible 960 × 540 debug window: the cropped Verdant Echo surface, ship, HUD, preview indicator, and controls were visible with OpenGL rendering. The game log contained no runtime errors. Planet-preview F7/F8 navigation and every individual world's appearance still need a visual pass; the live window was receiving user input during automated inspection, so keyboard automation was stopped. No export or physical Android test was performed.

Verified locally on 31 October 2026: Apple M1 / ARM64, macOS, Godot `4.7.2.stable.mono.official.ed1daf0bf`, .NET SDK `10.0.403` / runtime `10.0.14`. The incoming source is checkpointed at Git commit `ce1e4b6` in this project-local repository. The surrounding repository and its unrelated edits were left untouched.

## Implementation

All six production GDScript files and the old GDScript regression runner have been replaced with C#. Scene references use `scripts/Main.cs`; the project contains a .NET solution, typed simulation entities, separate input/camera/persistence/presentation classes, VS Code C# tasks and debugging configurations, and native macOS/Android export presets. Superseded scripts, their UIDs, and the Web shell/preset are removed. Currency and health remain double precision; resource counters are 64 bit.

Finger ownership now rejects replacement by a new finger and clears every held ID, including menu presses, on focus loss, pause, resize, and mode changes. Synthesized mouse input remains suppressed. The save filename, v1 wire fields, seeded planets, and pending collection rewards are preserved. Effects remain subtle; no visual redesign or economy rebalance was introduced.

## Commands and results

| Command / check | Result |
| --- | --- |
| Original checkpoint's `tests/smoke.gd`, in a disposable copy | 52 checks, 0 failures |
| `python3 tools/dev.py build` | 0 compiler warnings, 0 errors |
| `python3 tools/dev.py import` | C# scene and assets imported; global script cache regenerated |
| `python3 tools/dev.py test` | 87 checks, 0 failures |
| `python3 tools/dev.py test-rendered` | 87 checks, 0 failures, OpenGL rendering on M1 |
| `python3 tools/rendered_check.py` | 28 checks, 0 failures; live localhost connection and PNG capture |
| Android ARM64 `dotnet publish`, ExportDebug, self-contained | Passed using the Mono Android runtime pack |
| `python3 tools/dev.py export-macos` | Native ARM64 ZIP exported and ad-hoc signed |
| macOS ZIP CRC / `codesign --verify --deep --strict` | Passed |
| Exported macOS executable, rendering enabled, 90 frames | Exit 0; bundled runtime and assets load; no game runtime errors |
| `git diff --check`, Python tool syntax | Passed |

The C# suite instantiates the actual shipped main scene. Menus are activated through `Viewport.PushInput(InputEventScreenTouch)`, not by invoking callbacks or substituting mouse clicks. It covers cancellation, outside releases, modal barriers, exact-once purchases with emulated mouse injection, multi-finger steering/firing/weapon switching, actual movement and damage under held touch, independent releases, and lifecycle cleanup.

Gameplay checks cover all weapons, five ore types, progressively brighter health colors, delayed and exact collection, finite volatile cascades, overdrive spending/cooldown, pause, and automatic progression through two complete planets. Save checks cover all tiles and partial health, both legacy formats, pending rewards across reopening, repeat collection/reopening, capped offline income, future timestamps, cleared-sector restoration, and rejection of malformed payloads before partial state changes. Malformed-save tests intentionally emit two warnings; these are expected assertions, not runtime failures.

The 0.2 fixture was produced by the checkpointed GDScript serializer with a known wallet, damaged planet, and pending pickups. The 0.1-format fixture removes the four 0.2 fields; a physical user's 0.1 save was unavailable. A separate untouched sector-4 fixture verifies C# generation against original keys, ore types, shades, health, and values. All test storage is under `.godot/tests/`; the player's actual user save was not read or overwritten.

## Rendering and inspection

The opt-in C# inspection scene listened on **127.0.0.1:47831**, and the Python client connected to it, reported the `StarfallMiner` C# assembly, sent live touch input, and retrieved screenshots. It is a working local inspection alternative to an MCP bridge, not merely an untested server configuration. Inspection scenes and test classes are excluded from export resources and exported assemblies.

Visually reviewed:

- `builds/qa/auto-portrait.png`: spacious automatic mode at 540 × 960, square ore, small ship, wallet, prominent Take Control.
- `builds/qa/ship-systems.png`: separate weapon loadout and upgrade rows.
- `builds/qa/cargo-portrait.png`: separate extraction statistics.
- `builds/qa/manual-landscape.png`: flight HUD at 960 × 540, two held fingers, selected Ion, boost, compact wallet.
- `builds/qa/pause-landscape.png`: readable pause panel and working sound/resume controls.
- `builds/qa/auto-phone-size.png` and `manual-phone-size.png`: 390 × 844 and 844 × 390 desktop windows, captured after HUD fades settle.

The pass also checks mode-driven macOS window reshaping, retained planet identity/ship/projectiles/debris during mode changes, and actual second-finger menu interaction. PNGs and `rendered-results.json` are in `builds/qa/`; runtime logs there contain no game errors. The complete rendered regression log is `builds/qa/csharp-rendered-regression.log`; the final headless result is `builds/qa/csharp-headless-regression.log`.

These inputs are **injected Godot touch events**. The window changes are **macOS orientation requests and simulated phone-sized resizing**, not Android OS rotation. The historical 26-check browser result is not claimed for the migrated game. There is no supported C# Web export in this project.

## Native artifacts and Android identity

The runnable Apple Silicon application is `builds/macos/Starfall Miner.app`, also packaged as `builds/Starfall-Miner-macOS.zip`. It includes the .NET runtime, requires no SDK to run, and is locally ad-hoc signed. It is not notarized or signed with a Developer ID for public distribution. Packaged-resource checks exclude retired GDScript and QA scenes; exported assembly checks exclude the inspection code. Godot and .NET runtime notices are bundled.

Android retains `com.starfall.miner.demo` and increases the version to **0.3.0 / code 3**. The ARM64 APK targets API 36 with minimum API 24, inherited from the official template. JDK 17, Android command-line tools, SDK packages, and matching Godot .NET templates are installed under the ignored project build directory. Native APK export uses prebuilt templates, without Gradle or an Android .NET workload.

`python3 tools/dev.py export-android` and `python3 tools/verify_android.py` pass. The APK is `builds/Starfall-Miner-Android.apk`. Package ID, version code/name, ARM64-only native payload, compiled game DLL, excluded QA code, APK CRC, v2/v3 signatures, 16 KB ZIP alignment, and the load-segment alignment of all eleven native libraries are verified. Reports are under `builds/qa/android-*`.

The new debug certificate SHA-256 is `e00b88e85e434c4cd0dc51a61fd1f64fa8aafd6caef6b90f0fe63eeb672e5a33`. Its private key is ignored under `.godot/keys/`; this is a new local certificate, not the previous demo's verified certificate.

The prebuilt APK template requires `net9.0`; desktop remains `net10.0`. SDK 10 builds both. Android publishes .NET 9.0.22 runtime libraries. Microsoft ends .NET 9 support on **10 November 2026**; a later runtime upgrade requires a compatible newer template or Gradle targeting .NET 10. [Microsoft policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core). SDK 35/build-tools 35.0.1 are installed per Godot's export documentation; Godot accepts these tools for the prebuilt API-36 APK, with an informational fallback message.

The original demo APK and its private signing keystore were absent from the project, delivery directory, and standard Android/Godot key locations. A new local debug key cannot establish update compatibility with an installed demo. A matching original signing certificate is required for that claim. No original save should be erased to work around a signing mismatch.

## Final artifact checksums

| Artifact | Bytes | SHA-256 |
| --- | ---: | --- |
| `Starfall-Miner-macOS.zip` | 64,881,956 | `a79796fe3138e74e7739812df5e4dbc6c221e09cb678eee6e32040b10c1a8375` |
| `Starfall-Miner-Android.apk` | 105,192,208 | `3f17e57ca0c1a4f3deb2cee5fc27f02c4f5286ee91078b0b97604a9664a2d9e1` |

## Remaining limits

`adb devices -l` reports zero connected devices. No physical Android phone or emulator has been used for gameplay. Device graphics, physical touch, real OS rotation/animation, Android lifecycle behavior, and performance still require hardware validation. Godot documents mobile C# support as experimental. Desktop and injected-touch results do not substitute for that device pass.

Godot 4.7.2's headless editor/import/export process emits a shutdown diagnostic about `EditorSettings` and `export/android/shutdown_adb_on_exit`; successful exports/imports return 0. This editor-shutdown issue is separate from the clean rendered game logs. A transient disk-full failure during toolchain setup was resolved by removing redundant task-generated download chunks and archives, then retrying the interrupted build steps.
