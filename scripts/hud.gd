extends Control
## Auto mode is a quiet portrait dashboard; flight mode is a landscape cockpit.
const TouchButton = preload("res://scripts/touch_button.gd")
const INK := Color("e5f1f0")
const MUTED := Color("789197")
const MINT := Color("8ceacb")
const GOLD := Color("e6ca8c")
const LILAC := Color("b8acf0")
const PANEL := Color("0e1921")
const EDGE := Color("294047")
var game
var regular: Font = preload("res://assets/Interface.ttf")
var bold: Font = preload("res://assets/Display.ttf")
var mono: Font = preload("res://assets/Telemetry.ttf")
var panel_kind := ""
var buttons: Dictionary = {}
var credit_label: Label
var energy_label: Label
var rate_label: Label
var recovered_label: Label
var progress_label: Label
var boost_button: Button
var weapon_buttons: Array[Button] = []
var upgrade_costs: Array[Label] = []
var upgrade_levels: Array[Label] = []
var stat_values: Array[Label] = []
var modal: Control
var toast_panel: Panel
var toast_title: Label
var toast_subtitle: Label
var toast_left := 0.0
var notice_title := ""
var notice_subtitle := ""
var notice_color := MINT
var layout_pending := false
var reset_confirmation := false
var layout_tween: Tween

func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	var skin := Theme.new()
	skin.default_font = regular
	skin.default_font_size = 15
	theme = skin
	_rebuild()

func _style(fill: Color, edge: Color, line: int = 1) -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	style.bg_color = fill
	style.border_color = edge
	style.set_border_width_all(line)
	style.set_corner_radius_all(3)
	style.content_margin_left = 12
	style.content_margin_right = 12
	return style

func _label(parent: Node, text: String, rect: Rect2, font_size: int = 14, color: Color = INK, font: Font = null) -> Label:
	var label := Label.new()
	label.add_theme_font_size_override("font_size", font_size)
	label.add_theme_color_override("font_color", color)
	label.add_theme_font_override("font", font if font else regular)
	label.text = text
	label.position = rect.position
	label.size = rect.size
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	parent.add_child(label)
	return label

func _button(parent: Node, id: String, text: String, rect: Rect2, callback: Callable, font_size: int = 14, accent: Color = INK) -> Button:
	var button: Button = TouchButton.new()
	button.text = text
	button.focus_mode = Control.FOCUS_NONE
	button.add_theme_font_override("font", mono)
	button.add_theme_font_size_override("font_size", font_size)
	button.add_theme_color_override("font_color", accent)
	button.add_theme_color_override("font_hover_color", INK)
	button.add_theme_color_override("font_pressed_color", MINT)
	button.add_theme_stylebox_override("normal", _style(PANEL, EDGE))
	button.add_theme_stylebox_override("hover", _style(Color("172a31"), MINT))
	button.add_theme_stylebox_override("pressed", _style(Color("25463f"), MINT))
	button.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
	button.position = rect.position
	button.size = rect.size
	button.pressed.connect(callback)
	parent.add_child(button)
	buttons[id] = button
	return button

func _panel(parent: Node, rect: Rect2) -> Panel:
	var panel := Panel.new()
	panel.position = rect.position
	panel.size = rect.size
	panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	panel.add_theme_stylebox_override("panel", _style(PANEL, EDGE))
	parent.add_child(panel)
	return panel

func request_layout() -> void:
	if not layout_pending:
		layout_pending = true
		call_deferred("_rebuild")

func _rebuild() -> void:
	layout_pending = false
	for child in get_children():
		remove_child(child)
		child.queue_free()
	buttons.clear()
	weapon_buttons.clear()
	upgrade_costs.clear()
	upgrade_levels.clear()
	stat_values.clear()
	credit_label = null
	energy_label = null
	rate_label = null
	recovered_label = null
	progress_label = null
	boost_button = null
	var view: Vector2 = get_viewport_rect().size
	if game.auto_mode:
		_build_auto(view)
	else:
		_build_manual(view)
	_build_toast(view)
	if panel_kind != "":
		_build_modal(view)
	refresh()
	if game.transition_left > 0 and panel_kind == "":
		if is_instance_valid(layout_tween):
			layout_tween.kill()
		modulate.a = 0.2
		layout_tween = create_tween()
		layout_tween.tween_property(self, "modulate:a", 1.0, 0.28)
	else:
		modulate.a = 1.0

