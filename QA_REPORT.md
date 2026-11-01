# Starfall Miner 0.3 migration verification

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
