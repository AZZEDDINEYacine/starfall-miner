extends Button
## Real multi-touch menu control. Touches have their own finger ownership;
## they never depend on the first finger being synthesized into a mouse click.

var touch_finger := -1

func _input(event: InputEvent) -> void:
	if not is_visible_in_tree() or disabled:
		return
	if event is InputEventScreenTouch:
		if event.pressed and touch_finger < 0 and get_global_rect().has_point(event.position):
			touch_finger = event.index
			queue_redraw()
			get_viewport().set_input_as_handled()
		elif not event.pressed and event.index == touch_finger:
			var activate: bool = not event.canceled and get_global_rect().has_point(event.position)
			touch_finger = -1
			queue_redraw()
			get_viewport().set_input_as_handled()
			if activate:
				pressed.emit()
	elif event is InputEventScreenDrag and event.index == touch_finger:
		get_viewport().set_input_as_handled()

func _draw() -> void:
	if touch_finger >= 0:
		draw_style_box(get_theme_stylebox("pressed"), Rect2(Vector2.ZERO, size))
