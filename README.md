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

The CLI launcher uses `GODOT4` first, then an existing executable configured by `godotTools.editorPath.godot4` in `.vscode/settings.json`, then `godot-mono` / `godot4-mono` / `godot` on PATH, with the original macOS path as its final fallback. It prints the selected executable when launching Godot. This project requires the **.NET edition**: a standard Godot binary cannot load `.cs` scripts even when the .NET SDK is installed.

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

The HUD uses a midnight-navy observatory palette, open white/cyan telemetry, amber reactor segments, sharp Inter typography, a fine illuminated viewport frame and glass surfaces with restrained upper-edge highlights. Shared buttons distinguish normal, hover, pressed, selected, disabled and modal keyboard-focus states. Background depth comes from detailed static blue/violet nebula artwork, a cropped distant ringed planet, fine orbital lines and 156 foreground stars; decoration never receives input. Wide desktop windows use compact header modules, centered flight controls and a workshop with its fitted-hull preview beside the content. Portrait retains the flight hit areas; shorter workshop panels scroll their rows while keeping the preview, status and Back controls visible. The live hull sits over a static orbital drydock diagram. Phone orientation remains portrait.

On startup and mode changes, six manual HUD modules ignite in staggered order over **3 seconds**, with a white-hot overshoot, abrupt failed neon strikes, brief blackouts, diminishing contact chatter and visible stepped jitter. Modal cards use the same ignition on entry. Ship/Craft/Fleet/Market share one workshop visit: switching or reselecting tabs, paging, purchases and resizing keep it settled. Closing and reopening the workshop can play its entry ignition again. There is no whole-HUD slide or alpha fade on entry, and the new galaxy and terrain lighting are static. Touch targets remain usable during ignition, and closing a modal does not replay the full HUD sequence.

Manual mode is designed for one thumb: hold on space to guide the ship and drag that same finger to turn. The held point is updated as the camera follows, so the ship keeps moving while your thumb stays down rather than stopping at the initial world point. Release to stop. Dragging onto the ship enters a small dead zone; dragging away resumes steering without lifting. The ship slows on approach to blocking ore, mines from a safety stand-off, then continues as the path clears. A mint guide turns gold while mining. `resources/manual_flight.tres` exposes cruising speed, extra mining clearance, approach response, and the pointer dead zone in the Inspector.

Held manual mining gives the ship **1.5× damage and 1.6× firing cadence**, or **2.4× nominal weapon output** versus the same automatic loadout. A controlled 60-second starter corridor test produced 86 blocks / 172 CR manually versus 40 / 80 on autopilot; actual gains depend on steering, terrain and loadout. Independent drones receive no mode-switch buff. It does not shoot unrelated ore while idle or traveling through empty space. Weapon selection preserves held steering; pause, focus loss, resizing, modal opening, mode changes, and canceled releases clear it. Moving the held finger onto UI suspends movement until it returns to the play area. Desktop uses the same hold-and-drag behavior with the left mouse button.

New starting planets have **twice the previous diameter**, with radius 54 cells and roughly 9,000 blocks (about four times the previous ore area). Radius grows each sector as `54 + 6 × log2(sector)` cells: 54 / 60 / 66 / 78 at sectors 1 / 2 / 4 / 16. This keeps later planets increasing without the old radius cap or clipping against the former grid boundary. Existing saved planets keep their exact tiles; larger generation applies to the next sector or a reset.

Six weapon behaviors share five terrain types, reduced volatile cascades, eight-second overdrive, and endless sectors. New games start with one Photon lance; additional weapons and drones must be crafted. Ore becomes brighter as health falls in every design. Rewards, including rare materials, arrive through collectible fragments. Planet hardness accelerates with sector and continues beyond sector 501; credit richness increases slowly. The starter now hits one block, and equipment upgrades use restrained additive scaling.

## Crafting, fleet, and progression

The first Power/Rate upgrades cost **25 / 20 CR**, both affordable from the starting 80 CR. The first cannon costs 50 CR + 1 Titanium (130 CR including market materials), and the new continuous Flux beam costs 35 CR + 1 Titanium (115 CR including materials); Scout costs 180 CR + 2 Titanium. First purchases are discounted and later costs rise steeply; Power gains stay substantial at every rank. In a scripted automatic purchase plan the cannon arrives at 40 seconds, armed Scout at 2:30, second ship weapon at 5:30 and Nova wave at 9:15. See `PROGRESSION.md` for the reproducible benchmark, caveats and full curves.

