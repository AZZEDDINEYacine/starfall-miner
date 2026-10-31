extends Node2D
## Flat geometric world rendering. UI and touch controls live in the HUD canvas.
var game

func _draw() -> void:
	var g = game
	for ring in g.rings:
		var t: float = 1.0 - ring.life / ring.max_life
		draw_arc(ring.p, lerpf(ring.start, ring.finish, t), 0, TAU, 40, Color(ring.color, (1.0 - t) * 0.28), 1.0, true)
	for beam in g.beams:
		var alpha: float = clampf(beam.life / beam.max_life, 0, 1)
		draw_polyline(beam.points, Color(beam.color, alpha * 0.8), 1.5, true)
	for shell in g.projectiles:
		draw_rect(Rect2(shell.p - Vector2(2, 2), Vector2(4, 4)), Color("edc391"))
	for particle in g.particles:
		var alpha: float = clampf(particle.life / particle.max_life, 0, 1) * 0.5
		draw_rect(Rect2(particle.p, Vector2.ONE * minf(2.0, particle.size)), Color(particle.color, alpha))
	for fragment in g.debris:
		var colors := [Color("aad6cf"), Color("e8cd94"), Color("a1deee"), Color("dba7a3"), Color("ccbbef")]
		var color: Color = colors[int(fragment.kind)]
		draw_rect(Rect2(fragment.p - Vector2.ONE * fragment.size * 0.5, Vector2.ONE * fragment.size), Color(color, 0.72))
		if fragment.age > 0.22:
			draw_line(fragment.p, fragment.p - fragment.v.normalized() * 4.0, Color(color, 0.18), 1, true)
	if g.planet.blocks.has(g.target_key) and g.warp_timer <= 0:
		var target: Vector2 = g.CENTER + g.planet.key_point(g.target_key)
		var c: Color = Color(g.WEAPON_COLORS[g.weapon], 0.6)
		for direction in [Vector2(-1, -1), Vector2(1, -1), Vector2(-1, 1), Vector2(1, 1)]:
			var p: Vector2 = target + direction * 9.0
			draw_line(p, p - Vector2(direction.x * 3.5, 0), c, 1, true)
			draw_line(p, p - Vector2(0, direction.y * 3.5), c, 1, true)
	_draw_ship()

func _draw_ship() -> void:
	var g = game
	var color: Color = g.WEAPON_COLORS[g.weapon]
	draw_set_transform(g.ship_pos, g.ship_angle)
	# A single arrowhead and two fine lines replace the armored sprite and halo.
	var hull := PackedVector2Array([Vector2(15, 0), Vector2(-11, -8), Vector2(-6, 0), Vector2(-11, 8), Vector2(15, 0)])
	draw_colored_polygon(PackedVector2Array([Vector2(15, 0), Vector2(-11, -8), Vector2(-6, 0), Vector2(-11, 8)]), Color("101e27"))
	draw_polyline(hull, Color("d7edeb"), 1.35, true)
	draw_line(Vector2(2, 0), Vector2(11, 0), color, 1.6, true)
	if g.ship_velocity.length() > 4:
		var length: float = 7.0 + sin(g.clock * 32.0) * 2.0
		draw_line(Vector2(-10, 0), Vector2(-10 - length, 0), Color(color, 0.55), 1.4, true)
	draw_set_transform(Vector2.ZERO)
