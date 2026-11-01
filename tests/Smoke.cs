using Godot;
using System.Text.Json.Nodes;
using StarfallMiner;
using FileAccess = Godot.FileAccess;

/// <summary>Runs the shipped C# scene; all menu actions enter the Viewport as fingers.</summary>
public partial class Smoke : Node
{
    private int _checks;
    private readonly List<string> _failures = new();
    private Main _game = null!;
    private Window Root => GetTree().Root;
    private async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

    public override async void _Ready()
    {
        if (!OS.GetCmdlineUserArgs().Contains("--test")) { GD.PushError("Regression requires --test to isolate storage."); GetTree().Quit(2); return; }
        try { await Run(); }
        catch (Exception error) { Check(false, $"Unhandled regression exception: {error}"); }
        GD.Print($"RESULT: {_checks} checks; {_failures.Count} failures");
        foreach (var failure in _failures) GD.Print("FAILED: " + failure);
        GetTree().Quit(_failures.Count == 0 ? 0 : 1);
    }

    private void Check(bool condition, string description)
    {
        _checks++;
        GD.Print((condition ? "PASS: " : "FAIL: ") + description);
        if (!condition) _failures.Add(description);
    }

    private void Touch(Vector2 point, bool down, int finger = 7, bool canceled = false)
    {
        using var touch = new InputEventScreenTouch { Position = point, Index = finger, Pressed = down, Canceled = canceled };
        Root.PushInput(touch, true);
    }

    private void Mouse(Vector2 point, bool down, bool emulated)
    {
        using var mouse = new InputEventMouseButton
        {
            Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = down,
            Device = emulated ? (int)InputEvent.DeviceIdEmulation : 0
        };
        Root.PushInput(mouse, true);
    }

    private async Task Tap(string id, int finger = 8)
    {
        var p = _game.Hud.Buttons[id].GetGlobalRect().GetCenter();
        Touch(p, true, finger); Touch(p, false, finger);
        await Frame(); await Frame();
    }

    private async Task Resize(int width, int height)
    {
        Root.Size = new Vector2I(width, height);
        await Frame(); await Frame();
        _game.View.PlaceWorld(true);
        _game.Hud.Rebuild();
        _game.TransitionLeft = 0;
    }

    private static Dictionary<Vector2I, OreBlock> Snapshot(MiningPlanet planet) => planet.Blocks.ToDictionary(p => p.Key, p => p.Value.Copy());
    private static bool Near(double a, double b) => Math.Abs(a - b) <= Math.Max(1e-6, Math.Abs(a) * 1e-10);
    private static bool SameTiles(Dictionary<Vector2I, OreBlock> a, Dictionary<Vector2I, OreBlock> b) =>
        a.Count == b.Count && a.All(p => b.TryGetValue(p.Key, out var q) && Near(p.Value.Hp, q.Hp) && Near(p.Value.MaxHp, q.MaxHp) &&
            Near(p.Value.Value, q.Value) && p.Value.Kind == q.Kind && p.Value.Shade == q.Shade && p.Value.Material == q.Material && p.Value.MaterialAmount == q.MaterialAmount);
    private static OreBlock Block(int kind, double value = 3) => new() { Hp = 1, MaxHp = 1, Kind = kind, Value = value };
    private void WriteJson(JsonNode data)
    {
        using var file = FileAccess.Open(_game.Persistence.Path, FileAccess.ModeFlags.Write);
        file.StoreString(data.ToJsonString());
    }
    private JsonNode SavedJson() => JsonNode.Parse(FileAccess.GetFileAsString(_game.Persistence.Path))!;
    private void Fixture(string name)
    {
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://.godot/tests"));
        using var file = FileAccess.Open(_game.Persistence.Path, FileAccess.ModeFlags.Write);
        file.StoreString(FileAccess.GetFileAsString($"res://tests/fixtures/{name}.json"));
    }

