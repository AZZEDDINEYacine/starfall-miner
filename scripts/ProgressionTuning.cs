using Godot;

namespace StarfallMiner;

/// <summary>Fast first purchases, substantial weapon gains and steep later investment costs.</summary>
[GlobalClass]
public partial class ProgressionTuning : Resource
{
    [Export] public double[] EarlyPowerCosts { get; set; } = [25, 55, 110];
    [Export] public double[] EarlyRateCosts { get; set; } = [20, 45, 95];
    [Export] public double[] EarlyDroneCosts { get; set; } = [60, 140, 300];
    [Export] public double LaterPowerBase { get; set; } = 200;
    [Export] public double LaterRateBase { get; set; } = 160;
    [Export] public double LaterDroneBase { get; set; } = 500;
    [Export] public double LaterGrowth { get; set; } = 1.4;
    [Export] public double LaterPower { get; set; } = 1.15;
    [Export] public double SecondHardpointCost { get; set; } = 600;
    [Export] public double ThirdHardpointCost { get; set; } = 6000;
    [ExportGroup("Weapon output")]
    [Export] public double PowerGainPerLevel { get; set; } = 0.6;
    // Lance, cannon, ion, Nova wave, Bore, continuous Flux beam (damage/second).
    [Export] public double[] WeaponBaseDamage { get; set; } = [1.1, 3.2, 3.2, 5.8, 4.4, 6.8];

    public double Cost(int level, int type, double[] early, double later)
    {
        if (early == null || early.Length == 0 || level < 0 || type < 0) return double.PositiveInfinity;
        double value = level < early.Length ? early[Math.Max(0, level)] :
            later * Math.Pow(LaterGrowth, level - early.Length) * Math.Pow(level - early.Length + 1, LaterPower);
        double cost = Math.Floor(value * (1 + type * 0.75));
        return double.IsFinite(cost) && cost >= 0 ? cost : double.PositiveInfinity;
    }
}
