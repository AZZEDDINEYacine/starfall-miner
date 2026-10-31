extends Node2D
## Simulation coordinates stay fixed while the camera and HUD change orientation.

const PlanetScript = preload("res://scripts/planet.gd")
const SoundScript = preload("res://scripts/sound.gd")
const VisualScript = preload("res://scripts/visuals.gd")
const HUDScript = preload("res://scripts/hud.gd")
const CENTER := Vector2(270, 408)
const SAVE_PATH := "user://starfall_save_v1.json"
const WEAPONS := ["Photon lance", "Nova cannon", "Ion chain"]
const WEAPON_COLORS := [Color("79f3ce"), Color("ffb574"), Color("b5a2ff")]
const SECTOR_NAMES := ["THE CINDER BELT", "AZURE DRIFT", "VIOLET REACH", "VERDANT ECHO"]
const UPGRADE_BASE := [45.0, 60.0, 50.0]
const UPGRADE_GROWTH := [1.62, 1.7, 1.67]

var planet: MiningPlanet
var sound: MiningSound
var hud: Control
var visuals: Node2D
var stage: Node2D
var view_size := Vector2(540, 960)
var transition_left := 0.0
var camera_scale := 1.0
var camera_target_scale := 1.0
var layout_revision := 0
var rng := RandomNumberGenerator.new()
var clock := 0.0
var credits := 80.0
var flux := 0
var sector := 1
var weapon := 0
var levels: Array[int] = [0, 0, 0]
var auto_mode := true
var paused := false
var muted := false
var total_earned := 0.0
var total_mined := 0
var session_mined := 0
var session_credits := 0.0
var session_energy := 0
var lifetime_energy := 0
var orbit_angle := -2.2
var ship_pos := CENTER + Vector2(-155, -150)
var ship_angle := 0.0
var ship_velocity := Vector2.ZERO
var target_key := Vector2i(999, 999)
var aim_key := Vector2i(999, 999)
var shot_timer := 0.3
var tap_timer := 0.0
var overdrive_left := 0.0
var overdrive_cooldown := 0.0
var warp_timer := 0.0
var warp_duration := 2.8
var income_window := 0.0
var income_accum := 0.0
var income_rate := 0.0
var save_timer := 0.0
var hud_timer := 0.0
var hit_shake := 0.0
var fire_held := false
var joystick_active := false
var joystick_origin := Vector2.ZERO
var joystick_position := Vector2.ZERO
var joystick_id := -1
var touch_fire_id := -1
var aim_position := Vector2.ZERO
var pointer_down := false
var particles: Array[Dictionary] = []
var beams: Array[Dictionary] = []
var rings: Array[Dictionary] = []
var floating_texts: Array[Dictionary] = []
var projectiles: Array[Dictionary] = []
var debris: Array[Dictionary] = []
var stars: Array[Dictionary] = []
var no_save := false
var first_run := true
var help_open := false
var reset_armed := false
var web_qa := false
var qa_timer := 0.0

func _ready() -> void:
	no_save = "--test" in OS.get_cmdline_user_args()
	if OS.has_feature("web"):
		web_qa = bool(JavaScriptBridge.eval("new URLSearchParams(location.search).has('qa')"))
	rng.seed = 8086
	for i in range(38):
		stars.append({"p": Vector2(rng.randf(), rng.randf()), "a": rng.randf_range(0.1, 0.35)})
	stage = Node2D.new()
	stage.name = "PersistentWorld"
	add_child(stage)
	planet = PlanetScript.new()
	planet.position = CENTER
	stage.add_child(planet)
	planet.block_mined.connect(_on_block_mined)
	planet.planet_cleared.connect(_on_planet_cleared)
	planet.blast_triggered.connect(_on_chain_blast)
	sound = SoundScript.new()
	add_child(sound)
	planet.generate(sector)
	if not no_save:
		_load_progress()
	visuals = VisualScript.new()
	visuals.game = self
	stage.add_child(visuals)
	var canvas := CanvasLayer.new()
	add_child(canvas)
	hud = HUDScript.new()
	hud.game = self
	canvas.add_child(hud)
	view_size = get_viewport_rect().size
	get_viewport().size_changed.connect(_on_viewport_resized)
	_place_world(true)
	_apply_orientation()
	get_tree().auto_accept_quit = false
	get_tree().quit_on_go_back = false
	if first_run:
		toast("AUTOPILOT ONLINE", "Tap TAKE CONTROL to fly. Ship systems are below.", Color("79f3ce"), 3.0)
	queue_redraw()