    private async Task Run()
    {
        Root.Size = new Vector2I(540, 960);
        await Frame(); await Frame();
        _game = GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate<Main>();
        AddChild(_game);
        await Frame();
        _game.SetProcess(false);
        _game.Sound.Enabled = false;
        Check(_game.GetNode<Starfield>("Starfield") != null &&
              _game.View.Stage.GetParent() == _game.View &&
              _game.Planet.GetParent() == _game.View.Stage &&
              _game.Planet.GetNode<PlanetRenderer>("Renderer").Planet == _game.Planet &&
              _game.Hud.GetParent() is CanvasLayer,
              "Persistent world and HUD are authored in the Godot scene tree");
        Check(_game.Palette.WeaponLance == GD.Load<VisualPalette>("res://resources/starfall_palette.tres").WeaponLance &&
              _game.Hud.Theme.HasStylebox("normal", "Button") &&
              _game.Hud.Theme.HasStylebox("panel", "Panel") &&
              _game.Palette.Weapon(0) != _game.Palette.Weapon(1) &&
              _game.Planet.GenerationSettings == GD.Load<PlanetGenerationSettings>("res://resources/planet_generation.tres") &&
              _game.Planet.GetNode<PlanetRenderer>("Renderer").Style == _game.CurrentVisualStyle,
              "Inspector palette and shared HUD theme are loaded");
        Check(_game.IsTest && _game.Persistence.Path.StartsWith("res://.godot/"), "Tests cannot access the player's user save");
        var generated = new Dictionary<Vector2I, OreBlock>();
        var generator = new PlanetGenerator();
        double initialValue = generator.Generate(1, _game.Planet.GenerationSettings, generated);
        Check(SameTiles(generated, _game.Planet.Blocks) && Near(initialValue, _game.Planet.InitialValue),
              "Procedural generator produces data independently of scene and renderer");
        Check(_game.Planet.Blocks.Count > 2000, "New planets contain over two thousand destructible tiles");
        Check(!_game.Planet.ContainsPoint(_game.ShipPosition - Main.Center, 9), "The ship spawns outside the enlarged planet");
        var farShip = Main.Center + new Vector2(0, -850);
        var advanced = AutopilotSteering.Advance(farShip, Main.Center, _game.Planet, 0.05f);
        Check(advanced.DistanceTo(farShip) <= AutopilotSteering.MaxSpeed * 0.05f + 0.001f,
            "Autopilot cannot jump when the desired orbit is far away");
        var buriedShip = Main.Center + new Vector2(0, -100);
        var escaping = AutopilotSteering.Advance(buriedShip, Main.Center, _game.Planet, 0.05f);
        Check(escaping.DistanceTo(buriedShip) <= AutopilotSteering.MaxSpeed * 0.05f + 0.001f,
            "Collision recovery cannot teleport to the computed planetary surface");
        var position = farShip;
        bool speedLimited = true;
        for (int i = 0; i < 120; i++)
        {
            var next = AutopilotSteering.Advance(position, Main.Center, _game.Planet, 0.05f);
            speedLimited &= next.DistanceTo(position) <= AutopilotSteering.MaxSpeed * 0.05f + 0.001f;
            position = next;
        }
        Check(speedLimited && position.DistanceTo(farShip) > 20,
            "Long-distance autopilot approach is continuous and makes progress");
        var original = Snapshot(_game.Planet);
        _game.Planet.Generate(1);
        Check(SameTiles(original, _game.Planet.Blocks), "Sector generation is deterministic");
        Check(_game.Planet.Blocks.Values.Select(b => b.Kind).Distinct().Count() == 5, "Five ore types exist");
        Check(Input.EmulateMouseFromTouch, "Touch emulation remains enabled");
        Check(_game.Equipment.Weapons.Count == 1 && _game.Equipment.Drones.Count == 0 && !_game.HasShipWeaponType(1), "Fresh progression starts with one lance, not free advanced weapons or drones");
        _game.SetWeapon(1); await Frame(); await Frame();
        Check(_game.Weapon == 0 && _game.Hud.PanelKind == "fabricator", "Selecting an uncrafted weapon opens Fabricator without equipping it");
        _game.Hud.ClosePanel(); await Frame(); await Frame();
        // Existing menu ownership checks use a migrated, previously unlocked loadout.
        _game.Equipment = EquipmentState.Legacy(1, 0);

        await Tap("systems");
        Check(_game.Hud.PanelKind == "systems", "Actual Viewport touchscreen tap opens Ship Systems");
        await Tap("mode");
        Check(_game.AutoMode && _game.Hud.PanelKind == "systems", "Modal blocks touch buttons behind it");
        await Tap("tab_fleet");
        var cancelPoint = _game.Hud.Buttons["primary_2"].GetGlobalRect().GetCenter();
        Touch(cancelPoint, true, 8); Touch(cancelPoint, false, 8, true);
        await Frame();
        Check(_game.Weapon == 0, "Canceled touchscreen press does not activate");
        Touch(cancelPoint, true, 8); Touch(Vector2.Zero, false, 8);
        Check(_game.Weapon == 0, "Release outside a button does not activate");
        Touch(cancelPoint, true, 8); Touch(cancelPoint, false, 9);
        Check(_game.Weapon == 0 && _game.Hud.Buttons["primary_2"].TouchFinger == 8, "Unrelated finger cannot release a menu press");
        Touch(cancelPoint, false, 8, true);
        for (int i = 1; i <= 3; i++)
        {
            int weapon = i % 3;
            await Tap($"primary_{weapon + 1}");
            Check(_game.Weapon == weapon, $"Touch selects {Main.Weapons[weapon]}");
        }
        await Tap("tab_systems");
        double wallet = _game.Credits, cost = _game.UpgradeCost(2);
        var buyPoint = _game.Hud.Buttons["upgrade_2"].GetGlobalRect().GetCenter();
        Touch(buyPoint, true, 10);
        Mouse(buyPoint, true, true); Mouse(buyPoint, false, true);
        Touch(buyPoint, false, 10);
        Mouse(buyPoint, true, true); Mouse(buyPoint, false, true);
        Check(_game.Levels[2] == 1 && _game.Credits == wallet - cost, "Touch purchase plus synthesized mouse charges exactly once");
        Touch(buyPoint, true, 8);
        _game.Notification((int)NotificationWMWindowFocusOut);
        Touch(buyPoint, false, 8);
        Check(_game.Levels[2] == 1, "Focus loss cancels a held purchase before release");
        await Tap("done"); await Tap("cargo");
        Check(_game.Hud.PanelKind == "cargo", "Cargo Log opens by touch");
        await Tap("close");
        Check(_game.Hud.PanelKind == "", "Modal close works by touch");
        await Tap("pause");
        Check(_game.Paused, "Pause works by touch");
        bool muted = _game.Muted;
        await Tap("sound");
        Check(_game.Muted != muted, "Sound setting works by touch");
        await Tap("resume");
        Check(!_game.Paused, "Resume works by touch");

        int count = _game.Planet.Blocks.Count;
        var ship = _game.ShipPosition;
        ulong planetId = _game.Planet.GetInstanceId();
        _game.Projectiles.Add(new Shell { Position = ship, Target = Main.Center, Damage = 1, Life = 2, Speed = 10 });
        _game.Debris.Add(new DebrisFragment { Position = Main.Center, Value = 7, Size = 2 });
        var debrisIdentity = _game.Debris[0];
        await Tap("mode");
        Check(!_game.AutoMode, "Prominent Take Control switches mode by touch");
        Check(_game.Planet.GetInstanceId() == planetId && _game.Planet.Blocks.Count == count && _game.ShipPosition == ship &&
            _game.Projectiles.Count == 1 && ReferenceEquals(_game.Debris[0], debrisIdentity), "Mode switch preserves live planet, ship, projectiles and debris");
        _game.TransitionLeft = 0;
        await Resize(540, 960);
        Check(_game.View.Size.Y > _game.View.Size.X, "Entering manual mode keeps the portrait viewport");
        Check(!_game.Hud.Buttons.ContainsKey("systems") && _game.Hud.Buttons.ContainsKey("boost"),
            "Portrait navigation HUD exposes weapons and boost without sticks");
        var point = _game.View.WorldToScreen(_game.ShipPosition) + new Vector2(-70, 0);
        Touch(point, true, 2);
        Check(_game.Controls.PointerId == 2 && _game.Navigation.Active, "A held world press starts steering immediately");
        Touch(point + Vector2.Down * 20, true, 5);
        Touch(point, false, 5);
        Check(_game.Controls.PointerId == 2, "An unrelated finger cannot replace or release the steering pointer");
        await Tap("weapon_2", 3);
        Check(_game.Weapon == 2 && _game.Controls.PointerId == 2, "A menu finger changes weapon without stealing the steering pointer");
        Mouse(point, true, true); Mouse(point, false, true);
        Check(_game.Controls.PointerId == 2, "Synthesized mouse cannot complete a held steering pointer");
        Touch(point, false, 2, true);
        Check(!_game.Navigation.Active && _game.Controls.PointerId == -1, "Canceling the steering finger stops navigation");
        Touch(point, true, 2); Touch(new Vector2(20, 20), false, 2);
        Check(!_game.Navigation.Active && _game.Controls.PointerId == -1, "Releasing over the HUD cancels navigation");
        Touch(point, true, 2);
        using (var drag = new InputEventScreenDrag { Position = point + Vector2.Down * 40, Index = 2 }) Root.PushInput(drag, true);
        Touch(point, false, 2);
        Check(!_game.Navigation.Active, "Releasing after a steering drag stops navigation");
        Touch(point, true, 2); Touch(point, false, 2);
        Check(!_game.Navigation.Active && _game.Controls.PointerId == -1, "Lifting the thumb stops steering instead of leaving a waypoint");
        Touch(point, true, 2);
        _game.Notification((int)NotificationApplicationPaused);
        Check(!_game.Navigation.Active && _game.Controls.PointerId == -1, "OS pause stops travel and clears pending touches");
        Touch(point, true, 2);
        _game.Notification((int)NotificationWMWindowFocusOut);
        Touch(point, false, 2);
        Check(!_game.Navigation.Active && _game.Controls.PointerId == -1, "Focus loss cancels a pending destination");
        Touch(point, true, 2);
        await Resize(390, 844);
        Check(!_game.Navigation.Active && _game.Controls.PointerId == -1, "Viewport resizing cancels the active route");
        point = _game.View.WorldToScreen(_game.ShipPosition) + new Vector2(-70, 0);
        Touch(point, true, 2);
        await Tap("cargo", 3);
        Touch(point, false, 2);
        Check(!_game.Navigation.Active && _game.Controls.PointerId == -1 && _game.Hud.BlocksWorldInput,
            "Opening a modal cancels a pending steering pointer immediately");
        Touch(point, true, 2); Touch(point, false, 2);
        Check(!_game.Navigation.Active, "Modal consumes world navigation taps");
        await Tap("done");
        _game.Hud.Rebuild();
        point = _game.View.WorldToScreen(_game.ShipPosition) + new Vector2(-70, 0);
        Touch(point, true, 2);
        await Tap("mode", 3);
        Check(_game.AutoMode && !_game.Navigation.Active && _game.Controls.PointerId == -1,
            "Autopilot cancels the manual route without restarting the scene");
        await Resize(540, 960);
        _game.Projectiles.Clear();

        for (int weapon = 0; weapon < 3; weapon++)
        {
            _game.Planet.Generate(1); _game.WarpTimer = 0;
            _game.ShipPosition = Main.Center + Vector2.Left * (_game.Planet.SurfaceRadius(Mathf.Pi) + 25); _game.Weapon = weapon;
            _game.Projectiles.Clear(); _game.Debris.Clear(); Array.Clear(_game.Levels);
            int blocks = _game.Planet.Blocks.Count;
            double credits = _game.Credits;
            for (int shot = 0; shot < 12; shot++)
            {
                _game.TargetKey = _game.Planet.Nearest(_game.ShipPosition - Main.Center);
                _game.ShotTimer = 0; _game.Fire();
                for (int frame = 0; frame < 22; frame++) _game.UpdateEffects(0.05f);
            }
            Check(_game.Planet.Blocks.Count < blocks, $"Weapon {weapon} destroys ore");
            Check(_game.Credits > credits, $"Weapon {weapon} recovers collected credits");
        }
        for (int kind = 0; kind < 5; kind++)
        {
            var sample = new OreBlock { Kind = kind, Hp = 10, MaxHp = 10 };
            float full = PlanetRenderer.BlockColor(sample, _game.Palette).Luminance;
            sample.Hp = 5; float half = PlanetRenderer.BlockColor(sample, _game.Palette).Luminance;
            sample.Hp = 1; float weak = PlanetRenderer.BlockColor(sample, _game.Palette).Luminance;
            Check(full < half && half < weak, $"Ore {kind} remains progressively brighter as health falls");
        }
        _game.Debris.Clear(); _game.Projectiles.Clear(); _game.Planet.Generate(1);
        var crystal = _game.Planet.Blocks.First(p => p.Value.Kind == MiningPlanet.Crystal);
        double reward = crystal.Value.Value, before = _game.Credits;
        long energy = _game.Flux;
        _game.Planet.Damage(crystal.Key, 10000);
        Check(_game.Debris.Count == 2 && _game.Credits == before, "Destruction releases two tiny fragments without immediate credit");
        for (int i = 0; i < 60; i++) _game.UpdateDebris(0.05f);
        Check(_game.Debris.Count == 0 && Near(_game.Credits, before + reward) && _game.Flux == energy + 1, "Collection pays exact credits and energy");
        for (int i = 0; i < 60; i++) _game.UpdateDebris(0.05f);
        Check(Near(_game.Credits, before + reward), "Collected debris cannot pay twice");
        _game.Planet.Blocks.Clear();
        _game.Planet.Blocks[new Vector2I(0, 0)] = Block(3);
        _game.Planet.Blocks[new Vector2I(1, 0)] = Block(3);
        _game.Planet.Blocks[new Vector2I(2, 0)] = Block(1, 15);
        _game.Planet.ClearSent = false;
        _game.Planet.Damage(Vector2I.Zero, 2);
        Check(_game.Planet.Blocks.Count == 0 && _game.WarpTimer > 0, "Adjacent explosives cascade and schedule warp exactly once");
        _game.WarpTimer = 0; _game.Planet.Generate(1);
        _game.Flux = 10; _game.OverdriveCooldown = 0; _game.ActivateOverdrive();
        Check(_game.Flux == 5 && _game.OverdriveLeft == 8, "Overdrive consumes five energy and activates for eight seconds");
        _game.ActivateOverdrive();
        Check(_game.Flux == 5, "Cooldown prevents double spending");
        _game.TogglePause();
        before = _game.Credits; count = _game.Planet.Blocks.Count;
        for (int i = 0; i < 90; i++) _game.Step(1f / 30);
        Check(_game.Credits == before && _game.Planet.Blocks.Count == count, "Pause freezes mining and pending reward collection");
        _game.TogglePause(); await Frame();
        _game.ResetProgress(); await Frame();
        _game.Sound.Enabled = false; _game.Weapon = 1;
        _game.Levels[0] = _game.Levels[1] = 3; _game.Levels[2] = 2;
        // Warp integration is independent of campaign pacing; use sparse remnants rather
        // than requiring a deliberately weak starter to clear two full late-tier grids.
        for (int planet = 0; planet < 2; planet++)
        {
            _game.Planet.Blocks.Clear(); _game.Planet.ClearSent = false;
            _game.Planet.Blocks[Vector2I.Zero] = Block(MiningPlanet.Stone);
            _game.ShipPosition = Main.Center + Vector2.Left * 80;
            for (int i = 0; i < 1200 && _game.Sector < planet + 2; i++) _game.Step(1f / 30);
        }
        Check(_game.Sector >= 3, "Autopilot mines sparse remnants and completes two warp transitions");
        Check(_game.Levels.SequenceEqual(new[] { 3, 3, 2 }) && _game.Weapon == 1, "Warp retains upgrades and weapon selection");
        await TestSparseAutopilot();
        await TestSaves();
        await TestCrafting();
        await TestFleetEvolution();
        await TestModeTransitions();
        await TestHologramActivation();
        await TestVisualDesigns();
        await TestHeldSteeringAndCamera();
        Check(Main.Compact(1200000) == "1.2M" && Main.Compact(1e40).Length > 10, "Large currency formats without integer overflow");
        _game.QueueFree(); await Frame();
        _game = null!;
        GC.Collect(); GC.WaitForPendingFinalizers();
        await Frame();
    }

