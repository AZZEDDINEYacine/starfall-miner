using Godot;

namespace StarfallMiner;

/// <summary>Deterministic background decoration; independent of the simulation tick.</summary>
public partial class Starfield : Node2D
{
    [Export] public VisualPalette Palette { get; set; } = null!;
    public PlanetVisualStyle? Style { get; set; }
    private readonly (Vector2 Point, float Alpha)[] _stars = new (Vector2, float)[38];
    private readonly StyleBoxFlat _frame = new() { BgColor = Colors.Transparent, ShadowSize = 6, DrawCenter = false };

    public override void _Ready()
    {
        _frame.SetBorderWidthAll(1); _frame.SetCornerRadiusAll(24);
        using var rng = new RandomNumberGenerator { Seed = 8086 };
        for (int i = 0; i < _stars.Length; i++)
            _stars[i] = (new Vector2(rng.Randf(), rng.Randf()), rng.RandfRange(0.1f, 0.35f));
        GetViewport().SizeChanged += QueueRedraw;
    }

    public override void _Draw()
    {
        var size = GetViewportRect().Size;
        DrawRect(new Rect2(Vector2.Zero, size), Style != null && Style.Backdrop.A > 0 ? Style.Backdrop : Palette.Background);
        var treatment = Style?.Treatment ?? PlanetVisualStyle.Surface.FlatGrid;
        if (treatment == PlanetVisualStyle.Surface.NeonFrontier)
        {
            var grid = new Color(Style!.EdgeTint, 0.035f);
            for (float x = 0; x < size.X; x += 48) DrawLine(new Vector2(x, 0), new Vector2(x, size.Y), grid, 1);
            for (float y = 0; y < size.Y; y += 48) DrawLine(new Vector2(0, y), new Vector2(size.X, y), grid, 1);
        }
        foreach (var star in _stars)
            DrawRect(new Rect2(star.Point * size, Vector2.One), new Color(Style?.StarTint ?? Palette.Stars, star.Alpha));
        if (treatment == PlanetVisualStyle.Surface.NeonFrontier)
        {
            // Cyan instrument-glass frame echoes the supplied visual reference.
            var frame = new Rect2(7, 7, size.X - 14, size.Y - 14);
            _frame.BorderColor = new Color(Style!.EdgeTint, 0.55f);
            _frame.ShadowColor = new Color(Style.EdgeTint, 0.12f);
            DrawStyleBox(_frame, frame);
        }
    }

    public override void _ExitTree()
    {
        GetViewport().SizeChanged -= QueueRedraw;
        _frame.Dispose();
    }
}
