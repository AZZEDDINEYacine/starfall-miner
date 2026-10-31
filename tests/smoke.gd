extends SceneTree
## Actual game logic plus touch events through the real Viewport UI pipeline.
var checks := 0
var failures: Array[String] = []

func _initialize() -> void:
	call_deferred("_run")

func check(condition: bool, description: String) -> void:
	checks += 1
	if condition:
		print("PASS: " + description)
	else:
		failures.append(description)
		push_error("FAIL: " + description)

func touch(point: Vector2, down: bool, finger: int = 7, canceled: bool = false) -> void:
	var event := InputEventScreenTouch.new()
	event.position = point
	event.index = finger
	event.pressed = down
	event.canceled = canceled
	# Headless DisplayServer does not forward OS events. push_input enters the
	# real Viewport pipeline (_input, GUI, and _unhandled_input) with local coordinates.
	root.push_input(event, true)

func tap_button(game, id: String, finger: int = 8) -> void:
	var button: Button = game.hud.buttons[id]
	var point := button.get_global_rect().get_center()
	touch(point, true, finger)
	touch(point, false, finger)
	await process_frame
	await process_frame

func same_tiles(a: Dictionary, b: Dictionary) -> bool:
	if a.size() != b.size(): return false
	for key in a:
		if not b.has(key): return false
		for field in ["hp", "max_hp", "value"]:
			if not is_equal_approx(float(a[key][field]), float(b[key][field])): return false
		for field in ["kind", "shade"]:
			if a[key][field] != b[key][field]: return false
	return true

