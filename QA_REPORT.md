# Starfall Miner — technical designs, neon ignition, ship mounts and independent drones

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
