using Godot;

namespace StarfallMiner;

[GlobalClass]
public partial class EquipmentBlueprint : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string Title { get; set; } = "";
    [Export] public string Description { get; set; } = "";
    [Export] public bool Drone { get; set; }
    [Export] public int Type { get; set; }
    [Export] public int Rank { get; set; } = 1;
    [Export] public double Credits { get; set; }
    [Export] public int Titanium { get; set; }
    [Export] public int Iridium { get; set; }
    [Export] public int Aether { get; set; }
    [Export] public float Power { get; set; } = 1;
    [Export] public float FireRate { get; set; } = 1;
    [Export] public float Range { get; set; } = 190;
}
