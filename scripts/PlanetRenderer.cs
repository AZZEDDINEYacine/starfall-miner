using Godot;

namespace StarfallMiner;

/// <summary>One visible-cell renderer; resource cells own their entire square, not a rock inset.</summary>
public partial class PlanetRenderer : Node2D
{
    [Export] public VisualPalette Palette { get; set; } = null!;
    [Export] public PlanetVisualStyle Style { get; set; } = null!;
    [Export] public CraftingCatalog Catalog { get; set; } = null!;
    public Main? Game { get; set; }
    public MiningPlanet Planet => _planet;
    public int VisibleSpecials { get; private set; }
    public int AnimatedParticles { get; private set; }
    public const int ParticleBudget = 128;
    public float AnimationTime { get; private set; }
    public bool AnimationFrozen { get; set; }
    private MiningPlanet _planet = null!;
    private float _redrawTime;
    private static readonly Vector2I[] Neighbors = [Vector2I.Left, Vector2I.Up, Vector2I.Right, Vector2I.Down];
    private static readonly float[] SectorHueShift = [0, 0.12f, 0.24f, 0.58f, -0.11f, 0.06f];
    public static Color TerrainColor(Color ink, int sector)
    {
        float shift = SectorHueShift[Math.Max(0, sector - 1) % SectorHueShift.Length];
        return shift == 0 ? ink : Color.FromHsv(Mathf.PosMod(ink.H + shift, 1), ink.S, ink.V, ink.A);
    }

    public override void _Ready()
    {
        _planet = (MiningPlanet)GetParent();
        _planet.TilesChanged += OnTilesChanged;
        QueueRedraw();
    }
    public override void _ExitTree() { if (_planet != null) _planet.TilesChanged -= OnTilesChanged; }
    private void OnTilesChanged() => QueueRedraw();
    public void SeekAnimation(float seconds) { AnimationTime = Mathf.PosMod(seconds, 4096); QueueRedraw(); }
    public override void _Process(double delta)
    {
        if (AnimationFrozen || Game?.Paused == true) return;
        AnimationTime = Mathf.PosMod(AnimationTime + (float)delta, 4096);
        _redrawTime += (float)delta;
        if (_redrawTime >= 1f / 30) { _redrawTime = 0; if (VisibleSpecials > 0) QueueRedraw(); }
    }

