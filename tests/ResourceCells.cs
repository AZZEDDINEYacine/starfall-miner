using Godot;
using StarfallMiner;

public partial class Smoke
{
    private async Task TestResourceCells()
    {
        _game.ResetProgress(); await Frame(); await Frame(); _game.Sound.Enabled = false;
        var renderer = _game.Planet.GetNode<PlanetRenderer>("Renderer");
        Check(_game.VisualStyles.Count == 2 && _game.VisualStyles[0].Id == "neon_frontier" && _game.VisualStyles[1].Id == "classic" &&
            _game.Planet.Blocks.Values.Any(MiningPlanet.IsExplosive) && !_game.Planet.Blocks.Values.Any(b => b.Material >= 0 && b.Kind == MiningPlanet.Volatile),
            "Only Default and Original remain, and generated impact charges never hide in rare mineral cells");
        bool moving = true, contained = true;
        Check(!PlanetRenderer.HasReactor(0) && !PlanetRenderer.HasReactor(3) && PlanetRenderer.HasReactor(2),
            "Titanium and gold stay simple solid-color cells; only selected energy cells use the shared reactor effect");
        foreach (int design in new[] { 1, 2, 4, 5, 6 })
        for (int dot = 0; dot < 2; dot++)
        {
            var start = PlanetRenderer.ParticlePoint(design, 19373, dot, 0);
            moving &= start.DistanceTo(PlanetRenderer.ParticlePoint(design, 19373, dot, 0.4f)) > 0.1f;
            for (int frame = 0; frame < 120; frame++)
                contained &= new Rect2(-6, -6, 12, 12).HasPoint(PlanetRenderer.ParticlePoint(design, 19373, dot, frame / 30f));
        }
        Check(moving && contained, "Material glints move inside every special square without spilling into neighboring grid cells");
        var snapshot = Snapshot(_game.Planet); double credits = _game.Credits; ulong revision = _game.Planet.TopologyRevision;
        renderer.SeekAnimation(1); renderer.AnimationFrozen = true; renderer._Process(0.5);
        Check(renderer.AnimationTime == 1, "QA can freeze ore particle frames without advancing the world");
        renderer.AnimationFrozen = false; _game.Paused = true; renderer._Process(0.5);
        Check(renderer.AnimationTime == 1, "Paused design comparison freezes the moving ore particles");
        _game.Paused = false; renderer._Process(0.5);
        Check(Near(renderer.AnimationTime, 1.5) && SameTiles(snapshot, _game.Planet.Blocks) && _game.Credits == credits && _game.Planet.TopologyRevision == revision,
            "Ore animation changes presentation only, preserving tile health, rewards and topology");
        _game.Planet.Blocks.Clear(); _game.Planet.ClearSent = false;
        for (int y = -10; y <= 10; y++)
        for (int x = -10; x <= 10; x++) _game.Planet.Blocks[new Vector2I(x, y)] = new OreBlock { Hp = 1000, MaxHp = 1000, Material = 2, MaterialAmount = 1 };
        _game.ShipPosition = Main.Center; _game.SetAuto(false); await Frame(); await Frame();
        _game.View.PlaceWorld(true); _game.Planet.NotifyTilesChanged(); await Frame(); await Frame();
        if (DisplayServer.GetName() != "headless")
            Check(renderer.VisibleSpecials > 64 && renderer.AnimatedParticles > 0 && renderer.AnimatedParticles <= PlanetRenderer.ParticleBudget,
                "A dense rendered ore field respects the 128-particle budget with no per-cell scene nodes");

        _game.ResetProgress(); await Frame(); await Frame();
        _game.Sound.Enabled = false;
        _game.Planet.Blocks.Clear(); _game.Planet.ClearSent = false;
        _game.Planet.Blocks[Vector2I.Zero] = new OreBlock { Kind = MiningPlanet.Volatile, Hp = 1000, MaxHp = 1000, Value = 5 };
        _game.Planet.Blocks[Vector2I.Right] = new OreBlock { Hp = 100, MaxHp = 100, Value = 2 };
        _game.Planet.Blocks[new Vector2I(3, 0)] = new OreBlock { Hp = 100, MaxHp = 100, Value = 2 };
        _game.Planet.Blocks[Vector2I.Down] = new OreBlock { Hp = 3.2, MaxHp = 3.2, Value = 2 };
        _game.Planet.Damage(Vector2I.Zero, 0); _game.Planet.Damage(Vector2I.Zero, double.NaN);
        Check(_game.Planet.Blocks[Vector2I.Zero].Hp == 1000, "Zero/invalid damage does not accidentally ignite a charge");
        long mined = _game.TotalMined;
        _game.Planet.Damage(Vector2I.Zero, 0.01);
        Check(!_game.Planet.Blocks.ContainsKey(Vector2I.Zero) && _game.TotalMined == mined + 2 &&
            Near(_game.Planet.Blocks[Vector2I.Right].Hp, 100 - _game.Planet.GenerationSettings.ExplosiveDamage * 0.725) &&
            _game.Planet.Blocks[new Vector2I(3, 0)].Hp == 100,
            "The smallest real hit detonates a high-health charge, breaks shallow ordinary rock and preserves blocks beyond its radius");
        Check(!_game.Planet.Blocks.ContainsKey(Vector2I.Down) && _game.Rings.Any(r => r.Strength > 0.5f && r.Finish == _game.Planet.GenerationSettings.ExplosiveRadius),
            "Impact charges produce a bright readable shockwave matching their actual two-cell damage radius");
        int pickups = _game.Debris.Count; _game.Planet.Damage(Vector2I.Zero, 100000);
        Check(_game.Debris.Count == pickups && _game.TotalMined == mined + 2, "Repeated impacts on a detonated cell cannot create extra rewards");
        _game.Planet.Blocks[new Vector2I(4, 0)] = new OreBlock { Kind = MiningPlanet.Volatile, Material = 0, MaterialAmount = 2, Hp = 100, MaxHp = 100, Value = 2 };
        _game.Planet.Damage(new Vector2I(4, 0), 0.01);
        Check(Near(_game.Planet.Blocks[new Vector2I(4, 0)].Hp, 99.99),
            "A legacy rare-bearing volatile tile remains a resistant mineral cell rather than a disguised impact charge");

        _game.Planet.Blocks.Clear(); _game.Planet.ClearSent = false; _game.Debris.Clear(); _game.Rings.Clear();
        for (int y = 0; y < 32; y++)
        for (int x = 0; x < 32; x++) _game.Planet.Blocks[new Vector2I(x, y)] = new OreBlock { Kind = MiningPlanet.Volatile, Hp = 1000, MaxHp = 1000, Value = 2 };
        _game.Planet.Blocks[new Vector2I(40, 40)] = new OreBlock { Hp = 1000, MaxHp = 1000, Value = 2 };
        mined = _game.TotalMined;
        _game.Planet.Damage(Vector2I.Zero, 0.01);
        Check(_game.Planet.Blocks.Count == 1 && _game.TotalMined == mined + 1024 && _game.Debris.Sum(d => d.Value) == 2048 && _game.Rings.Count <= 64,
            "A 1,024-charge chain drains iteratively, rewards every charge once and bounds blast effects");
        _game.Planet.Blocks.Clear(); _game.Planet.ClearSent = false; _game.Debris.Clear();
        _game.Planet.Blocks[Vector2I.Zero] = new OreBlock { Kind = MiningPlanet.Volatile, Hp = 1000, MaxHp = 1000, Value = 3 };
        _game.Planet.Blocks[Vector2I.Right] = new OreBlock { Hp = 1, MaxHp = 1, Material = 0, MaterialAmount = 2, Value = 7 };
        _game.Planet.Blocks[new Vector2I(8, 8)] = new OreBlock { Hp = 100, MaxHp = 100, Value = 2 };
        _game.Planet.Damage(Vector2I.Zero, 0.01);
        Check(_game.Equipment.Materials[0] == 0 && _game.Debris.Count(d => d.Material == 0 && d.MaterialAmount == 2) == 1,
            "A blast-mined mineral remains one pending material reward until collection");
        _game.Persistence.Save(_game);
        var oldStyle = SavedJson(); oldStyle["visual_style"] = "prism"; WriteJson(oldStyle);
        var remaining = Snapshot(_game.Planet); credits = _game.Credits;
        Check(_game.Persistence.Load(_game) && _game.CurrentVisualStyle.Id == "neon_frontier" && SameTiles(remaining, _game.Planet.Blocks) && _game.Credits == credits,
            "A save selecting a removed preset falls back to Default without altering terrain or credits");
        for (int i = 0; i < 60; i++) _game.UpdateDebris(0.05f);
        _game.Persistence.Save(_game); _game.Persistence.Load(_game);
        for (int i = 0; i < 60; i++) _game.UpdateDebris(0.05f);
        Check(_game.Equipment.Materials[0] == 2 && _game.Credits == credits + 10,
            "Explosion pickup persistence preserves exact credits and rare materials without duplication");
        _game.Planet.Blocks.Clear(); _game.Planet.ClearSent = false;
        _game.Planet.Blocks[Vector2I.Zero] = new OreBlock { Kind = MiningPlanet.Volatile, Hp = 1000, MaxHp = 1000, Value = 1 };
        _game.Planet.Blocks[Vector2I.Right] = new OreBlock { Kind = MiningPlanet.Volatile, Hp = 1000, MaxHp = 1000, Value = 1 };
        int clearEvents = 0; void Cleared() => clearEvents++;
        _game.Planet.PlanetCleared += Cleared;
        _game.Planet.Damage(Vector2I.Zero, 0.01); _game.Planet.Damage(Vector2I.Zero, 0.01);
        _game.Planet.PlanetCleared -= Cleared;
        Check(clearEvents == 1 && _game.WarpTimer > 0, "A chain that clears the planet schedules progression exactly once");
        _game.ResetProgress(); await Frame(); await Frame();
    }
}
