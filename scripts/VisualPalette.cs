using Godot;

namespace StarfallMiner;

/// <summary>Godot-inspector-editable color palette. Visual values live in res://resources/starfall_palette.tres.</summary>
[GlobalClass]
public partial class VisualPalette : Resource
{
    public static VisualPalette Default => ResourceLoader.Load<VisualPalette>("res://resources/starfall_palette.tres");

    [ExportGroup("World & Ship")]
    [Export] public Color Background { get; set; }
    [Export] public Color Stars { get; set; }
    [Export] public Color NebulaBlue { get; set; } = new("315caa");
    [Export] public Color NebulaViolet { get; set; } = new("7550a9");
    [Export] public Texture2D? NebulaArtwork { get; set; }
    [Export] public Texture2D? DistantPlanetArtwork { get; set; }
    [Export(PropertyHint.Range, "0,1,0.01")] public float NebulaOpacity { get; set; } = 0.72f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float DistantPlanetOpacity { get; set; } = 0.46f;
    [ExportGroup("HUD")]
    [Export] public Color Ink { get; set; }
    [Export] public Color MutedInk { get; set; }
    [Export] public Color Mint { get; set; }
    [Export] public Color Gold { get; set; }
    [Export] public Color Lilac { get; set; }
    [Export] public Color PanelColor { get; set; }
    [Export] public Color Edge { get; set; }
    [ExportGroup("Button States")]
    [Export] public Color ButtonHover { get; set; }
    [Export] public Color ButtonPressed { get; set; }
    [Export] public Color ModeBackground { get; set; }
    [Export] public Color WeaponSelectedBackground { get; set; }
    [Export] public Color ResetWarning { get; set; }
    [ExportGroup("Overlays & Sticks")]
    [Export] public Color ModalScrim { get; set; }
    [Export] public Color StickOuter { get; set; }
    [Export] public Color StickInner { get; set; }
    [ExportGroup("Weapons")]
    [Export] public Color WeaponLance { get; set; }
    [Export] public Color WeaponNova { get; set; }
    [Export] public Color WeaponIon { get; set; }
    [ExportGroup("Ore (Intact)")]
    [Export] public Color StoneBase { get; set; }
    [Export] public Color GoldBase { get; set; }
    [Export] public Color CrystalBase { get; set; }
    [Export] public Color VolatileBase { get; set; }
    [Export] public Color CoreBase { get; set; }
    [ExportGroup("Ore (Damaged)")]
    [Export] public Color StoneDamaged { get; set; }
    [Export] public Color GoldDamaged { get; set; }
    [Export] public Color CrystalDamaged { get; set; }
    [Export] public Color VolatileDamaged { get; set; }
    [Export] public Color CoreDamaged { get; set; }
    [ExportGroup("Debris")]
    [Export] public Color DebrisStone { get; set; }
    [Export] public Color DebrisGold { get; set; }
    [Export] public Color DebrisCrystal { get; set; }
    [Export] public Color DebrisVolatile { get; set; }
    [Export] public Color DebrisCore { get; set; }
    [ExportGroup("Ships & Effects")]
    [Export] public Color ShipFill { get; set; }
    [Export] public Color ShipOutline { get; set; }
    [Export] public Color Projectile { get; set; }
    [Export] public Color EngineExhaust { get; set; }
    [Export] public Color ChainBlast { get; set; }
    [Export] public Color ChainSparks { get; set; }

    public Color Weapon(int index) => index switch
    {
        1 => WeaponNova,
        2 => WeaponIon,
        3 => new Color("65bfff"),
        4 => new Color("b7d9f2"),
        5 => new Color("7eeeff"),
        _ => WeaponLance
    };

    public Color OreBase(int kind) => kind switch
    {
        MiningPlanet.Gold => GoldBase,
        MiningPlanet.Crystal => CrystalBase,
        MiningPlanet.Volatile => VolatileBase,
        MiningPlanet.Core => CoreBase,
        _ => StoneBase
    };

    public Color OreDamaged(int kind) => kind switch
    {
        MiningPlanet.Gold => GoldDamaged,
        MiningPlanet.Crystal => CrystalDamaged,
        MiningPlanet.Volatile => VolatileDamaged,
        MiningPlanet.Core => CoreDamaged,
        _ => StoneDamaged
    };

    public Color Debris(int kind) => kind switch
    {
        MiningPlanet.Gold => DebrisGold,
        MiningPlanet.Crystal => DebrisCrystal,
        MiningPlanet.Volatile => DebrisVolatile,
        MiningPlanet.Core => DebrisCore,
        _ => DebrisStone
    };
}
