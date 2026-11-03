using Godot;

namespace StarfallMiner;

/// <summary>Local perception and steering rules; no planet-center orbit or global target lookup.</summary>
[GlobalClass]
public partial class AutopilotSettings : Resource
{
    [Export] public float SensorRange { get; set; } = 320;
    [Export] public float ConeHalfAngle { get; set; } = 85;
    [Export] public float MiningDistance { get; set; } = 88;
    [Export] public float MiningTolerance { get; set; } = 16;
    [Export] public float MinimumDistance { get; set; } = 45;
    [Export] public float Acceleration { get; set; } = 230;
    [Export] public float TurnSpeed { get; set; } = 2.6f;
    [Export] public float SearchSpeed { get; set; } = 120;
    [Export] public float ScanInterval { get; set; } = 0.10f;
    [Export] public float ClearanceStallTime { get; set; } = 1.1f;
    [Export] public float ClearanceProgress { get; set; } = 12;
}