    private async Task TestCrafting()
    {
        _game.ResetProgress(); await Frame(); await Frame();
        _game.Sound.Enabled = false;
        var rules = _game.Planet.GenerationSettings;
        bool depthGated = true, armoredDeposits = true;
        int[] deposits = [0, 0, 0];
        int cashOnly = 0;
        foreach (var (key, ore) in _game.Planet.Blocks)
        {
            double depth = 1 - MiningPlanet.KeyPoint(key).Length() / (MiningPlanet.BaseRadius * MiningPlanet.Cell);
            if (ore.Material >= 0)
            {
                deposits[ore.Material]++;
                double baseHp = rules.BaseHp + (MiningPlanet.BaseRadius - key.Length()) * rules.HpDepthBonus;
                double kindFactor = ore.Kind switch { MiningPlanet.Gold => rules.GoldHpMultiplier, MiningPlanet.Crystal => rules.CrystalHpMultiplier,
                    MiningPlanet.Volatile => rules.VolatileHpMultiplier, MiningPlanet.Core => rules.CoreHpMultiplier, _ => 1 };
                double armor = ore.Material == 0 ? rules.TitaniumHardness : ore.Material == 1 ? rules.IridiumHardness : rules.AetherHardness;
                armoredDeposits &= Near(ore.MaxHp, baseHp * kindFactor * armor) && armor >= 2.8;
                depthGated &= depth + 0.00001 >= (ore.Material == 0 ? rules.TitaniumDepth : ore.Material == 1 ? rules.IridiumDepth : rules.AetherDepth);
            }
            else if (ore.Kind is MiningPlanet.Stone or MiningPlanet.Gold) cashOnly++;
        }
        Check(depthGated && deposits.All(n => n > 0), "All three crafting materials occur only at their intended deep layers");
        Check(armoredDeposits, "Rare deposits retain their own material-specific 2.8x/4.5x/7x mining resistance on top of depth and sector hardness");
        Check(cashOnly > _game.Planet.Blocks.Count * 0.85, "The overwhelming majority of rocks only award credits");
        Check(rules.Toughness(20) > 15 && rules.Toughness(600) > rules.Toughness(500) && rules.Richness(20) < 4, "Planet hardness accelerates beyond fleet growth and continues past sector 500, while credits grow slowly");
        double credits = _game.Credits;
        Check(!_game.Craft(1) && _game.Credits == credits && _game.Equipment.Weapons.Count == 1,
            "Insufficient crafting resources leave money and inventory untouched");
        _game.Credits = 20000;
        Check(_game.TradeMaterial(0, 5, true) && _game.Credits == 17600 && _game.Equipment.Materials[0] == 5, "Market buys a five-unit material batch at the displayed credit price");
        Check(_game.TradeMaterial(0, 1, false) && _game.Credits == 17768 && _game.Equipment.Materials[0] == 4 && _game.IncomeAccum == 0,
            "Material sales pay less than purchases and cannot inflate offline mining income");
        Check(_game.Craft(1) && _game.Equipment.Weapons.Count == 2 && _game.Equipment.Materials[0] == 0 && _game.Credits == 17168,
            "Crafting a Nova creates an owned instance and consumes the complete recipe exactly once");
        Check(!_game.Craft(2), "Ion blueprint remains rank-gated even when using the crafting API");
        _game.TradeMaterial(0, 5, true);
        Check(_game.Craft(3) && _game.Equipment.Drones.Count == 1 && _game.Equipment.Drones[0].WeaponId == 0, "Crafted drones start unarmed and consume a drone bay");
        var drone = _game.Equipment.Drones[0];
        var nova = _game.Equipment.Weapons[1];
        Check(!_game.EquipDrone(drone.Id, _game.Equipment.ShipWeaponId) && drone.WeaponId == 0, "A weapon cannot be mounted on both the ship and a drone");
        Check(_game.EquipDrone(drone.Id, nova.Id) && _game.Equipment.Valid(), "A spare crafted weapon mounts on a drone with a valid unique assignment");
        double upgradeCost = _game.WeaponUpgradeCost(nova), before = _game.Credits;
        Check(_game.UpgradeEquipment(nova.Id, false) && nova.Level == 1 && _game.Credits == before - upgradeCost, "Credits upgrade the individual mounted weapon");
        Check(_game.UpgradeEquipment(drone.Id, true) && drone.Level == 1, "Credits also upgrade drone chassis independently");
        _game.AutoMode = false;
        _game.ReleaseControls();
        _game.Planet.Blocks.Clear();
        _game.Planet.Blocks[Vector2I.Zero] = new OreBlock { Hp = 100, MaxHp = 100, Value = 3, Material = 0, MaterialAmount = 2 };
        _game.Planet.Blocks[new Vector2I(0, 12)] = new OreBlock { Hp = 10000, MaxHp = 10000, Value = 3 };
        _game.Planet.ClearSent = false;
        _game.ShipPosition = Main.Center + Vector2.Left * 90;
        drone.Position = _game.ShipPosition;
        _game.ShotTimer = 999;
        _game.EquipDrone(drone.Id, 0);
        for (int i = 0; i < 40; i++) _game.Step(0.05f);
        Check(_game.Planet.Blocks[Vector2I.Zero].Hp == 100, "An unarmed drone never mines by itself");
        _game.EquipDrone(drone.Id, nova.Id); _game.DeployDrone(drone.Id);
        for (int i = 0; i < 120; i++) _game.Step(0.05f);
        Check(_game.Planet.Blocks[Vector2I.Zero].Hp < 100 && _game.Controls.PointerId == -1,
            "An armed drone autonomously fires its fitted weapon while the ship is idle");
        _game.Planet.Damage(Vector2I.Zero, 10000);
        long materialBefore = _game.Equipment.Materials[0];
        Check(_game.Debris.Any(d => d.Material == 0 && d.MaterialAmount == 2) && _game.Equipment.Materials[0] == materialBefore,
            "Rare materials are carried by pending fragments rather than paid twice at destruction");
        _game.Persistence.Save(_game); _game.Persistence.Load(_game);
        for (int i = 0; i < 65; i++) _game.UpdateDebris(0.05f);
        Check(_game.Equipment.Materials[0] == materialBefore + 2, "Saving and reopening preserves pending rare materials and collects them exactly once");
        _game.Persistence.Save(_game); _game.Persistence.Load(_game);
        for (int i = 0; i < 65; i++) _game.UpdateDebris(0.05f);
        Check(_game.Equipment.Materials[0] == materialBefore + 2, "Reopening after collection cannot duplicate crafting materials");
        Check(_game.Equipment.Drones.Count == 1 && _game.Equipment.Drones[0].Level == 1 &&
              _game.Equipment.FindWeapon(nova.Id) is { Level: 1 } && _game.Equipment.Drones[0].WeaponId == nova.Id,
            "Saves restore owned equipment, upgrade levels and unique drone mounts");
        var invalid = SavedJson();
        invalid["equipment"]!["drones"]![0]!["weapon_id"] = _game.Equipment.ShipWeaponId;
        WriteJson(invalid);
        var validEquipment = _game.Equipment;
        Check(!_game.Persistence.Load(_game) && ReferenceEquals(_game.Equipment, validEquipment), "Duplicate saved mounts reject the entire save before mutating the fleet");
        _game.Equipment.HighestSector = 4;
        Check(_game.EngineeringRank == 3 && _game.DroneBays == 3, "Sector milestones unlock engineering ranks and additional bays");
        _game.Credits = 100000;
        _game.Equipment.Materials = [20, 20, 20];
        _game.Craft(0); _game.Craft(2);
        foreach (int type in new[] { 0, 1, 2 })
        {
            var mount = _game.Equipment.Weapons.First(w => w.Type == type && w.Id != _game.Equipment.ShipWeaponId);
            var activeDrone = _game.Equipment.Drones[0];
            activeDrone.Docked = true; activeDrone.Recalling = true;
            _game.EquipDrone(activeDrone.Id, mount.Id); _game.DeployDrone(activeDrone.Id);
            _game.Planet.Blocks.Clear();
            _game.Planet.Blocks[Vector2I.Zero] = new OreBlock { Hp = 1000, MaxHp = 1000, Value = 3 };
            _game.Planet.ClearSent = false; _game.WarpTimer = 0;
            _game.Projectiles.Clear(); _game.Debris.Clear();
            _game.ShipPosition = Main.Center + Vector2.Left * 90;
            activeDrone.Position = _game.ShipPosition;
            activeDrone.ShotTimer = 0; _game.ShotTimer = 999;
            for (int i = 0; i < 60; i++) _game.Step(0.05f);
            Check(_game.Planet.Blocks[Vector2I.Zero].Hp < 1000, $"Drone mount uses the actual {Main.Weapons[type]} mining behavior");
        }
        _game.Craft(4); _game.Craft(5);
        _game.Equipment.Materials[0] = 10;
        before = _game.Credits;
        Check(_game.Equipment.Drones.Count == 3 && !_game.Craft(3) && _game.Credits == before && _game.Equipment.Materials[0] == 10,
            "Drone bay capacity rejects another craft without spending credits or materials");
        _game.Equipment.Drones[0].Docked = true; _game.Equipment.Drones[0].Recalling = true;
        var mountedWeapon = _game.Equipment.FindWeapon(_game.Equipment.Drones[0].WeaponId)!;
        Check(_game.EquipShip(mountedWeapon.Id) && _game.Equipment.Drones[0].WeaponId == 0 && _game.Equipment.Valid(),
            "Moving a drone weapon to the ship atomically clears the old mount");

        // Real touchscreen flow through the new menus, including duplicate mouse emulation.
        _game.ResetProgress(); await Frame(); await Frame();
        _game.Credits = 10000; _game.Equipment.Materials[0] = 20;
        await Tap("pause"); await Tap("workshop");
        var craftPoint = _game.Hud.Buttons["craft_1"].GetGlobalRect().GetCenter();
        Touch(craftPoint, true, 10); Mouse(craftPoint, true, true); Mouse(craftPoint, false, true); Touch(craftPoint, false, 10);
        Mouse(craftPoint, true, true); Mouse(craftPoint, false, true);
        await Frame(); await Frame();
        Check(_game.Equipment.Weapons.Count == 2 && _game.Credits == 9400 && _game.Equipment.Materials[0] == 16,
            "Touch crafting plus emulated mouse creates one item and charges one recipe");
        Check(!_game.Hud.ActivationRunning, "Inventory refresh does not replay the three-second ignition on every purchase");
        var statusLabel = _game.Hud.FindChildren("*", "Label", true, false).OfType<Label>().First(l => l.Text == _game.EquipmentMessage);
        var statusParent = (Control)statusLabel.GetParent();
        Check(statusLabel.GetRect().End.X <= statusParent.Size.X - 21 && statusLabel.Size.Y <= 60,
            "Workshop transaction feedback wraps inside its card rather than overflowing on phone layouts");
        await Tap("craft_drones"); await Tap("craft_3");
        await Tap("tab_fleet"); await Tap("fleet_drones");
        int droneId = _game.Equipment.Drones[0].Id;
        await Tap($"fit_{droneId}");
        Check(_game.Equipment.Drones[0].WeaponId == _game.Equipment.Weapons[1].Id, "Fleet touchscreen control equips the spare crafted weapon on the new drone");
        await Tap("tab_market"); before = _game.Credits;
        await Tap("buy_1");
        Check(_game.Equipment.Materials[1] == 1 && _game.Credits == before - 1800, "Intergalactic market touchscreen purchase updates credits and inventory");
        await Tap("done"); await Tap("resume");
        _game.ResetProgress(); await Frame(); await Frame();
    }

