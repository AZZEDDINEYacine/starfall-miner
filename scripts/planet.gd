class_name MiningPlanet
extends Node2D
## Compact, deterministic destructible tile map. No physics bodies per block.

signal block_mined(credits: float, flux: int, point: Vector2, kind: int)
signal planet_cleared
signal blast_triggered(point: Vector2)

const CELL := 16.0
const STONE := 0
const GOLD := 1
const CRYSTAL := 2
const VOLATILE := 3
const CORE := 4
const BASE_COLORS := [Color("355866"), Color("8a6836"), Color("386c96"), Color("8b4154"), Color("76539f")]
const HEALTH_COLORS := [Color("c3f1ed"), Color("ffe7a3"), Color("bcf3ff"), Color("ffbfad"), Color("f0d9ff")]
const PALETTES := [
	[Color("70535e"), Color("92616a"), Color("b17b78")],
	[Color("3f6777"), Color("518995"), Color("72a7ac")],
	[Color("62557f"), Color("82709b"), Color("a28cb5")],
	[Color("4f7361"), Color("71947a"), Color("93ae8b")],
]

var blocks: Dictionary = {}
var sector := 1
var total_blocks := 0
var initial_value := 0.0
var mined_value := 0.0
var impacts: Dictionary = {}
var rng := RandomNumberGenerator.new()
var phase := 0.0
var core_open := false
var clear_sent := false

func generate(sector_number: int) -> void:
	sector = sector_number
	rng.seed = sector * 104729 + 777
	blocks.clear()
	impacts.clear()
	initial_value = 0.0
	mined_value = 0.0
	clear_sent = false
	core_open = false
	var radius := 8.9 + minf((sector - 1) * 0.12, 0.8)
	var toughness := pow(1.27, minf(sector - 1, 500))
	for y in range(-10, 11):
		for x in range(-10, 11):
			var key := Vector2i(x, y)
			var dist := Vector2(x, y).length()
			var edge := radius + rng.randf_range(-0.5, 0.55)
			if dist > edge:
				continue
			var roll := rng.randf()
			var kind := STONE
			if roll < 0.115:
				kind = GOLD
			elif roll < 0.185:
				kind = CRYSTAL
			elif roll < 0.235 and dist > 2.5:
				kind = VOLATILE
			if key == Vector2i.ZERO:
				kind = CORE
			var hp := (2.7 + (radius - dist) * 0.12) * toughness
			if kind == GOLD:
				hp *= 1.3
			elif kind == CRYSTAL:
				hp *= 1.15
			elif kind == VOLATILE:
				hp *= 0.65
			elif kind == CORE:
				hp *= 10.0
			var value := (3.0 if kind == STONE else 15.0 if kind == GOLD else 6.0) * pow(1.19, minf(sector - 1, 500))
			if kind == CORE:
				value *= 20.0
			blocks[key] = {"hp": hp, "max_hp": hp, "kind": kind, "shade": rng.randi_range(0, 2), "value": value}
			initial_value += value
	total_blocks = blocks.size()
	queue_redraw()

func progress() -> float:
	return 1.0 - float(blocks.size()) / maxf(total_blocks, 1)

func key_point(key: Vector2i) -> Vector2:
	return Vector2(key) * CELL

func point_key(point: Vector2) -> Vector2i:
	return Vector2i(roundi(point.x / CELL), roundi(point.y / CELL))

func nearest(point: Vector2, exclude: Array = []) -> Vector2i:
	var best := Vector2i(999, 999)
	var best_dist := INF
	for key: Vector2i in blocks:
		if key in exclude:
			continue
		var dist := point.distance_squared_to(key_point(key))
		if dist < best_dist:
			best = key
			best_dist = dist
	return best

func contains_point(point: Vector2, padding: float = 0.0) -> bool:
	var key := point_key(point)
	for y in range(-1, 2):
		for x in range(-1, 2):
			var neighbor := key + Vector2i(x, y)
			if blocks.has(neighbor):
				var box := Rect2(key_point(neighbor) - Vector2.ONE * (CELL * 0.5 + padding), Vector2.ONE * (CELL + padding * 2.0))
				if box.has_point(point):
					return true
	return false

func surface_radius(angle: float) -> float:
	var axis := Vector2.from_angle(angle)
	var furthest := 16.0
	for key: Vector2i in blocks:
		var p := key_point(key)
		if absf(axis.cross(p)) < 29.0:
			furthest = maxf(furthest, axis.dot(p) + CELL * 0.5)
	return furthest

func damage(key: Vector2i, amount: float) -> void:
	if not blocks.has(key):
		return
	var block: Dictionary = blocks[key]
	block.hp -= amount
	impacts[key] = 0.13
	if block.hp <= 0.0:
		var kind: int = block.kind
		var value: float = block.value
		var p := key_point(key)
		blocks.erase(key)
		impacts.erase(key)
		mined_value += value
		block_mined.emit(value, 1 if kind == CRYSTAL else 5 if kind == CORE else 0, p + position, kind)
		if kind == VOLATILE:
			blast_triggered.emit(p + position)
			# Removing before recursing makes chains finite, including adjacent bombs.
			for y in range(-1, 2):
				for x in range(-1, 2):
					if x != 0 or y != 0:
						damage(key + Vector2i(x, y), amount * 0.8 + 5.0 * pow(1.27, minf(sector - 1, 500)))
	if blocks.is_empty() and not clear_sent:
		clear_sent = true
		planet_cleared.emit()
	queue_redraw()

func damage_radius(center: Vector2, radius: float, amount: float) -> void:
	# Snapshot keys: chain detonations can remove neighbors during the loop.
	for key: Vector2i in blocks.keys():
		var distance := key_point(key).distance_to(center)
		if distance <= radius:
			damage(key, amount * lerpf(1.0, 0.55, distance / radius))

func _process(delta: float) -> void:
	phase += delta
	for key: Vector2i in impacts.keys():
		impacts[key] -= delta
		if impacts[key] <= 0:
			impacts.erase(key)
	queue_redraw()

func _draw() -> void:
	for key: Vector2i in blocks:
		var block: Dictionary = blocks[key]
		var p := key_point(key)
		draw_rect(Rect2(p - Vector2(7, 7), Vector2(14, 14)), block_color(block))

func block_color(block: Dictionary) -> Color:
	var kind: int = clampi(int(block.kind), 0, 4)
	var damage_fraction := 1.0 - clampf(float(block.hp) / maxf(float(block.max_hp), 0.001), 0.0, 1.0)
	# Persistent brightness is the health indicator; no flash, cracks, bevel, or glyph.
	return BASE_COLORS[kind].lerp(HEALTH_COLORS[kind], pow(damage_fraction, 0.7))