For feature testing, **Pause → TEST / +1B CR** adds one billion credits per press and shows the current wallet. Grants save normally and do not count as mining income or research.

Open **Ship Systems**, or **Pause → Fabricator / Fleet / Market** during manual flight. The four tabs cover ship/refinery, crafting, owned equipment, and the intergalactic market. A live fitted-hull schematic stays visible throughout the workshop, including weapon and drone upgrades.

- Most rocks award credits only. Titanium deposits begin at 35% depth, Iridium at 60%, and Aether at 82%; the planetary core guarantees Aether. Full color-coded resource cells mark deposits in both designs; additional mining resistance is 2.8× / 4.5× / 7×.
- Craft Photon lances, Nova cannons, Ion chains, expanding Nova waves, Bore lasers, continuous Flux beams, and Scout/Survey/Atlas drones. Recipes mainly consume rare materials, with a smaller credit cost. The market buys/sells materials at fixed credit prices, providing an alternative to deep mining.
- Drones start unarmed. In **Fleet → Drones**, use **Fit / Change** to assign a reserve weapon. Press **Deploy** to send it mining independently. Each drone has one mount; **Ship** sells a second/third mothership hardpoint at ranks 2/3. **Fleet → Weapons → Fit Ship** adds auxiliaries, **Set Primary** selects the primary, and **Unfit Aux** frees a mount. All fitted ship weapons fire together.
- In **Fleet → Weapons**, separate **Power** and **Fire Rate** buttons upgrade each owned weapon. Every Power level adds 60% of base damage, reaching 4× at level 5 and 13× at level 20. Rate levels add 10% cadence each for the first three levels, then 2% each. Both tracks cost credits and follow the weapon onto ship or drone mounts. Both upgrade tracks have engineering-rank limits. Drone chassis have their own credit upgrades. Ship refinery upgrades improve credit recovery. Saved shared combat levels remain, with diminishing bonus formulas; new combat upgrades are per item.
- Engineering ranks unlock at sectors **1/2/4/7** or lifetime mining milestones **0/150/1,200/5,000 rocks**, whichever comes first. They unlock blueprints, raise item limits and expand the fleet to three bays. Ship Systems displays the next objective. See `PROGRESSION.md` for recipes, prices, and tuning rationale.

Armed drones choose their own routes around the planet and mine with the same weapon behavior as the ship. Use **Recall** before upgrades or weapon changes; service is rejected until docking. Return movement continues while the workshop is paused, with mining and rewards frozen. After servicing, press **Deploy** again. Missions persist across loading. There are no offline rare-resource rewards. Existing saved worlds retain their tiles/health and previously available weapons; newly generated planets use the deep-deposit economy. The market is available while finishing an older planet without deposit metadata.

The **Flux beam** is a rank-one craft in Fabricator → Weapons (second blueprint page); equip it as primary in Fleet → Weapons. In manual mode, holding and dragging aims it precisely through the pointer, independently of the ship's turning or navigation obstacle selection. It remains continuous while held, stops at the first rock face, and ceases immediately on release or cancellation. A seamless electrical hum follows firing and stops on release, pause or mute. Remaining damage after breaking a rock carries to the newly exposed face within the same frame. Power-max tests clear 41 deep ordinary sector-one rocks or 44 deep ordinary sector-four rocks in ten seconds without Rate upgrades or overdrive (15/30/60 fps). Harder rare deposits and later sectors still take longer. Power/Fire Rate upgrades improve damage per second; auxiliaries and drones support the same beam behavior. Nova wave now draws a translucent blue body, rough boundary and trailing outward rings.

## Compare world designs

Open **Pause → Compare World Designs**. The chooser stays at the bottom while the same frozen world previews each selection immediately. Resume keeps the chosen appearance; it is also remembered in the save. Switching never regenerates terrain, changes collision, alters remaining health or rewards, or moves the ship. It works in both manual and automatic modes.

Only two designs remain: **Clean Frontier (Default)** and **Original (Classic Grid)**. The other presets and their rendering branches have been removed. Older saves that selected one fall back to Clean Frontier while preserving all planet/equipment/progression data.

Clean Frontier samples one continuous detailed basalt texture across surviving ordinary squares, with fixed upper-left hemispheric lighting, broad relief and a blue rim on exposed edges. The texture is drawn only for remaining ordinary cells, so excavated gaps remain open space. `SurfaceTexture`, `TextureGain`, `SurfaceDetail` and `DirectionalLight` in its resource control the treatment. Original retains its flat grid; both retain damage brightening and deterministic sector hues. Resource/charge rendering, colors, collision and terrain data are unchanged. The distant ringed body is screen-space background decoration and has no gameplay identity.

