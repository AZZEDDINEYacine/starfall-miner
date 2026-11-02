using Godot;
using StarfallMiner;

public partial class Smoke
{
    private void PocketField()
    {
        PilotField(Vector2.Zero, 0);
        for (int y = -7; y <= 7; y++)
        for (int x = -9; x <= 9; x++)
        {
            bool chamber = Math.Abs(x) <= 2 && Math.Abs(y) <= 1;
            bool narrowExit = x >= 3 && y == 0;
            if (!chamber && !narrowExit) PilotRock(x, y, 3.2);
        }
        _game.Planet.TotalBlocks = _game.Planet.Blocks.Count;
        _game.Planet.NotifyTilesChanged();
    }

    private void BeamField()
    {
        NavigationField(Main.ContinuousLaser, Vector2I.Zero, 1000);
        _game.Equipment = EquipmentState.Fresh();
        _game.Equipment.Weapons.Add(new CraftedWeapon { Id = 2, Type = Main.ContinuousLaser });
        _game.Equipment.NextId = 3; _game.Equipment.ShipWeaponId = 2;
        _game.Planet.Blocks[Vector2I.Right] = new OreBlock { Hp = 1000, MaxHp = 1000, Value = 2 };
        _game.Weapon = Main.ContinuousLaser;
        _game.AutoMode = false;
    }

