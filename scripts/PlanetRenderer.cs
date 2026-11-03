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
    private Vector2 _surfaceSize, _pixelsPerWorld;
    private static readonly Vector2I[] Neighbors = [Vector2I.Left, Vector2I.Up, Vector2I.Right, Vector2I.Down];
    private static readonly float[] SectorHueShift = [0, 0.12f, 0.24f, 0.58f, -0.11f, 0.06f];
    private static readonly (Vector2 Center, float Radius)[] Basins =
        [(new(-0.32f, -0.24f), 0.26f), (new(0.4f, 0.18f), 0.18f),
         (new(-0.12f, 0.56f), 0.14f), (new(0.25f, -0.57f), 0.11f)];
    public static Color TerrainColor(Color ink, int sector)
    {
        float shift = SectorHueShift[Math.Max(0, sector - 1) % SectorHueShift.Length];
        return shift == 0 ? ink : Color.FromHsv(Mathf.PosMod(ink.H + shift, 1), ink.S, ink.V, ink.A);
    }

    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Linear;
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
            float light = side is 0 or 1 ? 0.9f : 0.28f;
            DrawLine(a, b, new Color(edge, Style.GlowStrength * light), 3, true);
            DrawLine(a, b, new Color(edge, light), 0.9f, true);
        }
    }
    private void DrawRock(Vector2 p, Vector2I key, OreBlock block, uint hash)
    {
        // Relief and fixed upper-left lighting shade surviving stone only. Every mark
        // stays inside its square; excavated gaps have no decorative underlay.
        float relief = Mathf.Sin(key.X * 0.19f + Mathf.Sin(key.Y * 0.13f) * 1.8f) *
            Mathf.Cos(key.Y * 0.17f - key.X * 0.07f);
        var normalized = p / Mathf.Max(16, Planet.MapRadius);
        float basinShade = 0;
        // Four color-only basins, sampled on surviving visible squares; never a
        // sphere, crater scene, or texture filling excavated terrain.
        foreach (var basin in Basins)
        {
            float distance = normalized.DistanceTo(basin.Center) / basin.Radius;
            if (distance >= 1.15f) continue;
            basinShade += Mathf.Clamp(1 - distance, 0, 1) * Style.SurfaceDetail;
            basinShade -= Mathf.Max(0, 1 - Mathf.Abs(distance - 0.96f) * 9) * Style.SurfaceDetail * 0.4f;
        }
        float hemisphere = Mathf.Sqrt(Mathf.Max(0, 1 - normalized.LengthSquared()));
        float sunlight = Mathf.Clamp(-normalized.X * 0.62f - normalized.Y * 0.7f + hemisphere * 0.5f, 0, 1);
        float illumination = Mathf.Clamp(Mathf.Lerp(1, 0.2f + sunlight * 0.9f, Style.DirectionalLight)
            - (relief + 1) * Style.SurfaceDetail * 0.2f - basinShade, 0.16f, 1);
        var color = TerrainColor(Style.BlockColor(block, Palette), Planet.Sector)
            .Darkened(1 - illumination);
        var face = new Rect2(p - Vector2.One * 8, Vector2.One * 16);
        if (Style.SurfaceTexture != null)
        {
            // The same source coordinates meet at adjacent tile edges. Sample only
            // surviving ordinary squares: no texture or sphere behind mined gaps.
            var sourceSize = face.Size * _pixelsPerWorld;
            var sourcePosition = ((face.Position + Vector2.One * Planet.MapRadius) * _pixelsPerWorld)
                .Clamp(Vector2.Zero, _surfaceSize - sourceSize);
            var source = new Rect2(sourcePosition, sourceSize);
            var tint = new Color(color.R * Style.TextureGain, color.G * Style.TextureGain, color.B * Style.TextureGain, 1);
            DrawTextureRectRegion(Style.SurfaceTexture, face, source, tint);
        }
        else DrawRect(face, color);
        if (Style.SurfaceTexture == null && hash % 5 == 0)
        {
            float inset = 2 + hash % 3;
            var a = p + new Vector2(-6, -inset);
            var b = p + new Vector2(1, -inset + 2);
            DrawLine(a, b, new Color(color.Lightened(0.2f), Style.SurfaceDetail * 2), 0.7f, true);
            DrawLine(b, p + new Vector2(6, inset), new Color(color.Darkened(0.4f), Style.SurfaceDetail * 2), 0.7f, true);
        }
        Exterior(p, EdgeMask(key));
    }
    public override void _Draw()
    {
        VisibleSpecials = AnimatedParticles = 0;
        if (Style.SurfaceTexture != null)
        {
            _surfaceSize = Style.SurfaceTexture.GetSize();
            _pixelsPerWorld = _surfaceSize / (Mathf.Max(16, Planet.MapRadius) * 2);
        }
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
            if (Style.Treatment == PlanetVisualStyle.Surface.FlatGrid)
                DrawRect(new Rect2(p - Vector2.One * Style.TileSize * 0.5f, Vector2.One * Style.TileSize), TerrainColor(Style.BlockColor(block, Palette), Planet.Sector));
            else
                DrawRock(p, key, block, hash);
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
