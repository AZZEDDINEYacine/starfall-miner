using Godot;
using System.Text.Json;
using System.Text;
using StarfallMiner;

/// <summary>Opt-in localhost JSON protocol for rendered C# inspection. Never exported.</summary>
public partial class QaBridge : Node
{
    private readonly TcpServer _server = new();
    private readonly List<Client> _clients = new();
    private Main _game = null!;
    private int _pilotOverlapFrames;
    private int _pilotCutFrames;
    private sealed class Client(StreamPeerTcp peer)
    {
        public StreamPeerTcp Peer = peer;
        public StringBuilder Buffer = new();
        public bool Busy;
        public double Age;
    }

    public override void _Ready()
    {
        if (!OS.GetCmdlineUserArgs().Contains("--qa")) { GD.PushError("Inspection requires --qa."); GetTree().Quit(2); return; }
        _game = GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate<Main>();
        AddChild(_game);
        var error = _server.Listen(47831, "127.0.0.1");
        if (error != Error.Ok) { GD.PushError($"Inspection listen failed: {error}"); GetTree().Quit(2); return; }
        GD.Print("INSPECTION READY 127.0.0.1:47831 (C#; isolated saves)");
    }

    public override void _Process(double delta)
    {
        while (_server.IsConnectionAvailable())
        {
            var peer = _server.TakeConnection();
            if (_clients.Count >= 8) peer.DisconnectFromHost(); else _clients.Add(new Client(peer));
        }
        for (int i = _clients.Count - 1; i >= 0; i--)
        {
            var client = _clients[i];
            if (client.Busy) continue;
            client.Age += delta;
            client.Peer.Poll();
            if (client.Peer.GetStatus() != StreamPeerTcp.Status.Connected || client.Age > 10)
            {
                client.Peer.DisconnectFromHost(); client.Peer.Dispose(); _clients.RemoveAt(i); continue;
            }
            int available = client.Peer.GetAvailableBytes();
            if (available == 0) continue;
            client.Buffer.Append(client.Peer.GetUtf8String(available));
            if (client.Buffer.Length > 65536) { Respond(client, new { error = "Request too large" }); continue; }
            if (!client.Buffer.ToString().Contains('\n')) continue;
            client.Busy = true;
            Dispatch(client);
        }
    }

