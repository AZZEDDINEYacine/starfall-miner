using Godot;

namespace StarfallMiner;

/// <summary>Inspector-editable rules for procedural generation; not a rendering resource.</summary>
[GlobalClass]
public partial class PlanetGenerationSettings : Resource
{
    [ExportGroup("Shape")]
    [Export] public double RadiusGrowthPerSector { get; set; } = 0.12;
    [Export] public double MaximumExtraRadius { get; set; } = 0.8;
    [Export] public float SurfaceNoiseMin { get; set; } = -0.5f;
    [Export] public float SurfaceNoiseMax { get; set; } = 0.55f;

    [ExportGroup("Ore Distribution")]
    // Cumulative probabilities. A roll below GoldThreshold is gold, etc.
    [Export(PropertyHint.Range, "0,1,0.001")]
    public double GoldThreshold { get; set; } = 0.115;
    [Export(PropertyHint.Range, "0,1,0.001")]
    public double CrystalThreshold { get; set; } = 0.185;
    [Export(PropertyHint.Range, "0,1,0.001")]
    public double VolatileThreshold { get; set; } = 0.235;
    [Export] public double MinimumVolatileDistance { get; set; } = 2.5;

    [ExportGroup("Legacy exponential tuning (unused)")]
    [Export] public double ToughnessGrowth { get; set; } = 1.27;
    [Export] public double RewardGrowth { get; set; } = 1.19;
    [ExportGroup("Block Health & Credits")]
    [Export] public double BaseHp { get; set; } = 4.6;
    [Export] public double HpDepthBonus { get; set; } = 0.48;
    [Export] public double GoldHpMultiplier { get; set; } = 1.3;
    [Export] public double CrystalHpMultiplier { get; set; } = 1.15;
    [Export] public double VolatileHpMultiplier { get; set; } = 0.65;
    [Export] public double CoreHpMultiplier { get; set; } = 10;
    [Export] public double StoneValue { get; set; } = 3;
    [Export] public double GoldValue { get; set; } = 15;
    [Export] public double OtherOreValue { get; set; } = 6;
    [Export] public double CoreValueMultiplier { get; set; } = 20;
    [ExportGroup("Deep deposits & campaign")]
    [Export] public double SectorHealthStep { get; set; } = 0.38;
    [Export] public double SectorValueStep { get; set; } = 0.07;
    [Export] public double TitaniumDepth { get; set; } = 0.35;
    [Export] public double IridiumDepth { get; set; } = 0.60;
    [Export] public double AetherDepth { get; set; } = 0.82;
    [Export] public double TitaniumChance { get; set; } = 0.11;
    [Export] public double IridiumChance { get; set; } = 0.08;
    [Export] public double AetherChance { get; set; } = 0.05;
    [Export] public double SectorHealthPower { get; set; } = 1.45;
    [Export] public double TitaniumHardness { get; set; } = 2.8;
    [Export] public double IridiumHardness { get; set; } = 4.5;
    [Export] public double AetherHardness { get; set; } = 7;
    public double Toughness(int sector)
    {
        double n = Math.Max(0, sector - 1);
        return 1 + n * SectorHealthStep + 0.18 * Math.Pow(n, SectorHealthPower);
    }
    public double Richness(int sector) => 1 + Math.Max(0, sector - 1) * SectorValueStep;
}
