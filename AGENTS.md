# Starfall Miner

Godot 4.7.2 **.NET**, .NET 10 SDK, C#. Desktop targets net10.0; the official prebuilt Android template requires net9.0. Start at `scenes/main.tscn` → `scripts/Main.cs`.

- `Main`: simulation, weapons, economy, loot, progression. `MiningPlanet`: seeded typed grid and damage.
- `FlightInput` / `TouchButton`: independent finger ownership. `WorldView`: camera and orientation only.
- `Hud` / `Visuals` / `MiningSound`: presentation. `SaveStore`: explicit legacy v1 JSON schema.
- Use `double` for health/currency/credit totals, `long` for accumulated resource counters, Godot vectors for positions. Avoid per-frame LINQ, dictionary-shaped entities, or fresh collections.
- Mode changes must preserve the live planet, ship, projectiles, and debris. Never reload the scene to rotate.

Commands from the project root:

```sh
python3 tools/dev.py import        # first checkout: build and asset import
python3 tools/dev.py build
python3 tools/dev.py test          # C# regressions; isolated .godot/tests saves
python3 tools/dev.py test-rendered # same suite with rendering
python3 tools/rendered_check.py    # live localhost connection, touch checks, screenshots
python3 tools/dev.py run
python3 tools/dev.py export-macos
python3 tools/dev.py export-android
```

`GODOT4` overrides the engine executable. VS Code tasks and launch configs are included. See README for template/toolchain setup. Export configurations exclude test code and QA scenes. Do not use browser output as C# validation.

Touch requirements: inject real `InputEventScreenTouch` / `InputEventScreenDrag` through `Viewport.PushInput`. Mouse-only tests are insufficient. Menu touches must work with a second finger while steering, and purchases must charge once despite synthesized mouse events. Canceled gestures and outside releases must not activate buttons. Modals must block controls behind them immediately, including during deferred rebuilds. Do not replace a held steering or firing finger. Release each owner independently; clear **all** flags and ownership IDs, including menu presses, on resize/orientation, mode change, pause, and focus loss.

Keep `user://starfall_save_v1.json`, `version: 1`, and serialized names compatible with 0.1/0.2 fixtures. Preserve remaining block health and pending pickup rewards exactly once. Tests/inspection must never touch a player's save. Do not run persistence suites concurrently.

Keep Android ID `com.starfall.miner.demo`; increase version code for a new release. Never claim update compatibility without matching certificates. Never commit keystores, export credentials, generated caches, or SDKs. Describe injected touch, desktop resizing, and physical Android testing separately. Record actual rendering inspection and platform limitations in QA_REPORT.md.
