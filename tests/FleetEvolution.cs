using Godot;
using StarfallMiner;

public partial class Smoke
{
    private async Task TestFleetEvolution()
    {
        _game.ResetProgress(); await Frame(); await Frame();
        _game.Sound.Enabled = false;
        _game.Credits = 100000;
        _game.Equipment.Materials = [100, 100, 100];
        double wallet = _game.Credits;
        Check(!_game.BuyHardpoint() && _game.Credits == wallet && _game.ShipHardpoints == 1,
            "A fresh mothership has one weapon and rank-gates extra hardpoints without charging");
        Check(_game.Equipment.Weapons.Count == 1 && _game.Equipment.MountedShipCount == 1,
            "Fresh equipment includes exactly one mounted starter weapon");
        _game.Equipment.HighestSector = 4;
        Check(_game.BuyHardpoint() && _game.BuyHardpoint() && _game.ShipHardpoints == 3,
            "Credit-funded rank milestones expand the mothership to three hardpoints");
        wallet = _game.Credits;
        Check(!_game.BuyHardpoint() && _game.Credits == wallet, "A fourth hardpoint cannot be purchased");
        _game.Craft(0); _game.Craft(1); _game.Craft(2); _game.Craft(6); _game.Craft(7); _game.Craft(3);
        var nova = _game.Equipment.Weapons.First(w => w.Type == 1);
        var wave = _game.Equipment.Weapons.First(w => w.Type == 3);
        var bore = _game.Equipment.Weapons.First(w => w.Type == 4);
        var spare = _game.Equipment.Weapons.First(w => w.Type == 0 && w.Id != _game.Equipment.ShipWeaponId);
        Check(_game.ToggleShipMount(nova.Id) && _game.ToggleShipMount(wave.Id) && _game.Equipment.MountedShipCount == 3 && _game.Equipment.Valid(),
            "Three distinct ship weapons can be fitted simultaneously with unique ownership");
        var drone = _game.Equipment.Drones[0];
        Check(!_game.EquipDrone(drone.Id, nova.Id) && drone.WeaponId == 0,
            "A drone cannot steal an auxiliary mothership weapon or gain a second mount");
        Check(_game.EquipDrone(drone.Id, bore.Id), "The new Bore laser fits a drone's single weapon mount");
        _game.Planet.Blocks.Clear(); _game.Planet.ClearSent = false;
        for (int x = 0; x < 3; x++) _game.Planet.Blocks[new Vector2I(x, 0)] = new OreBlock { Hp = 100, MaxHp = 100, Value = 1 };
        _game.ShipPosition = Main.Center + Vector2.Left * 80;
        _game.TargetKey = Vector2I.Zero; _game.ShotTimer = 0;
        _game.Fire();
        Check(Near(_game.Planet.Blocks[Vector2I.Zero].Hp, 98.9) && _game.Planet.Blocks[new Vector2I(1, 0)].Hp == 100,
            "Unupgraded starter damages one rock for 1.1 rather than piercing multiple layers");
        await Frame(); await Frame();
        for (int i = 0; i < 6; i++) _game.Step(0.05f);
        Check(_game.Projectiles.Count == 1 && _game.Waves.Count == 1,
            "Auxiliary cannon and Nova wave fire beside the primary weapon in the same simulation step");
        Check(Main.WeaponPower(20) == 2.6 && Main.WeaponRate(20) < 1.25f && _game.EquipmentLevelCap == 15,
            "Individual upgrades have restrained additive damage/cadence and rank-based level caps");

        _game.Equipment.ShipAuxWeaponIds = [0, 0];
        _game.EquipShip(wave.Id);
        _game.Projectiles.Clear(); _game.Waves.Clear(); _game.Debris.Clear();
        _game.Planet.Blocks.Clear(); _game.Planet.ClearSent = false;
        foreach (int x in new[] { 0, 4, 7 }) _game.Planet.Blocks[new Vector2I(x, 0)] = new OreBlock { Hp = 100, MaxHp = 100, Value = 1 };
        _game.TargetKey = Vector2I.Zero; _game.ShotTimer = 0; _game.Fire();
        _game.UpdateEffects(0.05f);
        double centerHp = _game.Planet.Blocks[Vector2I.Zero].Hp;
        Check(centerHp < 100 && _game.Planet.Blocks[new Vector2I(4, 0)].Hp == 100,
            "Nova damage begins at its center while distant blocks wait for the expanding front");
        for (int i = 0; i < 34; i++) _game.UpdateEffects(0.05f);
        double rimHp = _game.Planet.Blocks[new Vector2I(4, 0)].Hp;
        Check(centerHp < rimHp && rimHp < 100 && _game.Planet.Blocks[new Vector2I(7, 0)].Hp == 100 && _game.Waves.Count == 0,
            "Nova front deals stronger center damage, respects its radius and expires");
        Check(Near(centerHp, _game.Planet.Blocks[Vector2I.Zero].Hp), "An expanding Nova hits each block once instead of damaging its center every frame");

        _game.Planet.Blocks.Clear(); _game.Planet.ClearSent = false;
        _game.Planet.Blocks[Vector2I.Zero] = new OreBlock { Hp = 100, MaxHp = 100, Material = 0, MaterialAmount = 1 };
        _game.Planet.Blocks[Vector2I.Right] = new OreBlock { Hp = 100, MaxHp = 100 };
        _game.EquipShip(bore.Id); _game.TargetKey = Vector2I.Zero; _game.ShotTimer = 0; _game.Fire();
        Check(Near(_game.Planet.Blocks[Vector2I.Zero].Hp, 93.5) && Near(_game.Planet.Blocks[Vector2I.Right].Hp, 97.4),
            "Bore laser pierces two blocks and specializes in rare-deposit armor rather than dominating ordinary stone");
        _game.EquipShip(wave.Id); _game.EquipDrone(drone.Id, bore.Id);
        _game.Planet.Generate(1); _game.PlaceShipAtOrbit();
        _game.AutoMode = false; _game.ReleaseControls(); _game.ShotTimer = 999;
        _game.Equipment.ShipAuxWeaponIds = [0, 0];
        var ship = _game.ShipPosition;
        var hp = _game.Planet.Blocks.Values.Sum(b => b.Hp);
        Check(_game.DeployDrone(drone.Id) && !drone.Docked, "A fitted drone deploys from the mothership");
        for (int i = 0; i < 800; i++) _game.Step(0.05f);
        Check(drone.Position.DistanceTo(ship) > 150 && _game.ShipPosition == ship && _game.Planet.Blocks.Values.Sum(b => b.Hp) < hp,
            "A drone leaves the idle mothership, cruises the planet and independently mines terrain");
        wallet = _game.Credits; int level = drone.Level;
        Check(!_game.UpgradeEquipment(drone.Id, true) && !_game.UpgradeEquipment(bore.Id, false) && !_game.EquipDrone(drone.Id, spare.Id) && !_game.EquipShip(bore.Id) &&
            _game.Credits == wallet && drone.Level == level && drone.WeaponId == bore.Id,
            "Remote chassis upgrades, mounted weapon upgrades and transfers fail without spending or changing mounts");
        _game.RecallDrone(drone.Id); _game.Paused = true;
        hp = _game.Planet.Blocks.Values.Sum(b => b.Hp);
        for (int i = 0; i < 220; i++) _game.Step(0.05f);
        Check(drone.Docked && drone.Position.DistanceTo(_game.ShipPosition) < 12 && _game.Planet.Blocks.Values.Sum(b => b.Hp) == hp,
            "Recalled drones dock while the workshop is paused without mining or advancing the planet");
        Check(_game.UpgradeEquipment(drone.Id, true) && _game.UpgradeEquipment(bore.Id, false) && _game.EquipDrone(drone.Id, spare.Id),
            "Docking enables chassis and weapon upgrades and a new single weapon fit");
        _game.ToggleShipMount(nova.Id); _game.ToggleShipMount(bore.Id);
        _game.Persistence.Save(_game); _game.Persistence.Load(_game);
        Check(_game.Equipment.HardpointLevel == 2 && _game.Equipment.MountedShipCount == 3 && _game.Equipment.ShipMounted(bore.Id) &&
            _game.Equipment.FindWeapon(wave.Id)?.Type == 3 && _game.Equipment.FindWeapon(bore.Id)?.Level == 1 && _game.Equipment.Valid(),
            "Saves retain expanded ship hardpoints, new weapon types, upgrades and unique mounts");
        var invalid = SavedJson(); invalid["equipment"]!["ship_aux_weapon_ids"]![0] = _game.Equipment.ShipWeaponId;
        WriteJson(invalid); var original = _game.Equipment;
        Check(!_game.Persistence.Load(_game) && ReferenceEquals(original, _game.Equipment), "Duplicate auxiliary mounts reject the whole save before changing live equipment");
        _game.Persistence.Save(_game);
        _game.DeployDrone(_game.Equipment.Drones[0].Id);
        _game.Persistence.Save(_game); _game.Persistence.Load(_game);
        Check(!_game.Equipment.Drones[0].Docked && !_game.Equipment.Drones[0].Recalling, "A deployed mining mission persists across reloads instead of silently parking the drone");
        var oldEquipment = SavedJson(); oldEquipment["equipment"]!.AsObject().Remove("ship_aux_weapon_ids"); oldEquipment["equipment"]!.AsObject().Remove("hardpoint_level");
        WriteJson(oldEquipment);
        Check(_game.Persistence.Load(_game) && _game.ShipHardpoints == 1 && _game.Equipment.MountedShipCount == 1,
            "Earlier crafting saves without hardpoint fields load with a single primary mount");
        _game.Paused = false; _game.ResetProgress(); await Frame(); await Frame();
        _game.Credits = 10000; _game.Equipment.HighestSector = 4;
        await Tap("systems");
        var schematic = _game.Hud.FindChildren("*", "Control", true, false).OfType<ShipSchematic>().FirstOrDefault();
        Check(schematic != null && schematic.IsVisibleInTree(), "Ship upgrading displays the live fitted hull rather than hiding it behind statistics");
        var rect = _game.Hud.Buttons["hardpoint"].GetGlobalRect(); var point = rect.GetCenter();
        Touch(point, true, 10); Mouse(point, true, true); Mouse(point, false, true); Touch(point, false, 10);
        Mouse(point, true, true); Mouse(point, false, true); await Frame(); await Frame();
        Check(_game.ShipHardpoints == 2 && _game.Credits == 7600, "Injected touchscreen hardpoint purchase charges once despite mouse emulation");
        await Tap("tab_fleet");
        Check(_game.Hud.FindChildren("*", "Control", true, false).OfType<ShipSchematic>().Count() == 1, "The ship remains visible while fitting or upgrading individual weapons");
        _game.ResetProgress(); await Frame(); await Frame();
    }
}
