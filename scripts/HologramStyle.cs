using Godot;

namespace StarfallMiner;

/// <summary>Shared presentation only: holographic ink and inexpensive vector halo strength.</summary>
[GlobalClass]
public partial class HologramStyle : Resource
{
    [Export] public Color Cyan { get; set; } = new("67e8f3");
    [Export] public Color Dim { get; set; } = new("52818f");
    [Export] public Color Amber { get; set; } = new("edbd78");
    [Export] public Color Shadow { get; set; } = new("051019");
    [Export(PropertyHint.Range, "0,0.3,0.01")] public float HaloStrength { get; set; } = 0.09f;
}