func _process(delta: float) -> void:
	delta = minf(delta, 0.05)
	clock += delta
	transition_left = maxf(0.0, transition_left - delta)
	_place_world(false, delta)
	if web_qa:
		qa_timer += delta
		if qa_timer > 0.2:
			qa_timer = 0
			var state := {"credits": credits, "flux": flux, "sector": sector, "weapon": weapon, "levels": levels, "auto": auto_mode, "paused": paused, "blocks": planet.blocks.size(), "total": planet.total_blocks, "ship_x": ship_pos.x, "ship_y": ship_pos.y, "overdrive": overdrive_left, "warp": warp_timer, "mined": total_mined, "income": income_rate, "touch_steer": joystick_active, "touch_fire": fire_held, "pending_debris": debris.size(), "session_credits": session_credits, "session_energy": session_energy, "width": view_size.x, "height": view_size.y, "transition": transition_left, "panel": hud.panel_kind, "buttons": hud.qa_buttons(), "layout_revision": layout_revision}
			JavaScriptBridge.eval("window.starfallState = " + JSON.stringify(state) + ";")
	_update_effects(delta)
	queue_redraw()
	visuals.queue_redraw()
	if paused:
		return
	overdrive_left = maxf(0, overdrive_left - delta)
	overdrive_cooldown = maxf(0, overdrive_cooldown - delta)
	tap_timer = maxf(0, tap_timer - delta)
	shot_timer -= delta
	hit_shake = maxf(0.0, hit_shake - delta * 18.0)
	if warp_timer > 0:
		warp_timer -= delta
		ship_pos = ship_pos.lerp(CENTER + Vector2(0, -350), delta * 1.7)
		ship_angle = lerp_angle(ship_angle, -PI * 0.5, delta * 5.0)
		if warp_timer <= 0:
			_advance_sector()
	else:
		_move_ship(delta)
		var local_ship := ship_pos - CENTER
		target_key = planet.nearest(local_ship)
		if planet.blocks.has(aim_key):
			var aimed := _ray_target(ship_pos, CENTER + planet.key_point(aim_key))
			if planet.blocks.has(aimed):
				target_key = aimed
		if planet.blocks.has(target_key):
			var target_pos := CENTER + planet.key_point(target_key)
			ship_angle = lerp_angle(ship_angle, (target_pos - ship_pos).angle(), delta * 12.0)
			if (auto_mode or (not hud.blocks_world_input() and (fire_held or Input.is_physical_key_pressed(KEY_SPACE)))) and shot_timer <= 0:
				fire(false)
		if rng.randf() < delta * 35 and ship_velocity.length() > 3:
			var behind := ship_pos - Vector2.from_angle(ship_angle) * 17.0
			_add_particle(behind, -Vector2.from_angle(ship_angle) * 65 + Vector2(rng.randf_range(-10, 10), rng.randf_range(-10, 10)), Color("7aeacb"), 0.25, 2.4)
	income_window += delta
	if income_window >= 2.0:
		income_rate = lerpf(income_rate, income_accum / income_window, 0.65)
		income_accum = 0
		income_window = 0
	save_timer += delta
	if save_timer >= 8.0:
		save_timer = 0.0
		save_progress()
	hud_timer += delta
	if hud_timer >= 0.12:
		hud_timer = 0.0
		hud.refresh()

func _move_ship(delta: float) -> void:
	var previous := ship_pos
	if auto_mode:
		orbit_angle += delta * 0.24
		var desired_radius := planet.surface_radius(orbit_angle) + 53.0
		var desired := CENTER + Vector2.from_angle(orbit_angle) * desired_radius
		ship_pos = ship_pos.lerp(desired, 1.0 - exp(-delta * 2.6))
		# The shrinking irregular surface must never pull the hull into an intact tile.
		if planet.contains_point(ship_pos - CENTER, 8.0):
			ship_pos = CENTER + (ship_pos - CENTER).normalized() * (desired_radius + 14.0)
	else:
		var movement := Vector2.ZERO
		movement.x = float(Input.is_physical_key_pressed(KEY_D) or Input.is_physical_key_pressed(KEY_RIGHT)) - float(Input.is_physical_key_pressed(KEY_A) or Input.is_physical_key_pressed(KEY_LEFT))
		movement.y = float(Input.is_physical_key_pressed(KEY_S) or Input.is_physical_key_pressed(KEY_DOWN)) - float(Input.is_physical_key_pressed(KEY_W) or Input.is_physical_key_pressed(KEY_UP))
		if joystick_active:
			movement += (joystick_position - joystick_origin).limit_length(48.0) / 48.0
		if is_instance_valid(hud) and hud.blocks_world_input():
			movement = Vector2.ZERO
		movement = movement.limit_length(1.0)
		var displacement := movement * 190.0 * delta
		var next_x := ship_pos + Vector2(displacement.x, 0)
		if not planet.contains_point(next_x - CENTER, 9.0):
			ship_pos.x = next_x.x
		var next_y := ship_pos + Vector2(0, displacement.y)
		if not planet.contains_point(next_y - CENTER, 9.0):
			ship_pos.y = next_y.y
		var bounds := manual_world_bounds()
		if transition_left <= 0:
			ship_pos.x = clampf(ship_pos.x, bounds.position.x, bounds.end.x)
			ship_pos.y = clampf(ship_pos.y, bounds.position.y, bounds.end.y)
	ship_velocity = (ship_pos - previous) / maxf(delta, 0.001)

