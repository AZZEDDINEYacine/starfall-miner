using Godot;
using StarfallMiner;

public partial class Smoke
{
    private async Task TestWorkshopRates()
    {
        _game.ResetProgress(); await Frame(); await Frame();
        _game.Sound.Enabled = false;
        await Tap("systems");
        var modal = _game.Hud.Channels.First(c => c.Name == "Modal");
        Check(modal.Igniting && _game.Hud.Channels.Where(c => c != modal).All(c => !c.Igniting),
            "Entering Ship Systems ignites the workshop once without replaying flight telemetry");
        modal.Seek(0.8f); modal.SetProcess(false);
        ulong id = modal.GetInstanceId();
        await Tap("tab_systems");
        Check(_game.Hud.Channels.First(c => c.Name == "Modal").GetInstanceId() == id && modal.Age == 0.8f,
            "Tapping the selected workshop tab neither rebuilds nor restarts ignition");
        foreach (string tab in new[] { "fabricator", "fleet", "market", "systems", "fleet" })
        {
            await Tap("tab_" + tab);
            Check(_game.Hud.Channels.All(c => !c.Igniting && c.Brightness == 1 && !c.IsProcessing()),
                $"Workshop tab {tab} opens settled with no repeated flicker or jitter");
        }
        await Tap("fleet_weapons");
        await Resize(390, 844);
        Check(!_game.Hud.ActivationRunning, "Resizing an open workshop does not replay its ignition");
        _game.Credits = 10000;
        var weapon = _game.Equipment.FindWeapon(_game.Equipment.ShipWeaponId)!;
        float cooldown = _game.FiringCooldown;
        int power = weapon.Level;
        double wallet = _game.Credits, cost = _game.FireRateUpgradeCost(weapon);
        var rateButton = _game.Hud.Buttons[$"rate_upgrade_{weapon.Id}"];
        var point = rateButton.GetGlobalRect().GetCenter();
        Touch(point, true, 10); Mouse(point, true, true); Mouse(point, false, true); Touch(point, false, 10);
        Mouse(point, true, true); Mouse(point, false, true); await Frame(); await Frame();
        Check(weapon.RateLevel == 1 && weapon.Level == power && _game.Credits == wallet - cost && Near(cooldown / _game.FiringCooldown, 1.1),
            "Real touch plus mouse emulation buys one independent fire-rate level and reduces the actual ship cooldown");
        Check(!_game.Hud.ActivationRunning, "Purchasing fire rate keeps the workshop settled");
        var rateRect = _game.Hud.Buttons[$"rate_upgrade_{weapon.Id}"].GetGlobalRect();
        var powerRect = _game.Hud.Buttons[$"weapon_upgrade_{weapon.Id}"].GetGlobalRect();
        Check(!rateRect.Intersects(powerRect) && rateRect.End.X <= _game.Hud.Size.X &&
              _game.Hud.FindChildren("*", "Control", true, false).OfType<ShipSchematic>().Any(s => s.IsVisibleInTree()),
            "Phone-sized workshop separates Power and Fire Rate controls while retaining the visible ship");
        _game.Credits = 0; wallet = _game.Credits;
        Check(!_game.UpgradeFireRate(weapon.Id) && weapon.RateLevel == 1 && _game.Credits == wallet,
            "Unaffordable fire-rate tuning cannot change a level or spend credits");
        _game.Credits = 100000;
        weapon.RateLevel = _game.EquipmentLevelCap;
        wallet = _game.Credits;
        Check(!_game.UpgradeFireRate(weapon.Id) && _game.Credits == wallet,
            "Fire-rate progression respects its engineering-rank ceiling");
        weapon.RateLevel = 2;
        _game.Equipment.Materials = [100, 100, 100]; _game.Equipment.HighestSector = 4;
        _game.Craft(0); _game.Craft(1); _game.Craft(3); _game.BuyHardpoint();
        var spare = _game.Equipment.Weapons[1]; var nova = _game.Equipment.Weapons[2]; var drone = _game.Equipment.Drones[0];
        _game.ToggleShipMount(spare.Id); _game.EquipDrone(drone.Id, nova.Id);
        _game.UpgradeFireRate(spare.Id); _game.UpgradeFireRate(nova.Id);
        await Frame(); await Frame();
        await Tap("fleet_weapons"); await Tap("weapons_next");
        Check(_game.Hud.Buttons.ContainsKey($"rate_upgrade_{nova.Id}") && !_game.Hud.ActivationRunning,
            "Paged weapon inventories retain rate upgrades without re-igniting the workshop");
        await Tap("done");
        _game.Planet.Blocks.Clear(); _game.Planet.ClearSent = false;
        _game.Planet.Blocks[Vector2I.Zero] = new OreBlock { Hp = 10000, MaxHp = 10000, Value = 1 };
        _game.Planet.Blocks[new Vector2I(0, 12)] = new OreBlock { Hp = 10000, MaxHp = 10000, Value = 1 };
        _game.ShipPosition = Main.Center + Vector2.Left * 90;
        _game.ShotTimer = 999; _game.ShipAngle = 0; _game.Autopilot.Reset(0);
        _game.Step(0.3f);
        float auxInterval = _game.WeaponCooldown(spare.Type, spare.Level, spare.RateLevel);
        Check(_game.Beams.Count > 0 && auxInterval < _game.WeaponCooldown(spare.Type, spare.Level),
            "The fitted auxiliary uses its weapon's saved fire-rate tuning");
        _game.AutoMode = false; _game.ReleaseControls();
        drone.Position = _game.ShipPosition;
        _game.DeployDrone(drone.Id); drone.ShotTimer = 0;
        _game.Step(0.05f);
        float droneInterval = _game.WeaponCooldown(nova.Type, nova.Level, nova.RateLevel) / _game.Catalog.Chassis(drone.Type).FireRate;
        Check(Near(drone.ShotTimer, droneInterval) && drone.ShotTimer < _game.WeaponCooldown(nova.Type, nova.Level) / _game.Catalog.Chassis(drone.Type).FireRate,
            "A deployed drone schedules the shorter interval of its rate-tuned fitted weapon");
        wallet = _game.Credits;
        Check(!_game.UpgradeFireRate(nova.Id) && _game.Credits == wallet && nova.RateLevel == 1,
            "Remote drone weapon rate tuning is denied until docking without charging");
        _game.Persistence.Save(_game); _game.Persistence.Load(_game);
        Check(_game.Equipment.FindWeapon(weapon.Id)?.RateLevel == 2 && _game.Equipment.FindWeapon(spare.Id)?.RateLevel == 1 && _game.Equipment.FindWeapon(nova.Id)?.RateLevel == 1,
            "Independent primary, auxiliary and drone fire-rate levels survive saving and reopening");
        var invalid = SavedJson(); invalid["equipment"]!["weapons"]![0]!["rate_level"] = -1;
        WriteJson(invalid); var equipment = _game.Equipment;
        Check(!_game.Persistence.Load(_game) && ReferenceEquals(equipment, _game.Equipment),
            "Invalid saved fire-rate levels reject the entire save before changing live equipment");
        _game.Persistence.Save(_game);
        var legacy = SavedJson();
        foreach (var entry in legacy["equipment"]!["weapons"]!.AsArray()) entry!.AsObject().Remove("rate_level");
        WriteJson(legacy);
        Check(_game.Persistence.Load(_game) && _game.Equipment.Weapons.All(w => w.RateLevel == 0),
            "Earlier equipment saves without rate tuning keep their weapons and default to zero new rate levels");
        _game.AutoMode = true; _game.Hud.ClosePanel(); await Frame(); await Frame();
        await Tap("systems");
        Check(_game.Hud.Channels.First(c => c.Name == "Modal").Igniting, "A new workshop visit can play its entry ignition once again");
        _game.ResetProgress(); await Frame(); await Frame();
        await Resize(540, 960);
    }
}
