using Godot;

namespace StarfallMiner;

/// <summary>
/// Draws the procedural planet from its current tile data. No mining, procedural generation,
/// collision rules or rewards belong here. One CanvasItem draws all visible tiles.
/// </summary>
public partial class PlanetRenderer : Node2D
{
    [Export] public VisualPalette Palette { get; set; } = null!;
    [Export] public PlanetVisualStyle Style { get; set; } = null!;
    [Export] public CraftingCatalog Catalog { get; set; } = null!;

    private MiningPlanet _planet = null!;
    private readonly Vector2[] _polygon = new Vector2[6], _triangle = new Vector2[3], _diamond = new Vector2[5], _quad = new Vector2[4];
    private static readonly Vector2I[] Neighbors = [Vector2I.Left, Vector2I.Up, Vector2I.Right, Vector2I.Down];
    public MiningPlanet Planet => _planet;

    public override void _Ready()
    {
        _planet = (MiningPlanet)GetParent();
        _planet.TilesChanged += OnTilesChanged;
        QueueRedraw();
    }

    public override void _ExitTree()
    {
        if (_planet != null) _planet.TilesChanged -= OnTilesChanged;
    }

    private void OnTilesChanged() => QueueRedraw();

    public static Color BlockColor(OreBlock block, VisualPalette palette, float exponent = 0.7f)
    {
        int kind = Math.Clamp(block.Kind, 0, 4);
        double fraction = 1 - Math.Clamp(block.Hp / Math.Max(block.MaxHp, 0.001), 0, 1);
        return palette.OreBase(kind).Lerp(palette.OreDamaged(kind), (float)Math.Pow(fraction, exponent));
    }

    private int EdgeMask(Vector2I key)
    {
        int mask = 0;
        for (int i = 0; i < 4; i++) if (!Planet.Blocks.ContainsKey(key + Neighbors[i])) mask |= 1 << i;
        return mask;
    }

    private void Line(Vector2 from, Vector2 to, Color color, float width = 1)
    {
        if (Style.GlowStrength > 0)
            DrawLine(from, to, new Color(color, Style.GlowStrength), width + 4, true);
        DrawLine(from, to, color, width, true);
    }

    private void Exterior(Vector2 point, int mask, Color color)
    {
        float r = MiningPlanet.Cell * 0.5f;
        if ((mask & 1) != 0) Line(point + new Vector2(-r, -r), point + new Vector2(-r, r), color, 1.1f);
        if ((mask & 2) != 0) Line(point + new Vector2(-r, -r), point + new Vector2(r, -r), color, 1.1f);
        if ((mask & 4) != 0) Line(point + new Vector2(r, -r), point + new Vector2(r, r), color, 1.1f);
        if ((mask & 8) != 0) Line(point + new Vector2(-r, r), point + new Vector2(r, r), color, 1.1f);
    }