    private async Task TestSparseAutopilot()
    {
        _game.ResetProgress(); await Frame();
        _game.Sound.Enabled = false;
        Vector2I[][] remnants =
        [
            [new(26, 0)], [new(-26, 0)], [new(0, 26)], [new(0, -26)],
            [new(26, 0), new(25, 0), new(-25, -1)]
        ];
        ulong planetId = _game.Planet.GetInstanceId();
        for (int weapon = 0; weapon < 3; weapon++)
        for (int scenario = 0; scenario < remnants.Length; scenario++)
        {
            _game.WarpTimer = 0;
            _game.Planet.Blocks.Clear();
            foreach (var key in remnants[scenario]) _game.Planet.Blocks[key] = new OreBlock { Hp = 5, MaxHp = 5, Value = 3 };
            _game.Planet.TotalBlocks = remnants[scenario].Length;
            _game.Planet.ClearSent = false;
            _game.Planet.NotifyTilesChanged();
            _game.Projectiles.Clear(); _game.Debris.Clear();
            _game.Weapon = weapon;
            _game.ShotTimer = 0;
            _game.AimKey = _game.TargetKey = MiningPlanet.NoTarget;
            var first = MiningPlanet.KeyPoint(remnants[scenario][0]);
            _game.ShipPosition = Main.Center - first.Normalized() * 600;
            bool speedLimited = true, hullClear = true;
            for (int i = 0; i < 900 && _game.WarpTimer <= 0; i++)
            {
                var previous = _game.ShipPosition;
                _game.Step(1f / 30);
                // Shell impact can start the warp during UpdateEffects, before movement.
                if (_game.WarpTimer <= 0)
                    speedLimited &= _game.ShipPosition.DistanceTo(previous) <= AutopilotSteering.MaxSpeed / 30 + 0.001f;
                hullClear &= !_game.Planet.ContainsPoint(_game.ShipPosition - Main.Center, 8);
            }
            Check(_game.Planet.Blocks.Count == 0 && _game.WarpTimer > 0,
                $"Autopilot weapon {weapon} clears sparse remnant scenario {scenario} from beyond firing range");
            Check(speedLimited && hullClear && _game.Planet.GetInstanceId() == planetId,
                $"Sparse approach {weapon}/{scenario} stays speed-bounded, collision-free, and preserves the live planet");
        }
        _game.WarpTimer = 0;
        _game.Planet.Generate(1);
        _game.PlaceShipAtOrbit();
    }