Resources occupy their **own complete colored grid squares**, with no rock inset, badge, stripes or identifying glyph. Titanium is silver, Iridium violet, Aether amber; gold and crystals use gold/cyan, and impact charges are red. Titanium/gold stay plain. Selected energy cells share a subtle fusion-like hot core with circulating plasma; animation is capped at 128 orbiting particles, uses no per-cell nodes and pauses with the world.

Rock hue changes between sectors in both designs, including damage rims and common debris. Resource colors stay consistent so material identification does not change.

**Red charges detonate on the first damaging hit.** A targeted charge previews its **two-cell (32 world-unit) blast radius** with a dashed circle. A bright shockwave expands quickly to the actual radius and stays readable while fading. Local falloff damage breaks nearby shallow rock, chips tougher ore and can trigger other charges; new planets contain fewer charges to keep chains limited. Large chains resolve iteratively and preserve exact pickup rewards.

The portrait orientation is configured in `project.godot` and applied at mobile startup. The earlier Android jump-cut helper remains in the source but is no longer called by mode changes.

## Architecture

- `scripts/Main.cs`: simulation coordination, movement, weapons, collection, economy, and progression.
- `scripts/ProgressionTuning.cs`, `resources/progression.tres`: early purchase discounts, later upgrade curves and hardpoint prices.
- `scripts/Main.Progression.cs`, `scripts/EquipmentState.cs`: owned equipment, crafting/trades, unique mounts, credit upgrades, rank goals, and autonomous drone behavior.
- `scripts/Hud.Workshop.cs`, `resources/crafting_catalog.tres`: Fabricator/Fleet/Market screens and editor-authored recipes, material prices/colors, and rank milestones.
- `scripts/MiningPlanet.cs`, `scripts/PlanetGenerator.cs`: seeded typed ore grid, damage, chain reactions, and hull collisions; generation settings live in `resources/planet_generation.tres`.
- `scripts/ManualNavigation.cs`, `scripts/AutopilotSteering.cs`, `resources/autopilot_flight.tres`: held steering and a local-perception reactive pilot with authored sensing/clearance rules.
- `scripts/SurveyMinimap.cs`: cached terrain survey, live heading/ship/drone markers and camera footprint, independent of autopilot decisions.
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
python3 tools/qa_client.py '{"command":"state"}'
python3 tools/qa_client.py '{"command":"screenshot","name":"review.png"}'
python3 tools/qa_client.py '{"command":"touch","x":108,"y":428,"finger":2,"pressed":true}'
python3 tools/qa_client.py '{"command":"quit"}'
```

The helper is named `qa_client.py` so it does not shadow Python's standard-library `inspect` module when launching scripts from `tools/` (including Python 3.14's argument-parser color support).

The C# QA scene binds only `127.0.0.1:47831`, requires `--qa`, and uses isolated saves. It supports state, freeze, resize, touch, drag, emulated mouse, engine mouse press/release (`desktop_mouse`) and motion (`desktop_motion`), lifecycle notifications, screenshots, and quit. Desktop events use `Input.ParseInputEvent` with window-scaled coordinates; touch injection uses `Viewport.PushInput`. Neither is a physical hardware test. PNGs are restricted to `builds/qa/`. Rendering is forced for screenshots even when macOS occludes the window behind the IDE. Runtime logs and `rendered-results.json` are also under `builds/qa/`. The QA scene is not enabled by the normal main scene and is excluded from native exports.

There is no Web export preset. The older browser demo in the surrounding delivery directory is legacy GDScript output and does not validate this C# game. Godot 4 currently does not support official C# Web exports.

## Official references

- [C# setup, VS Code, SDKs, and platform caveats](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html)
- [C# platform support](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/)
- [Android export requirements and signing](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_android.html)
- [Android command-line tools](https://developer.android.com/studio#command-tools)

## License

Original code, procedural graphics, and synthesized sound are MIT-licensed; see `LICENSE`. The interface bundles unmodified Inter Regular and Semibold from the [official Inter 4.1 release](https://github.com/rsms/inter/releases/tag/v4.1), with its SIL Open Font License in `assets/INTER_LICENSE.txt`. Both export presets include that notice. Retained DejaVu fonts keep `assets/FONT_LICENSE.txt`. No installed system font is required. `THIRD_PARTY_NOTICES.txt` contains the Godot and engine dependency notices. Auto Drill: Galaxy was a gameplay reference; none of its assets or code are included.

The user-approved original space artwork is bundled under `assets/art/`; `assets/art/README.md` records the built-in imagegen prompts, dimensions and intended use. The two supplied visual references are not bundled. No remote service, downloaded image or installed image package is required at runtime. Small UI glass surfaces are authored SVGs under `assets/ui/`.

## Editing presentation in Godot

`scenes/main.tscn` contains the actual persistent scene tree: `Starfield`, `WorldView/PersistentWorld/Planet`, `Visuals`, `MiningSound`, `FlightInput` and the `UI/Hud` instance. Open this scene to inspect the hierarchy; **do not create these nodes again from C#**. `Main.cs` coordinates them and owns simulation state, rather than constructing a scene by hand.

- **Change the planet’s shape, ore probabilities and difficulty:** open `resources/planet_generation.tres`. This is gameplay **design data**; `PlanetGenerator.cs` uses it to generate a new deterministic tile grid for each sector. The tiles are **not** prefabbed, and every new planet is generated at runtime. Existing saved tile grids are preserved until the next generated sector.
- **Change the planet’s presentation:** `resources/world_styles/neon_frontier.tres` supplies Default and `resources/planet_visual_style.tres` supplies Original. The `VisualStyles` array on `StarfallMiner` registers their pause-menu order. Keep their IDs stable for saved preferences. `PlanetRenderer` draws the same tile data with visible-cell culling; rare-resource colors come from `resources/crafting_catalog.tres`.
- **Change world colors:** edit the selected preset's `OreColors` / `DamagedColors`, `Backdrop`, `StarTint`, and `EdgeTint`. These are independent of the geometry treatment. `resources/starfall_palette.tres` remains the shared weapon/ship/modal palette and the fallback for Classic Grid. `resources/hologram_style.tres` controls terminal HUD inks and halos.
- **Change fonts and reusable UI:** open `scenes/ui/hud.tscn`. Fonts, shared theme, and the reusable `TouchButton` / `Panel` scenes are assigned in the Inspector. Adjust margins, borders and corner radii in `resources/hud_theme.tres`; runtime colors come from the palette (the theme is duplicated and tinted at startup).
- **Change the static galaxy:** the palette's `NebulaArtwork`, `DistantPlanetArtwork` and opacity fields supply the shared images under `assets/art/`. `Starfield` cover-crops the nebula, places the distant planet primarily behind the command area and draws three fixed orbital arcs. Its 18 procedural cloud patches remain a fallback when no nebula is assigned. There is no animation process, parallax or input surface.
- **Change glass surfaces:** `resources/hologram_style.tres` references the two nine-slice SVG surfaces in `assets/ui/`. `GlassSurface.cs` paints them beneath native control text. `ObservatoryFrame.cs` supplies the static edge frame; all decorative controls ignore input and do not process frames.
- **Change holographic activation:** `resources/mode_transition.tres` exposes `ActivationDuration` (3 s), `StaggerFraction`, `IgnitionPeak`, and `JitterPixels`, alongside the camera settings. `scenes/ui/hologram_channel.tscn` supplies the reusable module. `resources/hud_atmosphere.tres` is the low-resolution gradient that keeps floating readouts legible over ore.
- **Change manual handling:** open `resources/manual_flight.tres` to tune `CruiseSpeed`, `MiningStandOff`, `ApproachResponse`, and `PointerDeadZone`. These are gameplay settings, separate from the camera and HUD animation resource.
- **Change logic:** `Main.cs` handles game state, `ManualNavigation.cs` handles held steering and safe path clearing, `PlanetGenerator.cs` handles pure procedural tile creation, `MiningPlanet.cs` handles destructible ore state and collisions, `PlanetRenderer.cs` handles drawing, `WorldView.cs` handles portrait framing and camera following, `Hud.cs` handles responsive UI positions and interactions, `Visuals.cs` draws transient effects, and `Starfield.cs` owns the background.

The destructible grid, particles, ship, and route marker are drawn procedurally. Thousands of tiny changing cells should not become thousands of editor nodes. The responsive HUD layout and modals are still generated in `Hud.cs`, so not every label or control is individually editable in the scene tree. The palette and reusable button/panel appearances are editor-editable.

Run `python3 tools/dev.py test`, `python3 tools/dev.py test-rendered`, and inspect both modes in portrait with Godot 4.7.2 .NET / .NET 10. A standalone game window remains the verified macOS rendering path; embedded editor play has not been revalidated since removing mode-driven resizing.

For the new artwork, run `python3 tools/dev.py build`, then open this project in the existing **.NET Godot editor** and wait for the new PNG/SVG imports. On the Fedora setup the headless `import` command has a recorded native editor-exit crash; use the normal editor import path for this step. Then launch with `python3 tools/dev.py run` and run the documented checks sequentially. The newer artwork/glass revision has only been inspected in source and as image assets by the assistant; earlier passing test counts do not validate it.

## Procedural planet data / rendering boundary

The main scene instances `scenes/world/planet.tscn`, which contains a **Planet** simulation node with a **Renderer** child. `PlanetGenerator.Generate(sector, settings, tiles)` creates the seeded `Dictionary<Vector2I, OreBlock>` with no scene or drawing dependencies; `MiningPlanet` owns that data, collision and damage rules, emits mining events, and announces visible changes with `TilesChanged`. `PlanetRenderer` listens to those changes, reads the blocks and `VisualPalette`/`PlanetVisualStyle`, and draws only visible cells. Changing the look preserves ore position, health, mining speed, loot, and saved terrain; only the optional appearance preference changes.

Editor resources author appearance and generation rules rather than individual tiles. Do not put thousands of ore cells into `main.tscn`: generate the state and render it in one CanvasItem. The v1 save format and exact saved tiles are retained; current generation intentionally produces larger planets than the original prototype. The ship and dynamic effects are drawn in `Visuals.cs`; a future improvement could give the ship its own editable appearance resource or scene.

## Autopilot and handoff animation (latest)

- `AutopilotSteering.cs` uses local rules rather than a planet-centered orbit: scan a forward cone for the first visible face; keep a target long enough to finish it; approach and brake toward an 88-unit center distance; aim and shoot in the mining band; separate from nearby rocks; sweep movement in small steps and slide/back out at contact. Small lateral corrections vary over time. If ore disappears from view, turn to look around before surveying expanding legs from last contact. The pilot uses neither hidden ore, the global nearest-block list, nor minimap data. Top speed stays **155 world units/s**, with acceleration and turn limits. Slight inherited hull overlaps use continuous penetration-reducing recovery. `ManualNavigation.cs` retains held steering, up to **190 units/s** and 40 units of extra mining clearance. Switching back to autopilot resets transient velocity/target memory and preserves the live excavated planet and ship position.
- `resources/autopilot_flight.tres` exposes sensing range (320), forward half-cone (85°), target distance/tolerance, separation, acceleration, turn rate and scan interval. Forward rays stop at the first rock and firing rechecks line of sight. Independent drone transfer routes remain separate from the mothership's local pilot.
- A read-only **Survey minimap** appears in both flight modes. It shows remaining terrain, the ship and heading, deployed/returning drones, and the camera footprint. Far-away ship markers clamp to its edge. The coarse terrain raster updates at most five times per second when terrain changes; marker motion stays fluid. Camera-only redraws reuse the raster, and touching the map does not start hidden steering.
- `WorldView.cs` follows the ship and eases manual zoom without changing world coordinates. Both modes use portrait framing.
- `Hud.cs` swaps portrait interfaces while retaining the live world. `HologramChannel.cs` energizes modules in staggered order, with diminishing luminance fluctuations and minimal geometric jitter. Control geometry and hit regions move together; held steering remains responsive throughout.
- `scenes/ui/mode_status.tscn` is a compact link-status readout using the same ignition envelope. It stabilizes after 3 seconds and stays readable rather than covering the flight view with a large announcement.
- Open `resources/mode_transition.tres` to tune camera and ignition timing without editing C#. The earlier slide/status-duration fields have been replaced by the activation controls above.

Holographic glow uses a few antialiased vector halo strokes around controls and instrument traces; labels stay sharp. The HUD atmosphere is a 1 × 128 gradient texture. No HDR renderer change, full-screen blur, extra viewport, or runtime-generated glow texture is required. Animation processing stops for each module once settled. Physical mobile frame time and thermal behavior still require device profiling.

**Desktop note:** Switching modes preserves the native window dimensions. Hold the left mouse button and move to steer; release to stop.

Run `python3 tools/dev.py test` for the expanded autopilot regression checks, and `python3 tools/dev.py run` to judge the animation visually. The latter is important: animation timing and look-and-feel need a real viewport review.