    private object State()
    {
        var buttons = new Dictionary<string, object>();
        foreach (var (id, button) in _game.Hud.Buttons)
        {
            if (!button.IsProcessingInput() || !button.IsVisibleInTree()) continue;
            var rect = button.GetGlobalRect();
            buttons[id] = new { x = rect.Position.X, y = rect.Position.Y, w = rect.Size.X, h = rect.Size.Y };
        }
        return new
        {
            engine = Engine.GetVersionInfo()["string"].AsString(), language = "C#", assembly = typeof(Main).Assembly.GetName().Name,
            credits = _game.Credits, flux = _game.Flux, sector = _game.Sector, weapon = _game.Weapon,
            levels = _game.Levels, auto = _game.AutoMode, paused = _game.Paused, muted = _game.Muted,
            engineering_rank = _game.EngineeringRank, drone_bays = _game.DroneBays,
            materials = _game.Equipment.Materials,
            owned_weapons = _game.Equipment.Weapons.Select(w => new { id = w.Id, type = w.Type, level = w.Level, rate_level = w.RateLevel }).ToArray(),
            drones = _game.Equipment.Drones.Select(d => new { id = d.Id, type = d.Type, level = d.Level, weapon_id = d.WeaponId, docked = d.Docked, recalling = d.Recalling, x = d.Position.X, y = d.Position.Y }).ToArray(),
            ship_weapon_id = _game.Equipment.ShipWeaponId,
            ship_hardpoints = _game.ShipHardpoints, ship_mounts = _game.Equipment.MountedShipCount,
            ship_aux_weapon_ids = _game.Equipment.ShipAuxWeaponIds,
            visible_ship_previews = _game.Hud.FindChildren("*", "Control", true, false).OfType<ShipSchematic>().Count(c => c.IsVisibleInTree()),
            waves = _game.Waves.Count,
            lasers = _game.LaserBeams.Select(b => new { active = b.Active, hit = b.Hit, x = b.Origin.X, y = b.Origin.Y, end_x = b.End.X, end_y = b.End.Y }).ToArray(),
            laser_audio_playing = _game.Sound.LaserLoopPlaying, laser_audio_starts = _game.Sound.LaserLoopStarts,
            terrain_hue = PlanetRenderer.TerrainColor(_game.CurrentVisualStyle.OreColor(0, _game.Palette), _game.Planet.Sector).H,
            special_time = _game.Planet.GetNode<PlanetRenderer>("Renderer").AnimationTime,
            special_particles = _game.Planet.GetNode<PlanetRenderer>("Renderer").AnimatedParticles,
            impact_charges = _game.Planet.Blocks.Values.Count(MiningPlanet.IsExplosive),
            objective = _game.NextObjective,
            pilot_state = _game.Autopilot.State.ToString(), pilot_can_fire = _game.Autopilot.CanFire,
            pilot_sensor_hits = _game.Autopilot.SensorHits, pilot_overlap_frames = _pilotOverlapFrames,
            pilot_cut_frames = _pilotCutFrames,
            hull_overlap = _game.Planet.ContainsPoint(_game.ShipPosition - Main.Center, AutopilotSteering.HullPadding),
            planet_radius = _game.Planet.MapRadius,
            minimap = _game.Hud.Minimap == null ? null : new {
                x = _game.Hud.Minimap.GetGlobalRect().Position.X, y = _game.Hud.Minimap.GetGlobalRect().Position.Y,
                w = _game.Hud.Minimap.Size.X, h = _game.Hud.Minimap.Size.Y,
                raster_revision = _game.Hud.Minimap.RasterRevision, raster_updates = _game.Hud.Minimap.RasterUpdates },
            blocks = _game.Planet.Blocks.Count, planet_id = _game.Planet.GetInstanceId(),
            planet_hp = _game.Planet.Blocks.Values.Sum(b => b.Hp),
            visual_style = _game.CurrentVisualStyle.Id, visual_style_index = _game.VisualStyleIndex,
            visual_styles = _game.VisualStyles.Select(s => new { id = s.Id, title = s.Title }).ToArray(),
            ship_x = _game.ShipPosition.X, ship_y = _game.ShipPosition.Y,
            pointer_id = _game.Controls.PointerId, navigating = _game.Navigation.Active,
            pointer_x = _game.Controls.PointerPosition.X, pointer_y = _game.Controls.PointerPosition.Y,
            ship_speed = _game.ShipVelocity.Length(), mining_stand_off = _game.FlightSettings.MiningStandOff,
            clearing_path = _game.Navigation.ClearingPath,
            destination_x = _game.Navigation.Destination.X, destination_y = _game.Navigation.Destination.Y,
            pending_debris = _game.Debris.Count, projectiles = _game.Projectiles.Count,
            width = _game.View.Size.X, height = _game.View.Size.Y, panel = _game.Hud.PanelKind,
            window_width = GetWindow().Size.X, window_height = GetWindow().Size.Y,
            transition = _game.TransitionLeft, layout_revision = _game.View.LayoutRevision, buttons,
            layout_auto = _game.View.LayoutAutoMode,
            hud_alpha = _game.Hud.Modulate.A, camera_scale = _game.View.CameraScale,
            activation_duration = _game.Hud.ActivationDuration, activation_running = _game.Hud.ActivationRunning,
            hologram_channels = _game.Hud.Channels.Select(c => new { name = c.Name.ToString(), id = c.GetInstanceId(), brightness = c.Brightness,
                age = c.Age, order = c.Order, processing = c.IsProcessing(), alpha = c.Modulate.A, x = c.Position.X, y = c.Position.Y }).ToArray(),
            camera_x = _game.View.CameraFocus.X, camera_y = _game.View.CameraFocus.Y,
            ship_screen_x = _game.View.WorldToScreen(_game.ShipPosition).X, ship_screen_y = _game.View.WorldToScreen(_game.ShipPosition).Y,
            aim_x = _game.Navigation.AimDirection.X, aim_y = _game.Navigation.AimDirection.Y,
            mined = _game.TotalMined, shot_cooldown = _game.FiringCooldown,
            save_path = _game.Persistence.Path
        };
    }