    private async Task TestModeTransitions()
    {
        _game.ResetProgress(); await Frame();
        await Resize(540, 960);
        var ship = _game.ShipPosition;
        var planet = _game.Planet.GetInstanceId();
        var size = Root.Size;
        float scale = _game.View.CameraScale;
        await Tap("mode");
        Check(!_game.View.LayoutAutoMode && Root.Size == size && _game.View.Size.Y > _game.View.Size.X,
            "Manual mode commits its portrait layout without requesting an OS resize");
        Check(_game.ModeStatus.Visible && _game.View.Motion.ManualZoom > scale,
            "Manual mode retains the resource-driven close-up and status indicator");
        Check(_game.Planet.GetInstanceId() == planet && _game.ShipPosition == ship,
            "Mode presentation preserves the live world and ship");
        _game.View.PlaceWorld(false, 0.05f);
        Check(_game.View.CameraScale > scale && _game.View.CameraScale < _game.View.TargetScale,
            "Manual framing eases toward the authored zoom");
        float previousError = Mathf.Abs(_game.View.CameraScale - _game.View.TargetScale);
        bool monotonic = true;
        for (int i = 0; i < 40; i++)
        {
            _game.View.PlaceWorld(false, 0.05f);
            float error = Mathf.Abs(_game.View.CameraScale - _game.View.TargetScale);
            monotonic &= error <= previousError + 0.00001f;
            previousError = error;
        }
        Check(monotonic && previousError < 0.001f, "Camera converges smoothly without a rotation handoff");
        _game.Hud.Rebuild();
        var point = _game.View.WorldToScreen(ship) + Vector2.Left * 70;
        Touch(point, true, 2);
        Check(_game.Navigation.Active, "Manual mode accepts a held portrait steering pointer");
        _game.SetAuto(true);
        _game.SetAuto(false);
        _game.SetAuto(true);
        await Frame(); await Frame();
        Check(_game.View.LayoutAutoMode && _game.AutoMode && Root.Size == size &&
              !_game.Navigation.Active && _game.Hud.Buttons.ContainsKey("systems"),
            "Rapid mode reversals retain portrait and discard stale routes and HUDs");
        await Resize(390, 844);
        var phoneSize = Root.Size;
        _game.SetAuto(false); await Frame(); await Frame();
        Check(Root.Size == phoneSize && !_game.View.LayoutAutoMode && _game.Hud.Buttons.ContainsKey("weapon_2"),
            "Phone-sized manual mode retains the same native portrait window");
        _game.SetAuto(true); await Frame(); await Frame();
        await Resize(540, 960);
    }

