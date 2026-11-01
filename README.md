# Starfall Miner 0.3 — Godot C#

The current level-design branch is described in [LEVEL_DESIGN.md](LEVEL_DESIGN.md).

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

## Develop on Windows

Use **Godot 4.7.2 .NET** and the **.NET 10 SDK**. The regular Godot editor cannot run this C# project, and a different Godot release may not match the project SDK. The default `NuGet.Config` points to a Mac app bundle; `tools/dev.py` selects `NuGet.Windows.Config` on Windows, which restores the matching Godot C# packages from NuGet.org. Set `GODOT4` to the full path of the matching Godot .NET executable and put `dotnet` on `PATH` before using the Windows VS Code launch configuration. `py -3 tools/dev.py build` builds the C# assembly; then the **Starfall Miner C# (Windows)** debug configuration opens the game. Debugging new C# changes requires a local build. Exporting an APK is a separate action.

To inspect the level design immediately, choose **Planet preview (Windows)** in VS Code Run and Debug after opening this folder. It builds and launches the real game at the first world; **F8** moves forward and **F7** moves back through all 13 worlds. Preview play uses a fresh session and never reads or writes your normal save. Use the regular Windows launch configuration or Godot F5 for your saved campaign.

Tests and inspection require `--test` and `--qa`, respectively. These runs never load or modify `user://starfall_save_v1.json`; their explicit persistence tests use `.godot/tests/`. Do not run two persistence regression suites concurrently because they share that disposable file.

## Controls and behavior

| Action | Touch | Desktop |
| --- | --- | --- |
| Enter manual flight | TAKE CONTROL | Button or Tab |
| Return to autopilot | AUTO PILOT | Button or Tab |
| Steer | Drag lower-left flight area | WASD / arrows |
| Fire | Hold FIRE; tap ore for a bonus shot | Space / click ore |
| Weapons | Ship Systems or flight selectors | 1 / 2 / 3 |
| Overdrive | BOOST, costing 5 energy | E |
| Upgrades | Ship Systems | Same |
| Resource statistics | Cargo Log / wallet | Same |
| Pause / sound | II | Button or P / Escape |

Automatic mode requests portrait orientation. Manual flight requests landscape on Android and reshapes the window on macOS. Camera and HUD transitions retain the live planet, ship, projectiles, and collectible fragments. Left steering and right firing have independent ownership; another finger can change weapons. Focus loss, OS pause, resizing, modal opening, and mode changes clear held input, including button presses.

The three original weapons, five block types, volatile cascades, upgrades, eight-second overdrive, sector bonuses, and endless progression are retained. Flat square blocks become persistently brighter as health falls. Each destroyed block emits two tiny fragments, one carrying its reward; credits and energy arrive on collection. Visible effects remain restrained and bounded. Difficulty and reward growth retain the original sector-501 cap.

## Architecture

- `scripts/Main.cs`: simulation coordination, movement, weapons, collection, economy, and progression.
- `scripts/MiningPlanet.cs`: seeded typed ore grid, damage, chain reactions, persistent health colors.
- `scripts/SimulationModels.cs`: typed blocks, fragments, shells, and short-lived effects. Currency, health, and accumulated credit values use `double`; accumulated resource counters use `long`; positions use Godot vectors.
- `scripts/FlightInput.cs`, `scripts/TouchButton.cs`: world gestures and independent direct-touch menu ownership.
- `scripts/WorldView.cs`: camera transforms, resize handling, and orientation requests.
- `scripts/Hud.cs`, `scripts/Visuals.cs`, `scripts/MiningSound.cs`: presentation and synthesized sound.
- `scripts/SaveStore.cs`: validated v1 JSON and atomic temporary-file replacement.
- `tests/Smoke.cs`: actual C# scene integration tests with Viewport touch injection and legacy-save fixtures.
- `tests/QaBridge.cs`: opt-in localhost inspection scene, excluded from exported assemblies and resources.

The pre-migration game is recoverable from Git checkpoint `ce1e4b6`. Superseded GDScript and the Web export shell have been retired.

## Saves

The production filename and wire format remain **`user://starfall_save_v1.json`**, with `version: 1` and the original serialized field names. The application name and Android package identity remain unchanged. Both 0.1-format saves without pickup fields and 0.2 saves with pending collectible rewards are tested. C# planet generation is checked against the original serializer's seeded planet output.

Saving occurs every eight seconds, after purchases, weapon/mode/sound changes and boost activation, on sector completion/progression, pause, focus loss, and normal close. Pending rewards and wallet values are committed together, preventing lost or duplicated collection on reopening a saved state. Automatic mode retains offline credits at 25% of the last observed rate, capped at four hours, without destroying planet blocks offline. Catch-up is immediately committed so it cannot be claimed twice. Malformed saves are rejected before applying partial state.

## Native exports

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

The APK path is `builds/Starfall-Miner-Android.apk`, package **`com.starfall.miner.demo`**, version **0.3.0 / code 3**, ARM64, minimum API 24, target API 36. **The previous demo's private signing key and original APK are absent locally. A newly signed APK cannot be claimed to update the installed demo without comparing signing certificates.** Uninstalling an existing demo may erase its save; retain the matching original signing key for an in-place update.

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

The C# QA scene binds only `127.0.0.1:47831`, requires `--qa`, and uses isolated saves. It supports state, freeze, resize, touch, drag, emulated mouse, lifecycle notifications, screenshots, and quit. PNGs are restricted to `builds/qa/`. Rendering is forced for screenshots even when macOS occludes the window behind the IDE. Runtime logs and `rendered-results.json` are also under `builds/qa/`. This is the working local inspection mechanism; no unverified MCP server is required. It is not enabled by the normal main scene and is excluded from native exports.

There is no Web export preset. The older browser demo in the surrounding delivery directory is legacy GDScript output and does not validate this C# game. Godot 4 currently does not support official C# Web exports.

## Official references

- [C# setup, VS Code, SDKs, and platform caveats](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html)
- [C# platform support](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/)
- [Android export requirements and signing](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_android.html)
- [Android command-line tools](https://developer.android.com/studio#command-tools)

## License

Original code, procedural graphics, and synthesized sound are MIT-licensed; see `LICENSE`. DejaVu fonts retain `assets/FONT_LICENSE.txt`. `THIRD_PARTY_NOTICES.txt` contains the Godot and engine dependency notices. Auto Drill: Galaxy was a gameplay reference; none of its assets or code are included.