func _on_viewport_resized() -> void:
	view_size = get_viewport_rect().size
	layout_revision += 1
	release_controls()
	_place_world(false)
	if is_instance_valid(hud):
		hud.request_layout()

func _place_world(snap: bool = false, delta: float = 0.0) -> void:
	if not is_instance_valid(stage):
		return
	view_size = get_viewport_rect().size
	var center: Vector2
	if auto_mode:
		var top := 218.0
		var bottom := view_size.y - 194.0
		center = Vector2(view_size.x * 0.5, (top + bottom) * 0.5)
		camera_target_scale = minf((view_size.x - 48) / 450.0, (bottom - top - 12) / 450.0)
	else:
		center = Vector2(view_size.x * 0.5, view_size.y * 0.50)
		camera_target_scale = minf((view_size.x - 180) / 450.0, (view_size.y - 106) / 450.0)
	camera_target_scale = clampf(camera_target_scale, 0.6, 1.35)
	camera_scale = camera_target_scale if snap else lerpf(camera_scale, camera_target_scale, 1.0 - exp(-delta * 8.0))
	stage.scale = Vector2.ONE * camera_scale
	# Re-anchor during the OS rotation; the simulated planet, ship, and loot never move.
	stage.position = center - CENTER * camera_scale

func screen_to_world(point: Vector2) -> Vector2:
	return (point - stage.position) / maxf(camera_scale, 0.01)

func world_to_screen(point: Vector2) -> Vector2:
	return point * camera_scale + stage.position

func manual_world_bounds() -> Rect2:
	var start := screen_to_world(Vector2(22, 94))
	var finish := screen_to_world(view_size - Vector2(22, 22))
	return Rect2(start, finish - start)

func stick_center() -> Vector2:
	return Vector2(108, view_size.y - 112)

func fire_center() -> Vector2:
	return Vector2(view_size.x - 108, view_size.y - 112)

func release_controls() -> void:
	fire_held = false
	joystick_active = false
	joystick_id = -1
	touch_fire_id = -1
	pointer_down = false

func _apply_orientation() -> void:
	if OS.has_feature("android") or OS.has_feature("ios"):
		DisplayServer.screen_set_orientation(DisplayServer.SCREEN_PORTRAIT if auto_mode else DisplayServer.SCREEN_LANDSCAPE)
	elif not OS.has_feature("web") and not no_save and DisplayServer.get_name() != "headless":
		get_window().size = Vector2i(540, 960) if auto_mode else Vector2i(960, 540)
	elif OS.has_feature("web"):
		JavaScriptBridge.eval("window.dispatchEvent(new CustomEvent('starfallmode', {detail: {auto: %s}}));" % ("true" if auto_mode else "false"))

func _ray_target(from: Vector2, toward: Vector2) -> Vector2i:
	var distance := from.distance_to(toward)
	if distance < 1.0:
		return Vector2i(999, 999)
	var direction := (toward - from) / distance
	for step in range(0, int(minf(distance + 10.0, 420.0)), 4):
		var key := planet.point_key(from + direction * step - CENTER)
		if planet.blocks.has(key):
			return key
	return Vector2i(999, 999)

func damage_multiplier() -> float:
	return pow(1.46, levels[0]) * (1.5 if overdrive_left > 0 else 1.0)

