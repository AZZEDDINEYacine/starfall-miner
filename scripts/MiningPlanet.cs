using Godot;

namespace StarfallMiner;

/// <summary>Seeded destructible grid, with no physics object or node per tile.</summary>
public partial class MiningPlanet : Node2D
{
    public const float Cell = 16;
    public const int Stone = 0, Gold = 1, Crystal = 2, Volatile = 3, Core = 4;
    public static readonly Vector2I NoTarget = new(999, 999);
    private static readonly Color[] BaseColors = [new("355866"), new("8a6836"), new("386c96"), new("8b4154"), new("76539f")];
    private static readonly Color[] HealthColors = [new("c3f1ed"), new("ffe7a3"), new("bcf3ff"), new("ffbfad"), new("f0d9ff")];

    public event Action<double, long, Vector2, int>? BlockMined;
    public event Action? PlanetCleared;
    public event Action<Vector2>? BlastTriggered;
    public Dictionary<Vector2I, OreBlock> Blocks { get; } = new(1500);
    public int Sector { get; private set; } = 1;
    public SectorProfile Profile { get; private set; } = SectorDesign.ForSector(1);
    public int TotalBlocks { get; set; }
    public double InitialValue { get; private set; }
    public double MinedValue { get; private set; }
    public bool ClearSent { get; set; }
    private readonly RandomNumberGenerator _rng = new();
    // Nested volatile cascades mutate Blocks. Reuse one snapshot for area damage.
    private readonly List<Vector2I> _areaKeys = new(1500);

    public void Generate(int sector)
    {
        Sector = sector;
        Profile = SectorDesign.ForSector(sector);
        _rng.Seed = (ulong)sector * 104729 + 777;
        Blocks.Clear();
        InitialValue = MinedValue = 0;
        ClearSent = false;
        var profile = Profile;
        double radius = profile.Radius;
        // The larger mining surface already lengthens a sector. Keep per-block
        // toughness below the old exponential growth so the campaign stays playable.
        double toughness = Math.Pow(1.16, Math.Min(sector - 1, 500));
        double valueScale = Math.Pow(1.19, Math.Min(sector - 1, 500)) * (profile.Rare ? 1.25 : 1);
        for (int y = -23; y <= 23; y++)
        for (int x = -23; x <= 23; x++)
        {
            var key = new Vector2I(x, y);
            double dist = Math.Sqrt(x * x + y * y);
            double angle = Math.Atan2(y, x);
            if (!Inside(profile.Layout, x, y, dist, angle, radius + _rng.RandfRange(-0.45f, 0.45f))) continue;
            float roll = _rng.Randf();
            int kind = roll < 0.115 ? Gold : roll < 0.185 ? Crystal : roll < 0.235 && dist > 2.5 ? Volatile : Stone;
            double resistance = 1;
            PlaceVeins(profile.Layout, x, y, dist, angle, roll, ref kind, ref resistance);
            if (key == Vector2I.Zero) { kind = Core; resistance = 1; }
            double hp = Math.Max(1, 2.7 + (radius - dist) * 0.08) * toughness * resistance;
            hp *= kind switch { Gold => 1.3, Crystal => 1.15, Volatile => 0.65, Core => 10, _ => 1 };
            double value = (kind == Stone ? 3 : kind == Gold ? 15 : 6) * valueScale;
            if (kind == Core) value *= 20;
            Blocks.Add(key, new OreBlock { Hp = hp, MaxHp = hp, Kind = kind, Shade = _rng.RandiRange(0, 2), Value = value });
            InitialValue += value;
        }
        TotalBlocks = Blocks.Count;
        QueueRedraw();
    }

    private static bool Circle(int x, int y, float cx, float cy, float radius)
    {
        float dx = x - cx, dy = y - cy;
        return dx * dx + dy * dy <= radius * radius;
    }

    private static bool Inside(PlanetLayout layout, int x, int y, double dist, double angle, double radius)
    {
        double a = x * 0.92 + y * 0.38, b = -x * 0.38 + y * 0.92;
        double ring = Math.Sqrt(a * a + b * b * 2.5);
        switch (layout)
        {
            case PlanetLayout.Craters:
                return dist < radius && !Circle(x, y, -10, -8, 4.1f) && !Circle(x, y, 9, -8, 3.5f) && !Circle(x, y, 6, 10, 3.8f);
            case PlanetLayout.Fissures:
                return dist < radius && !(dist > 6 && Math.Abs(Math.Sin(angle * 1.5)) < 0.065);
            case PlanetLayout.Canyon:
                return dist < radius && !(dist > 5.5 && x < 14 && Math.Abs(y - x * 0.48 - 2) < 1.4);
            case PlanetLayout.Islands:
                return Circle(x, y, -10, -8, 6.8f) || Circle(x, y, 9, -8, 7.1f) ||
                       Circle(x, y, -9, 9, 6.2f) || Circle(x, y, 9, 10, 6.1f) || Circle(x, y, 0, 0, 5.2f);
            case PlanetLayout.Rings:
                return dist < 12.4 || ring > 16.7 && ring < 20.4;
            case PlanetLayout.Diagonal:
                return Math.Sqrt(a * a * 0.86 + b * b * 1.16) < radius;
            case PlanetLayout.IceVault:
                return dist < radius && !(dist > 8 && (Math.Abs(x) < 1 || Math.Abs(y) < 1));
            case PlanetLayout.Obsidian:
                return dist < radius * (1 + 0.10 * Math.Cos(angle * 5)) && !(dist > 8 && Math.Abs(Math.Sin(angle * 4)) < 0.055);
            case PlanetLayout.Aurelia:
                return dist < 14.7 || dist > 16.1 && dist < 20.4 && Math.Abs((x + y * 2 + 120) % 5 - 2.5) < 1.85;
            case PlanetLayout.Foundry:
                return dist < 9.5 || Math.Abs(x) < 5 && Math.Abs(y) < 20 || Math.Abs(y) < 5 && Math.Abs(x) < 20 ||
                       dist > 14 && dist < 18 && Math.Abs(Math.Sin(angle * 2)) > 0.73;
            default:
                return dist < radius;
        }
    }

