using Godot;

namespace StarfallMiner;

/// <summary>
/// Produces seeded, deterministic ore data. Does not know about scenes, drawing, cameras or HUD.
/// Rare deposits use normalized depth; existing saved grids are restored verbatim.
/// </summary>
public sealed class PlanetGenerator
{
    private readonly RandomNumberGenerator _rng = new();

    /// <returns>Total initial value of the new planet.</returns>
    public double Generate(int sector, PlanetGenerationSettings rules, Dictionary<Vector2I, OreBlock> tiles)
    {
        _rng.Seed = (ulong)sector * 104729 + 777;
        tiles.Clear();
        double totalValue = 0;
        double radius = MiningPlanet.BaseRadius + Math.Min((sector - 1) * rules.RadiusGrowthPerSector, rules.MaximumExtraRadius);
        double toughness = rules.Toughness(sector);
        for (int y = -MiningPlanet.MaxGridExtent; y <= MiningPlanet.MaxGridExtent; y++)
        for (int x = -MiningPlanet.MaxGridExtent; x <= MiningPlanet.MaxGridExtent; x++)
        {
            var key = new Vector2I(x, y);
            double dist = new Vector2(x, y).Length();
            if (dist > radius + _rng.RandfRange(rules.SurfaceNoiseMin, rules.SurfaceNoiseMax)) continue;
            float roll = _rng.Randf();
            int kind = roll < rules.GoldThreshold ? MiningPlanet.Gold :
                roll < rules.CrystalThreshold ? MiningPlanet.Crystal :
                roll < rules.VolatileThreshold && dist > rules.MinimumVolatileDistance ? MiningPlanet.Volatile : MiningPlanet.Stone;
            if (key == Vector2I.Zero) kind = MiningPlanet.Core;
            double hp = (rules.BaseHp + (radius - dist) * rules.HpDepthBonus) * toughness;
            hp *= kind switch
            {
                MiningPlanet.Gold => rules.GoldHpMultiplier,
                MiningPlanet.Crystal => rules.CrystalHpMultiplier,
                MiningPlanet.Volatile => rules.VolatileHpMultiplier,
                MiningPlanet.Core => rules.CoreHpMultiplier,
                _ => 1
            };
            double value = (kind == MiningPlanet.Stone ? rules.StoneValue :
                kind == MiningPlanet.Gold ? rules.GoldValue : rules.OtherOreValue) *
                rules.Richness(sector);
            if (kind == MiningPlanet.Core) value *= rules.CoreValueMultiplier;
            double depth = Math.Max(0, (radius - dist) / radius);
            double deposit = _rng.Randf();
            int material = key == Vector2I.Zero ? 2 :
                depth >= rules.AetherDepth && deposit < rules.AetherChance ? 2 :
                depth >= rules.IridiumDepth && deposit >= rules.AetherChance && deposit < rules.IridiumChance + rules.AetherChance ? 1 :
                depth >= rules.TitaniumDepth && deposit >= rules.IridiumChance + rules.AetherChance && deposit < rules.TitaniumChance + rules.IridiumChance + rules.AetherChance ? 0 : -1;
            if (material >= 0) hp *= material == 0 ? rules.TitaniumHardness : material == 1 ? rules.IridiumHardness : rules.AetherHardness;
            tiles.Add(key, new OreBlock
            {
                Hp = hp, MaxHp = hp, Kind = kind,
                Shade = _rng.RandiRange(0, 2), Value = value, Material = material,
                MaterialAmount = material < 0 ? 0 : material == 0 || key == Vector2I.Zero ? 2 : 1
            });
            totalValue += value;
        }
        return totalValue;
    }
}