    private async Task TestPocketAndLaser()
    {
        _game.ResetProgress(); await Frame(); await Frame(); _game.Sound.Enabled = false;
        float normalStall = _game.PilotSettings.ClearanceStallTime;
        PocketField(); int before = _game.Planet.Blocks.Count;
        _game.PilotSettings.ClearanceStallTime = 1000;
        for (int frame = 0; frame < 360; frame++) _game.Step(1f / 30);
        Check(_game.Planet.Blocks.Count == before,
            "Narrow-exit fixture reproduces the old stand-off deadlock when the clearance-cut rule is disabled");
        _game.PilotSettings.ClearanceStallTime = normalStall;
        foreach (float dt in new[] { 1f / 60, 1f / 30, 1f / 15 })
        {
            PocketField(); before = _game.Planet.Blocks.Count;
            bool clear = true, bounded = true, widened = false;
            float maxTravel = 0;
            for (int frame = 0; frame < (int)(40 / dt); frame++)
            {
                var start = _game.ShipPosition;
                _game.Step(dt);
                widened |= _game.Autopilot.State == AutopilotSteering.Behavior.Widen;
                maxTravel = Mathf.Max(maxTravel, _game.ShipPosition.DistanceTo(Main.Center));
                clear &= !_game.Planet.ContainsPoint(_game.ShipPosition - Main.Center, AutopilotSteering.HullPadding);
                bounded &= _game.ShipPosition.DistanceTo(start) <= AutopilotSteering.MaxSpeed * dt + 0.002f;
            }
            GD.Print($"POCKET {1 / dt:0}fps: mined {before - _game.Planet.Blocks.Count}, travel {maxTravel:0.0}, clear {clear}, widen {widened}");
            Check(clear && bounded && widened && before - _game.Planet.Blocks.Count >= 4 && maxTravel > 25,
                $"Trapped pilot makes its own room by mining visible faces, then resumes movement at {1 / dt:0} fps without hull clipping or teleportation");
        }

        _game.ResetProgress(); await Frame(); await Frame();
        _game.Credits = 115; _game.TradeMaterial(0, 1, true);
        Check(_game.Craft(8) && _game.Credits == 0 && _game.Equipment.Materials[0] == 0 && _game.Equipment.Weapons[^1].Type == Main.ContinuousLaser,
            "Flux beam is craftable at rank one for 35 CR plus one Titanium, or 115 CR including market material");
        var beam = _game.Equipment.Weapons[^1];
        Check(_game.EquipShip(beam.Id) && _game.Equipment.Valid(), "Continuous laser fits the existing unique primary mount");
        _game.UpgradeFireRate(beam.Id); // Unaffordable: does not partially change saved equipment.
        _game.Persistence.Save(_game); _game.Persistence.Load(_game);
        Check(_game.Weapon == Main.ContinuousLaser && _game.Equipment.FindWeapon(beam.Id)?.Type == Main.ContinuousLaser,
            "The added beam weapon round-trips in the existing version-one save without renumbering older weapons");

        _game.SetAuto(false); await Frame(); await Frame(); await Resize(390, 844);
        BeamField(); await Frame(); await Frame();
        WorldHold(_game.ShipPosition + new Vector2(110, 8));
        _game.Step(1f / 60);
        var laser = _game.LaserBeams[0];
        var pointerDirection = (_game.Navigation.Destination - _game.ShipPosition).Normalized();
        Check(laser.Active && laser.Hit && Mathf.Abs((laser.End - laser.Origin).Normalized().Cross(pointerDirection)) < 0.0001f &&
            Mathf.Abs(_game.Navigation.AimDirection.Cross(pointerDirection)) > 0.01f,
            "Injected held thumb aims the continuous beam precisely through the pointer even when navigation aims at a blocking tile center");
        Check(Near(laser.End.X, Main.Center.X - 8) && _game.Planet.Blocks[Vector2I.Zero].Hp < 1000 && _game.Planet.Blocks[Vector2I.Right].Hp == 1000,
            "Laser terminates at the actual first tile face and cannot damage a tile behind it");
        WorldHold(_game.ShipPosition + new Vector2(130, -60));
        _game.Step(1f / 60); laser = _game.LaserBeams[0];
        Check(laser.Active && !laser.Hit && (laser.End - laser.Origin).Y < -30 &&
            Mathf.Abs((laser.End - laser.Origin).Normalized().Cross((_game.Navigation.Destination - _game.ShipPosition).Normalized())) < 0.0001f,
            "Dragging the same held thumb immediately redirects an uninterrupted beam into empty space without snapping to nearby ore");
        ReleaseWorld();
        Check(!_game.LaserBeams[0].Active && !_game.Navigation.Active, "Releasing the beam's owner stops it immediately without a fading aim trail");

        double[] damage = new double[2];
        for (int trial = 0; trial < 2; trial++)
        {
            BeamField(); await Frame(); await Frame();
            WorldHold(_game.ShipPosition + Vector2.Right * 120);
            float dt = trial == 0 ? 1f / 30 : 1f / 60;
            bool uninterrupted = true;
            for (int frame = 0; frame < (int)Math.Round(1 / dt); frame++) { _game.Step(dt); uninterrupted &= _game.LaserBeams[0].Active; }
            damage[trial] = 1000 - _game.Planet.Blocks[Vector2I.Zero].Hp;
            Check(uninterrupted && _game.Controls.PointerId == 2, $"Continuous beam stays lit on every frame under a stationary held thumb at {1 / dt:0} fps");
            ReleaseWorld();
        }
        Check(Math.Abs(damage[0] - damage[1]) < 0.001 && Math.Abs(damage[0] - _game.Progression.WeaponBaseDamage[Main.ContinuousLaser] * 2.4) < 0.001,
            "Beam damage integrates elapsed time independently of frame rate and retains the 2.4x manual advantage");
        BeamField(); await Frame(); await Frame(); WorldHold(_game.ShipPosition + Vector2.Right * 120); _game.Step(0.05f);
        _game.Hud.OpenPanel("systems");
        Check(!_game.Navigation.Active && !_game.LaserBeams[0].Active, "Opening a modal immediately cancels held beam ownership and damage");
        _game.Hud.ClosePanel(); await Frame(); await Frame();
        BeamField(); await Frame(); await Frame(); WorldHold(_game.ShipPosition + Vector2.Right * 120); _game.Step(0.05f);
        Touch(_game.Controls.PointerPosition, false, 2, true);
        Check(!_game.LaserBeams[0].Active && _game.Controls.PointerId == -1, "Canceled pointer release immediately extinguishes the continuous beam");

        // The same weapon is real on auxiliary and autonomous drone mounts.
        _game.ReleaseControls(); BeamField();
        _game.AutoMode = true; _game.Weapon = 0; _game.Equipment.ShipWeaponId = 1;
        _game.Equipment.HardpointLevel = 1; _game.Equipment.ShipAuxWeaponIds[0] = 2;
        _game.ShipPosition = Main.Center + Vector2.Left * 88; _game.Autopilot.Reset(0); _game.ShotTimer = 999;
        for (int frame = 0; frame < 90; frame++) _game.Step(1f / 60);
        Check(_game.LaserBeams[1].Active && _game.Planet.Blocks[Vector2I.Zero].Hp < 1000,
            "An auxiliary beam mines continuously when the local automatic pilot is ready to fire");
        _game.Equipment.ShipAuxWeaponIds[0] = 0;
        _game.Equipment.Drones.Add(new MiningDrone { Id = 3, Type = 0, WeaponId = 2, Recalling = false, Docked = false, Position = Main.Center + Vector2.Left * 88 });
        _game.Equipment.NextId = 4; _game.AutoMode = false;
        double hp = _game.Planet.Blocks[Vector2I.Zero].Hp; _game.Step(0.05f);
        Check(_game.LaserBeams[3].Active && _game.Planet.Blocks[Vector2I.Zero].Hp < hp && _game.Equipment.Valid(),
            "An armed independent drone uses real continuous beam damage with no held ship pointer");
        _game.Paused = true; hp = _game.Planet.Blocks[Vector2I.Zero].Hp; _game.Step(0.5f);
        Check(!_game.LaserBeams.Any(b => b.Active) && _game.Planet.Blocks[Vector2I.Zero].Hp == hp,
            "Paused servicing extinguishes all beams and cannot secretly mine");

        var firstColor = PlanetRenderer.TerrainColor(_game.CurrentVisualStyle.OreColor(0, _game.Palette), 1);
        var secondColor = PlanetRenderer.TerrainColor(_game.CurrentVisualStyle.OreColor(0, _game.Palette), 2);
        Check(firstColor != secondColor && Math.Abs(firstColor.V - secondColor.V) < 0.001f,
            "Successive sectors change rock hue while preserving terrain contrast and fixed resource color codes");
        _game.ResetProgress(); await Frame(); await Frame(); await Resize(540, 960);
    }
}