    private static void PlaceVeins(PlanetLayout layout, int x, int y, double dist, double angle, float roll, ref int kind, ref double resistance)
    {
        switch (layout)
        {
            case PlanetLayout.Garden:
                if (dist > 4 && Math.Abs(y - x * 0.6) < 1.2) kind = Gold;
                break;
            case PlanetLayout.Craters:
                if (dist > 5 && Math.Abs(y + 5) < 1.4) kind = Gold;
                break;
            case PlanetLayout.Fissures:
                resistance = dist > 10 ? 1.55 : 1;
                if (dist > 6 && Math.Abs(Math.Sin(angle * 1.5)) < 0.19) { kind = Gold; resistance = 0.75; }
                break;
            case PlanetLayout.Vault:
                resistance = dist > 10 ? 1.5 : 1;
                if ((Circle(x, y, -8, 2, 3) || Circle(x, y, 7, -6, 3) || Circle(x, y, 5, 8, 3)) && roll < 0.65) kind = Volatile;
                break;
            case PlanetLayout.Canyon:
                if (dist > 5 && Math.Abs(y - x * 0.48 - 2) < 3.2) kind = roll < 0.18 ? Volatile : Gold;
                break;
            case PlanetLayout.Islands:
                if (dist > 7 && roll < 0.34) kind = Gold;
                if (dist > 7 && roll > 0.95) kind = Volatile;
                break;
            case PlanetLayout.Bands:
                int band = (y + 24) / 4 % 3;
                resistance = band == 0 ? 1.55 : 0.85;
                if (band == 1 && roll < 0.48) kind = Crystal;
                if (band == 2 && roll < 0.38) kind = Gold;
                break;
            case PlanetLayout.Rings:
                if (dist > 12.5) { resistance = 0.75; kind = roll < 0.48 ? Gold : roll < 0.72 ? Crystal : Stone; }
                break;
            case PlanetLayout.Diagonal:
                resistance = 1.18;
                if (Math.Abs((x + y * 0.65 + 60) % 6 - 3) < 0.8) { kind = Crystal; resistance = 0.75; }
                break;
            case PlanetLayout.IceVault:
                resistance = dist > 11 ? 1.55 : 1;
                if ((Math.Abs(x) < 2 || Math.Abs(y) < 2) && dist > 4) { kind = Crystal; resistance = 0.72; }
                if (dist < 9 && dist > 3 && roll < 0.28) kind = Gold;
                break;
            case PlanetLayout.Obsidian:
                resistance = 1.55;
                if (dist > 8 && Math.Abs(Math.Sin(angle * 4)) < 0.17) { kind = Volatile; resistance = 0.72; }
                if (dist < 9 && roll < 0.35) kind = Crystal;
                break;
            case PlanetLayout.Aurelia:
                resistance = dist > 16 ? 0.8 : 1.5;
                if (dist > 16) kind = roll < 0.70 ? Gold : Crystal;
                if (dist > 8 && dist < 13 && roll > 0.89) kind = Volatile;
                break;
            case PlanetLayout.Foundry:
                resistance = 1.6;
                if (dist > 5 && (Math.Abs(x) < 2 || Math.Abs(y) < 2)) { kind = Crystal; resistance = 0.76; }
                if (dist > 9 && dist < 15 && roll < 0.45) kind = Volatile;
                if (dist > 14 && roll < 0.55) kind = Gold;
                break;
        }
    }

