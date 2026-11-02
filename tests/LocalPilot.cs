using Godot;
using StarfallMiner;

public partial class Smoke
{
    private void PilotField(Vector2 position, float heading)
    {
        _game.ReleaseControls(); _game.AutoMode = true; _game.Paused = false;
        _game.Planet.Blocks.Clear(); _game.Planet.ClearSent = false;
        _game.WarpTimer = 0; _game.ShotTimer = 0;
        _game.Projectiles.Clear(); _game.Waves.Clear(); _game.Debris.Clear();
        _game.Equipment = EquipmentState.Fresh(); _game.Weapon = 0; Array.Clear(_game.Levels);
        _game.ShipPosition = Main.Center + position; _game.ShipAngle = heading; _game.Autopilot.Reset(heading);
        _game.TargetKey = _game.AimKey = MiningPlanet.NoTarget;
    }
    private void PilotRock(int x, int y, double hp = 12) => _game.Planet.Blocks[new Vector2I(x, y)] = new OreBlock { Hp = hp, MaxHp = hp, Value = 1.5 };

    private async Task TestLocalPilot()
    {
        _game.ResetProgress(); await Frame(); await Frame(); _game.Sound.Enabled = false;
        var rules = _game.Planet.GenerationSettings;
        Check(MiningPlanet.BaseRadius == 54 && _game.Planet.Blocks.Count > 9000 && _game.Planet.Blocks.Keys.Any(k => Math.Abs(k.X) > 30),
            "Double-diameter planets generate a real expanded grid instead of clipping against the old 30-cell bounds");
        var generator = new PlanetGenerator(); var grid = new Dictionary<Vector2I, OreBlock>();
        int previous = 0; bool growing = true;
        for (int sector = 1; sector <= 5; sector++)
        {
            generator.Generate(sector, rules, grid);
            growing &= grid.Count > previous; previous = grid.Count;
        }
        Check(growing && rules.RadiusForSector(1000) > rules.RadiusForSector(999) && rules.RadiusForSector(1_000_000) < MiningPlanet.MaxGridExtent - 1,
            "Planet area grows each early level and authored radius continues increasing through late supported sectors");
        _game.Sector = 4; _game.Planet.Generate(4); _game.PlaceShipAtOrbit();
        var terrain = Snapshot(_game.Planet); _game.Persistence.Save(_game); _game.Persistence.Load(_game);
        Check(SameTiles(terrain, _game.Planet.Blocks) && _game.Planet.Blocks.Keys.Any(k => Math.Abs(k.X) > 54),
            "Expanded planets round-trip their actual extended coordinates and partial health in the existing v1 save");

        PilotField(new Vector2(-230, 0), 0); PilotRock(0, 0, 1000);
        var visible = new AutopilotSteering { Settings = _game.PilotSettings }; visible.Reset(0);
        var hidden = new AutopilotSteering { Settings = _game.PilotSettings }; hidden.Reset(0);
        var first = visible.Advance(_game.ShipPosition, Main.Center, _game.Planet, 0.05f);
        var velocity = visible.Velocity; var target = visible.Target;
        PilotRock(-26, 12, 99999); // Behind the ship and outside near-field hull sensors.
        var second = hidden.Advance(_game.ShipPosition, Main.Center, _game.Planet, 0.05f);
        Check(first == second && hidden.Velocity == velocity && hidden.Target == target && target == Vector2I.Zero,
            "Hidden rocks behind the ship do not influence its local target or steering decisions");
        PilotField(new Vector2(120, 0), 0); PilotRock(0, 0, 1000);
        _game.Step(1f / 60);
        Check(_game.Autopilot.Target == MiningPlanet.NoTarget && !_game.Autopilot.CanFire,
            "A rock behind the nose is initially unknown rather than acquired through a global nearest-block lookup");
        for (int frame = 0; frame < 300; frame++) _game.Step(1f / 60);
        Check(_game.Planet.Blocks[Vector2I.Zero].Hp < 1000 && !_game.Planet.ContainsPoint(_game.ShipPosition - Main.Center, AutopilotSteering.HullPadding),
            "When manual flight leaves the nose pointing away, a local look-around reacquires nearby ore and resumes safe mining");
        PilotField(new Vector2(-150, 0), 0); PilotRock(0, 0, 1000); PilotRock(1, 0, 1000);
        _game.Step(0.05f);
        Check(_game.Autopilot.Target == Vector2I.Zero && _game.Planet.Blocks[Vector2I.Right].Hp == 1000,
            "Forward first-hit sensors choose the exposed face rather than ore hidden behind it");
        bool safe = true, firedInBand = true; double before = 1000;
        for (int frame = 0; frame < 360; frame++)
        {
            double hp = _game.Planet.Blocks[Vector2I.Zero].Hp;
            _game.Step(1f / 60);
            safe &= !_game.Planet.ContainsPoint(_game.ShipPosition - Main.Center, AutopilotSteering.HullPadding);
            if (_game.Planet.Blocks[Vector2I.Zero].Hp < hp)
                firedInBand &= Mathf.Abs(_game.ShipPosition.DistanceTo(Main.Center) - _game.PilotSettings.MiningDistance) <= _game.PilotSettings.MiningTolerance + 4;
        }
        Check(safe && firedInBand && _game.Planet.Blocks[Vector2I.Zero].Hp < before && _game.Autopilot.State == AutopilotSteering.Behavior.Mine,
            "Pilot approaches, brakes and fires in its preferred distance band without touching the rock");
        var start = _game.ShipPosition;
        for (int i = 0; i < 240; i++) _game.Step(1f / 60);
        Check(_game.ShipPosition.DistanceTo(start) < 20 && Mathf.Abs((_game.ShipPosition - Main.Center).Angle() - (start - Main.Center).Angle()) < 0.3f,
            "Mining holds a local hover with small corrections instead of circling the planet");

        foreach (float dt in new[] { 1f / 60, 1f / 30, 1f / 15 })
        {
            for (int shape = 0; shape < 3; shape++)
            {
                PilotField(new Vector2(-112, shape == 2 ? 8 : 0), 0);
                for (int y = -5; y <= 5; y++)
                for (int x = -3; x <= 7; x++)
                {
                    // A manually cut bay, a staggered ledge, and an L-shaped concavity.
                    bool empty = shape == 0 ? x < 0 && Math.Abs(y) < 3 :
                        shape == 1 ? x < (y < 1 ? 0 : -2) : x < 0 && y <= 1;
                    if (!empty) PilotRock(x, y, 4);
                }
                _game.Planet.TotalBlocks = _game.Planet.Blocks.Count;
                var initial = _game.ShipPosition; long mined = _game.TotalMined;
                bool hullClear = true, bounded = true;
                for (int frame = 0; frame < (int)(16 / dt) && _game.WarpTimer == 0; frame++)
                {
                    var last = _game.ShipPosition; _game.Step(dt);
                    hullClear &= !_game.Planet.ContainsPoint(_game.ShipPosition - Main.Center, AutopilotSteering.HullPadding);
                    bounded &= _game.ShipPosition.DistanceTo(last) <= AutopilotSteering.MaxSpeed * dt + 0.002f;
                }
                Check(hullClear && bounded && _game.TotalMined > mined && _game.ShipPosition.DistanceTo(initial) > 8,
                    $"Local pilot mines and moves safely through manually carved terrain {shape} at {Mathf.RoundToInt(1 / dt)} fps");
            }
        }
        PilotField(new Vector2(15, 15), 0); PilotRock(0, 0, 1000);
        bool overlaps = _game.Planet.ContainsPoint(_game.ShipPosition - Main.Center, AutopilotSteering.HullPadding);
        start = _game.ShipPosition;
        for (int frame = 0; frame < 120; frame++) _game.Step(1f / 60);
        Check(overlaps && !_game.Planet.ContainsPoint(_game.ShipPosition - Main.Center, AutopilotSteering.HullPadding) &&
            _game.ShipPosition.DistanceTo(start) > 25 && _game.Autopilot.RecoveryCount > 0,
            "An inherited corner overlap recovers continuously instead of leaving autopilot wedged against the block");

        PilotField(new Vector2(-112, 0), 0);
        for (int y = -4; y <= 4; y++)
        for (int x = -3; x <= 8; x++)
            if (!(x < 0 && Math.Abs(y) < 3)) PilotRock(x, y, 2);
        _game.Planet.TotalBlocks = _game.Planet.Blocks.Count;
        _game.SetAuto(false); await Frame(); await Frame(); _game.View.PlaceWorld(true);
        long manualMined = _game.TotalMined;
        WorldHold(_game.ShipPosition + Vector2.Right * 110);
        for (int frame = 0; frame < 150; frame++) _game.Step(1f / 30);
        var cutTiles = Snapshot(_game.Planet); ulong planetId = _game.Planet.GetInstanceId();
        bool manuallyCut = _game.TotalMined > manualMined;
        var handoff = _game.ShipPosition; long autoMined = _game.TotalMined;
        _game.SetAuto(true); await Frame(); await Frame();
        Check(manuallyCut && _game.Planet.GetInstanceId() == planetId && SameTiles(cutTiles, _game.Planet.Blocks) && _game.ShipPosition == handoff && _game.Controls.PointerId == -1,
            "Real held manual excavation hands its changed topography and ship position to autopilot without rebuilding or teleporting");
        bool safeHandoff = true;
        for (int frame = 0; frame < 600; frame++)
        {
            _game.Step(1f / 30);
            safeHandoff &= !_game.Planet.ContainsPoint(_game.ShipPosition - Main.Center, AutopilotSteering.HullPadding);
        }
        Check(safeHandoff && _game.TotalMined > autoMined && _game.ShipPosition.DistanceTo(handoff) > 8,
            "Autopilot recovers from actual manually changed terrain and continues mining without snagging the hull");

        _game.ResetProgress(); await Frame(); await Frame();
        _game.SetAuto(false); await Frame(); await Frame(); _game.View.PlaceWorld(true);
        var map = _game.Hud.Minimap!; map.RefreshMap(true);
        var mapRect = map.GetGlobalRect(); var mapPoint = mapRect.GetCenter();
        Touch(mapPoint, true, 3); Touch(mapPoint, false, 3);
        Check(_game.Controls.PointerId == -1 && !_game.Navigation.Active && mapRect.End.X <= _game.View.Size.X && mapRect.End.Y < _game.View.Size.Y - 234,
            "Survey minimap stays inside the flight HUD and does not become a hidden steering surface");
        int updates = map.RasterUpdates; ulong revision = _game.Planet.TopologyRevision;
        _game.ShipPosition += Vector2.Left * 20; _game.View.PlaceWorld(true); map.RefreshMap();
        Check(map.RasterUpdates == updates && _game.Planet.TopologyRevision == revision,
            "Following camera motion reuses the minimap terrain raster instead of rebuilding it every frame");
        var key = _game.Planet.Blocks.Keys.First(); _game.Planet.Damage(key, 100000); map.RefreshMap();
        Check(map.RasterUpdates == updates + 1 && map.RasterRevision == _game.Planet.TopologyRevision,
            "Mining changes the cached minimap terrain while moving markers stay independent");
        _game.ShipPosition = Main.Center + Vector2.Right * 10000;
        var clipped = map.MapPoint(_game.ShipPosition);
        Check(new Rect2(Vector2.Zero, map.Size).HasPoint(clipped), "A distant ship remains indicated at the survey-map edge");
        await Resize(390, 844);
        map = _game.Hud.Minimap!;
        Check(map.GetGlobalRect().End.X <= _game.View.Size.X && map.Size.X >= 80, "Phone-sized portrait HUD retains a bounded readable minimap");
        _game.ResetProgress(); await Frame(); await Frame(); await Resize(540, 960);
    }
}