func fire(manual_tap: bool = false) -> void:
	if paused or warp_timer > 0 or planet.blocks.is_empty():
		return
	if manual_tap and tap_timer > 0:
		return
	if not manual_tap and shot_timer > 0:
		return
	var key := target_key
	if not planet.blocks.has(key):
		key = planet.nearest(ship_pos - CENTER)
	if not planet.blocks.has(key):
		return
	var target := CENTER + planet.key_point(key)
	if ship_pos.distance_to(target) > 245.0:
		return
	var base_cooldown: float = [0.24, 0.86, 0.62][weapon]
	var cooldown := base_cooldown / (1.0 + levels[1] * 0.18) / (2.1 if overdrive_left > 0 else 1.0)
	if manual_tap:
		tap_timer = 0.16
	else:
		shot_timer = maxf(0.055, cooldown)
	var multiplier := damage_multiplier() * (1.15 if manual_tap else 1.0)
	var muzzle := ship_pos + Vector2.from_angle((target - ship_pos).angle()) * 19.0
	if weapon == 0:
		var direction := (target - muzzle).normalized()
		var hit_keys: Array[Vector2i] = []
		var endpoint := target
		for step in range(0, 53, 4):
			var candidate := planet.point_key(target + direction * step - CENTER)
			if planet.blocks.has(candidate) and candidate not in hit_keys:
				hit_keys.append(candidate)
				endpoint = CENTER + planet.key_point(candidate)
				if hit_keys.size() >= 3:
					break
		for i in range(hit_keys.size()):
			planet.damage(hit_keys[i], 1.65 * multiplier * pow(0.68, i))
		_add_beam(PackedVector2Array([muzzle, endpoint]), WEAPON_COLORS[0], 0.11, 3.0)
		_spawn_sparks(target, WEAPON_COLORS[0], 5)
		sound.play("laser", rng.randf_range(0.94, 1.06))
	elif weapon == 1:
		projectiles.append({"p": muzzle, "target": target, "damage": 4.4 * multiplier, "speed": 390.0, "life": 1.5})
		sound.play("blast", 1.6)
	else:
		var hit_keys: Array = [key]
		var path := PackedVector2Array([muzzle])
		var last := target
		for i in range(5):
			var hit: Vector2i = hit_keys[-1]
			if not planet.blocks.has(hit):
				break
			var p := CENTER + planet.key_point(hit)
			var from_point := path[-1]
			path.append(from_point.lerp(p, 0.4) + Vector2(rng.randf_range(-9, 9), rng.randf_range(-9, 9)))
			path.append(from_point.lerp(p, 0.72) + Vector2(rng.randf_range(-7, 7), rng.randf_range(-7, 7)))
			path.append(p)
			planet.damage(hit, 2.3 * multiplier * pow(0.87, i))
			_spawn_sparks(p, WEAPON_COLORS[2], 3)
			last = p
			var next := planet.nearest(last - CENTER, hit_keys)
			if next.x == 999 or planet.key_point(next).distance_to(last - CENTER) > 61.0:
				break
			hit_keys.append(next)
		_add_beam(path, WEAPON_COLORS[2], 0.18, 2.2)
		sound.play("arc", rng.randf_range(0.93, 1.08))
	if manual_tap:
		add_ring(target, 6.0, 26.0, WEAPON_COLORS[weapon], 0.22)

func set_weapon(index: int) -> void:
	weapon = clampi(index, 0, 2)
	shot_timer = minf(shot_timer, 0.1)
	hud.refresh()
	sound.play("mine", 0.7 + weapon * 0.15)

func set_auto(value: bool) -> void:
	if value == auto_mode:
		return
	auto_mode = value
	orbit_angle = (ship_pos - CENTER).angle()
	aim_key = Vector2i(999, 999)
	release_controls()
	transition_left = 0.55
	_apply_orientation()
	hud.close_panel()
	hud.request_layout()
	sound.play("upgrade", 0.65 if not value else 0.85)
	save_progress()

func upgrade_cost(index: int) -> float:
	return floorf(UPGRADE_BASE[index] * pow(UPGRADE_GROWTH[index], levels[index]))

func buy_upgrade(index: int) -> bool:
	var cost := upgrade_cost(index)
	if credits < cost:
		toast("MORE ORE NEEDED", "Keep mining to afford this upgrade.", Color("ffb574"), 1.6)
		return false
	credits -= cost
	levels[index] += 1
	sound.play("upgrade")
	hud.refresh()
	save_progress()
	return true

