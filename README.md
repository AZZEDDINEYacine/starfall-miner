# Starfall Miner 0.3.1 — Godot C#

A native space-mining incremental using Godot **4.7.2 .NET** and the **.NET 10 SDK**. Desktop targets .NET 10; Android targets .NET 9 to match the official prebuilt template. All production gameplay, input, procedural visuals, UI, and sound are C#. Open `project.godot` with the .NET edition of Godot and press F5, or use the commands below.

## Develop on this Mac

The detected machine is Apple Silicon (`arm64`), with Godot at `/Applications/Godot_mono.app/Contents/MacOS/Godot` and .NET SDK 10.0.403. Python 3 is used for development commands; there are no third-party Python or gameplay NuGet dependencies.

```sh
python3 tools/dev.py import         # build C# and import assets on a fresh checkout
python3 tools/dev.py build          # compile the actual game
python3 tools/dev.py run            # rendered game, normal player save
python3 tools/dev.py test           # isolated C# regression suite
python3 tools/dev.py test-rendered  # the same suite with rendering enabled
python3 tools/rendered_check.py     # rendered inspection + touchscreen checks + PNGs
```

SDK caches are kept under `.godot/`, outside source control. `NuGet.Config` includes the matching packages bundled with this Mac's Godot installation plus NuGet.org for runtime packs. Set `GODOT4` to the engine executable and adjust that feed if installing Godot elsewhere. `StarfallMiner.sln`, VS Code build/test/export tasks, and C# launch/attach configurations are included. Install the recommended Microsoft C# extension for debugging. The F5 launch configuration uses this Mac's Godot executable explicitly.

Tests and inspection require `--test` and `--qa`, respectively. These runs never load or modify `user://starfall_save_v1.json`; their explicit persistence tests use `.godot/tests/`. Do not run two persistence regression suites concurrently because they share that disposable file.

Do not build or export an Android APK/AAB unless the user explicitly requests it. Use local C# builds and desktop checks for normal development. Existing packages do not include subsequent source changes.

## Controls and behavior

| Action | Touch | Desktop |
| --- | --- | --- |
| Enter manual flight | TAKE CONTROL | Button or Tab |
| Return to autopilot | AUTO PILOT | Button or Tab |
| Steer in manual mode | Hold a thumb on space; drag to turn | Hold left mouse and move it |
| Stop | Release the thumb, or drag it onto the ship | Release left mouse |
| Mine in manual mode | Automatically fires at rocks obstructing the route | Same |
| Weapons | Ship Systems or flight selectors | 1 / 2 / 3 |
| Overdrive | BOOST, costing 5 energy | E |
| Upgrades | Ship Systems | Same |
| Resource statistics | Cargo Log / wallet | Same |
| Pause / sound | II | Button or P / Escape |

Both modes stay in portrait. Switching modes changes the HUD and eases the camera zoom using `resources/mode_transition.tres`; it never rotates or resizes the native window. Camera and HUD transitions retain the live planet, ship, projectiles, and collectible fragments. The camera follows the ship in both modes, without confining movement to screen edges.

The HUD uses open holographic telemetry rather than boxed statistic cards: cyan capital readouts, amber reactor segments, angular controls, and a manual-flight reticle with live velocity and heading. On startup and mode changes, six manual HUD modules ignite in staggered order over **3 seconds**, with a white-hot overshoot, abrupt failed neon strikes, brief blackouts, diminishing contact chatter and visible stepped jitter, and a softly glowing settled state. Modal cards use the same ignition. There is no whole-HUD slide or alpha fade on entry. Touch targets remain usable during ignition, and closing a modal does not replay the full HUD sequence.

Manual mode is designed for one thumb: hold on space to guide the ship and drag that same finger to turn. The held point is updated as the camera follows, so the ship keeps moving while your thumb stays down rather than stopping at the initial world point. Release to stop. Dragging onto the ship enters a small dead zone; dragging away resumes steering without lifting. The ship slows on approach to blocking ore, mines from a safety stand-off, then continues as the path clears. A mint guide turns gold while mining. `resources/manual_flight.tres` exposes cruising speed, extra mining clearance, approach response, and the pointer dead zone in the Inspector.