func _build_auto(v: Vector2) -> void:
	_label(self, "STARFALL", Rect2(24, 22, v.x - 116, 34), 24, INK, mono)
	_label(self, "AUTO MINING  /  SECTOR %02d" % game.sector, Rect2(24, 61, v.x - 116, 24), 11, MINT, mono)
	_button(self, "pause", "II", Rect2(v.x - 80, 25, 56, 56), game.toggle_pause, 18)
	var cargo := _button(self, "cargo_top", "", Rect2(24, 106, v.x - 48, 70), open_panel.bind("cargo"))
	_label(cargo, "CREDITS", Rect2(16, 8, 230, 18), 10, MUTED, mono)
	credit_label = _label(cargo, "0", Rect2(16, 28, 244, 34), 27, GOLD, mono)
	_label(cargo, "ENERGY", Rect2(v.x - 184, 8, 120, 18), 10, MUTED, mono)
	energy_label = _label(cargo, "0", Rect2(v.x - 184, 28, 120, 34), 25, LILAC, mono)
	progress_label = _label(self, "", Rect2(24, 190, v.x - 230, 24), 11, MUTED, mono)
	rate_label = _label(self, "", Rect2(v.x - 202, 190, 178, 24), 11, MINT, mono)
	rate_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	var mode := _button(self, "mode", "", Rect2(24, v.y - 188, v.x - 48, 82), game.set_auto.bind(false), 18, MINT)
	mode.add_theme_stylebox_override("normal", _style(Color("122c28"), MINT))
	_label(mode, "TAKE CONTROL   →", Rect2(20, 11, v.x - 90, 33), 20, MINT, mono)
	_label(mode, "MANUAL FLIGHT  /  ROTATE TO LANDSCAPE", Rect2(20, 46, v.x - 90, 23), 10, MUTED, mono)
	var dock_width := (v.x - 64.0) * 0.5
	_button(self, "systems", "SHIP SYSTEMS", Rect2(24, v.y - 88, dock_width, 62), open_panel.bind("systems"), 13)
	_button(self, "cargo", "CARGO LOG", Rect2(40 + dock_width, v.y - 88, dock_width, 62), open_panel.bind("cargo"), 13)

func _build_manual(v: Vector2) -> void:
	_button(self, "mode", "←  AUTO PILOT", Rect2(24, 18, 190, 62), game.set_auto.bind(true), 15, MINT)
	if v.x > 830:
		_label(self, "SECTOR %02d" % game.sector, Rect2(238, 25, 196, 42), 13, MUTED, mono)
	var stats_x := maxf(230.0, v.x - 450.0)
	var stats_w := maxf(210.0, v.x - 106.0 - stats_x)
	var cargo := _button(self, "cargo", "", Rect2(stats_x, 18, stats_w, 62), open_panel.bind("cargo"))
	_label(cargo, "CR", Rect2(12, 7, 40, 16), 9, MUTED, mono)
	credit_label = _label(cargo, "", Rect2(12, 24, stats_w * 0.62, 25), 20, GOLD, mono)
	_label(cargo, "ENERGY", Rect2(stats_w * 0.68, 7, stats_w * 0.3, 16), 9, MUTED, mono)
	energy_label = _label(cargo, "", Rect2(stats_w * 0.68, 24, stats_w * 0.3, 25), 20, LILAC, mono)
	_button(self, "pause", "II", Rect2(v.x - 80, 18, 56, 62), game.toggle_pause, 17)
	for i in range(3):
		var button := _button(self, "weapon_%d" % i, ["LANCE", "NOVA", "ION"][i], Rect2(v.x * 0.5 - 129 + 90 * i, v.y - 88, 78, 62), game.set_weapon.bind(i), 13, game.WEAPON_COLORS[i])
		weapon_buttons.append(button)
	recovered_label = _label(self, "", Rect2(v.x * 0.5 - 204, v.y - 124, 408, 24), 12, MUTED, mono)
	recovered_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	boost_button = _button(self, "boost", "BOOST", Rect2(v.x - 166, v.y - 251, 116, 62), game.activate_overdrive, 13, LILAC)
	if v.x < v.y:
		_label(self, "ROTATE PHONE FOR FLIGHT", Rect2(60, 94, v.x - 120, 28), 11, MINT, mono).horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER

func _build_toast(v: Vector2) -> void:
	var width := minf(v.x - 72, 460.0)
	var y := 230.0 if game.auto_mode else v.y - 184.0
	toast_panel = _panel(self, Rect2((v.x - width) * 0.5, y, width, 62))
	toast_panel.z_index = 15
	toast_title = _label(toast_panel, notice_title, Rect2(14, 7, width - 28, 22), 12, notice_color, mono)
	toast_subtitle = _label(toast_panel, notice_subtitle, Rect2(14, 30, width - 28, 25), 11, MUTED)
	toast_subtitle.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	toast_panel.visible = toast_left > 0

func open_panel(kind: String) -> void:
	game.release_controls()
	panel_kind = kind
	reset_confirmation = false
	request_layout()

func close_panel() -> void:
	panel_kind = ""
	reset_confirmation = false
	request_layout()

func _close_action() -> void:
	if game.paused:
		game.toggle_pause()
	else:
		close_panel()

func set_pause_visible(value: bool) -> void:
	panel_kind = "pause" if value else ""
	reset_confirmation = false
	request_layout()

func blocks_world_input() -> bool:
	return panel_kind != ""

func is_ui_point(point: Vector2) -> bool:
	if panel_kind != "":
		return true
	for button: Button in buttons.values():
		if is_instance_valid(button) and button.is_visible_in_tree() and button.get_global_rect().has_point(point):
			return true
	return false

func _build_modal(v: Vector2) -> void:
	# Raw multi-touch controls also respect the modal's input barrier.
	for button: Button in buttons.values():
		button.set_process_input(false)
		button.mouse_filter = Control.MOUSE_FILTER_IGNORE
	modal = Control.new()
	modal.size = v
	modal.mouse_filter = Control.MOUSE_FILTER_STOP
	modal.z_index = 30
	add_child(modal)
	var dim := ColorRect.new()
	dim.size = v
	dim.color = Color(0.02, 0.035, 0.05, 0.94)
	dim.mouse_filter = Control.MOUSE_FILTER_IGNORE
	modal.add_child(dim)
	var width := minf(v.x - 48.0, 560.0)
	var height := minf(v.y - 48.0, 656.0 if panel_kind == "systems" else 480.0)
	var card := _panel(modal, Rect2((v.x - width) * 0.5, (v.y - height) * 0.5, width, height))
	var titles := {"systems": "SHIP SYSTEMS", "cargo": "EXTRACTION LOG", "pause": "FLIGHT PAUSED"}
	_label(card, titles.get(panel_kind, "FLIGHT DECK"), Rect2(22, 20, width - 110, 34), 20, INK, mono)
	_button(card, "close", "×", Rect2(width - 74, 15, 52, 48), _close_action, 23)
	if panel_kind == "systems":
		_build_systems(card, width, height)
	elif panel_kind == "cargo":
		_build_cargo(card, width, height)
	else:
		_build_pause(card, width, height)

