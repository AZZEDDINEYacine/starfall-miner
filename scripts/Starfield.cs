using Godot;

namespace StarfallMiner;

/// <summary>Static galaxy decoration; redraws only on resize or appearance changes.</summary>
public partial class Starfield : Node2D
{
    [Export] public VisualPalette Palette { get; set; } = null!;
    [Export] public Texture2D GalaxyCloud { get; set; } = null!;
    public PlanetVisualStyle? Style { get; set; }
    private readonly (Vector2 Point, float Alpha, float Radius)[] _stars = new (Vector2, float, float)[156];
    private readonly (Vector2 Point, Vector2 Scale, float Alpha)[] _clouds = new (Vector2, Vector2, float)[18];

    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Linear;
        using var rng = new RandomNumberGenerator { Seed = 8086 };
        for (int i = 0; i < _stars.Length; i++)
            _stars[i] = (new Vector2(rng.Randf(), rng.Randf()), rng.RandfRange(0.18f, 0.75f), i % 11 == 0 ? 1.25f : 0.65f);
        for (int i = 0; i < _clouds.Length; i++)
        {
            float along = i / (float)(_clouds.Length - 1);
            _clouds[i] = (new Vector2(along, 0.74f - along * 0.49f + rng.RandfRange(-0.12f, 0.12f)),
                new Vector2(rng.RandfRange(0.32f, 0.62f), rng.RandfRange(0.12f, 0.27f)), rng.RandfRange(0.08f, 0.18f));
        }
        SetProcess(false);
        GetViewport().SizeChanged += QueueRedraw;
    }

    public override void _Draw()
    {
        var size = GetViewportRect().Size;
        DrawRect(new Rect2(Vector2.Zero, size), Style != null && Style.Backdrop.A > 0 ? Style.Backdrop : Palette.Background);
        if (Palette.NebulaArtwork != null)
        {
            var extent = Palette.NebulaArtwork.GetSize();
            float scale = Mathf.Max(size.X / extent.X, size.Y / extent.Y);
            extent *= scale;
            DrawTextureRect(Palette.NebulaArtwork, new Rect2((size - extent) * 0.5f, extent), false,
                new Color(1, 1, 1, Palette.NebulaOpacity));
        }
        else for (int i = 0; i < _clouds.Length; i++)
        {
            var cloud = _clouds[i];
            var extent = new Vector2(size.X * cloud.Scale.X, size.Y * cloud.Scale.Y);
            DrawSetTransform(cloud.Point * size, -0.32f);
            DrawTextureRect(GalaxyCloud, new Rect2(-extent * 0.5f, extent), false,
                new Color(i % 3 == 0 ? Palette.NebulaViolet : Palette.NebulaBlue, cloud.Alpha));
        }
        DrawSetTransform(Vector2.Zero);
        if (Palette.DistantPlanetArtwork != null)
        {
            float width = Mathf.Clamp(size.X * 0.8f, 430, 960);
            var extent = Palette.DistantPlanetArtwork.GetSize();
            extent *= width / extent.X;
            // This cropped distant body lives primarily behind the command area.
            // It has no world coordinates, mining target, collision or camera motion.
            var position = new Vector2(size.X - extent.X * 0.62f, size.Y - Mathf.Min(300, extent.Y * 0.75f));
            DrawTextureRect(Palette.DistantPlanetArtwork, new Rect2(position, extent), false,
                new Color(0.8f, 0.9f, 1, Palette.DistantPlanetOpacity));
            DrawSetTransform(position + extent * 0.5f, -0.42f, new Vector2(1, 0.34f));
            for (int orbit = 0; orbit < 3; orbit++)
                DrawArc(Vector2.Zero, width * (0.43f + orbit * 0.045f), 0.2f, 5.7f, 80,
                    new Color(orbit == 1 ? Palette.Lilac : Palette.Mint, 0.12f - orbit * 0.025f), 1, true);
            DrawSetTransform(Vector2.Zero);
        }
        for (int i = 0; i < _stars.Length; i++)
        {
            var star = _stars[i];
            var p = star.Point * size;
            var ink = i % 7 == 0 ? Palette.Lilac : Style?.StarTint ?? Palette.Stars;
            DrawCircle(p, star.Radius, new Color(ink, star.Alpha), true, -1, true);
            if (i % 37 != 0) continue;
            DrawCircle(p, 4, new Color(ink, 0.035f), true, -1, true);
            DrawLine(p - new Vector2(3, 0), p + new Vector2(3, 0), new Color(ink, 0.22f), 1, true);
            DrawLine(p - new Vector2(0, 3), p + new Vector2(0, 3), new Color(ink, 0.22f), 1, true);
        }
    }

    public override void _ExitTree()
    {
        GetViewport().SizeChanged -= QueueRedraw;
    }
}