func activate_overdrive() -> void:
	if paused or overdrive_cooldown > 0 or warp_timer > 0:
		return
	if flux < 5:
		toast("ENERGY REQUIRED", "Mine blue crystals. Overdrive costs 5 energy.", Color("b5a2ff"), 2.5)
		return
	flux -= 5
	overdrive_left = 8.0
	overdrive_cooldown = 28.0
	add_ring(ship_pos, 10, 90, Color("b5a2ff"), 0.6)
	sound.play("upgrade", 0.8)
	toast("REACTOR OVERDRIVE", "Double fire rate. 50% more damage. 8 seconds.", Color("b5a2ff"), 2.5)
	hud.refresh()

func _on_block_mined(value: float, energy: int, point: Vector2, kind: int) -> void:
	var earned := value * (1.0 + levels[2] * 0.3)
	total_mined += 1
	session_mined += 1
	# Two small fragments drift briefly, then magnetize into the ship. Only the
	# first carries the reward, so every block pays exactly once at collection.
	for i in range(2):
		debris.append({"p": point + Vector2(rng.randf_range(-3, 3), rng.randf_range(-3, 3)), "v": Vector2.from_angle(rng.randf() * TAU) * rng.randf_range(20, 42), "age": -i * 0.035, "value": earned if i == 0 else 0.0, "energy": energy if i == 0 else 0, "kind": kind, "size": 2.4 if i == 0 else 1.6})
	if kind == MiningPlanet.GOLD or kind == MiningPlanet.CRYSTAL:
		sound.play("mine", 1.15 if kind == MiningPlanet.GOLD else 1.35)
	if aim_key == planet.point_key(point - CENTER):
		aim_key = Vector2i(999, 999)

func _collect_fragment(fragment: Dictionary) -> void:
	var value: float = fragment.value
	var energy: int = fragment.energy
	credits += value
	flux += energy
	total_earned += value
	income_accum += value
	session_credits += value
	session_energy += energy
	lifetime_energy += energy

func _update_debris(delta: float) -> void:
	for i in range(debris.size() - 1, -1, -1):
		var fragment: Dictionary = debris[i]
		fragment.age += delta
		var toward: Vector2 = ship_pos - fragment.p
		if toward.length() < 12.0 or fragment.age > 2.15:
			_collect_fragment(fragment)
			debris.remove_at(i)
			continue
		if fragment.age < 0.16:
			fragment.p += fragment.v * delta
		else:
			var speed: float = minf(650.0, 95.0 + (fragment.age - 0.16) * 780.0)
			fragment.v = fragment.v.lerp(toward.normalized() * speed, 1.0 - exp(-delta * 14.0))
			var step: Vector2 = fragment.v * delta
			if step.length() >= toward.length():
				_collect_fragment(fragment)
				debris.remove_at(i)
			else:
				fragment.p += step
	# Very large cascades retain every reward while bounding visible particles.
	while debris.size() > 360:
		_collect_fragment(debris[0])
		debris.remove_at(0)

func _on_chain_blast(point: Vector2) -> void:
	add_ring(point, 4, 57, Color("ffab7a"), 0.38)
	_spawn_sparks(point, Color("ffbd83"), 4)
	hit_shake = minf(hit_shake + 1.5, 5.0)
	sound.play("blast", 1.2)

func _on_planet_cleared() -> void:
	if warp_timer > 0:
		return
	warp_timer = warp_duration
	var bonus := floorf(150.0 * pow(1.22, minf(sector - 1, 500)))
	credits += bonus
	total_earned += bonus
	flux += 5
	session_credits += bonus
	session_energy += 5
	lifetime_energy += 5
	projectiles.clear()
	sound.play("warp")
	add_ring(CENTER, 30, 260, Color("b5a2ff"), 1.4)
	toast("SECTOR %02d COMPLETE" % sector, "+%s credits  ·  +5 energy  ·  preparing warp" % compact(bonus), Color("79f3ce"), 2.7)
	hud.refresh()
	save_progress()

func _advance_sector() -> void:
	sector += 1
	planet.generate(sector)
	orbit_angle = -2.2
	ship_pos = CENTER + Vector2.from_angle(orbit_angle) * 205
	aim_key = Vector2i(999, 999)
	shot_timer = 0.4
	projectiles.clear()
	toast("ENTERING SECTOR %02d" % sector, SECTOR_NAMES[(sector - 1) % 4] + "  /  tougher ore, richer rewards", Color("b5a2ff"), 2.8)
	hud.refresh()
	save_progress()