    private async void Dispatch(Client client)
    {
        try
        {
            using var document = JsonDocument.Parse(client.Buffer.ToString().Trim());
            var request = document.RootElement;
            string command = request.GetProperty("command").GetString()!;
            switch (command)
            {
                case "state": break;
                case "freeze": _game.SetProcess(!request.GetProperty("value").GetBoolean()); break;
                case "legacy_controls_fixture":
                    _game.Credits = 80;
                    _game.Equipment = EquipmentState.Legacy(_game.Sector, _game.Weapon);
                    _game.Hud.RequestLayout();
                    break;
                case "economy_fixture":
                    _game.ReleaseControls();
                    _game.Equipment = EquipmentState.Fresh();
                    _game.Equipment.HighestSector = 4;
                    _game.Equipment.Materials = [20, 10, 3];
                    _game.Credits = 50000;
                    _game.Weapon = 0;
                    Array.Clear(_game.Levels);
                    _game.WarpTimer = 0;
                    _game.Planet.Generate(1);
                    _game.PlaceShipAtOrbit();
                    _game.Projectiles.Clear(); _game.Debris.Clear();
                    _game.Paused = false;
                    _game.Hud.ClosePanel();
                    _game.View.PlaceWorld(true);
                    break;
                case "drone_mining_trial":
                    _game.Paused = false;
                    _game.WarpTimer = 0;
                    _game.SetAuto(false);
                    _game.Hud.ClosePanel();
                    _game.ShotTimer = 999;
                    _game.Planet.Blocks.Clear();
                    _game.Planet.Blocks[Vector2I.Zero] = new OreBlock { Hp = 100, MaxHp = 100, Value = 3, Material = 0, MaterialAmount = 2 };
                    _game.Planet.Blocks[new Vector2I(0, 12)] = new OreBlock { Hp = 10000, MaxHp = 10000, Value = 3 };
                    _game.Planet.ClearSent = false;
                    _game.Planet.TotalBlocks = 2;
                    _game.ShipPosition = Main.Center + Vector2.Left * 90;
                    foreach (var drone in _game.Equipment.Drones) { drone.Position = _game.ShipPosition; drone.ShotTimer = 0; drone.Recalling = drone.Docked = false; }
                    _game.View.PlaceWorld(true);
                    _game.Planet.NotifyTilesChanged();
                    break;
                case "fleet_cruise_trial":
                    _game.Paused = false; _game.WarpTimer = 0;
                    _game.SetAuto(false); _game.Hud.ClosePanel();
                    _game.Planet.Generate(1); _game.PlaceShipAtOrbit();
                    _game.ShotTimer = 999;
                    _game.Projectiles.Clear(); _game.Waves.Clear(); _game.Debris.Clear();
                    foreach (var miner in _game.Equipment.Drones) _game.DeployDrone(miner.Id);
                    _game.View.PlaceWorld(true);
                    break;
                case "deep_deposits_fixture":
                    _game.SetVisualStyle(0, false);
                    _game.ReleaseControls(); _game.Paused = false; _game.WarpTimer = 0;
                    _game.SetAuto(false); _game.Hud.ClosePanel();
                    _game.Planet.Blocks.Clear(); _game.Planet.ClearSent = false;
                    for (int y = -11; y <= 11; y++)
                    for (int x = 0; x < 12; x++)
                        _game.Planet.Blocks[new Vector2I(x, y)] = new OreBlock { Hp = 12, MaxHp = 12, Value = 1.5 };
                    for (int mineral = 0; mineral < 3; mineral++)
                    {
                        var ore = _game.Planet.Blocks[new Vector2I(0, (mineral - 1) * 3)];
                        ore.Material = mineral; ore.MaterialAmount = 1;
                    }
                    _game.Planet.Blocks[new Vector2I(0, -6)].Kind = MiningPlanet.Gold;
                    _game.Planet.Blocks[new Vector2I(0, 6)].Kind = MiningPlanet.Crystal;
                    _game.Planet.Blocks[new Vector2I(0, -9)].Kind = MiningPlanet.Volatile;
                    _game.Planet.Blocks[new Vector2I(0, 9)].Kind = MiningPlanet.Core;
                    _game.ShipPosition = Main.Center + Vector2.Left * 70;
                    _game.ShipAngle = 0;
                    _game.Planet.TotalBlocks = _game.Planet.Blocks.Count;
                    _game.View.PlaceWorld(true); _game.Planet.NotifyTilesChanged();
                    break;
                case "special_frame":
                    var specialRenderer = _game.Planet.GetNode<PlanetRenderer>("Renderer");
                    specialRenderer.AnimationFrozen = true;
                    specialRenderer.SeekAnimation(request.GetProperty("seconds").GetSingle());
                    break;
                case "impact_fixture":
                    _game.ReleaseControls(); _game.Paused = false; _game.WarpTimer = 0;
                    _game.SetAuto(false); _game.Hud.ClosePanel();
                    _game.Equipment = EquipmentState.Fresh(); _game.Credits = 80; Array.Clear(_game.Levels); _game.Weapon = 0;
                    _game.Planet.Blocks.Clear(); _game.Planet.ClearSent = false;
                    _game.Planet.Blocks[Vector2I.Zero] = new OreBlock { Kind = MiningPlanet.Volatile, Hp = 1000, MaxHp = 1000, Value = 3 };
                    _game.Planet.Blocks[Vector2I.Right] = new OreBlock { Kind = MiningPlanet.Volatile, Hp = 1000, MaxHp = 1000, Value = 3 };
                    _game.Planet.Blocks[Vector2I.Down] = new OreBlock { Hp = 1, MaxHp = 1, Value = 3 };
                    _game.Planet.Blocks[Vector2I.One] = new OreBlock { Hp = 1, MaxHp = 1, Material = 0, MaterialAmount = 2, Value = 7 };
                    _game.Planet.Blocks[new Vector2I(4, 0)] = new OreBlock { Hp = 100, MaxHp = 100, Value = 2 };
                    _game.Planet.TotalBlocks = 5; _game.Planet.RestoreMapExtent(); _game.Planet.NotifyTilesChanged();
                    _game.ShipPosition = Main.Center + Vector2.Left * 88; _game.ShipAngle = 0; _game.ShotTimer = 0;
                    _game.TargetKey = Vector2I.Zero;
                    _game.Debris.Clear(); _game.Projectiles.Clear(); _game.Waves.Clear(); _game.Rings.Clear(); _game.Particles.Clear();
                    _game.View.PlaceWorld(true);
                    break;
                case "nova_wave_trial":
                    _game.Paused = false; _game.WarpTimer = 0;
                    _game.SetAuto(true); _game.Hud.ClosePanel();
                    _game.Equipment.ShipAuxWeaponIds = [0, 0];
                    foreach (var miner in _game.Equipment.Drones) { miner.Recalling = miner.Docked = true; }
                    var waveWeapon = _game.Equipment.Weapons.First(w => w.Type == 3);
                    _game.EquipShip(waveWeapon.Id);
                    _game.Planet.Blocks.Clear(); _game.Planet.ClearSent = false;
                    foreach (int x in new[] { 0, 4, 7 }) _game.Planet.Blocks[new Vector2I(x, 0)] = new OreBlock { Hp = 100, MaxHp = 100, Value = 1 };
                    _game.ShipPosition = Main.Center + Vector2.Left * 80;
                    _game.TargetKey = Vector2I.Zero; _game.ShotTimer = 0;
                    _game.Waves.Clear(); _game.Projectiles.Clear(); _game.Debris.Clear();
                    _game.Fire(); _game.View.PlaceWorld(true); _game.Planet.NotifyTilesChanged();
                    break;
                case "laser_fixture":
                    _game.ReleaseControls(); _game.Paused = false; _game.WarpTimer = 0;
                    _game.SetAuto(false); _game.Hud.ClosePanel();
                    _game.Equipment = EquipmentState.Fresh(); _game.Weapon = Main.ContinuousLaser; Array.Clear(_game.Levels);
                    _game.Equipment.Weapons.Add(new CraftedWeapon { Id = 2, Type = Main.ContinuousLaser });
                    _game.Equipment.NextId = 3; _game.Equipment.ShipWeaponId = 2;
                    bool maxPower = request.TryGetProperty("max_power", out var maxBeam) && maxBeam.GetBoolean();
                    if (maxPower)
                    {
                        _game.Sector = 4; _game.Planet.Generate(4); _game.Equipment.HighestSector = 7;
                        _game.Equipment.Weapons[1].Level = _game.EquipmentLevelCap;
                    }
                    _game.Planet.Blocks.Clear(); _game.Planet.ClearSent = false;
                    double beamHp = maxPower ? (_game.Planet.GenerationSettings.BaseHp + _game.Planet.GenerationSettings.HpDepthBonus * _game.Planet.GenerationSettings.RadiusForSector(4)) * _game.Planet.GenerationSettings.Toughness(4) : 1000;
                    for (int y = maxPower ? 0 : -4; y <= (maxPower ? 0 : 4); y++)
                    for (int x = 0; x <= (maxPower ? 149 : 4); x++)
                        _game.Planet.Blocks[new Vector2I(x, y)] = new OreBlock { Hp = beamHp, MaxHp = beamHp, Value = 2 };
                    _game.Planet.TotalBlocks = _game.Planet.Blocks.Count; _game.Planet.RestoreMapExtent(); _game.Planet.NotifyTilesChanged();
                    _game.ShipPosition = Main.Center + Vector2.Left * 88; _game.ShipAngle = 0;
                    _game.Waves.Clear(); _game.Projectiles.Clear(); _game.Debris.Clear(); _game.Beams.Clear();
                    _game.View.PlaceWorld(true); _game.Hud.Refresh();
                    break;
                case "trapped_pocket":
                    _game.ReleaseControls(); _game.Paused = false; _game.WarpTimer = 0;
                    _game.SetAuto(true); _game.Hud.ClosePanel();
                    _game.Equipment = EquipmentState.Fresh(); _game.Weapon = 0; Array.Clear(_game.Levels);
                    _game.Planet.Blocks.Clear(); _game.Planet.ClearSent = false;
                    for (int y = -7; y <= 7; y++)
                    for (int x = -9; x <= 9; x++)
                        if (!(Math.Abs(x) <= 2 && Math.Abs(y) <= 1) && !(x >= 3 && y == 0))
                            _game.Planet.Blocks[new Vector2I(x, y)] = new OreBlock { Hp = 3.2, MaxHp = 3.2, Value = 2 };
                    _game.Planet.TotalBlocks = _game.Planet.Blocks.Count; _game.Planet.RestoreMapExtent(); _game.Planet.NotifyTilesChanged();
                    _game.ShipPosition = Main.Center; _game.ShipAngle = 0; _game.ShotTimer = 0;
                    _game.ShipVelocity = Vector2.Zero; _game.Autopilot.Reset(0); _pilotOverlapFrames = _pilotCutFrames = 0;
                    _game.Waves.Clear(); _game.Projectiles.Clear(); _game.Debris.Clear(); _game.Beams.Clear();
                    _game.View.PlaceWorld(true); _game.Hud.Refresh();
                    break;
                case "carved_bay":
                    _game.ReleaseControls(); _game.Paused = false; _game.WarpTimer = 0;
                    _game.SetAuto(false); _game.Hud.ClosePanel();
                    _game.Equipment = EquipmentState.Fresh(); _game.Weapon = 0; Array.Clear(_game.Levels);
                    _game.Planet.Blocks.Clear(); _game.Planet.ClearSent = false;
                    for (int y = -5; y <= 5; y++)
                    for (int x = -3; x <= 9; x++)
                        if (!(x < 0 && Math.Abs(y) < 3)) _game.Planet.Blocks[new Vector2I(x, y)] = new OreBlock { Hp = 2, MaxHp = 2, Value = 1.5 };
                    _game.Planet.TotalBlocks = _game.Planet.Blocks.Count;
                    _game.Planet.RestoreMapExtent(); _game.Planet.NotifyTilesChanged();
                    _game.ShipPosition = Main.Center + Vector2.Left * 112; _game.ShipAngle = 0; _game.ShotTimer = 0;
                    _game.ShipVelocity = Vector2.Zero; _game.Autopilot.Reset(0); _pilotOverlapFrames = 0;
                    _game.Waves.Clear(); _game.Projectiles.Clear(); _game.Debris.Clear();
                    _game.View.PlaceWorld(true); _game.Hud.Refresh();
                    break;
                case "planet_growth":
                    int growthSector = Math.Clamp(request.GetProperty("sector").GetInt32(), 1, 1000);
                    _game.ReleaseControls(); _game.Paused = false; _game.WarpTimer = 0;
                    _game.Sector = growthSector; _game.Equipment.HighestSector = Math.Max(_game.Equipment.HighestSector, growthSector);
                    _game.SetAuto(true); _game.Hud.ClosePanel();
                    _game.Planet.Generate(growthSector); _game.PlaceShipAtOrbit();
                    _game.Waves.Clear(); _game.Projectiles.Clear(); _game.Debris.Clear();
                    _game.View.PlaceWorld(true); _game.Hud.Refresh();
                    break;
                case "sparse_autopilot":
                    // Isolated QA reproduction of a final outer-edge block, never player data.
                    _game.ReleaseControls();
                    _game.SetAuto(true);
                    _game.WarpTimer = _game.OverdriveLeft = 0;
                    _game.ShotTimer = 0;
                    _game.Weapon = 0;
                    Array.Clear(_game.Levels);
                    _game.AimKey = _game.TargetKey = MiningPlanet.NoTarget;
                    _game.Projectiles.Clear(); _game.Debris.Clear(); _game.Beams.Clear();
                    _game.Planet.Blocks.Clear();
                    _game.Planet.Blocks[new Vector2I(26, 0)] = new OreBlock { Hp = 5, MaxHp = 5, Value = 3 };
                    _game.Planet.TotalBlocks = 1;
                    _game.Planet.ClearSent = false;
                    _game.Planet.NotifyTilesChanged();
                    _game.ShipPosition = Main.Center + Vector2.Left * 600;
                    _game.ShipAngle = 0; _game.Autopilot.Reset(0);
                    _game.ShipVelocity = Vector2.Zero;
                    _game.View.PlaceWorld(true);
                    _game.Hud.Refresh();
                    break;
                case "activation_frame":
                    float activationAge = Math.Clamp(request.GetProperty("seconds").GetSingle(), 0, _game.Hud.ActivationDuration);
                    foreach (var channel in _game.Hud.Channels) { channel.Seek(activationAge); channel.SetProcess(false); }
                    _game.ModeStatus.Seek(activationAge);
                    _game.ModeStatus.SetProcess(false);
                    break;
                case "activation_replay":
                    foreach (var channel in _game.Hud.Channels) channel.Activate(true);
                    _game.ModeStatus.Play(_game.AutoMode);
                    break;
                case "advance_view":
                    float seconds = Math.Clamp(request.GetProperty("seconds").GetSingle(), 0, 2);
                    for (float remaining = seconds; remaining > 0; remaining -= 1f / 60)
                    {
                        float step = Math.Min(remaining, 1f / 60);
                        _game.TransitionLeft = Math.Max(0, _game.TransitionLeft - step);
                        _game.View.PlaceWorld(false, step);
                    }
                    break;
                case "advance_simulation":
                    float simulation = Math.Clamp(request.GetProperty("seconds").GetSingle(), 0, 5);
                    for (float remaining = simulation; remaining > 0; remaining -= 1f / 60)
                    {
                        _game.Step(Math.Min(remaining, 1f / 60));
                        if (_game.Autopilot.State == AutopilotSteering.Behavior.Widen) _pilotCutFrames++;
                        if (_game.AutoMode && _game.WarpTimer <= 0 && _game.Planet.ContainsPoint(_game.ShipPosition - Main.Center, AutopilotSteering.HullPadding)) _pilotOverlapFrames++;
                    }
                    break;
                case "resize":
                    GetWindow().Size = new Vector2I(Math.Clamp(request.GetProperty("width").GetInt32(), 320, 1920), Math.Clamp(request.GetProperty("height").GetInt32(), 320, 1920));
                    break;
                case "touch":
                    using (var touch = new InputEventScreenTouch
                    {
                        Position = Point(request), Index = request.GetProperty("finger").GetInt32(),
                        Pressed = request.GetProperty("pressed").GetBoolean(),
                        Canceled = request.TryGetProperty("canceled", out var canceled) && canceled.GetBoolean()
                    }) GetViewport().PushInput(touch, true);
                    break;
                case "drag":
                    using (var drag = new InputEventScreenDrag { Position = Point(request), Index = request.GetProperty("finger").GetInt32() })
                        GetViewport().PushInput(drag, true);
                    break;
                case "mouse":
                    using (var mouse = new InputEventMouseButton
                    {
                        Position = Point(request), GlobalPosition = Point(request), ButtonIndex = MouseButton.Left,
                        Pressed = request.GetProperty("pressed").GetBoolean(), Device = (int)InputEvent.DeviceIdEmulation
                    }) GetViewport().PushInput(mouse, true);
                    break;
                case "desktop_mouse":
                    // Enter through Input, rather than bypassing window scaling and input emulation.
                    var windowPoint = GetViewport().GetFinalTransform() * Point(request);
                    bool down = request.GetProperty("pressed").GetBoolean();
                    using (var desktopMouse = new InputEventMouseButton
                    {
                        Position = windowPoint, GlobalPosition = windowPoint, WindowId = GetWindow().GetWindowId(),
                        ButtonIndex = MouseButton.Left, ButtonMask = down ? MouseButtonMask.Left : 0,
                        Pressed = down, Device = 0
                    }) Input.ParseInputEvent(desktopMouse);
                    break;
                case "desktop_motion":
                    var motionPoint = GetViewport().GetFinalTransform() * Point(request);
                    using (var desktopMotion = new InputEventMouseMotion
                    {
                        Position = motionPoint, GlobalPosition = motionPoint, WindowId = GetWindow().GetWindowId(),
                        ButtonMask = MouseButtonMask.Left, Device = 0
                    }) Input.ParseInputEvent(desktopMotion);
                    break;
                case "lifecycle":
                    _game.Notification(request.GetProperty("event").GetString() switch
                    {
                        "focus_out" => (int)NotificationWMWindowFocusOut,
                        "pause" => (int)NotificationApplicationPaused,
                        _ => throw new ArgumentException("Unknown lifecycle event")
                    });
                    break;
                case "screenshot":
                    if (DisplayServer.GetName() == "headless") throw new InvalidOperationException("Screenshot requires rendering enabled.");
                    string name = request.GetProperty("name").GetString()!;
                    if (name.GetFile() != name || !name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Screenshot requires a PNG filename.");
                    string path = "res://builds/qa/" + name;
                    DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://builds/qa"));
                    // macOS can skip display frames for occluded windows. Force an
                    // actual renderer pass so inspection also works behind the IDE.
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    foreach (var child in _game.View.Stage.GetChildren())
                        if (child is CanvasItem item) item.QueueRedraw();
                    // CanvasItem redraws are deferred. Let their commands update before
                    // forcing an occluded macOS window to render the current fixture.
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    RenderingServer.ForceDraw(false);
                    using (var image = GetViewport().GetTexture().GetImage())
                    {
                        var error = image.SavePng(path);
                        if (error != Error.Ok) throw new IOException($"Screenshot failed: {error}");
                    }
                    Respond(client, new { screenshot = ProjectSettings.GlobalizePath(path), state = State() });
                    return;
                case "quit":
                    Respond(client, new { ok = true });
                    Callable.From(() => GetTree().Quit()).CallDeferred();
                    return;
                default: throw new ArgumentException("Unknown inspection command");
            }
            // Deferred UI rebuilds and resize signals must settle before reporting state.
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Respond(client, State());
        }
        catch (Exception error) { Respond(client, new { error = error.Message }); }
    }

    private static Vector2 Point(JsonElement request) => new(request.GetProperty("x").GetSingle(), request.GetProperty("y").GetSingle());
    private static void Respond(Client client, object response)
    {
        client.Peer.PutData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response) + "\n"));
        client.Peer.DisconnectFromHost();
        client.Busy = false;
        client.Age = 11;
    }

    public override void _ExitTree()
    {
        _server.Stop(); _server.Dispose();
        foreach (var client in _clients) { client.Peer.DisconnectFromHost(); client.Peer.Dispose(); }
    }
}