    private void Triangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        _triangle[0] = a; _triangle[1] = b; _triangle[2] = c;
        DrawColoredPolygon(_triangle, color);
    }

    private void Mineral(Vector2 p, int kind, Color color, float size = 4)
    {
        switch (kind)
        {
            case MiningPlanet.Gold:
                DrawLine(p + new Vector2(-size, size), p + new Vector2(-1, -1), color, 1.6f, true);
                DrawLine(p + new Vector2(-1, -1), p + new Vector2(2, 1), color, 1.6f, true);
                DrawLine(p + new Vector2(2, 1), p + new Vector2(size, -size), color, 1.6f, true);
                break;
            case MiningPlanet.Crystal:
                _diamond[0] = p + Vector2.Up * size; _diamond[1] = p + Vector2.Right * size * 0.65f;
                _diamond[2] = p + Vector2.Down * size; _diamond[3] = p + Vector2.Left * size * 0.65f;
                _diamond[4] = _diamond[0];
                for (int i = 0; i < 4; i++) _quad[i] = _diamond[i];
                DrawColoredPolygon(_quad, new Color(color, 0.5f));
                DrawPolyline(_diamond, color, 1, true);
                break;
            case MiningPlanet.Volatile:
                DrawCircle(p, 2, color);
                for (int i = 0; i < 3; i++)
                {
                    var axis = Vector2.FromAngle(i * Mathf.Tau / 3 - Mathf.Pi * 0.5f);
                    DrawLine(p + axis * 3, p + axis * (size + 1), color, 1.2f, true);
                }
                break;
            case MiningPlanet.Core:
                DrawArc(p, size + 0.5f, 0, Mathf.Tau, 16, color, 1, true);
                DrawCircle(p, 2, color);
                break;
        }
    }

    public override void _Draw()
    {
        float tileSize = Style.TileSize;
        float r = tileSize * 0.5f;
        var extent = Vector2.One * tileSize;
        var half = Vector2.One * r;
        var visible = (GetGlobalTransformWithCanvas().AffineInverse() * GetViewportRect()).Grow(MiningPlanet.Cell);
        foreach (var (key, block) in Planet.Blocks)
        {
            var p = MiningPlanet.KeyPoint(key);
            if (!visible.HasPoint(p)) continue;
            var color = Style.BlockColor(block, Palette);
            int mask = EdgeMask(key);
            uint hash = unchecked((uint)(key.X * 73856093 ^ key.Y * 19349663));
            var accent = Style.DamagedColor(block.Kind, Palette);
            switch (Style.Treatment)
            {
                case PlanetVisualStyle.Surface.FlatGrid:
                    DrawRect(new Rect2(p - half, extent), color);
                    break;
                case PlanetVisualStyle.Surface.NeonFrontier:
                    float rim = mask != 0 ? 0.75f : 0;
                    if (rim == 0)
                        for (int i = 0; i < 4; i++)
                            if (!Planet.Blocks.ContainsKey(key + Neighbors[i] * 2)) { rim = 0.28f; break; }
                    var neon = color.Darkened((hash % 4) * 0.055f).Lerp(Style.DamagedColor(MiningPlanet.Stone, Palette), rim);
                    DrawRect(new Rect2(p - half, extent), neon);
                    Exterior(p, mask, Style.EdgeTint);
                    break;
                case PlanetVisualStyle.Surface.Contour:
                    DrawRect(new Rect2(p - half, extent), color.Darkened((hash % 3) * 0.035f));
                    if ((int)new Vector2(key.X, key.Y).Length() % 4 == 0)
                        DrawLine(p + new Vector2(-r, 0), p + new Vector2(r, 0), new Color(accent, 0.22f), 0.7f);
                    Exterior(p, mask, Style.EdgeTint);
                    break;
                case PlanetVisualStyle.Surface.Lithography:
                    DrawRect(new Rect2(p - half, extent), color);
                    DrawLine(p + new Vector2(-r, r), p + new Vector2(r, r), new Color(accent, 0.12f), 0.6f);
                    if (hash % 5 == 0) DrawLine(p + new Vector2(-4, -2), p + new Vector2(4, -2), new Color(accent, 0.17f), 0.7f);
                    Exterior(p, mask, Style.EdgeTint);
                    break;
                case PlanetVisualStyle.Surface.Carbon:
                    DrawRect(new Rect2(p - half, extent), color.Darkened((key.X + key.Y & 1) * 0.08f));
                    if (hash % 7 == 0) DrawRect(new Rect2(p - Vector2.One, Vector2.One * 2), new Color(accent, 0.3f));
                    Exterior(p, mask, new Color(Style.EdgeTint, 0.8f));
                    break;
                case PlanetVisualStyle.Surface.Faultline:
                    DrawRect(new Rect2(p - half, extent), color);
                    if ((key.X + key.Y * 2) % 7 == 0)
                        DrawLine(p + new Vector2(-r, -2), p + new Vector2(r, 2), new Color(accent, 0.23f), 0.8f);
                    Exterior(p, mask, Style.EdgeTint);
                    break;
                case PlanetVisualStyle.Surface.Monolith:
                    DrawRect(new Rect2(p - half, extent), color.Darkened((hash % 2) * 0.025f));
                    if (key.X % 4 == 0) DrawLine(p + new Vector2(-r, -r), p + new Vector2(-r, r), new Color(accent, 0.16f), 0.7f);
                    Exterior(p, mask, Style.EdgeTint);
                    break;
                case PlanetVisualStyle.Surface.Spectral:
                    DrawRect(new Rect2(p - half, extent), color.Darkened(Mathf.PosMod(key.Y, 5) * 0.045f));
                    if (mask != 0) DrawRect(new Rect2(p - Vector2.One * 3, Vector2.One * 6), new Color(accent, 0.14f), false, 0.6f);
                    Exterior(p, mask, Style.EdgeTint);
                    break;
                case PlanetVisualStyle.Surface.Circuit:
                    DrawRect(new Rect2(p - half, extent), color);
                    if (hash % 8 == 0)
                    {
                        DrawLine(p + new Vector2(-r, 0), p, new Color(accent, 0.22f), 0.7f);
                        DrawLine(p, p + new Vector2(0, r), new Color(accent, 0.22f), 0.7f);
                        DrawRect(new Rect2(p - Vector2.One, Vector2.One * 2), new Color(accent, 0.4f));
                    }
                    Exterior(p, mask, Style.EdgeTint);
                    break;
                case PlanetVisualStyle.Surface.Radar:
                    DrawRect(new Rect2(p - half, extent), new Color(color, 0.42f));
                    if (mask != 0 || key.X % 4 == 0 && key.Y % 4 == 0)
                    {
                        DrawLine(p - Vector2.Right * 2, p + Vector2.Right * 2, new Color(accent, 0.4f), 0.7f);
                        DrawLine(p - Vector2.Down * 2, p + Vector2.Down * 2, new Color(accent, 0.4f), 0.7f);
                    }
                    Exterior(p, mask, Style.EdgeTint);
                    break;
                case PlanetVisualStyle.Surface.Fractured:
                case PlanetVisualStyle.Surface.Solar:
                    float chip = 2 + hash % 3;
                    _polygon[0] = p + new Vector2(-r + chip, -r + 0.5f);
                    _polygon[1] = p + new Vector2(r - 1, -r + 1);
                    _polygon[2] = p + new Vector2(r, r - chip);
                    _polygon[3] = p + new Vector2(r - chip, r);
                    _polygon[4] = p + new Vector2(-r + 1, r - 0.5f);
                    _polygon[5] = p + new Vector2(-r, -r + chip);
                    DrawColoredPolygon(_polygon, color.Darkened((hash % 3) * 0.035f));
                    Triangle(_polygon[0], _polygon[1], p + new Vector2(1, -1), color.Lightened(0.13f));
                    DrawLine(p + new Vector2(-4, 3), p + new Vector2(3, 1), color.Darkened(0.35f), 1, true);
                    if (Style.Treatment == PlanetVisualStyle.Surface.Solar)
                    {
                        DrawLine(p + new Vector2(-r + 2, 4), p + new Vector2(1, 0), new Color(Style.EdgeTint, 0.52f), 1, true);
                        Exterior(p, mask, new Color(Style.EdgeTint, 0.8f));
                    }
                    if (block.Kind != MiningPlanet.Stone) Mineral(p, block.Kind, accent, 4.5f);
                    break;
                case PlanetVisualStyle.Surface.Prism:
                    _polygon[0] = p + new Vector2(-r + 3, -r); _polygon[1] = p + new Vector2(r, -r + 3);
                    _polygon[2] = p + new Vector2(r - 1, r - 2); _polygon[3] = p + new Vector2(1, r);
                    _polygon[4] = p + new Vector2(-r, r - 3); _polygon[5] = p + new Vector2(-r, -r + 3);
                    DrawColoredPolygon(_polygon, color);
                    Triangle(_polygon[0], _polygon[1], p, color.Lightened(0.28f));
                    Triangle(_polygon[3], _polygon[4], p, color.Darkened(0.32f));
                    DrawLine(_polygon[0], p, new Color(accent, 0.35f), 1, true);
                    if (block.Kind != MiningPlanet.Stone) Mineral(p, block.Kind, accent, 3);
                    break;
                case PlanetVisualStyle.Surface.Survey:
                    DrawRect(new Rect2(p - half, extent), new Color(color, 0.10f));
                    DrawRect(new Rect2(p - half, extent), new Color(color, 0.55f), false, 0.7f);
                    if (block.Kind == MiningPlanet.Stone)
                        DrawLine(p - new Vector2(2, 0), p + new Vector2(2, 0), new Color(accent, 0.35f), 0.7f);
                    else Mineral(p, block.Kind, accent, 4);
                    Exterior(p, mask, Style.EdgeTint);
                    break;
                case PlanetVisualStyle.Surface.Abyssal:
                    DrawCircle(p, r + 1, new Color(accent, Style.GlowStrength * 0.35f));
                    DrawCircle(p, r - 0.5f, color.Darkened(0.14f));
                    DrawCircle(p + new Vector2(-1, -1), r - 2, color);
                    DrawArc(p, r - 1.5f, -2.5f, -0.6f, 10, new Color(accent, block.Kind == 0 ? 0.20f : 0.65f), 1, true);
                    if (block.Kind != MiningPlanet.Stone) DrawCircle(p, 2.5f, accent);
                    break;
                case PlanetVisualStyle.Surface.Relic:
                    DrawRect(new Rect2(p - half, extent), color.Darkened((hash % 3) * 0.055f));
                    DrawLine(p + new Vector2(-r + 1, -r + 1), p + new Vector2(r - 1, -r + 1), color.Lightened(0.2f), 1);
                    DrawLine(p + new Vector2(r - 1, -r + 1), p + new Vector2(r - 1, r - 1), color.Darkened(0.35f), 1);
                    if (block.Kind != MiningPlanet.Stone) Mineral(p, block.Kind, accent, 4);
                    else if (hash % 4 == 0)
                    {
                        DrawLine(p + new Vector2(-3, 3), p + new Vector2(0, -3), color.Darkened(0.3f), 1);
                        DrawLine(p + new Vector2(0, -3), p + new Vector2(3, 3), color.Darkened(0.3f), 1);
                    }
                    break;
                case PlanetVisualStyle.Surface.Chromatic:
                    DrawRect(new Rect2(p - half, extent), color);
                    Triangle(p + new Vector2(-r, r), p + new Vector2(-r, 0), p + new Vector2(0, r), color.Darkened(0.42f));
                    if (block.Kind != MiningPlanet.Stone) Mineral(p, block.Kind, color.Darkened(0.72f), 4);
                    else if (hash % 3 == 0)
                        DrawLine(p + new Vector2(1, -4), p + new Vector2(4, -1), color.Darkened(0.45f), 1.2f);
                    break;
            }
            if (block.Material >= 0) DrawRareDeposit(p, block.Material);
        }
    }

    private void DrawRareDeposit(Vector2 p, int kind)
    {
        var ink = Catalog.MaterialColors[kind];
        // Dark cut-out and unique silhouettes remain identifiable without their colors.
        DrawRect(new Rect2(p - Vector2.One * 5.8f, Vector2.One * 11.6f), new Color("030a10"));
        if (kind == 0) // Titanium: three parallel metal ingots, with notched ends.
        {
            for (int i = -1; i <= 1; i++)
            {
                float x = i * 3.2f;
                DrawLine(p + new Vector2(x, -4), p + new Vector2(x, 4), ink, 1.5f, true);
                DrawLine(p + new Vector2(x - 1, -4), p + new Vector2(x + 1, -4), ink, 0.7f, true);
            }
        }
        else if (kind == 1) // Iridium: offset nested square lattice.
        {
            DrawRect(new Rect2(p - Vector2.One * 4.3f, Vector2.One * 8.6f), ink, false, 1.1f);
            DrawRect(new Rect2(p - Vector2.One * 2, Vector2.One * 4), ink, false, 1.1f);
            DrawLine(p + new Vector2(-4, 4), p + new Vector2(4, -4), ink, 0.8f, true);
        }
        else // Aether: suspended nucleus and split orbital brackets.
        {
            DrawArc(p, 5, -1.2f, 1.2f, 12, ink, 1.3f, true);
            DrawArc(p, 5, Mathf.Pi - 1.2f, Mathf.Pi + 1.2f, 12, ink, 1.3f, true);
            DrawLine(p + Vector2.Up * 3, p + Vector2.Down * 3, ink, 2, true);
            DrawCircle(p, 1.4f, Colors.White);
        }
    }
}