Manual firing retains its initial 25% cadence advantage over autopilot. It does not shoot unrelated ore while idle or traveling through empty space. Weapon selection preserves held steering; pause, focus loss, resizing, modal opening, mode changes, and canceled releases clear it. Moving the held finger onto UI suspends movement until it returns to the play area. Desktop uses the same hold-and-drag behavior with the left mouse button.

New planets have roughly three times the original diameter and nine times the ore. Existing saved planets keep their exact tiles; larger generation applies to the next sector or a reset.

Five weapon behaviors share five terrain types, reduced volatile cascades, eight-second overdrive, and endless sectors. New games start with one Photon lance; additional weapons and drones must be crafted. Ore becomes brighter as health falls in every design. Rewards, including rare materials, arrive through collectible fragments. Planet hardness accelerates with sector and continues beyond sector 501; credit richness increases slowly. The starter now hits one block, and equipment upgrades use restrained additive scaling.

## Crafting, fleet, and progression

Open **Ship Systems**, or **Pause → Fabricator / Fleet / Market** during manual flight. The four tabs cover ship/refinery, crafting, owned equipment, and the intergalactic market. A live fitted-hull schematic stays visible throughout the workshop, including weapon and drone upgrades.

- Most rocks award credits only. Titanium deposits begin at 35% depth, Iridium at 60%, and Aether at 82%; the planetary core guarantees Aether. Distinct ingot/lattice/orbital glyphs mark deposits in every design; additional mining resistance is 2.8× / 4.5× / 7×.
- Craft Photon lances, Nova cannons, Ion chains, expanding Nova waves, Bore lasers, and Scout/Survey/Atlas drones. Recipes mainly consume rare materials, with a smaller credit cost. The market buys/sells materials at fixed credit prices, providing an alternative to deep mining.
- Drones start unarmed. In **Fleet → Drones**, use **Fit / Change** to assign a reserve weapon. Press **Deploy** to send it mining independently. Each drone has one mount; **Ship** sells a second/third mothership hardpoint at ranks 2/3. **Fleet → Weapons → Fit Ship** adds auxiliaries, **Set Primary** selects the primary, and **Unfit Aux** frees a mount. All fitted ship weapons fire together.
- Credits upgrade each owned weapon's power/rate and each drone chassis's effectiveness. Ship refinery upgrades improve credit recovery. Saved shared combat levels remain, with diminishing bonus formulas; new combat upgrades are per item.
- Engineering ranks at sectors **1, 2, 4, and 7** unlock blueprints, raise item level limits, and expand the fleet to three drone bays. Ship Systems displays the next objective. See `PROGRESSION.md` for recipes, prices, and tuning rationale.

Armed drones choose their own routes around the planet and mine with the same weapon behavior as the ship. Use **Recall** before upgrades or weapon changes; service is rejected until docking. Return movement continues while the workshop is paused, with mining and rewards frozen. After servicing, press **Deploy** again. Missions persist across loading. There are no offline rare-resource rewards. Existing saved worlds retain their tiles/health and previously available weapons; newly generated planets use the deep-deposit economy. The market is available while finishing an older planet without deposit metadata.

## Compare world designs

Open **Pause → Compare World Designs**. The chooser stays at the bottom while the same frozen world previews each selection immediately. Resume keeps the chosen appearance; it is also remembered in the save. Switching never regenerates terrain, changes collision, alters remaining health or rewards, or moves the ship. It works in both manual and automatic modes.