func _update_effects(delta: float) -> void:
	for i in range(particles.size() - 1, -1, -1):
		var p: Dictionary = particles[i]
		p.life -= delta
		if p.life <= 0:
			particles.remove_at(i)
		else:
			p.p += p.v * delta
			p.v *= 1.0 - delta * 2.0
	for collection in [beams, rings, floating_texts]:
		for i in range(collection.size() - 1, -1, -1):
			collection[i].life -= delta
			if collection[i].life <= 0:
				collection.remove_at(i)
	for label in floating_texts:
		label.p.y -= 25 * delta
	if paused:
		return
	_update_debris(delta)
	for i in range(projectiles.size() - 1, -1, -1):
		# A projectile hit can clear the planet and clear this whole array.
		if i >= projectiles.size():
			continue
		var shell: Dictionary = projectiles[i]
		shell.life -= delta
		var direction: Vector2 = shell.target - shell.p
		if direction.length() <= shell.speed * delta or shell.life <= 0:
			var impact: Vector2 = shell.target
			var damage: float = shell.damage
			projectiles.remove_at(i)
			add_ring(impact, 4, 43, WEAPON_COLORS[1], 0.4)
			_spawn_sparks(impact, WEAPON_COLORS[1], 6)
			hit_shake = 3.0
			planet.damage_radius(impact - CENTER, 36.0, damage)
			sound.play("blast")
		else:
			shell.p += direction.normalized() * shell.speed * delta
			_add_particle(shell.p, -direction.normalized() * 25, WEAPON_COLORS[1], 0.22, 2)
	if particles.size() > 280:
		particles = particles.slice(particles.size() - 280)
	if floating_texts.size() > 35:
		floating_texts = floating_texts.slice(floating_texts.size() - 35)

func _spawn_sparks(point: Vector2, color: Color, count: int) -> void:
	for i in range(count):
		_add_particle(point + Vector2(rng.randf_range(-5, 5), rng.randf_range(-5, 5)), Vector2.from_angle(rng.randf() * TAU) * rng.randf_range(18, 90), color, rng.randf_range(0.22, 0.55), rng.randf_range(1.3, 3.8))

func _add_particle(point: Vector2, velocity: Vector2, color: Color, life: float, size: float) -> void:
	particles.append({"p": point, "v": velocity, "color": color, "life": life, "max_life": life, "size": size})

func _add_beam(points: PackedVector2Array, color: Color, life: float, width: float) -> void:
	beams.append({"points": points, "color": color, "life": life, "max_life": life, "width": width})

func add_ring(point: Vector2, start: float, finish: float, color: Color, life: float) -> void:
	rings.append({"p": point, "start": start, "finish": finish, "color": color, "life": life, "max_life": life})

func toast(title: String, subtitle: String, color: Color, duration: float) -> void:
	if is_instance_valid(hud):
		hud.show_toast(title, subtitle, color, duration)
	else:
		call_deferred("toast", title, subtitle, color, duration)

func _input(event: InputEvent) -> void:
	if event is InputEventMouse and event.device == InputEvent.DEVICE_ID_EMULATION:
		# Touch buttons handle real fingers directly; suppress the synthesized
		# first-finger mouse so it cannot activate a second action or steal a stick.
		get_viewport().set_input_as_handled()
		return
	# Release is global: lifting a thumb over a HUD button must also release the stick.
	if event is InputEventScreenTouch and not event.pressed:
		_handle_pointer(event.position, false, event.index)
	elif event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT and not event.pressed:
		_handle_pointer(event.position, false, -2)

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouse and event.device == InputEvent.DEVICE_ID_EMULATION:
		return
	if event is InputEventKey and event.pressed and not event.echo:
		match event.physical_keycode:
			KEY_1: set_weapon(0)
			KEY_2: set_weapon(1)
			KEY_3: set_weapon(2)
			KEY_TAB: set_auto(not auto_mode)
			KEY_E: activate_overdrive()
			KEY_P, KEY_ESCAPE: toggle_pause()
	if paused or warp_timer > 0 or transition_left > 0 or hud.blocks_world_input():
		return
	if event is InputEventScreenTouch:
		_handle_pointer(event.position, event.pressed, event.index)
	elif event is InputEventScreenDrag:
		if event.index == joystick_id:
			joystick_position = event.position
		elif event.index != touch_fire_id:
			aim_position = event.position
	elif event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		_handle_pointer(event.position, event.pressed, -2)
	elif event is InputEventMouseMotion and pointer_down:
		if joystick_id == -2:
			joystick_position = event.position
		else:
			aim_position = event.position