func _build_systems(card: Panel, width: float, height: float) -> void:
	var compact := height < 600
	_label(card, "LOADOUT", Rect2(22, 75, width - 44, 24), 11, MUTED, mono)
	var slot_w := (width - 64) / 3.0
	for i in range(3):
		var button := _button(card, "weapon_%d" % i, "", Rect2(22 + i * (slot_w + 10), 108, slot_w, 72 if compact else 90), game.set_weapon.bind(i), 12)
		_label(button, ["PHOTON", "NOVA", "ION"][i], Rect2(12, 12, slot_w - 24, 23), 13, game.WEAPON_COLORS[i], mono)
		_label(button, ["PIERCE", "BLAST", "CHAIN"][i], Rect2(12, 39, slot_w - 24, 20), 10, MUTED, mono)
		weapon_buttons.append(button)
	var title_y := 190.0 if compact else 220.0
	_label(card, "UPGRADES", Rect2(22, title_y, width - 44, 24), 11, MUTED, mono)
	var row_height := 58.0 if compact else 82.0
	var top := title_y + 34
	for i in range(3):
		var button := _button(card, "upgrade_%d" % i, "", Rect2(22, top + i * (row_height + 12), width - 44, row_height), _buy.bind(i))
		_label(button, ["FIREPOWER", "FIRE RATE", "ORE VALUE"][i], Rect2(14, 8, width - 210, 25), 14, INK, mono)
		upgrade_levels.append(_label(button, "", Rect2(14, 32, width - 210, 22), 10, MUTED, mono))
		upgrade_costs.append(_label(button, "", Rect2(width - 188, 8, 126, row_height - 16), 17, GOLD, mono))
	_button(card, "done", "BACK TO MINING", Rect2(22, height - 70, width - 44, 48), close_panel, 13, MINT)

func _build_cargo(card: Panel, width: float, height: float) -> void:
	_label(card, "RECOVERED BY THE SHIP", Rect2(22, 76, width - 44, 22), 11, MINT, mono)
	var labels := ["THIS FLIGHT / CREDITS", "THIS FLIGHT / ENERGY", "BLOCKS MINED", "LIFETIME / CREDITS", "LIFETIME / ENERGY"]
	for i in range(labels.size()):
		var y := 118.0 + i * 52
		_label(card, labels[i], Rect2(22, y, width - 196, 31), 11, MUTED, mono)
		var value := _label(card, "", Rect2(width - 178, y, 154, 31), 21, GOLD if i == 0 or i == 3 else LILAC if i == 1 or i == 4 else INK, mono)
		value.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
		stat_values.append(value)
	_button(card, "done", "CLOSE LOG", Rect2(22, height - 70, width - 44, 48), close_panel, 13, MINT)

func _build_pause(card: Panel, width: float, height: float) -> void:
	var guide := "AUTO  /  Mines and collects on its own.\nMANUAL  /  Left thumb steers, right thumb fires.\n\nGold pays credits. Blue ore powers the boost.\nDamaged blocks grow brighter before breaking.\n\nDesktop: WASD + Space · 1/2/3 weapons · Tab mode"
	var label := _label(card, guide, Rect2(22, 90, width - 44, height - 282), 13, MUTED)
	label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	label.vertical_alignment = VERTICAL_ALIGNMENT_TOP
	_button(card, "resume", "RESUME", Rect2(22, height - 176, width - 44, 54), game.toggle_pause, 15, MINT)
	var half := (width - 56) * 0.5
	_button(card, "sound", "SOUND OFF" if not game.muted else "SOUND ON", Rect2(22, height - 104, half, 54), _sound, 11)
	_button(card, "reset", "RESET DEMO", Rect2(34 + half, height - 104, half, 54), _reset, 11)

func _sound() -> void:
	game.toggle_sound()
	request_layout()

func _reset() -> void:
	if not reset_confirmation:
		reset_confirmation = true
		buttons.reset.text = "TAP TO CONFIRM"
		buttons.reset.add_theme_color_override("font_color", Color("f2a998"))
	else:
		game.reset_progress()

func _buy(index: int) -> void:
	if game.buy_upgrade(index):
		refresh()

