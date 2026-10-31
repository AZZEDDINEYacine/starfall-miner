# Starfall Miner 0.2

An editable Godot 4.5.2 space-mining incremental, written in GDScript. Import `project.godot` and press F5. The game has no package, plugin, account, or network dependency.

## This revision

- Menu buttons handle real touchscreen fingers directly, including a second finger while the first steers. Synthesized mouse input cannot duplicate purchases or steal a joystick.
- Automatic mining uses a portrait dashboard with a prominent **TAKE CONTROL** action. Loadout and upgrades live in **SHIP SYSTEMS**; collected-resource statistics live in **CARGO LOG**.
- Manual flight requests landscape orientation on Android. Management panels disappear, leaving a joystick, fire control, three small weapon selectors, boost, and compact resources. **AUTO PILOT** returns to portrait.
- Mode changes retain the same planet, ship, projectiles, and debris. The camera eases its zoom and the flight HUD fades in without a scene reload.
- The graphics use flat squares and a geometric ship. Blocks grow persistently brighter as their health decreases.
- Destroyed ore produces two small fragments. They drift briefly, then magnetize into the ship. Credits and energy arrive on collection, with no floating reward-text clutter.

## Controls

| Action | Touch | Desktop |
| --- | --- | --- |
| Enter manual flight | TAKE CONTROL | Button or Tab |
| Return to auto mining | AUTO PILOT | Button or Tab |
| Steer | Drag the lower-left flight area | WASD / arrows |
| Fire | Hold the right FIRE circle; tap ore for a bonus shot | Space / click ore |
| Change weapons | Ship Systems in auto; bottom selectors in flight | 1 / 2 / 3 or buttons |
| Overdrive | BOOST, costing 5 energy | E |
| Upgrades | Ship Systems | Same |
| Resource statistics | Cargo Log or the flight resource strip | Same |
| Pause and sound | II | Button or P / Escape |

The Android APK changes orientation automatically. Desktop Godot changes its window shape. Web browsers may require the user to rotate their phone or resize the window; the simulation and controls adapt without restarting.

## Mining and progression

Autopilot flies, targets, fires, and collects resources on its own. All three weapons are available immediately: a rapid piercing lance, an area-damage cannon, and lightning that links up to five targets. Credits purchase firepower, fire rate, and ore-yield upgrades. Blue ore supplies energy for an eight-second overdrive. Red ore chains explosions through nearby blocks. Destroy every block to collect the sector bonus and warp to another planet. Equipment and upgrades carry over.

## Saves and updating

The package ID and signing certificate are unchanged from 0.1, and the Android version code has increased to 2. The APK can update the earlier installation. Its existing save remains readable. Uncollected debris is now saved as well, preventing lost or duplicated rewards after reopening. The save location remains `user://starfall_save_v1.json`.

Saving happens every eight seconds, on purchases and sector changes, on pause, and on focus loss or a normal close. Automatic mode awards catch-up credits on reopening at 25% of the last observed rate, capped at four hours. This does not simulate planet destruction while the game is closed. Session statistics restart on reopening; lifetime statistics remain saved. Android and browser saves are separate.

## Project files

- `scripts/main.gd`: simulation, orientation, camera, input, weapons, debris collection, and saves.
- `scripts/hud.gd`: separate portrait and landscape interfaces, loadout, upgrades, and resource log.
- `scripts/touch_button.gd`: finger-owned touch buttons with canceled-touch and modal handling.
- `scripts/planet.gd`: grid generation, damage, chain reactions, and brightness-based health.
- `scripts/visuals.gd`: flat ship, beams, impacts, and debris.
- `scripts/sound.gd`: synthesized sound effects.
- `tests/smoke.gd`: game logic and real Viewport touch-input regression checks.
- `assets/`: fonts and their license, icon, and web loader.
- `export_presets.cfg`: Web, Android, and Linux presets.

## Validation

Use disposable user data for the save/load tests:

```bash
XDG_DATA_HOME="$(mktemp -d)" godot --headless --path . --script res://tests/smoke.gd -- --test
```

The suite sends `InputEventScreenTouch` events through the real Viewport input pipeline to exercise menu activation, modal barriers, canceled touches, single purchases, mode switches, multi-finger weapon selection, and firing. It also checks mining, increasing block brightness, debris collection and exact rewards, progression, pause, and saves. The separate browser pass uses actual emulated touchscreen events for every menu action. Mouse clicks are not substituted for those menu tests.

The APK has been packaged and signed, but physical Android hardware behavior still needs a device pass, particularly the OS rotation animation and lifecycle. The previous touchscreen-menu failure was reproduced before the controls were replaced.

## Export

Use matching Godot 4.5.2 export templates. Android uses Java 17 and the Android SDK configured in Godot's Editor Settings. The preset exports an ARM64 APK for Android 7.0 or newer without Gradle. Its debug signing key is kept outside the source project; use your own local debug key when exporting. Publishing needs your own release signing and AAB configuration.

The Web preset uses the single-threaded template and needs an HTTP(S) server. The outer demo package includes a Python launcher. Do not open its HTML directly as a local file.

Official documentation:

- https://docs.godotengine.org/en/4.5/tutorials/export/exporting_for_android.html
- https://docs.godotengine.org/en/4.5/classes/class_displayserver.html#class-displayserver-method-screen-set-orientation
- https://docs.godotengine.org/en/4.5/classes/class_input.html

## License

Original game code, procedural graphics, and synthesized sound are MIT-licensed; see `LICENSE`. DejaVu fonts retain `assets/FONT_LICENSE.txt`. `THIRD_PARTY_NOTICES.txt` contains Godot's license and engine dependency notices. Auto Drill: Galaxy was a gameplay reference; its assets and code are not included.
