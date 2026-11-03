using Godot;
using System.Text.Json;
using StarfallMiner;

/// <summary>Opt-in deterministic economy playthrough. Uses real simulation and transactions; no player saves.</summary>
public partial class BalanceRun : Node
{
    private Main _game = null!;
    private readonly Dictionary<string, double> _milestones = new();
    private readonly List<object> _samples = new();
    private float _seconds;
    private async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    public override async void _Ready()
    {
        if (!OS.GetCmdlineUserArgs().Contains("--test")) { GetTree().Quit(2); return; }
        try
        {
            _game = GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate<Main>(); AddChild(_game);
            _game.SetProcess(false); _game.Sound.Enabled = false;
            await Frame(); await Frame();
            int minutes = 60; string label = "current";
            foreach (var arg in OS.GetCmdlineUserArgs())
            {
                if (arg.StartsWith("--balance-minutes=")) minutes = Math.Clamp(int.Parse(arg.Split('=')[1]), 1, 180);
                if (arg.StartsWith("--balance-label=")) label = arg.Split('=')[1];
            }
            double lastEarned = 0; long lastMined = 0;
            int steps = minutes * 60 * 20;
            for (int tick = 0; tick <= steps; tick++)
            {
                _seconds = tick * 0.05f;
                if (tick % 100 == 0)
                {
                    PurchasePlan();
                    if (_game.Hud.LayoutPending) { await Frame(); await Frame(); }
                }
                if (tick % 6000 == 0)
                {
                    double interval = tick == 0 ? 1 : 300;
                    double rate = (_game.TotalEarned - lastEarned) / interval;
                    long mined = _game.TotalMined;
                    _samples.Add(new { seconds = _seconds, sector = _game.Sector, credits = _game.Credits, earned = _game.TotalEarned,
                        mined, credits_per_minute = rate * 60, blocks_per_minute = (mined - lastMined) * 60 / interval,
                        rank = _game.EngineeringRank, mounts = _game.Equipment.MountedShipCount, weapons = _game.Equipment.Weapons.Count,
                        drones = _game.Equipment.Drones.Count, remaining_blocks = _game.Planet.Blocks.Count });
                    lastEarned = _game.TotalEarned; lastMined = mined;
                    GD.Print($"BALANCE {label}: {_seconds / 60:0} min / sector {_game.Sector} / mined {mined} / {rate * 60:0.0} CR per min / {_game.Equipment.MountedShipCount} mounts / {_game.Equipment.Drones.Count} drones");
                    await Frame();
                }
                if (tick < steps)
                {
                    int previousSector = _game.Sector;
                    _game.Step(0.05f);
                    if (_game.Sector > previousSector) Mark("sector_" + _game.Sector);
                }
            }
            string result = JsonSerializer.Serialize(new { label, profile = "Guided automatic miner; checks purchases every five simulated seconds; no gifted resources or forced clears", minutes,
                milestones_seconds = _milestones, samples = _samples }, new JsonSerializerOptions { WriteIndented = true });
            DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://builds/qa"));
            using var file = Godot.FileAccess.Open($"res://builds/qa/balance-{label}.json", Godot.FileAccess.ModeFlags.Write);
            file.StoreString(result);
            _game.QueueFree(); await Frame(); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    private void Mark(string key) { if (!_milestones.ContainsKey(key)) _milestones[key] = _seconds; }
    private bool Build(int index)
    {
        var recipe = _game.Catalog.Recipes[index];
        if (_game.EngineeringRank < recipe.Rank) return false;
        int[] need = [recipe.Titanium, recipe.Iridium, recipe.Aether];
        double total = recipe.Credits;
        for (int i = 0; i < 3; i++) total += Math.Max(0, need[i] - _game.Equipment.Materials[i]) * _game.Catalog.MarketPrices[i];
        if (_game.Credits < total) return false;
        for (int i = 0; i < 3; i++)
            while (_game.Equipment.Materials[i] < need[i]) _game.TradeMaterial(i, (int)Math.Min(5, need[i] - _game.Equipment.Materials[i]), true);
        return _game.Craft(index);
    }
    private void PurchasePlan()
    {
        var starter = _game.Equipment.Weapons[0];
        if (starter.Level == 0 && _game.Credits >= _game.WeaponUpgradeCost(starter)) { _game.UpgradeEquipment(starter.Id, false); Mark("first_power"); return; }
        if (starter.RateLevel == 0 && _game.Credits >= _game.FireRateUpgradeCost(starter)) { _game.UpgradeFireRate(starter.Id); Mark("first_rate"); return; }
        CraftedWeapon? cannon = null, wave = null;
        foreach (var item in _game.Equipment.Weapons) { if (item.Type == 1) cannon ??= item; if (item.Type == 3) wave ??= item; }
        if (cannon == null)
        {
            if (Build(1)) { cannon = _game.Equipment.Weapons[^1]; _game.EquipShip(cannon.Id); Mark("first_cannon"); }
            return;
        }
        if (cannon.Level == 0 && _game.Credits >= _game.WeaponUpgradeCost(cannon)) { _game.UpgradeEquipment(cannon.Id, false); return; }
        if (cannon.RateLevel == 0 && _game.Credits >= _game.FireRateUpgradeCost(cannon)) { _game.UpgradeFireRate(cannon.Id); return; }
        if (_game.Equipment.Drones.Count == 0)
        {
            if (Build(3)) { var drone = _game.Equipment.Drones[^1]; _game.EquipDrone(drone.Id, starter.Id); _game.DeployDrone(drone.Id); Mark("first_armed_drone"); }
            return;
        }
        for (int level = 1; level < 3; level++)
        {
            if (cannon.Level == level) { if (_game.Credits >= _game.WeaponUpgradeCost(cannon)) _game.UpgradeEquipment(cannon.Id, false); return; }
            if (cannon.RateLevel == level) { if (_game.Credits >= _game.FireRateUpgradeCost(cannon)) _game.UpgradeFireRate(cannon.Id); return; }
        }
        if (_game.ShipHardpoints == 1 && _game.EngineeringRank >= 2)
        {
            if (_game.Credits >= _game.HardpointCost) { _game.BuyHardpoint(); Mark("second_hardpoint"); }
            return;
        }
        if (_game.ShipHardpoints > _game.Equipment.MountedShipCount)
        {
            if (Build(1)) { _game.ToggleShipMount(_game.Equipment.Weapons[^1].Id); Mark("second_ship_weapon"); }
            return;
        }
        if (wave == null && _game.EngineeringRank >= 2)
        {
            if (Build(6))
            {
                wave = _game.Equipment.Weapons[^1];
                int old = _game.Equipment.ShipAuxWeaponIds[0];
                if (old > 0) _game.ToggleShipMount(old);
                if (_game.ShipHardpoints > 1) _game.ToggleShipMount(wave.Id);
                else _game.EquipShip(wave.Id);
                Mark("first_nova_wave");
            }
            return;
        }
        foreach (var item in _game.Equipment.Weapons)
            if (_game.Equipment.ShipMounted(item.Id) && item.Level < Math.Min(5, _game.EquipmentLevelCap) && _game.Credits >= _game.WeaponUpgradeCost(item))
            { _game.UpgradeEquipment(item.Id, false); return; }
        if (_game.EngineeringRank >= 3 && _game.ShipHardpoints < 3 && _game.Credits >= _game.HardpointCost) { _game.BuyHardpoint(); Mark("third_hardpoint"); }
    }
}