| Design | Treatment |
| --- | --- |
| Clean Frontier | Retained reference-inspired simple pixels and clear luminous rim |
| Original | The existing Classic Grid, explicitly named and placed beside Clean Frontier |
| Contour | Restrained depth isolines |
| Lithograph | Etched horizontal strata |
| Carbon | Sparse metal grain and tiny registration marks |
| Faultline | Fine diagonal geological cuts |
| Monolith | Large plate seams |
| Spectral | Muted sensor bands |
| Circuit | Sparse embedded traces |
| Radar | Low-ink plotting marks and clear silhouette |

The eight new treatments favor simple technical geometry and muted mineral palettes. Older Fractured/Prism/Holo Survey/Solar Forge/Abyssal/Relic/Chromatic designs remain in the later picker pages for saved-style compatibility. Use the arrow controls to page. Clean Frontier stays the default, and its color remains independently editable. Distinct rare-material glyphs appear in every style. Appearance switches preserve the entire live simulation.

The portrait orientation is configured in `project.godot` and applied at mobile startup. The earlier Android jump-cut helper remains in the source but is no longer called by mode changes.

## Architecture

- `scripts/Main.cs`: simulation coordination, movement, weapons, collection, economy, and progression.
- `scripts/Main.Progression.cs`, `scripts/EquipmentState.cs`: owned equipment, crafting/trades, unique mounts, credit upgrades, rank goals, and autonomous drone behavior.
- `scripts/Hud.Workshop.cs`, `resources/crafting_catalog.tres`: Fabricator/Fleet/Market screens and editor-authored recipes, material prices/colors, and rank milestones.
- `scripts/MiningPlanet.cs`, `scripts/PlanetGenerator.cs`: seeded typed ore grid, damage, chain reactions, and hull collisions; generation settings live in `resources/planet_generation.tres`.
- `scripts/ManualNavigation.cs`, `scripts/AutopilotSteering.cs`: held steering with safe path clearing and bounded autonomous steering.
- `scripts/SimulationModels.cs`: typed blocks, fragments, shells, and short-lived effects. Currency, health, and accumulated credit values use `double`; accumulated resource counters use `long`; positions use Godot vectors.
- `scripts/FlightInput.cs`, `scripts/TouchButton.cs`: world gestures and independent direct-touch menu ownership.
- `scripts/WorldView.cs`: camera transforms, resize handling, and coordinated mode presentation.
- `scripts/Hud.cs`, `scripts/HologramChannel.cs`, `scripts/Visuals.cs`, `scripts/PlanetRenderer.cs`, `scripts/Starfield.cs`, `scripts/ModeStatusOverlay.cs`, `scripts/MiningSound.cs`: presentation and synthesized sound. Scenes and `.tres` resources own reusable UI, palettes, terrain appearance, and ignition settings.
- `scripts/SaveStore.cs`: validated v1 JSON and atomic temporary-file replacement.
- `tests/Smoke.cs`: actual C# scene integration tests with Viewport touch injection and legacy-save fixtures.
- `tests/QaBridge.cs`: opt-in localhost inspection scene, excluded from exported assemblies and resources.

The pre-migration game is recoverable from Git checkpoint `ce1e4b6`. Superseded GDScript and the Web export shell have been retired.

## Saves

The production filename remains **`user://starfall_save_v1.json`**, with `version: 1` and the original serialized field names. The optional `visual_style` ID remembers the selected design; missing or unknown IDs use the default without rejecting the saved world. The application name and Android package identity remain unchanged. Both 0.1-format saves without pickup fields and 0.2 saves with pending collectible rewards are tested. Legacy planets restore exactly from fixtures; larger generation has separate determinism checks. Held steering is transient: loading a manual save leaves the ship idle until a fresh press.

Optional equipment data now stores material inventory, individual weapons/drones, upgrade levels, unique mounts, and the highest reached sector. Tiles and pending fragments carry optional rare-material rewards. Old saves default these safely and keep all three previously available weapon types plus their shared upgrade levels. Optional auxiliary mounts and hardpoint levels preserve earlier equipment saves; a saved recall/deploy order resumes drone missions. Invalid or duplicate mounts reject the save before any state is applied. Rare fragment rewards and the inventory are saved together to prevent duplication on reopening.