    // Kept for the frozen v0.2 generation fixture. Production always uses Generate.
    public void GenerateLegacyForFixture(int sector)
    {
        Sector = sector;
        Profile = SectorDesign.ForSector(sector);
        _rng.Seed = (ulong)sector * 104729 + 777;
        Blocks.Clear(); InitialValue = MinedValue = 0; ClearSent = false;
        double radius = 8.9 + Math.Min((sector - 1) * 0.12, 0.8);
        double toughness = Math.Pow(1.27, Math.Min(sector - 1, 500));
        for (int y = -10; y <= 10; y++)
        for (int x = -10; x <= 10; x++)
        {
            var key = new Vector2I(x, y);
            double dist = new Vector2(x, y).Length();
            if (dist > radius + _rng.RandfRange(-0.5f, 0.55f)) continue;
            float roll = _rng.Randf();
            int kind = roll < 0.115 ? Gold : roll < 0.185 ? Crystal : roll < 0.235 && dist > 2.5 ? Volatile : Stone;
            if (key == Vector2I.Zero) kind = Core;
            double hp = (2.7 + (radius - dist) * 0.12) * toughness;
            hp *= kind switch { Gold => 1.3, Crystal => 1.15, Volatile => 0.65, Core => 10, _ => 1 };
            double value = (kind == Stone ? 3 : kind == Gold ? 15 : 6) * Math.Pow(1.19, Math.Min(sector - 1, 500));
            if (kind == Core) value *= 20;
            Blocks.Add(key, new OreBlock { Hp = hp, MaxHp = hp, Kind = kind, Shade = _rng.RandiRange(0, 2), Value = value });
            InitialValue += value;
        }
        TotalBlocks = Blocks.Count;
        QueueRedraw();
    }

    public float Progress => 1f - (float)Blocks.Count / Math.Max(TotalBlocks, 1);
    public static Vector2 KeyPoint(Vector2I key) => new Vector2(key.X, key.Y) * Cell;
    public static Vector2I PointKey(Vector2 point) => new(Mathf.RoundToInt(point.X / Cell), Mathf.RoundToInt(point.Y / Cell));

    public Vector2I Nearest(Vector2 point, List<Vector2I>? exclude = null)
    {
        var best = NoTarget;
        float distance = float.PositiveInfinity;
        foreach (var key in Blocks.Keys)
        {
            if (exclude?.Contains(key) == true) continue;
            float next = point.DistanceSquaredTo(KeyPoint(key));
            if (next >= distance) continue;
            best = key;
            distance = next;
        }
        return best;
    }

    public bool ContainsPoint(Vector2 point, float padding = 0)
    {
        var key = PointKey(point);
        for (int y = -1; y <= 1; y++)
        for (int x = -1; x <= 1; x++)
        {
            var neighbor = key + new Vector2I(x, y);
            if (Blocks.ContainsKey(neighbor) && new Rect2(KeyPoint(neighbor) - Vector2.One * (Cell * 0.5f + padding),
                    Vector2.One * (Cell + padding * 2)).HasPoint(point)) return true;
        }
        return false;
    }

    public float SurfaceRadius(float angle)
    {
        var axis = Vector2.FromAngle(angle);
        float furthest = 16;
        foreach (var key in Blocks.Keys)
        {
            var p = KeyPoint(key);
            if (Mathf.Abs(axis.Cross(p)) < 29) furthest = Mathf.Max(furthest, axis.Dot(p) + Cell * 0.5f);
        }
        return furthest;
    }

    public void Damage(Vector2I key, double amount)
    {
        if (!Blocks.TryGetValue(key, out var block)) return;
        block.Hp -= amount;
        if (block.Hp <= 0)
        {
            Blocks.Remove(key); // Remove before recursion so adjacent bombs terminate.
            MinedValue += block.Value;
            var p = KeyPoint(key);
            BlockMined?.Invoke(block.Value, block.Kind == Crystal ? 1 : block.Kind == Core ? 5 : 0, p + Position, block.Kind);
            if (block.Kind == Volatile)
            {
                BlastTriggered?.Invoke(p + Position);
                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                        if (x != 0 || y != 0) Damage(key + new Vector2I(x, y), amount * 0.8 + 5 * Math.Pow(1.16, Math.Min(Sector - 1, 500)));
            }
        }
        if (Blocks.Count == 0 && !ClearSent)
        {
            ClearSent = true;
            PlanetCleared?.Invoke();
        }
        QueueRedraw();
    }

    public void DamageRadius(Vector2 center, float radius, double amount)
    {
        _areaKeys.Clear();
        _areaKeys.AddRange(Blocks.Keys);
        foreach (var key in _areaKeys)
        {
            float distance = KeyPoint(key).DistanceTo(center);
            if (distance <= radius) Damage(key, amount * Mathf.Lerp(1, 0.55f, distance / radius));
        }
    }

    public static Color BlockColor(OreBlock block)
    {
        int kind = Math.Clamp(block.Kind, 0, 4);
        double fraction = 1 - Math.Clamp(block.Hp / Math.Max(block.MaxHp, 0.001), 0, 1);
        return BaseColors[kind].Lerp(HealthColors[kind], (float)Math.Pow(fraction, 0.7));
    }

    public Color ThemedBlockColor(OreBlock block)
    {
        if (block.Kind != Stone) return BlockColor(block);
        double damaged = 1 - Math.Clamp(block.Hp / Math.Max(block.MaxHp, 0.001), 0, 1);
        return Profile.StoneColor.Lerp(HealthColors[Stone], (float)Math.Pow(damaged, 0.7));
    }

    public override void _Draw()
    {
        foreach (var (key, block) in Blocks)
            DrawRect(new Rect2(KeyPoint(key) - new Vector2(7, 7), new Vector2(14, 14)), ThemedBlockColor(block));
    }
}