func _handle_pointer(point: Vector2, pressed: bool, id: int) -> void:
	if not pressed:
		if id == joystick_id:
			joystick_active = false
			joystick_id = -1
		if id == touch_fire_id:
			fire_held = false
			touch_fire_id = -1
		if id == -2:
			pointer_down = false
		return
	if paused or transition_left > 0 or hud.blocks_world_input() or hud.is_ui_point(point):
		return
	if auto_mode and (point.y < 222 or point.y > view_size.y - 194):
		return
	if not auto_mode and point.y < 82:
		return
	if id == -2:
		pointer_down = true
	if not auto_mode and point.x < view_size.x * 0.36 and point.y > view_size.y * 0.48:
		joystick_id = id
		joystick_active = true
		joystick_origin = point
		joystick_position = point
		return
	if not auto_mode and point.distance_to(fire_center()) < 62:
		fire_held = true
		touch_fire_id = id
		shot_timer = 0
		return
	aim_position = screen_to_world(point)
	var key := _ray_target(ship_pos, aim_position)
	if planet.blocks.has(key):
		aim_key = key
		target_key = key
		fire(true)

func toggle_pause() -> void:
	paused = not paused
	release_controls()
	hud.set_pause_visible(paused)
	if paused:
		save_progress()

func toggle_sound() -> void:
	muted = not muted
	sound.enabled = not muted
	hud.refresh()

func reset_progress() -> void:
	if FileAccess.file_exists(SAVE_PATH):
		DirAccess.remove_absolute(SAVE_PATH)
	credits = 80.0
	flux = 0
	sector = 1
	levels = [0, 0, 0]
	weapon = 0
	total_earned = 0
	total_mined = 0
	session_mined = 0
	session_credits = 0
	session_energy = 0
	lifetime_energy = 0
	income_rate = 0
	income_accum = 0
	warp_timer = 0
	overdrive_left = 0
	overdrive_cooldown = 0
	auto_mode = true
	planet.generate(1)
	projectiles.clear()
	particles.clear()
	beams.clear()
	debris.clear()
	orbit_angle = -2.2
	ship_pos = CENTER + Vector2.from_angle(orbit_angle) * 205
	aim_key = Vector2i(999, 999)
	paused = false
	hud.set_pause_visible(false)
	release_controls()
	transition_left = 0.55
	_apply_orientation()
	hud.request_layout()
	save_progress()

func save_progress() -> void:
	if no_save:
		return
	var tiles: Array = []
	for key: Vector2i in planet.blocks:
		var block: Dictionary = planet.blocks[key].duplicate()
		block.x = key.x
		block.y = key.y
		tiles.append(block)
	var pickups: Array = []
	for fragment in debris:
		pickups.append({"x": fragment.p.x, "y": fragment.p.y, "age": fragment.age, "value": fragment.value, "energy": fragment.energy, "kind": fragment.kind, "size": fragment.size})
	var data := {"version": 1, "credits": credits, "flux": flux, "sector": sector, "weapon": weapon, "levels": levels, "auto": auto_mode, "muted": muted, "total_earned": total_earned, "total_mined": total_mined, "rate": income_rate, "timestamp": Time.get_unix_time_from_system(), "tiles": tiles, "total_blocks": planet.total_blocks, "cleared": warp_timer > 0, "overdrive_cooldown": overdrive_cooldown, "pickups": pickups, "lifetime_energy": lifetime_energy, "ship_x": ship_pos.x, "ship_y": ship_pos.y}
	var file := FileAccess.open(SAVE_PATH + ".tmp", FileAccess.WRITE)
	if file:
		file.store_string(JSON.stringify(data, "", true, true))
		file.close()
		var error := DirAccess.rename_absolute(SAVE_PATH + ".tmp", SAVE_PATH)
		if error != OK:
			push_warning("Could not commit game save: %s" % error_string(error))
	else:
		push_warning("Could not write game save.")