    private async Task TestHologramActivation()
    {
        _game.SetAuto(false); await Frame(); await Frame();
        _game.View.PlaceWorld(true);
        var channels = _game.Hud.Channels;
        Check(channels.Count == 6 && _game.Hud.ActivationDuration >= 2.9f && _game.Hud.ActivationDuration <= 3.1f,
            "Manual holographic HUD uses six staggered modules with a perceptible three-second activation budget");
        var first = channels[0];
        var last = channels[^1];
        float duration = _game.Hud.ActivationDuration;
        first.Seek(duration * 0.05f); last.Seek(duration * 0.05f);
        Check(first.Brightness > 1 && last.Brightness == 0 && first.Modulate.A == 1 && last.Modulate.A == 0,
            "Identity ignites with luminous overshoot before the command module, without a HUD alpha fade");
        first.Seek(duration * 0.055f); float peak1 = first.Brightness;
        first.Seek(duration * 0.13f); float dip1 = first.Brightness;
        first.Seek(duration * 0.21f); float peak2 = first.Brightness;
        first.Seek(duration * 0.29f); float dip2 = first.Brightness;
        first.Seek(duration * 0.39f); float peak3 = first.Brightness;
        Check(peak1 > peak2 && peak2 > peak3 && peak3 > 1 && dip1 < dip2 && dip2 < 1,
            "Ignition has three diminishing peaks and progressively smaller irregular brightness dips");
        var held = _game.View.WorldToScreen(_game.ShipPosition) + Vector2.Left * 70;
        Touch(held, true, 2);
        Check(_game.Navigation.Active && _game.Controls.PointerId == 2,
            "Held steering is responsive during holographic ignition");
        foreach (var channel in channels) channel.Seek(duration);
        Check(channels.All(c => !c.IsProcessing() && !c.Igniting && c.Brightness == 1 && c.Modulate.A == 1),
            "Every HUD module stabilizes within the authored duration and stops animation processing");
        Check(_game.Controls.PointerId == 2, "Ignition settling never releases the steering finger");
        ReleaseWorld();
        first.Seek(duration); var restingPosition = first.Position;
        bool boundedJitter = true;
        for (int i = 0; i < 12; i++)
        {
            first.Seek(duration * i / 12);
            boundedJitter &= first.Position.DistanceTo(restingPosition) <= _game.Hud.Motion.JitterPixels * 1.1f;
        }
        first.Seek(duration * 0.05f); var strike = first.Position;
        first.Seek(duration * 0.064f); float beforeBreak = first.Brightness;
        first.Seek(duration * 0.066f); float afterBreak = first.Brightness;
        Check(strike.DistanceTo(restingPosition) > 0.3f && beforeBreak - afterBreak > 1,
            "Neon starter has visible displaced strikes and abrupt millisecond-scale luminance breaks instead of a smooth pulse");
        Check(boundedJitter, "Neon ignition jitter remains bounded by its authored displacement budget");
        first.Seek(duration);
        await Tap("cargo");
        Check(_game.Hud.BlocksWorldInput && !_game.Navigation.Active,
            "Extraction-log modal still blocks the world over holographic telemetry");
        await Tap("done");
        Check(!_game.Hud.ActivationRunning, "Closing a modal does not replay the entire HUD ignition");
        _game.SetAuto(true); await Frame(); await Frame();
    }

    private async Task TestVisualDesigns()
    {
        _game.ResetProgress(); await Frame();
        _game.Planet.Blocks.First().Value.Hp *= 0.4;
        var tiles = Snapshot(_game.Planet);
        var generation = _game.Planet.GenerationSettings;
        ulong planetId = _game.Planet.GetInstanceId();
        var ship = _game.ShipPosition;
        double credits = _game.Credits;
        var fragment = new DebrisFragment { Position = Main.Center, Value = 7, Size = 2 };
        _game.Debris.Add(fragment);
        await Tap("pause"); await Tap("designs");
        Check(_game.VisualStyles.Count >= 17 && _game.VisualStyles.Select(s => s.Treatment).Distinct().Count() == _game.VisualStyles.Count,
            "Pause comparison retains Original and Clean Frontier, eight new technical treatments and archived designs");
        for (int i = 0; i < _game.VisualStyles.Count; i++)
        {
            while (!_game.Hud.Buttons.ContainsKey($"design_{i}")) await Tap("design_next");
            await Tap($"design_{i}");
            var art = _game.CurrentVisualStyle;
            _game.Step(0.1f);
            Check(_game.VisualStyleIndex == i && _game.Paused && _game.Hud.PanelKind == "designs" &&
                  _game.Planet.GetNode<PlanetRenderer>("Renderer").Style == art && _game.GetNode<Starfield>("Starfield").Style == art,
                $"Touch selection previews {art.Title} immediately while remaining paused");
            Check(_game.Planet.GetInstanceId() == planetId && SameTiles(tiles, _game.Planet.Blocks) &&
                  _game.Planet.GenerationSettings == generation && _game.ShipPosition == ship && _game.Credits == credits &&
                  _game.Debris.Count == 1 && ReferenceEquals(_game.Debris[0], fragment),
                $"{art.Title} preserves live terrain, partial health, generation, ship, wallet and pending rewards");
            bool readableDamage = true;
            for (int kind = 0; kind < 5; kind++)
            {
                var block = new OreBlock { Kind = kind, Hp = 10, MaxHp = 10 };
                float full = art.BlockColor(block, _game.Palette).Luminance;
                block.Hp = 5; float half = art.BlockColor(block, _game.Palette).Luminance;
                block.Hp = 1;
                readableDamage &= full < half && half < art.BlockColor(block, _game.Palette).Luminance;
            }
            Check(readableDamage, $"{art.Title} retains readable persistent damage for all five minerals");
        }
        await Tap("back");
        Check(_game.Paused && _game.Hud.PanelKind == "pause", "Leaving visual comparison returns to pause without resuming simulation");
        await Tap("resume");
        Check(!_game.Paused && _game.Hud.PanelKind == "", "Resume keeps the selected world design");
        _game.SetVisualStyle(0, false);
    }

    private void WorldHold(Vector2 world, int finger = 2)
    {
        var point = _game.View.WorldToScreen(world);
        if (_game.Controls.PointerId == finger)
        {
            using var drag = new InputEventScreenDrag { Position = point, Index = finger };
            Root.PushInput(drag, true);
        }
        else Touch(point, true, finger);
    }

    private void ReleaseWorld(int finger = 2) => Touch(_game.Controls.PointerPosition, false, finger);

    private void NavigationField(int weapon, Vector2I obstacle, double hp = 3)
    {
        _game.ReleaseControls();
        _game.WarpTimer = _game.OverdriveLeft = 0;
        _game.Planet.Blocks.Clear();
        _game.Planet.Blocks[obstacle] = new OreBlock { Hp = hp, MaxHp = hp, Value = 3 };
        _game.Planet.Blocks[new Vector2I(0, 9)] = new OreBlock { Hp = 10000, MaxHp = 10000, Value = 15, Kind = 1 };
        _game.Planet.ClearSent = false;
        _game.Planet.TotalBlocks = 2;
        _game.Planet.NotifyTilesChanged();
        _game.ShipPosition = Main.Center + new Vector2(-60, 0);
        _game.ShipVelocity = Vector2.Zero;
        _game.Weapon = weapon;
        _game.ShotTimer = 0;
        _game.Projectiles.Clear(); _game.Beams.Clear(); _game.Debris.Clear();
        Array.Clear(_game.Levels);
        _game.View.PlaceWorld(true);
        _game.Hud.Rebuild();
    }