    public static Color BlockColor(OreBlock block, VisualPalette palette, float exponent = 0.7f)
    {
        float damage = (float)(1 - Math.Clamp(block.Hp / Math.Max(block.MaxHp, 0.001), 0, 1));
        return palette.OreBase(block.Kind).Lerp(palette.OreDamaged(block.Kind), Mathf.Pow(damage, exponent));
    }
    public static bool HasReactor(int design) => design is 1 or 2 or 4 or 5 or 6;
    public static Vector2 ParticlePoint(int design, uint seed, int index, float time)
    {
        float phase = seed % 1021 * 0.019f + index * Mathf.Pi;
        float angle = phase + time * (1.25f + seed % 5 * 0.08f);
        float radius = 3.8f + index * 0.6f;
        return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.78f);
    }
    private int EdgeMask(Vector2I key)
    {
        int mask = 0;
        for (int i = 0; i < 4; i++) if (!Planet.Blocks.ContainsKey(key + Neighbors[i])) mask |= 1 << i;
        return mask;
    }
    private void Exterior(Vector2 p, int mask)
    {
        for (int side = 0; side < 4; side++)
        {
            if ((mask & 1 << side) == 0) continue;
            var a = p + (side switch { 0 => new Vector2(-8, -8), 1 => new Vector2(-8, -8), 2 => new Vector2(8, -8), _ => new Vector2(-8, 8) });
            var b = p + (side switch { 0 => new Vector2(-8, 8), 1 => new Vector2(8, -8), 2 => new Vector2(8, 8), _ => new Vector2(8, 8) });
            var edge = TerrainColor(Style.EdgeTint, Planet.Sector);
            DrawLine(a, b, new Color(edge, Style.GlowStrength), 4, true);
            DrawLine(a, b, edge, 1.1f, true);
        }
    }
    public override void _Draw()
    {
        VisibleSpecials = AnimatedParticles = 0;
        var transform = GetGlobalTransformWithCanvas();
        var visible = (transform.AffineInverse() * GetViewportRect()).Grow(MiningPlanet.Cell);
        int left = Mathf.FloorToInt(visible.Position.X / 16), right = Mathf.CeilToInt(visible.End.X / 16);
        int top = Mathf.FloorToInt(visible.Position.Y / 16), bottom = Mathf.CeilToInt(visible.End.Y / 16);
        for (int y = top; y <= bottom; y++)
        for (int x = left; x <= right; x++)
        {
            var key = new Vector2I(x, y);
            if (!Planet.Blocks.TryGetValue(key, out var block)) continue;
            var p = MiningPlanet.KeyPoint(key);
            uint hash = unchecked((uint)(x * 73856093 ^ y * 19349663));
            if (block.Material >= 0 || block.Kind != MiningPlanet.Stone)
            {
                VisibleSpecials++;
                var screen = transform * p;
                DrawSpecial(p, block, hash, screen.Y > 230 && screen.Y < GetViewportRect().Size.Y - 234);
                continue;
            }
            var color = TerrainColor(Style.BlockColor(block, Palette), Planet.Sector);
            if (Style.Treatment == PlanetVisualStyle.Surface.FlatGrid)
                DrawRect(new Rect2(p - Vector2.One * Style.TileSize * 0.5f, Vector2.One * Style.TileSize), color);
            else
            {
                int mask = EdgeMask(key);
                float rim = mask != 0 ? 0.75f : 0;
                if (rim == 0)
                    for (int i = 0; i < 4; i++)
                        if (!Planet.Blocks.ContainsKey(key + Neighbors[i] * 2)) { rim = 0.28f; break; }
                var neon = color.Darkened(hash % 4 * 0.055f).Lerp(TerrainColor(Style.DamagedColor(MiningPlanet.Stone, Palette), Planet.Sector), rim);
                DrawRect(new Rect2(p - Vector2.One * 8, Vector2.One * 16), neon);
                Exterior(p, mask);
            }
        }
    }
    private void DrawSpecial(Vector2 p, OreBlock block, uint hash, bool inFlightArea)
    {
        int design = block.Material >= 0 ? block.Material : block.Kind switch { MiningPlanet.Gold => 3, MiningPlanet.Crystal => 4, MiningPlanet.Core => 5, _ => 6 };
        var ink = block.Material >= 0 ? Catalog.MaterialColors[block.Material] : design == 6 ? new Color("ff7161") : Palette.OreDamaged(block.Kind);
        float damage = (float)(1 - Math.Clamp(block.Hp / Math.Max(block.MaxHp, 0.001), 0, 1));
        var face = ink.Darkened(0.64f).Lerp(ink.Darkened(0.22f), damage);
        // This opaque material face replaces the entire grid square; no stone underlay/badge.
        DrawRect(new Rect2(p - Vector2.One * 7.6f, Vector2.One * 15.2f), face);
        DrawRect(new Rect2(p - Vector2.One * 7.1f, Vector2.One * 14.2f), new Color(ink, 0.72f + damage * 0.28f), false, 0.8f);
        if (!HasReactor(design) || !inFlightArea || AnimatedParticles >= ParticleBudget) return;
        // A shared tiny fusion chamber: a hot core and flowing plasma, never ore glyphs.
        float warmth = 0.85f + 0.15f * Mathf.Sin(AnimationTime * 2 + hash % 17);
        DrawCircle(p, 3.2f, new Color(ink, 0.10f * warmth));
        DrawCircle(p, 1.8f, new Color(ink, 0.32f * warmth));
        DrawCircle(p, 0.9f, ink.Lightened(0.55f));
        for (int dot = 0; dot < 2 && AnimatedParticles < ParticleBudget; dot++)
        {
            var particle = p + ParticlePoint(design, hash, dot, AnimationTime);
            float angle = (particle - p).Angle();
            DrawArc(p, (particle - p).Length(), angle - 0.8f, angle, 8, new Color(ink, 0.42f), 0.7f, true);
            DrawCircle(particle, 0.8f, ink.Lightened(0.35f));
            AnimatedParticles++;
        }
    }
}
