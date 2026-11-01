using Godot;

namespace StarfallMiner;

/// <summary>Gameplay tuning for held steering and safe mining approaches.</summary>
[GlobalClass]
public partial class ManualFlightSettings : Resource
{
    [Export(PropertyHint.Range, "60,350,5")] public float CruiseSpeed { get; set; } = 190;
    // Additional clearance beyond the collision hull, measured in world units.
    [Export(PropertyHint.Range, "16,100,1")] public float MiningStandOff { get; set; } = 40;
    [Export(PropertyHint.Range, "1,8,0.25")] public float ApproachResponse { get; set; } = 3;
    [Export(PropertyHint.Range, "8,48,1")] public float PointerDeadZone { get; set; } = 24;
}
