using Godot;

namespace StarfallMiner;

/// <summary>Editor-authored recipes, market rates and progression milestones.</summary>
[GlobalClass]
public partial class CraftingCatalog : Resource
{
    [Export] public Godot.Collections.Array<EquipmentBlueprint> Recipes { get; set; } = [];
    [Export] public string[] MaterialNames { get; set; } = ["Titanium", "Iridium", "Aether core"];
    [Export] public Color[] MaterialColors { get; set; } = [new("a6b8c7"), new("b37aef"), new("ffa847")];
    [Export] public int[] MarketPrices { get; set; } = [80, 1200, 4200];
    [Export] public int[] RankSectors { get; set; } = [1, 2, 4, 7];

    [Export] public long[] RankMiningMilestones { get; set; } = [0, 150, 1200, 5000];

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