    private async Task TestHeldSteeringAndCamera()
    {
        _game.ResetProgress(); await Frame();
        await Resize(540, 960);
        _game.SetAuto(false); await Frame(); await Frame();
        _game.Sound.Enabled = false;
        var sceneId = _game.Planet.GetInstanceId();

        NavigationField(0, Vector2I.Zero, 10000);
        _game.ShipPosition = Main.Center + new Vector2(-180, 0);
        _game.View.PlaceWorld(true);
        WorldHold(_game.ShipPosition + Vector2.Right * 110);
        _game.Step(0.05f);
        float cruising = _game.ShipVelocity.Length();
        bool safeApproach = true;
        float minimumCenterDistance = MiningPlanet.Cell * 0.5f + ManualNavigation.HullPadding + _game.FlightSettings.MiningStandOff;
        for (int i = 0; i < 120; i++)
        {
            _game.Step(0.05f);
            safeApproach &= Main.Center.X - _game.ShipPosition.X >= minimumCenterDistance - 0.01f;
        }
        Check(safeApproach && _game.Navigation.ClearingPath && _game.Controls.PointerId == 2,
            "Held steering preserves the configured safety clearance while mining a durable rock");
        Check(cruising > 180 && _game.ShipVelocity.Length() < 5 && _game.Planet.Blocks[Vector2I.Zero].Hp < 10000,
            "Ship slows from cruising speed to a safe mining hover while continuing to fire");
        ReleaseWorld();

        for (int weapon = 0; weapon < 3; weapon++)
        {
            NavigationField(weapon, Vector2I.Zero, 100);
            var goal = Main.Center + new Vector2(60, 0); // Visible empty space on the other side of a rock.
            WorldHold(goal);
            Check(_game.Navigation.Active, $"Weapon {weapon}: a held portrait press steers across ore");
            bool neverInside = true;
            for (int i = 0; i < 60; i++)
            {
                _game.Step(1f / 60);
                neverInside &= !_game.Planet.ContainsPoint(_game.ShipPosition - Main.Center, ManualNavigation.HullPadding);
            }
            Check(neverInside && _game.ShipPosition.X < Main.Center.X - 16 && _game.Navigation.ClearingPath,
                $"Weapon {weapon}: ship stops safely before a durable blocking rock");
            Check(_game.Planet.Blocks[Vector2I.Zero].Hp < 100,
                $"Weapon {weapon}: a destination across a rock automatically starts mining");
            _game.Planet.Blocks[Vector2I.Zero].Hp = 1;
            for (int i = 0; i < 600 && _game.ShipPosition.X < goal.X; i++)
            {
                _game.Step(1f / 60);
                neverInside &= !_game.Planet.ContainsPoint(_game.ShipPosition - Main.Center, ManualNavigation.HullPadding);
            }
            Check(neverInside && _game.Navigation.Active && _game.ShipPosition.X >= goal.X,
                $"Weapon {weapon}: resumes held steering after clearing the path");
            ReleaseWorld();
            double hp = _game.Planet.Blocks[new Vector2I(0, 9)].Hp;
            for (int i = 0; i < 120; i++) _game.Step(1f / 60);
            Check(hp == 10000 && _game.Planet.Blocks[new Vector2I(0, 9)].Hp == hp && _game.Projectiles.Count == 0,
                $"Weapon {weapon}: idle navigation does not mine unrelated ore");
        }

        NavigationField(0, new Vector2I(0, 1));
        WorldHold(Main.Center + new Vector2(60, 0));
        for (int i = 0; i < 600 && _game.ShipPosition.X < Main.Center.X + 60; i++) _game.Step(1f / 60);
        ReleaseWorld();
        Check(!_game.Planet.Blocks.ContainsKey(new Vector2I(0, 1)) && !_game.Navigation.Active &&
              _game.ShipPosition.X > Main.Center.X + 59,
            "A rock grazing the hull is aimed at and cleared even when the route centerline misses it");

        NavigationField(0, Vector2I.Zero);
        _game.ShipPosition = Main.Center + new Vector2(-60, -60);
        _game.View.PlaceWorld(true);
        WorldHold(Main.Center + new Vector2(60, 60));
        bool diagonalSafe = true;
        for (int i = 0; i < 600 && _game.ShipPosition.X < Main.Center.X + 60; i++)
        {
            _game.Step(1f / 60);
            diagonalSafe &= !_game.Planet.ContainsPoint(_game.ShipPosition - Main.Center, ManualNavigation.HullPadding);
        }
        ReleaseWorld();
        Check(diagonalSafe && !_game.Navigation.Active && _game.ShipPosition.DistanceTo(Main.Center + new Vector2(60, 60)) < 5,
            "Diagonal travel mines through obstacles without cutting through tile corners");

        NavigationField(0, Vector2I.Zero, 10000);
        var start = _game.ShipPosition;
        var away = start + Vector2.Left * 80;
        WorldHold(away);
        var chosen = _game.Navigation.Destination;
        _game.Step(0.05f);
        Check(_game.ShipPosition.X < start.X && _game.Beams.Count == 0 && !_game.Navigation.ClearingPath,
            "An empty-space route travels without firing at nearby off-route ore");
        for (int i = 0; i < 30; i++) _game.Step(0.05f);
        Check(_game.Navigation.Active && _game.ShipPosition.X < chosen.X - 80,
            "A stationary held thumb keeps steering beyond the original world point as the camera follows");
        var redirect = _game.ShipPosition + Vector2.Up * 70;
        WorldHold(redirect);
        var before = _game.ShipPosition;
        _game.Step(0.05f);
        Check(_game.ShipPosition.Y < before.Y && Math.Abs(_game.ShipPosition.X - before.X) < 0.01f,
            "Dragging the same held finger redirects travel immediately");
        WorldHold(_game.ShipPosition);
        before = _game.ShipPosition;
        _game.Step(0.05f);
        Check(!_game.Navigation.Active && _game.Controls.PointerId == 2 && _game.ShipPosition == before,
            "Dragging the held thumb onto the ship stops movement without losing the steering finger");
        WorldHold(_game.ShipPosition + Vector2.Left * 90);
        Check(_game.Navigation.Active && _game.Controls.PointerId == 2, "Dragging out of the ship dead zone resumes steering");
        ReleaseWorld();
        before = _game.ShipPosition;
        for (int i = 0; i < 20; i++) _game.Step(0.05f);
        Check(!_game.Navigation.Active && _game.ShipPosition == before, "Releasing the thumb stops travel on subsequent frames");

        var mousePoint = _game.View.WorldToScreen(_game.ShipPosition) + Vector2.Left * 100;
        Mouse(mousePoint, true, false);
        Check(_game.Navigation.Active && _game.Controls.PointerId == -2, "Holding the desktop mouse starts steering immediately");
        start = _game.ShipPosition;
        for (int i = 0; i < 160; i++) _game.Step(0.05f);
        Check(_game.ShipPosition.DistanceTo(start) > 1000 && _game.Navigation.Active &&
              _game.Planet.GetInstanceId() == sceneId, "Holding the mouse steers continuously across the world without stopping at a waypoint");
        Check(_game.View.CameraFocus.DistanceTo(_game.ShipPosition) < 35,
            "Camera follows the ship continuously during held steering");
        var mouseDrag = _game.View.WorldToScreen(_game.ShipPosition) + Vector2.Up * 100;
        using (var motion = new InputEventMouseMotion { Position = mouseDrag, ButtonMask = MouseButtonMask.Left }) Root.PushInput(motion, true);
        before = _game.ShipPosition;
        _game.Step(0.05f);
        Check(_game.ShipPosition.Y < before.Y && Math.Abs(_game.ShipPosition.X - before.X) < 0.01f,
            "Moving the held desktop mouse changes the steering direction");
        Mouse(mouseDrag, false, false);
        before = _game.ShipPosition;
        _game.Step(0.05f);
        Check(!_game.Navigation.Active && _game.Controls.PointerId == -1 && _game.ShipPosition == before,
            "Releasing the desktop mouse stops steering and clears ownership");
        WorldHold(_game.ShipPosition + Vector2.Left * 100);
        _game.TogglePause(); await Frame();
        before = _game.ShipPosition;
        _game.Step(0.05f);
        _game.TogglePause(); await Frame();
        _game.Step(0.05f);
        Check(!_game.Navigation.Active && _game.ShipPosition == before, "Pause and resume require a fresh steering press");
        for (int weapon = 0; weapon < 3; weapon++)
        {
            _game.Weapon = weapon;
            _game.AutoMode = true;
            float autoCooldown = _game.FiringCooldown;
            _game.AutoMode = false;
            Check(Near(autoCooldown / _game.FiringCooldown, Main.ManualFireRateMultiplier),
                $"Manual weapon {weapon} retains its initial 25-percent firing-cadence advantage");
        }
        _game.Levels[1] = 100;
        _game.OverdriveLeft = 8;
        _game.AutoMode = true;
        float cappedAuto = _game.FiringCooldown;
        _game.AutoMode = false;
        Check(Near(cappedAuto / _game.FiringCooldown, Main.ManualFireRateMultiplier),
            "Manual cadence advantage survives upgrades and overdrive");
        _game.SetAuto(true); await Frame(); await Frame();
        for (int i = 0; i < 90; i++) _game.Step(0.05f);
        Check(_game.View.CameraFocus.DistanceTo(_game.ShipPosition) < 35, "Autopilot retains ship-following camera framing");
    }