Saving occurs every eight seconds, after purchases, weapon/mode/sound changes and boost activation, on sector completion/progression, pause, focus loss, and normal close. Pending rewards and wallet values are committed together, preventing lost or duplicated collection on reopening a saved state. Automatic mode retains offline credits at 25% of the last observed rate, capped at four hours, without destroying planet blocks offline. Catch-up is immediately committed so it cannot be claimed twice. Malformed saves are rejected before applying partial state.

## Native exports

Only export Android when explicitly requested. The portrait navigation changes are source-only and are not included in the existing APK.

Official matching .NET templates are fetched selectively and CRC-checked from the official archive. Godot 4.7's ARM64 preset expects an ARM64-named template binary; the preparation script uses Apple's `lipo` to thin the official universal template without modifying engine code.

```sh
python3 tools/fetch_templates.py macos.zip android_debug.apk version.txt
python3 tools/dev.py export-macos
```

The macOS ZIP is `builds/Starfall-Miner-macOS.zip`. Extract it and open `Starfall Miner.app`. The application includes its .NET runtime and does not require the SDK. It is locally ad-hoc signed, not Developer ID signed or notarized for public distribution.

Android setup is project-local and downloads SHA-256-verified official JDK 17 and Android command-line tools, then installs SDK platform 35, build-tools 35.0.1, and platform-tools. It presents/accepts the standard Android SDK licenses during the authorized installation. Prebuilt APK export does not require an NDK, CMake, Android workload, or Gradle build.

```sh
python3 tools/setup_android.py
python3 tools/dev.py export-android
python3 tools/verify_android.py     # repeat APK identity/signature/alignment verification
```

The Android command configures Godot's two SDK editor settings; their prior contents are backed up to `.godot/editor-settings-before-android.tres`. It creates a local debug signing key under `.godot/keys/` if no `GODOT_ANDROID_KEYSTORE_DEBUG_PATH` is supplied. The key is ignored by Git and should be retained for future builds using that certificate. To use an existing key, set the official `GODOT_ANDROID_KEYSTORE_DEBUG_PATH`, `GODOT_ANDROID_KEYSTORE_DEBUG_USER`, and `GODOT_ANDROID_KEYSTORE_DEBUG_PASSWORD` environment variables before exporting. No private signing material is committed.

The APK path is `builds/Starfall-Miner-Android.apk`, package **`com.starfall.miner.demo`**, version **0.3.1 / code 4**, ARM64, minimum API 24, target API 36. **The previous demo's private signing key and original APK are absent locally. A newly signed APK cannot be claimed to update the installed demo without comparing signing certificates.** Uninstalling an existing demo may erase its save; retain the matching original signing key for an in-place update.

The official 4.7.2 prebuilt APK template supports a `net9.0` target. The installed .NET 10 SDK compiles it without installing a separate .NET 9 SDK. Target SDK 36 and minimum SDK 24 come from the template; SDK overrides require Gradle. Build-tools 35.0.1 are accepted for this export, as verified by successful packaging and signature/alignment checks.

Godot's official documentation describes C# Android support as experimental. Physical Android input, OS rotation animation, suspend/resume, and device-specific graphics require a phone pass. Desktop resizing and injected Viewport touch events are identified separately in `QA_REPORT.md`.

The template's .NET 9 target reaches the end of Microsoft support on **10 November 2026**. A later runtime update requires a compatible newer template or a Gradle export targeting .NET 10. [Microsoft support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).

## Local inspection

```sh
python3 tools/dev.py qa
# In a second terminal:
python3 tools/inspect.py '{"command":"state"}'
python3 tools/inspect.py '{"command":"screenshot","name":"review.png"}'
python3 tools/inspect.py '{"command":"touch","x":108,"y":428,"finger":2,"pressed":true}'
python3 tools/inspect.py '{"command":"quit"}'
```

