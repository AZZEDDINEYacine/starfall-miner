# Starfall Miner

**Build preference:** Do not build or export an Android APK/AAB unless the user explicitly requests it. Gameplay development and verification should use local C# builds, headless regressions, and rendered desktop runs. An earlier request to produce an APK does not authorize packaging later changes.

Godot 4.7.2 **.NET**, .NET 10 SDK, C#. Desktop targets net10.0; the official prebuilt Android template requires net9.0. Start at `scenes/main.tscn` → `scripts/Main.cs`.

- `Main`: simulation, weapons, economy, loot, progression. `MiningPlanet`: seeded typed grid and damage.
- `FlightInput` / `TouchButton`: independent finger ownership. `WorldView`: camera and orientation only.
- `Hud` / `Visuals` / `MiningSound`: presentation. `SaveStore`: explicit legacy v1 JSON schema.
- `Main.Progression` / `EquipmentState`: recipes, material inventory, unique item/mount ownership, market transactions, credit upgrades, ranks and drone mining. `Hud.Workshop` / `ShipSchematic`: paged workshop controls with a persistent fitted-hull preview. Recipes and rank thresholds are authored in `resources/crafting_catalog.tres`; deposit resistance/depths and accelerating sector growth in `resources/planet_generation.tres`.
- Use `double` for health/currency/credit totals, `long` for accumulated resource counters, Godot vectors for positions. Avoid per-frame LINQ, dictionary-shaped entities, or fresh collections.
- Mode changes must preserve the live planet, ship, projectiles, and debris. Never reload the scene to rotate.
- Both modes remain portrait. Manual play requires a held world pointer: press starts steering, dragging changes direction, release/cancellation stops. Recompute the steering point as the camera follows, so a stationary held thumb keeps moving beyond its original world position. `ManualNavigation` slows to a safe stand-off while firing at blocking rocks, then continues. Tune speed/clearance in `resources/manual_flight.tres`. Do not replace this with released-tap waypoints, sticks, a separate fire control, or mode-driven window resizing without an explicit design change.
- HUD presentation uses open holographic telemetry and `HologramChannel` modules. `resources/mode_transition.tres` owns the 3-second ignition budget/stagger/overshoot/jitter (abrupt neon strikes, blackouts and stepped displacement; avoid smooth breathing pulses); `resources/hologram_style.tres` owns ink and vector halos. Keep activation responsive, bounded, and independent of simulation. Settled modules stop processing; do not reintroduce whole-HUD slide/fade entrances or large per-frame blur effects.
- World designs are `PlanetVisualStyle` resources registered in `Main.VisualStyles` and compared from pause. Keep the reference-inspired Clean Frontier simple; color is independent of geometry. A visual switch must preserve the live planet, tile health, collision, generation settings, wallet, ship and pending rewards. Original/Classic Grid and Clean Frontier must remain easy to select; eight minimal technical designs precede the older decorative archive. Keep stable style IDs and the optional `visual_style` save field backward compatible. Reuse drawing buffers and cull tiles; do not create per-cell scene nodes.

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

Touch requirements: inject real `InputEventScreenTouch` / `InputEventScreenDrag` through `Viewport.PushInput`. Test movement while the finger remains DOWN, drag-to-turn, continued steering under a stationary thumb as the camera follows, release-to-stop, and mining stand-off/slowdown. Also test the engine mouse-input path at scaled window sizes, not just a direct Viewport click that sets a flag. Menu touches must work while steering, and purchases must charge once despite synthesized mouse events. Canceled/outside releases must stop steering and must not activate buttons. Modals must block controls immediately, including during deferred rebuilds. Do not replace the steering finger with another finger. Clear ownership, navigation, and menu presses on resize, mode change, pause, and focus loss. Weapon selection may retain held steering.

Keep `user://starfall_save_v1.json`, `version: 1`, and serialized names compatible with 0.1/0.2 fixtures. Preserve remaining block health and pending pickup rewards exactly once. Tests/inspection must never touch a player's save. Do not run persistence suites concurrently.

Crafting/trades/upgrades must validate the full transaction before spending. Never mount one weapon twice or leave the ship unarmed. The mothership has one primary plus up to two purchased auxiliary hardpoints; each drone has one mount. Drones cruise independent mining routes and must recall/dock before chassis upgrades, mounted-weapon upgrades or weapon transfers. Recall transfers may continue during paused workshop servicing without mining or collecting rewards. Persist their recall/deploy orders. Drones require fitted weapons and use real weapon behavior; they must not silently mine while unarmed or paused. Save rare-fragment payloads with inventory so collection cannot duplicate rewards. Preserve legacy weapon access, shared upgrade levels (now diminishing bonus formulas), and exact saved worlds; new generation uses the new economy. Offline income awards credits only, and market sales must not inflate its observed mining rate.

Keep Android ID `com.starfall.miner.demo`; increase version code for a new release. Never claim update compatibility without matching certificates. Never commit keystores, export credentials, generated caches, or SDKs. Describe injected touch, desktop resizing, and physical Android testing separately. Record actual rendering inspection and platform limitations in QA_REPORT.md.