func refresh() -> void:
	if is_instance_valid(credit_label):
		credit_label.text = game.compact(game.credits)
	if is_instance_valid(energy_label):
		energy_label.text = game.compact(game.flux)
	if is_instance_valid(rate_label):
		rate_label.text = ("%.1f" % game.income_rate if game.income_rate < 1000 else game.compact(game.income_rate)) + " CR / S"
	if is_instance_valid(progress_label):
		progress_label.text = "%d%% EXTRACTED / %02d" % [int(game.planet.progress() * 100), game.sector]
	if is_instance_valid(recovered_label):
		recovered_label.text = "RECOVERED  +%s CR  /  +%s E" % [game.compact(game.session_credits), game.compact(game.session_energy)]
	for i in range(weapon_buttons.size()):
		var active: bool = game.weapon == i
		weapon_buttons[i].add_theme_stylebox_override("normal", _style(Color("172d31") if active else PANEL, game.WEAPON_COLORS[i] if active else EDGE))
	for i in range(upgrade_costs.size()):
		upgrade_costs[i].text = game.compact(game.upgrade_cost(i)) + " CR"
		upgrade_costs[i].add_theme_color_override("font_color", GOLD if game.credits >= game.upgrade_cost(i) else MUTED)
		upgrade_levels[i].text = "LV %d / %s" % [game.levels[i], ["+46% DAMAGE", "+18% SPEED", "+30% YIELD"][i]]
	if stat_values.size() == 5:
		var values := [game.session_credits, game.session_energy, game.total_mined, game.total_earned, game.lifetime_energy]
		for i in range(5):
			stat_values[i].text = game.compact(values[i])
	if is_instance_valid(boost_button):
		boost_button.text = "BOOST %ds" % ceili(game.overdrive_left) if game.overdrive_left > 0 else "%ds" % ceili(game.overdrive_cooldown) if game.overdrive_cooldown > 0 else "BOOST / 5 E"
	queue_redraw()

func show_toast(title: String, subtitle: String, color: Color, duration: float) -> void:
	notice_title = title
	notice_subtitle = subtitle
	notice_color = color
	toast_left = duration
	if is_instance_valid(toast_title):
		toast_title.text = title
		toast_subtitle.text = subtitle
		toast_title.add_theme_color_override("font_color", color)
		toast_panel.visible = true

func qa_buttons() -> Dictionary:
	var result := {}
	for id in buttons:
		var button: Button = buttons[id]
		if is_instance_valid(button) and button.is_visible_in_tree() and button.is_processing_input():
			var r := button.get_global_rect()
			result[id] = {"x": r.position.x, "y": r.position.y, "w": r.size.x, "h": r.size.y}
	return result

func _process(delta: float) -> void:
	if is_instance_valid(toast_panel):
		toast_left = maxf(0.0, toast_left - delta)
		toast_panel.visible = toast_left > 0 and panel_kind == ""
		toast_panel.modulate.a = minf(1.0, toast_left * 4.0)
	queue_redraw()

func _draw() -> void:
	if not is_instance_valid(game):
		return
	var v: Vector2 = get_viewport_rect().size
	if game.auto_mode:
		draw_rect(Rect2(24, 222, v.x - 48, 2), EDGE)
		draw_rect(Rect2(24, 222, (v.x - 48) * game.planet.progress(), 2), MINT)
	else:
		var origin: Vector2 = game.joystick_origin if game.joystick_active else game.stick_center()
		var stick: Vector2 = origin + (game.joystick_position - origin).limit_length(56) if game.joystick_active else origin
		draw_arc(origin, 62, 0, TAU, 48, Color(0.48, 0.73, 0.73, 0.24), 1.2, true)
		draw_circle(stick, 24, Color(0.48, 0.73, 0.73, 0.09))
		draw_arc(stick, 24, 0, TAU, 32, Color(0.59, 0.86, 0.8, 0.6), 1.2, true)
		var fire: Vector2 = game.fire_center()
		var color: Color = game.WEAPON_COLORS[game.weapon]
		draw_circle(fire, 52, Color(color, 0.1 if game.fire_held else 0.035))
		draw_arc(fire, 52, 0, TAU, 48, Color(color, 0.8), 1.5, true)
		draw_string(mono, fire + Vector2(-18, 5), "FIRE", HORIZONTAL_ALIGNMENT_LEFT, -1, 15, color)