    private async Task TestSaves()
    {
        _game.Credits = 12345; _game.Flux = 17;
        _game.Debris.Add(new DebrisFragment { Position = Main.Center, Value = 12.5, Energy = 1, Kind = 2, Size = 2 });
        var tiles = Snapshot(_game.Planet);
        int count = _game.Debris.Count;
        double pending = _game.Debris.Sum(f => f.Value);
        long energy = _game.Debris.Sum(f => f.Energy);
        _game.SetVisualStyle(2, false);
        Check(_game.Persistence.Save(_game), "Atomic C# save writes successfully");
        _game.SetVisualStyle(0, false);
        _game.Credits = _game.Flux = 0; _game.Debris.Clear();
        _game.Navigation.SetDestination(_game.ShipPosition + Vector2.Left * 100);
        Check(_game.Persistence.Load(_game), "C# save loads successfully");
        Check(_game.CurrentVisualStyle.Id == "contour", "Save restores the selected visual design independently of terrain data");
        Check(!_game.Navigation.Active && _game.Controls.PointerId == -1, "Save loading cancels transient navigation and pending touches");
        Check(_game.Credits == 12345 && _game.Flux == 17, "Save restores wallet");
        Check(SameTiles(tiles, _game.Planet.Blocks), "Save restores every remaining block and health value");
        Check(_game.Debris.Count == count, "Save restores pending collectible debris");
        for (int i = 0; i < 65; i++) _game.UpdateDebris(0.05f);
        Check(Near(_game.Credits, 12345 + pending) && _game.Flux == 17 + energy, "Restored debris pays exact pending rewards once");
        _game.Persistence.Save(_game); _game.Persistence.Load(_game);
        for (int i = 0; i < 65; i++) _game.UpdateDebris(0.05f);
        Check(Near(_game.Credits, 12345 + pending) && _game.Flux == 17 + energy, "Reopening after collection cannot duplicate saved rewards");

        foreach (string fixture in new[] { "gdscript-v01", "gdscript-v02" })
        {
            Fixture(fixture);
            var raw = SavedJson();
            var expected = raw["tiles"]!.AsArray().ToDictionary(t => new Vector2I((int)t!["x"]!, (int)t["y"]!), t => new OreBlock
            {
                Hp = (double)t!["hp"]!, MaxHp = (double)t["max_hp"]!, Value = (double)t["value"]!, Kind = (int)t["kind"]!, Shade = (int)t["shade"]!
            });
            Check(_game.Persistence.Load(_game), $"Original serializer {fixture} fixture is readable");
            Check(_game.Credits == 12345.75 && _game.Flux == 17 && _game.Sector == 4 && _game.Weapon == 2 &&
                !_game.AutoMode && _game.Levels.SequenceEqual(new[] { 3, 2, 1 }), $"{fixture} preserves wallet, upgrades, sector, weapon and mode");
            Check(SameTiles(expected, _game.Planet.Blocks), $"{fixture} preserves original tiles and partial health");
            Check(_game.VisualStyleIndex == 0, $"{fixture} without a design preference loads the default appearance safely");
            if (fixture == "gdscript-v02")
            {
                Check(_game.Debris.Count == 2 && _game.ShipPosition == new Vector2(77.5f, 408.25f), "0.2 preserves ship and pending fragments");
                for (int i = 0; i < 65; i++) _game.UpdateDebris(0.05f);
                Check(_game.Credits == 12383.25 && _game.Flux == 18, "Original 0.2 pending reward survives migration and pays once");
            }
            else Check(_game.Debris.Count == 0 && _game.LifetimeEnergy == 0, "0.1 missing debris fields default safely");
        }
        Fixture("gdscript-generation");
        var generation = SavedJson()["tiles"]!.AsArray().ToDictionary(t => new Vector2I((int)t!["x"]!, (int)t["y"]!), t => new OreBlock
        {
            Hp = (double)t!["hp"]!, MaxHp = (double)t["max_hp"]!, Value = (double)t["value"]!, Kind = (int)t["kind"]!, Shade = (int)t["shade"]!
        });
        Check(_game.Persistence.Load(_game) && SameTiles(generation, _game.Planet.Blocks),
            "Original generated-planet saves still load every tile without resizing their world");
        _game.Planet.Generate(4);
        Check(_game.Planet.Blocks.Count > generation.Count * 8, "New planet generation has roughly nine times the original ore capacity");
        Fixture("gdscript-v02");
        var offline = SavedJson();
        offline["auto"] = true; offline["credits"] = 10; offline["rate"] = 20;
        offline["timestamp"] = Time.GetUnixTimeFromSystem() - 10 * 3600;
        WriteJson(offline);
        _game.Persistence.Load(_game);
        Check(_game.Credits == 72010, "Offline income remains 25 percent and caps at four hours");
        _game.Persistence.Load(_game);
        Check(_game.Credits == 72010, "Offline catch-up is committed and cannot be claimed twice");
        offline["auto"] = false; WriteJson(offline); _game.Persistence.Load(_game);
        Check(_game.Credits == 10, "Manual mode receives no offline mining income");
        offline["timestamp"] = Time.GetUnixTimeFromSystem() + 3600; offline["auto"] = true;
        WriteJson(offline); _game.Persistence.Load(_game);
        Check(_game.Credits == 10, "Future save timestamp cannot create offline income");
        offline["cleared"] = true; offline["tiles"] = new JsonArray(); WriteJson(offline);
        _game.Persistence.Load(_game);
        Check(_game.Sector == 5 && _game.Planet.Blocks.Count > 200 && _game.Credits == 10, "Reopening a cleared sector advances once without repeating the bonus");
        _game.Persistence.Load(_game);
        Check(_game.Sector == 5, "Reopening completed-sector migration cannot skip a second planet");
        var malformed = SavedJson(); malformed["levels"] = new JsonArray(1, 2); WriteJson(malformed);
        string damaged = FileAccess.GetFileAsString(_game.Persistence.Path);
        var before = Snapshot(_game.Planet); double wallet = _game.Credits;
        Check(!_game.Persistence.Load(_game) && _game.Credits == wallet && SameTiles(before, _game.Planet.Blocks) &&
            FileAccess.GetFileAsString(_game.Persistence.Path) == damaged, "Malformed save is rejected without mutating game or overwriting file");
        Fixture("gdscript-v02");
        var badPickup = SavedJson(); badPickup["pickups"]![0]!["value"] = -1; WriteJson(badPickup);
        Check(!_game.Persistence.Load(_game) && _game.Credits == wallet, "Invalid pending rewards reject the whole save before applying state");
        await Frame();
    }
}
