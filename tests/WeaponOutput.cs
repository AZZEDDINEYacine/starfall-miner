using Godot;
using StarfallMiner;

public partial class Smoke
{
    private async Task TestWeaponOutput()
    {
        _game.ResetProgress(); await Frame(); await Frame(); await Resize(390, 844);
        _game.SetAuto(false); await Frame(); await Frame(); _game.Sound.Enabled = false;
        foreach (int sector in new[] { 1, 4 })
        foreach (float dt in new[] { 1f / 60, 1f / 30, 1f / 15 })
        {
            BeamField();
            _game.Sector = sector; _game.Planet.Generate(sector);
            var rules = _game.Planet.GenerationSettings;
            double hp = (rules.BaseHp + rules.HpDepthBonus * rules.RadiusForSector(sector)) * rules.Toughness(sector);
            _game.Planet.Blocks.Clear(); _game.Planet.ClearSent = false;
            for (int x = 0; x < 150; x++) _game.Planet.Blocks[new Vector2I(x, 0)] = new OreBlock { Hp = hp, MaxHp = hp, Value = 2 };
            _game.Planet.TotalBlocks = 150; _game.Planet.NotifyTilesChanged();
            _game.Equipment.HighestSector = sector == 1 ? 1 : 7;
            var item = _game.Equipment.Weapons[1]; item.Level = _game.EquipmentLevelCap; item.RateLevel = 0;
            _game.View.PlaceWorld(true); await Frame(); await Frame();
            long start = _game.TotalMined;
            WorldHold(_game.ShipPosition + Vector2.Right * 150);
            for (int frame = 0; frame < (int)Math.Round(10 / dt); frame++) _game.Step(dt);
            long mined = _game.TotalMined - start;
            GD.Print($"LASER THROUGHPUT: sector {sector}, Power {item.Level}, Rate 0, {hp:0.00} HP, {1 / dt:0} fps: {mined} rocks/10s");
            Check(mined >= 40 && !_game.Planet.ContainsPoint(_game.ShipPosition - Main.Center, ManualNavigation.HullPadding),
                $"Held Power-max laser melts at least four deepest ordinary rocks per second in sector {sector} at {1 / dt:0} fps without Rate or overdrive");
            ReleaseWorld();
        }

        // Real weapon behavior, not a damage formula assertion: every weapon's
        // Power investment must deliver a large gain at the same target/loadout.
        for (int type = 0; type < Main.Weapons.Length; type++)
        {
            double[] damage = new double[2];
            for (int trial = 0; trial < 2; trial++)
            {
                _game.ReleaseControls(); _game.Paused = false; _game.WarpTimer = _game.OverdriveLeft = 0;
                _game.Equipment = EquipmentState.Fresh();
                var item = _game.Equipment.Weapons[0]; item.Type = type; item.Level = trial == 0 ? 0 : 20;
                _game.Weapon = type; _game.AutoMode = type != Main.ContinuousLaser;
                _game.Planet.Blocks.Clear(); _game.Planet.ClearSent = false;
                _game.Planet.Blocks[Vector2I.Zero] = new OreBlock { Hp = 100000, MaxHp = 100000, Value = 2 };
                _game.ShipPosition = Main.Center + Vector2.Left * 88;
                _game.TargetKey = Vector2I.Zero; _game.ShotTimer = 0;
                _game.Projectiles.Clear(); _game.Waves.Clear();
                if (type == Main.ContinuousLaser)
                {
                    _game.View.PlaceWorld(true); WorldHold(Main.Center + Vector2.Right * 50); _game.Step(0.05f);
                }
                else { _game.Fire(); for (int frame = 0; frame < 40; frame++) _game.UpdateEffects(0.05f); }
                damage[trial] = 100000 - _game.Planet.Blocks[Vector2I.Zero].Hp;
            }
            Check(damage[0] > 0 && damage[1] >= damage[0] * 12.9,
                $"{Main.Weapons[type]} delivers at least thirteen times fresh per-hit output at Power 20 through its real weapon behavior");
        }

        BeamField(); _game.View.PlaceWorld(true); await Frame(); await Frame();
        _game.Sound.Enabled = true;
        int starts = _game.Sound.LaserLoopStarts;
        WorldHold(_game.ShipPosition + Vector2.Right * 140);
        for (int frame = 0; frame < 90; frame++) _game.Step(1f / 60);
        Check(_game.Sound.LaserLoopPlaying && _game.Sound.LaserLoopStarts == starts + 1,
            "Held beam starts one sustained laser sound, never a fresh one-shot or restarted loop every frame");
        ReleaseWorld();
        Check(!_game.Sound.LaserLoopPlaying, "Releasing the laser immediately stops its sustained sound");
        WorldHold(_game.ShipPosition + Vector2.Right * 140); _game.Step(0.05f);
        _game.Sound.Enabled = false;
        Check(!_game.Sound.LaserLoopPlaying, "Muting stops an already-playing laser loop immediately");
        _game.Sound.Enabled = true; _game.Step(0.05f);
        Check(_game.Sound.LaserLoopPlaying, "Unmuting a still-held firing beam resumes the sustained sound");
        _game.TogglePause();
        Check(!_game.Sound.LaserLoopPlaying, "Pause cancels held firing and silences the continuous laser");
        _game.ResetProgress(); await Frame(); await Frame(); _game.Sound.Enabled = false; await Resize(540, 960);
    }
}
