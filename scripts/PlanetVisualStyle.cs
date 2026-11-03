using Godot;

namespace StarfallMiner;

/// <summary>Inspector-editable presentation parameters, independent of ore generation or damage.</summary>
[GlobalClass]
public partial class PlanetVisualStyle : Resource
{
    public enum Surface { FlatGrid, NeonFrontier }
    [Export] public string Id { get; set; } = "classic";
    [Export] public string Title { get; set; } = "CLASSIC GRID";
    [Export] public string Description { get; set; } = "Original flat pixels";
    [Export] public Surface Treatment { get; set; }
    [Export] public Color[] OreColors { get; set; } = [];
    [Export] public Color[] DamagedColors { get; set; } = [];
    [Export] public Color Backdrop { get; set; } = new(0, 0, 0, 0);
    [Export] public Color StarTint { get; set; } = Colors.White;
    [Export] public Color EdgeTint { get; set; } = Colors.White;
    [Export(PropertyHint.Range, "0,0.5,0.01")] public float GlowStrength { get; set; } = 0.08f;
    [Export(PropertyHint.Range, "1,16,0.5")]
    public float TileSize { get; set; } = 14f;

    [Export(PropertyHint.Range, "0.1,3,0.05")]
    public float DamageBrightnessExponent { get; set; } = 0.7f;
    [ExportGroup("Rock surface (presentation only)")]
    [Export] public Texture2D? SurfaceTexture { get; set; }
    [Export(PropertyHint.Range, "1,4,0.05")] public float TextureGain { get; set; } = 2.6f;
    [Export(PropertyHint.Range, "0,0.3,0.01")] public float SurfaceDetail { get; set; } = 0.1f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float DirectionalLight { get; set; } = 0.22f;

    public Color OreColor(int kind, VisualPalette fallback) => OreColors.Length == 5 ? OreColors[Math.Clamp(kind, 0, 4)] : fallback.OreBase(kind);
    public Color DamagedColor(int kind, VisualPalette fallback) => DamagedColors.Length == 5 ? DamagedColors[Math.Clamp(kind, 0, 4)] : fallback.OreDamaged(kind);
    public Color BlockColor(OreBlock block, VisualPalette fallback)
    {
        float damage = (float)(1 - Math.Clamp(block.Hp / Math.Max(block.MaxHp, 0.001), 0, 1));
        return OreColor(block.Kind, fallback).Lerp(DamagedColor(block.Kind, fallback), Mathf.Pow(damage, DamageBrightnessExponent));
    }
}
