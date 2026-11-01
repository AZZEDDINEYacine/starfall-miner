using Godot;

namespace StarfallMiner;

/// <summary>Editor-authored recipes, market rates and progression milestones.</summary>
[GlobalClass]
public partial class CraftingCatalog : Resource
{
    [Export] public Godot.Collections.Array<EquipmentBlueprint> Recipes { get; set; } = [];
    [Export] public string[] MaterialNames { get; set; } = ["Titanium", "Iridium", "Aether core"];
    [Export] public Color[] MaterialColors { get; set; } = [new("c2e3eb"), new("df9cff"), new("ffe0a0")];
    [Export] public int[] MarketPrices { get; set; } = [480, 1800, 6400];
    [Export] public int[] RankSectors { get; set; } = [1, 2, 4, 7];

    public EquipmentBlueprint Weapon(int type)
    {
        foreach (var recipe in Recipes) if (!recipe.Drone && recipe.Type == type) return recipe;
        throw new ArgumentOutOfRangeException(nameof(type));
    }
    public EquipmentBlueprint Chassis(int type)
    {
        foreach (var recipe in Recipes) if (recipe.Drone && recipe.Type == type) return recipe;
        throw new ArgumentOutOfRangeException(nameof(type));
    }
}