The C# QA scene binds only `127.0.0.1:47831`, requires `--qa`, and uses isolated saves. It supports state, freeze, resize, touch, drag, emulated mouse, engine mouse press/release (`desktop_mouse`) and motion (`desktop_motion`), lifecycle notifications, screenshots, and quit. Desktop events use `Input.ParseInputEvent` with window-scaled coordinates; touch injection uses `Viewport.PushInput`. Neither is a physical hardware test. PNGs are restricted to `builds/qa/`. Rendering is forced for screenshots even when macOS occludes the window behind the IDE. Runtime logs and `rendered-results.json` are also under `builds/qa/`. The QA scene is not enabled by the normal main scene and is excluded from native exports.

There is no Web export preset. The older browser demo in the surrounding delivery directory is legacy GDScript output and does not validate this C# game. Godot 4 currently does not support official C# Web exports.

## Official references

- [C# setup, VS Code, SDKs, and platform caveats](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html)
- [C# platform support](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/)
- [Android export requirements and signing](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_android.html)
- [Android command-line tools](https://developer.android.com/studio#command-tools)

## License

Original code, procedural graphics, and synthesized sound are MIT-licensed; see `LICENSE`. DejaVu fonts retain `assets/FONT_LICENSE.txt`. `THIRD_PARTY_NOTICES.txt` contains the Godot and engine dependency notices. Auto Drill: Galaxy was a gameplay reference; none of its assets or code are included.

## Editing presentation in Godot

`scenes/main.tscn` contains the actual persistent scene tree: `Starfield`, `WorldView/PersistentWorld/Planet`, `Visuals`, `MiningSound`, `FlightInput` and the `UI/Hud` instance. Open this scene to inspect the hierarchy; **do not create these nodes again from C#**. `Main.cs` coordinates them and owns simulation state, rather than constructing a scene by hand.

- **Change the planet’s shape, ore probabilities and difficulty:** open `resources/planet_generation.tres`. This is gameplay **design data**; `PlanetGenerator.cs` uses it to generate a new deterministic tile grid for each sector. The tiles are **not** prefabbed, and every new planet is generated at runtime. Existing saved tile grids are preserved until the next generated sector.
- **Change the planet’s presentation:** the eight new presets are resources under `resources/world_styles/`; `resources/planet_visual_style.tres` supplies Classic Grid. Each exposes a surface treatment, mineral/damage colors, cell size, background, edge tint, and glow. The `VisualStyles` array on `StarfallMiner` registers their pause-menu order. Keep IDs stable for saved preferences. `PlanetRenderer` draws the same generated tile data for every style, using reusable polygon buffers and visible-cell culling.
- **Change world colors:** edit the selected preset's `OreColors` / `DamagedColors`, `Backdrop`, `StarTint`, and `EdgeTint`. These are independent of the geometry treatment. `resources/starfall_palette.tres` remains the shared weapon/ship/modal palette and the fallback for Classic Grid. `resources/hologram_style.tres` controls terminal HUD inks and halos.
- **Change fonts and reusable UI:** open `scenes/ui/hud.tscn`. Fonts, shared theme, and the reusable `TouchButton` / `Panel` scenes are assigned in the Inspector. Adjust margins, borders and corner radii in `resources/hud_theme.tres`; runtime colors come from the palette (the theme is duplicated and tinted at startup).
- **Change holographic activation:** `resources/mode_transition.tres` exposes `ActivationDuration` (3 s), `StaggerFraction`, `IgnitionPeak`, and `JitterPixels`, alongside the camera settings. `scenes/ui/hologram_channel.tscn` supplies the reusable module. `resources/hud_atmosphere.tres` is the low-resolution gradient that keeps floating readouts legible over ore.
- **Change manual handling:** open `resources/manual_flight.tres` to tune `CruiseSpeed`, `MiningStandOff`, `ApproachResponse`, and `PointerDeadZone`. These are gameplay settings, separate from the camera and HUD animation resource.
- **Change logic:** `Main.cs` handles game state, `ManualNavigation.cs` handles held steering and safe path clearing, `PlanetGenerator.cs` handles pure procedural tile creation, `MiningPlanet.cs` handles destructible ore state and collisions, `PlanetRenderer.cs` handles drawing, `WorldView.cs` handles portrait framing and camera following, `Hud.cs` handles responsive UI positions and interactions, `Visuals.cs` draws transient effects, and `Starfield.cs` owns the background.

The destructible grid, particles, ship, and route marker are drawn procedurally. Thousands of tiny changing cells should not become thousands of editor nodes. The responsive HUD layout and modals are still generated in `Hud.cs`, so not every label or control is individually editable in the scene tree. The palette and reusable button/panel appearances are editor-editable.

Run `python3 tools/dev.py test`, `python3 tools/dev.py test-rendered`, and inspect both modes in portrait with Godot 4.7.2 .NET / .NET 10. A standalone game window remains the verified macOS rendering path; embedded editor play has not been revalidated since removing mode-driven resizing.

## Procedural planet data / rendering boundary

The main scene instances `scenes/world/planet.tscn`, which contains a **Planet** simulation node with a **Renderer** child. `PlanetGenerator.Generate(sector, settings, tiles)` creates the seeded `Dictionary<Vector2I, OreBlock>` with no scene or drawing dependencies; `MiningPlanet` owns that data, collision and damage rules, emits mining events, and announces visible changes with `TilesChanged`. `PlanetRenderer` listens to those changes, reads the blocks and `VisualPalette`/`PlanetVisualStyle`, and draws only visible cells. Changing the look preserves ore position, health, mining speed, loot, and saved terrain; only the optional appearance preference changes.

Editor resources author appearance and generation rules rather than individual tiles. Do not put thousands of ore cells into `main.tscn`: generate the state and render it in one CanvasItem. The v1 save format and exact saved tiles are retained; current generation intentionally produces larger planets than the original prototype. The ship and dynamic effects are drawn in `Visuals.cs`; a future improvement could give the ship its own editable appearance resource or scene.

## Autopilot and handoff animation (latest)

- `AutopilotSteering.cs` owns automatic flight navigation, capped at **155 world units per second** with bounded collision avoidance. If the planet-centered orbit would strand remaining ore outside mining reach, it approaches and orbits the nearest surviving block instead, retaining a firing-range buffer. `ManualNavigation.cs` follows the held pointer at up to **190 world units per second**, slowing to maintain mining clearance. The default stand-off adds 40 world units beyond the hull collision check; it does not snap or teleport the ship away from terrain.
- `WorldView.cs` follows the ship and eases manual zoom without changing world coordinates. Both modes use portrait framing.
- `Hud.cs` swaps portrait interfaces while retaining the live world. `HologramChannel.cs` energizes modules in staggered order, with diminishing luminance fluctuations and minimal geometric jitter. Control geometry and hit regions move together; held steering remains responsive throughout.
- `scenes/ui/mode_status.tscn` is a compact link-status readout using the same ignition envelope. It stabilizes after 3 seconds and stays readable rather than covering the flight view with a large announcement.
- Open `resources/mode_transition.tres` to tune camera and ignition timing without editing C#. The earlier slide/status-duration fields have been replaced by the activation controls above.

Holographic glow uses a few antialiased vector halo strokes and font outlines; the atmosphere is a 1 × 128 gradient texture. No HDR renderer change, full-screen blur, extra viewport, or runtime-generated glow texture is required. Animation processing stops for each module once settled. Physical mobile frame time and thermal behavior still require device profiling.

**Desktop note:** Switching modes preserves the native window dimensions. Hold the left mouse button and move to steer; release to stop.

Run `python3 tools/dev.py test` for the expanded autopilot regression checks, and `python3 tools/dev.py run` to judge the animation visually. The latter is important: animation timing and look-and-feel need a real viewport review.
