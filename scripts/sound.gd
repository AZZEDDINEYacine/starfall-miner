class_name MiningSound
extends Node
## Tiny generated synth effects: no external samples, downloads, or dependencies.

var enabled := true
var players: Array[AudioStreamPlayer] = []
var sounds: Dictionary = {}
var last_shot := 0.0

func _ready() -> void:
	for i in range(8):
		var player := AudioStreamPlayer.new()
		player.volume_db = -15.0
		add_child(player)
		players.append(player)
	sounds.laser = _tone(680.0, 200.0, 0.075, 0.12)
	sounds.blast = _tone(110.0, 28.0, 0.24, 0.65)
	sounds.arc = _tone(1000.0, 360.0, 0.14, 0.27)
	sounds.mine = _tone(820.0, 1420.0, 0.055, 0.03)
	sounds.upgrade = _tone(480.0, 1050.0, 0.25, 0.0)
	sounds.warp = _tone(110.0, 1700.0, 0.7, 0.14)

func play(kind: String, pitch: float = 1.0) -> void:
	if not enabled or not sounds.has(kind):
		return
	for player in players:
		if not player.playing:
			player.stream = sounds[kind]
			player.pitch_scale = pitch
			player.play()
			return

func _tone(start: float, finish: float, duration: float, noise: float) -> AudioStreamWAV:
	var data := PackedByteArray()
	var rate := 22050
	var count := int(rate * duration)
	data.resize(count * 2)
	var phase := 0.0
	var generator := RandomNumberGenerator.new()
	generator.seed = int(start)
	for i in range(count):
		var t := float(i) / count
		phase += TAU * lerpf(start, finish, t) / rate
		var envelope := pow(1.0 - t, 2.4) * minf(t * 50.0, 1.0)
		var sample := (sin(phase) * (1.0 - noise) + generator.randf_range(-1, 1) * noise) * envelope * 0.5
		data.encode_s16(i * 2, int(sample * 32767))
	var stream := AudioStreamWAV.new()
	stream.format = AudioStreamWAV.FORMAT_16_BITS
	stream.mix_rate = rate
	stream.data = data
	return stream