func _load_progress() -> void:
	if not FileAccess.file_exists(SAVE_PATH):
		return
	var file := FileAccess.open(SAVE_PATH, FileAccess.READ)
	if file == null:
		return
	var data = JSON.parse_string(file.get_as_text())
	if not data is Dictionary or int(data.get("version", 0)) != 1:
		return
	# Reject incomplete or damaged files instead of trapping the player in an empty sector.
	if not data.get("tiles") is Array or not data.get("levels") is Array or data.levels.size() != 3:
		return
	first_run = false
	credits = maxf(0.0, float(data.get("credits", 80)))
	flux = maxi(0, int(data.get("flux", 0)))
	sector = clampi(int(data.get("sector", 1)), 1, 1000000)
	weapon = clampi(int(data.get("weapon", 0)), 0, 2)
	for i in range(3):
		levels[i] = clampi(int(data.levels[i]), 0, 500)
	auto_mode = bool(data.get("auto", true))
	muted = bool(data.get("muted", false))
	sound.enabled = not muted
	total_earned = maxf(0, float(data.get("total_earned", 0)))
	total_mined = maxi(0, int(data.get("total_mined", 0)))
	lifetime_energy = maxi(0, int(data.get("lifetime_energy", 0)))
	ship_pos = Vector2(float(data.get("ship_x", ship_pos.x)), float(data.get("ship_y", ship_pos.y)))
	debris.clear()
	if data.get("pickups", []) is Array:
		for pickup in data.get("pickups", []):
			if pickup is Dictionary and pickup.has_all(["x", "y", "value", "energy", "kind"]):
				debris.append({"p": Vector2(float(pickup.x), float(pickup.y)), "v": Vector2.ZERO, "age": clampf(float(pickup.get("age", 0.0)), 0, 2.0), "value": maxf(0, float(pickup.value)), "energy": maxi(0, int(pickup.energy)), "kind": clampi(int(pickup.kind), 0, 4), "size": float(pickup.get("size", 2.0))})
	planet.generate(sector)
	if data.get("cleared", false):
		sector += 1
		planet.generate(sector)
	elif not data.tiles.is_empty():
		var restored: Dictionary = {}
		for block in data.tiles:
			if not block is Dictionary or not block.has_all(["x", "y", "hp", "max_hp", "kind", "shade", "value"]):
				continue
			var key := Vector2i(int(block.x), int(block.y))
			if abs(key.x) <= 10 and abs(key.y) <= 10 and float(block.hp) > 0:
				restored[key] = {"hp": float(block.hp), "max_hp": float(block.max_hp), "kind": clampi(int(block.kind), 0, 4), "shade": clampi(int(block.shade), 0, 2), "value": maxf(0, float(block.value))}
		if not restored.is_empty():
			planet.blocks = restored
			planet.total_blocks = maxi(restored.size(), int(data.get("total_blocks", planet.total_blocks)))
	var away := clampf(Time.get_unix_time_from_system() - float(data.get("timestamp", Time.get_unix_time_from_system())), 0, 4 * 3600)
	overdrive_cooldown = maxf(0, float(data.get("overdrive_cooldown", 0)) - away)
	income_rate = maxf(0.0, float(data.get("rate", 0)))
	if away > 45 and auto_mode and income_rate > 0:
		var offline := floorf(away * income_rate * 0.25)
		credits += offline
		total_earned += offline
		toast("WELCOME BACK", "Autopilot earned %s credits while away." % compact(offline), Color("79f3ce"), 5)
	# Mark the catch-up as claimed immediately, including if the app exits during the toast.
	save_progress()

func _notification(what: int) -> void:
	if what == NOTIFICATION_WM_GO_BACK_REQUEST:
		if is_instance_valid(hud):
			toggle_pause()
	elif what == NOTIFICATION_WM_CLOSE_REQUEST:
		save_progress()
		get_tree().quit()
	elif what == NOTIFICATION_APPLICATION_PAUSED or what == NOTIFICATION_WM_WINDOW_FOCUS_OUT:
		if is_instance_valid(planet):
			fire_held = false
			joystick_active = false
			save_progress()

static func compact(value: float) -> String:
	var magnitude := absf(value)
	var suffixes := ["", "K", "M", "B", "T", "Qa", "Qi"]
	var tier := 0
	while magnitude >= 1000.0 and tier < suffixes.size() - 1:
		magnitude /= 1000.0
		value /= 1000.0
		tier += 1
	if tier == 0:
		return str(int(value))
	if magnitude >= 100:
		return "%.0f%s" % [value, suffixes[tier]]
	return "%.1f%s" % [value, suffixes[tier]]

func _draw() -> void:
	draw_rect(Rect2(Vector2.ZERO, view_size), Color("080d16"))
	for star in stars:
		var point: Vector2 = star.p * view_size
		draw_rect(Rect2(point, Vector2.ONE), Color(0.65, 0.8, 0.88, star.a))
