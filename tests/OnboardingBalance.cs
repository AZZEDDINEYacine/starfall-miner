using Godot;
using StarfallMiner;

public partial class Smoke
{
    private async Task TestOnboardingAndManualProductivity()
    {
        _game.ResetProgress(); await Frame(); await Frame(); _game.Sound.Enabled = false;
        var weapon = _game.Equipment.Weapons[0];
        Check(_game.WeaponUpgradeCost(weapon) == 25 && _game.FireRateUpgradeCost(weapon) == 20 &&
            _game.UpgradeEquipment(weapon.Id, false) && _game.UpgradeFireRate(weapon.Id) && _game.Credits == 35,
            "The starting 80 credits fund two immediately useful upgrades while leaving credits to mine with");
        Check(Near(_game.WeaponPower(1), 1.6) && Near(Main.FireRateBonus(1), 1.1) &&
            _game.WeaponPower(5) >= 4 && _game.WeaponPower(20) >= 13 &&
            Main.FireRateBonus(4) - Main.FireRateBonus(3) < Main.FireRateBonus(1) - Main.FireRateBonus(0),
            "Power upgrades remain substantial through every rank; Rate has front-loaded gains without exponential damage compounding");
        weapon.Level = 0; double first = _game.WeaponUpgradeCost(weapon);
        weapon.Level = 3; double middle = _game.WeaponUpgradeCost(weapon);
        weapon.Level = 12; double late = _game.WeaponUpgradeCost(weapon);
        Check(first < middle && late > middle * 100 && late / first > 1000,
            "Later equipment investment costs accelerate steeply after the discounted first three purchases");
        weapon.Level = 0;
        var authoredCosts = _game.Progression.EarlyPowerCosts; double invalidWallet = _game.Credits;
        _game.Progression.EarlyPowerCosts = [-25, 55, 110];
        bool rejected = !_game.UpgradeEquipment(weapon.Id, false) && _game.Credits == invalidWallet && weapon.Level == 0;
        _game.Progression.EarlyPowerCosts = authoredCosts;
        Check(rejected, "An invalid authored discount cannot credit the wallet or partially upgrade equipment");
        Check(_game.Catalog.Recipes[1].Credits + _game.Catalog.Recipes[1].Titanium * _game.Catalog.MarketPrices[0] == 130 &&
            _game.Catalog.Recipes[3].Titanium == 2 && _game.Catalog.MarketPrices[2] > _game.Catalog.MarketPrices[0] * 40,
            "First cannon and Scout use affordable Titanium while deep Aether remains a substantial investment");
        _game.TotalMined = 149;
        Check(_game.EngineeringRank == 1, "First research milestone is not awarded before the threshold");
        await Frame(); await Frame();
        await Tap("systems"); await Tap("tab_fabricator"); await Tap("craft_weapons");
        Check(_game.Hud.Buttons["craft_2"].Text == "LOCKED", "Advanced recipes show their research lock before the mining milestone");
        var milestoneTile = _game.Planet.Blocks.First(p => p.Value.Kind == MiningPlanet.Stone && p.Value.Material < 0).Key;
        _game.Planet.Damage(milestoneTile, 100000);
        await Frame(); await Frame();
        Check(_game.Hud.Buttons["craft_2"].Text == "CRAFT" && !_game.Hud.ActivationRunning,
            "Earning mining research refreshes newly unlocked blueprints immediately without replaying workshop ignition");
        _game.Hud.ClosePanel(); await Frame(); await Frame();
        Check(_game.EngineeringRank == 2 && _game.DroneBays == 2 && _game.Sector == 1,
            "Mining 150 rocks unlocks the second rank and ship expansion without requiring a whole giant planet clear");
        _game.Persistence.Save(_game); _game.Persistence.Load(_game);
        Check(_game.EngineeringRank == 2 && _game.TotalMined == 150, "Mining-based research uses the existing saved lifetime counter and survives reopening");
        var rules = _game.Planet.GenerationSettings;
        Check(rules.Toughness(20) / rules.Richness(20) > rules.Toughness(4) / rules.Richness(4) &&
            rules.Toughness(100) / rules.Richness(100) > rules.Toughness(20) / rules.Richness(20),
            "Later planet resistance keeps rising faster than per-block credit richness");

        double[] earned = new double[2]; long[] mined = new long[2]; double[] damage = new double[2];
        for (int mode = 0; mode < 2; mode++)
        {
            _game.ResetProgress(); await Frame(); await Frame();
            _game.Sound.Enabled = false;
            _game.Planet.Blocks.Clear(); _game.Planet.ClearSent = false;
            for (int x = 0; x < 150; x++)
                _game.Planet.Blocks[new Vector2I(x, 0)] = new OreBlock { Hp = 3.2, MaxHp = 3.2, Value = 2 };
            _game.Planet.TotalBlocks = 150; _game.Planet.RestoreMapExtent(); _game.Planet.NotifyTilesChanged();
            _game.ShipPosition = Main.Center + Vector2.Left * 88; _game.ShipAngle = 0; _game.Autopilot.Reset(0);
            _game.ShotTimer = 0; _game.Debris.Clear();
            double start = _game.Credits;
            if (mode == 1) { _game.SetAuto(false); await Frame(); await Frame(); }
            _game.View.PlaceWorld(true);
            if (mode == 1) WorldHold(_game.ShipPosition + Vector2.Right * 110);
            for (int frame = 0; frame < 1200; frame++) _game.Step(0.05f);
            ReleaseWorld();
            for (int frame = 0; frame < 50; frame++) _game.UpdateDebris(0.05f);
            earned[mode] = _game.Credits - start; mined[mode] = _game.TotalMined;
            damage[mode] = (150 - _game.Planet.Blocks.Count) * 3.2 + _game.Planet.Blocks.Values.Sum(b => 3.2 - b.Hp);
        }
        GD.Print($"PRODUCTIVITY 60s: auto {mined[0]} blocks / {earned[0]:0} CR; held manual {mined[1]} blocks / {earned[1]:0} CR");
        Check(mined[0] > 0 && mined[1] >= mined[0] * 2 && earned[1] >= earned[0] * 2,
            "Actual held-thumb manual mining earns at least twice the blocks and credits of autopilot with the identical starter loadout");
        Check(damage[1] > damage[0] * 2,
            "Manual productivity is real ship damage and mining throughput rather than a display-only earnings multiplier");

        _game.ResetProgress(); await Frame(); await Frame();
        _game.Equipment = EquipmentState.Legacy(1, 0); _game.Equipment.HighestSector = 4; _game.Equipment.HardpointLevel = 1;
        _game.ToggleShipMount(2);
        await Frame(); await Frame();
        double autoDamage = _game.ShipDamageMultiplier; float autoCooldown = _game.FiringCooldown;
        _game.AutoMode = false;
        Check(Near(_game.ShipDamageMultiplier / autoDamage, 1.5) && Near(autoCooldown / _game.FiringCooldown, 1.6) &&
            Near((_game.ShipDamageMultiplier / _game.FiringCooldown) / (autoDamage / autoCooldown), 2.4),
            "Manual ship tuning provides 1.5x hit damage and 1.6x cadence, or 2.4x nominal weapon output");
        double[] droneDamage = new double[2]; float[] droneCadence = new float[2];
        _game.Equipment.ShipAuxWeaponIds = [0, 0]; _game.Equipment.Drones.Clear();
        _game.Equipment.Drones.Add(new MiningDrone { Id = 4, Type = 0, WeaponId = 2 }); _game.Equipment.NextId = 5;
        for (int mode = 0; mode < 2; mode++)
        {
            _game.ReleaseControls(); _game.AutoMode = mode == 0; _game.WarpTimer = 0;
            _game.Planet.Blocks.Clear(); _game.Planet.ClearSent = false;
            _game.Planet.Blocks[Vector2I.Zero] = new OreBlock { Hp = 100000, MaxHp = 100000, Value = 2 };
            _game.ShipPosition = Main.Center + Vector2.Left * 88; _game.ShipAngle = 0; _game.Autopilot.Reset(0); _game.ShotTimer = 999;
            var drone = _game.Equipment.Drones[0]; drone.Position = _game.ShipPosition; drone.ShotTimer = 0; drone.Target = MiningPlanet.NoTarget; drone.RetargetTimer = 0;
            drone.WeaponId = 2; drone.Recalling = drone.Docked = false;
            _game.Projectiles.Clear(); _game.Waves.Clear();
            _game.Step(0.05f); _game.UpdateEffects(0.5f);
            droneDamage[mode] = 100000 - _game.Planet.Blocks[Vector2I.Zero].Hp; droneCadence[mode] = drone.ShotTimer;
        }
        Check(droneDamage[0] > 0 && Near(droneDamage[0], droneDamage[1]) && Near(droneCadence[0], droneCadence[1]),
            "Simply switching to manual mode does not grant an automatic productivity buff to independent drones");
        _game.ResetProgress(); await Frame(); await Frame();
        _game.Credits = 10000; _game.Equipment.Materials = [20, 20, 20]; _game.Equipment.HighestSector = 4;
        _game.Craft(1); _game.Craft(3);
        var cruiser = _game.Equipment.Drones[0]; var cannon = _game.Equipment.Weapons[1];
        _game.EquipDrone(cruiser.Id, cannon.Id); _game.UpgradeEquipment(cannon.Id, false); _game.UpgradeFireRate(cannon.Id); _game.UpgradeEquipment(cruiser.Id, true);
        _game.SetAuto(false); _game.Planet.Generate(1); _game.PlaceShipAtOrbit(); _game.ShotTimer = 999; _game.DeployDrone(cruiser.Id);
        await Frame(); await Frame();
        var idleShip = _game.ShipPosition; double initialHp = _game.Planet.Blocks.Values.Sum(b => b.Hp);
        float greatestSeparation = 0; bool clearHull = true;
        for (int frame = 0; frame < 800; frame++)
        {
            _game.Step(0.05f);
            greatestSeparation = Mathf.Max(greatestSeparation, cruiser.Position.DistanceTo(idleShip));
            clearHull &= !_game.Planet.ContainsPoint(cruiser.Position - Main.Center, 8);
        }
        GD.Print($"DRONE TRANSFER: greatest separation {greatestSeparation:0.0}; mined HP {initialHp - _game.Planet.Blocks.Values.Sum(b => b.Hp):0.0}");
        Check(greatestSeparation > 150 && clearHull && _game.ShipPosition == idleShip && _game.Planet.Blocks.Values.Sum(b => b.Hp) < initialHp,
            "A tuned cannon drone clears jagged lips, cruises away and mines independently instead of repeating the same blocked approach");
        _game.ResetProgress(); await Frame(); await Frame();
    }
}