func _run() -> void:
	root.size = Vector2i(540, 960)
	await process_frame
	await process_frame
	var script = load("res://scripts/main.gd")
	var game = script.new()
	root.add_child(game)
	await process_frame
	game.set_process(false)
	game.sound.enabled = false
	check(game.planet.blocks.size() > 200, "Destructible planet is generated")
	var original: Dictionary = game.planet.blocks.duplicate(true)
	game.planet.generate(1)
	check(same_tiles(original, game.planet.blocks), "Sector generation is deterministic")
	var kinds := {}
	for block in game.planet.blocks.values(): kinds[block.kind] = true
	check(kinds.size() == 5, "Five ore types exist")
	check(Input.is_emulating_mouse_from_touch(), "Touch emulation is enabled in the shipped project")
	# Real touch events through the Viewport, without calling a button callback.
	await tap_button(game, "systems")
	check(game.hud.panel_kind == "systems", "A real touchscreen tap opens Ship Systems")
	await tap_button(game, "mode")
	check(game.auto_mode and game.hud.panel_kind == "systems", "A modal blocks touch buttons behind it")
	var cancel_point: Vector2 = game.hud.buttons.weapon_1.get_global_rect().get_center()
	touch(cancel_point, true, 8)
	touch(cancel_point, false, 8, true)
	await process_frame
	check(game.weapon == 0, "A canceled touchscreen press does not activate a button")
	await tap_button(game, "weapon_1")
	check(game.weapon == 1, "A real touchscreen tap selects Nova")
	await tap_button(game, "weapon_2")
	check(game.weapon == 2, "A real touchscreen tap selects Ion")
	await tap_button(game, "weapon_0")
	check(game.weapon == 0, "A real touchscreen tap selects Lance")
	var wallet: float = game.credits
	var cost: float = game.upgrade_cost(0)
	await tap_button(game, "upgrade_0")
	if game.levels[0] != 1 or game.credits != wallet - cost:
		print("Purchase detail: wallet=", wallet, " cost=", cost, " now=", game.credits, " level=", game.levels[0], " panel=", game.hud.panel_kind)
	check(game.levels[0] == 1 and game.credits == wallet - cost, "A touch purchase charges exactly once, without an emulated-mouse duplicate")
	await tap_button(game, "done")
	await tap_button(game, "cargo")
	check(game.hud.panel_kind == "cargo", "Cargo Log opens by touch")
	await tap_button(game, "close")
	check(game.hud.panel_kind == "", "Modal close works by touch")
	await tap_button(game, "pause")
	check(game.paused, "Pause works by touch")
	var muted_before: bool = game.muted
	await tap_button(game, "sound")
	check(game.muted != muted_before, "Sound setting works by touch")
	await tap_button(game, "resume")
	check(not game.paused, "Resume works by touch")
	var count_before: int = game.planet.blocks.size()
	var ship_before: Vector2 = game.ship_pos
	var planet_id: int = game.planet.get_instance_id()
	await tap_button(game, "mode")
	check(not game.auto_mode, "The prominent mode switch works by touch")
	check(game.planet.get_instance_id() == planet_id and game.planet.blocks.size() == count_before and game.ship_pos == ship_before, "Changing flight mode preserves the live planet and ship")
	game.transition_left = 0
	# Resize the real Window to exercise layout, rather than resetting the game.
	root.size = Vector2i(1120, 540)
	await process_frame
	await process_frame
	game._place_world(true)
	game.hud._rebuild()
	check(game.view_size.x > game.view_size.y, "Landscape uses a wide flight viewport")
	check(not game.hud.buttons.has("systems") and game.hud.buttons.has("boost"), "Flight HUD removes management controls and exposes flight controls")
	var stick: Vector2 = game.stick_center()
	touch(stick, true, 2)
	await process_frame
	check(game.joystick_active and game.joystick_id == 2, "A touchscreen finger owns the flight stick")
	await tap_button(game, "weapon_2", 3)
	check(game.weapon == 2 and game.joystick_active and game.joystick_id == 2, "A second finger can change weapons without stealing the stick")
	touch(game.fire_center(), true, 4)
	await process_frame
	check(game.fire_held and game.joystick_active, "Two-finger steering and firing work together")
	touch(Vector2(950, 50), false, 4)
	await process_frame
	check(not game.fire_held and game.joystick_active, "Releasing fire over the HUD releases only the fire finger")
	touch(stick, false, 2)
	await process_frame
	check(not game.joystick_active, "Releasing the steering finger releases the stick")
	await tap_button(game, "mode")
	root.size = Vector2i(540, 960)
	await process_frame
	await process_frame
	game.transition_left = 0
	check(game.auto_mode, "Return to autopilot works by touch")
	for weapon in range(3):
		game.planet.generate(1)
		game.warp_timer = 0
		game.ship_pos = game.CENTER + Vector2(-192, 0)
		game.weapon = weapon
		game.projectiles.clear()
		game.debris.clear()
		game.levels.assign([0, 0, 0])
		var blocks: int = game.planet.blocks.size()
		var credits: float = game.credits
		for shot in range(12):
			game.target_key = game.planet.nearest(game.ship_pos - game.CENTER)
			game.shot_timer = 0
			game.fire(false)
			for frame in range(22): game._update_effects(0.05)
		check(game.planet.blocks.size() < blocks, "Weapon %d destroys ore" % weapon)
		check(game.credits > credits, "Weapon %d recovers credits via collected debris" % weapon)
	for kind in range(5):
		var sample := {"kind": kind, "hp": 10.0, "max_hp": 10.0}
		var full: Color = game.planet.block_color(sample)
		sample.hp = 5.0
		var half: Color = game.planet.block_color(sample)
		sample.hp = 1.0
		var weak: Color = game.planet.block_color(sample)
		check(full.get_luminance() < half.get_luminance() and half.get_luminance() < weak.get_luminance(), "Ore type %d gets progressively brighter as health falls" % kind)
	game.debris.clear()
	game.projectiles.clear()
	game.planet.generate(1)
	var crystal := Vector2i.ZERO
	for key in game.planet.blocks:
		if game.planet.blocks[key].kind == 2:
			crystal = key
			break
	var reward: float = game.planet.blocks[crystal].value
	var credits_before: float = game.credits
	var energy_before: int = game.flux
	game.planet.damage(crystal, 10000)
	check(game.debris.size() == 2 and game.credits == credits_before, "Breaking ore creates subtle debris without crediting before collection")
	for i in range(60): game._update_debris(0.05)
	check(game.debris.is_empty() and is_equal_approx(game.credits, credits_before + reward) and game.flux == energy_before + 1, "Debris homes to the ship and pays the exact credit and energy reward")
	for i in range(60): game._update_debris(0.05)
	check(is_equal_approx(game.credits, credits_before + reward), "Collected debris cannot pay twice")
	game.planet.blocks = {
		Vector2i(0, 0): {"hp": 1.0, "max_hp": 1.0, "kind": 3, "shade": 0, "value": 3.0},
		Vector2i(1, 0): {"hp": 1.0, "max_hp": 1.0, "kind": 3, "shade": 0, "value": 3.0},
		Vector2i(2, 0): {"hp": 1.0, "max_hp": 1.0, "kind": 1, "shade": 0, "value": 15.0}}
	game.planet.clear_sent = false
	game.planet.damage(Vector2i.ZERO, 2)
	check(game.planet.blocks.is_empty() and game.warp_timer > 0, "Adjacent explosives cascade and clearing schedules warp")
	game.warp_timer = 0
	game.planet.generate(1)
	game.flux = 10
	game.overdrive_cooldown = 0
	game.activate_overdrive()
	check(game.flux == 5 and game.overdrive_left == 8, "Overdrive consumes energy and activates")
	game.activate_overdrive()
	check(game.flux == 5, "Overdrive cannot spend twice during cooldown")
	game.toggle_pause()
	var paused_credits: float = game.credits
	var paused_blocks: int = game.planet.blocks.size()
	for i in range(90): game._process(1.0 / 30.0)
	check(game.credits == paused_credits and game.planet.blocks.size() == paused_blocks, "Pause freezes mining and debris collection")
	game.toggle_pause()
	await process_frame
	game.reset_progress()
	await process_frame
	game.sound.enabled = false
	game.weapon = 1
	game.levels.assign([3, 3, 2])
	game.auto_mode = true
	for i in range(18000):
		game._process(1.0 / 30.0)
		if game.sector >= 3: break
	check(game.sector >= 3, "Autopilot clears two full planets and continues into sector 3")
	check(game.levels == [3, 3, 2] and game.weapon == 1, "Upgrades and weapons carry through warp")
	game.credits = 12345
	game.flux = 17
	game.no_save = false
	game.save_progress()
	game.no_save = true
	var saved_tiles: Dictionary = game.planet.blocks.duplicate(true)
	var pickup_count: int = game.debris.size()
	var pending_value := 0.0
	var pending_energy := 0
	for fragment in game.debris:
		pending_value += float(fragment.value)
		pending_energy += int(fragment.energy)
	game.credits = 0
	game.flux = 0
	game.debris.clear()
	game._load_progress()
	check(game.credits == 12345 and game.flux == 17, "Save/load preserves the wallet")
	check(same_tiles(saved_tiles, game.planet.blocks), "Save/load preserves every block and remaining health")
	check(game.debris.size() == pickup_count, "Uncollected debris survives saving and reopening")
	for i in range(65): game._update_debris(0.05)
	check(is_equal_approx(game.credits, 12345 + pending_value) and game.flux == 17 + pending_energy, "Restored debris pays once without losing resources")
	# The Android update must also retain a save written before debris existed.
	var legacy = JSON.parse_string(FileAccess.get_file_as_string(game.SAVE_PATH))
	for key in ["pickups", "lifetime_energy", "ship_x", "ship_y"]:
		legacy.erase(key)
	legacy.timestamp = Time.get_unix_time_from_system()
	var legacy_file := FileAccess.open(game.SAVE_PATH, FileAccess.WRITE)
	legacy_file.store_string(JSON.stringify(legacy, "", true, true))
	legacy_file.close()
	game.credits = 0
	game._load_progress()
	check(game.credits == 12345 and game.flux == 17 and same_tiles(saved_tiles, game.planet.blocks), "An original 0.1 save migrates with wallet and planet intact")
	check(script.compact(1200000.0) == "1.2M", "Large incremental currency is readable")
	print("RESULT: %d checks; %d failures" % [checks, failures.size()])
	for failure in failures: print("FAILED: " + failure)
	game.queue_free()
	await process_frame
	quit(0 if failures.is_empty() else 1)
