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
    [Export] public Color Text { get; set; } = new("edf3ff");
    [Export] public Color Disabled { get; set; } = new("69758e");
    [Export] public Color Focus { get; set; } = new("c5b4ff");
    [Export] public Texture2D? ControlSurface { get; set; }
    [Export] public Texture2D? PanelSurface { get; set; }
    [Export(PropertyHint.Range, "0,0.3,0.01")] public float HaloStrength { get; set; } = 0.09f;
}
