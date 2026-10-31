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
    public Dictionary<Vector2I, OreBlock> Blocks { get; } = new(441);
    public int Sector { get; private set; } = 1;
    public int TotalBlocks { get; set; }
    public double InitialValue { get; private set; }
    public double MinedValue { get; private set; }
    public bool ClearSent { get; set; }
    private readonly RandomNumberGenerator _rng = new();
    // Nested volatile cascades mutate Blocks. Reuse one snapshot for area damage.
    private readonly List<Vector2I> _areaKeys = new(441);

    public void Generate(int sector)
    {
        Sector = sector;
        _rng.Seed = (ulong)sector * 104729 + 777;
        Blocks.Clear();
        InitialValue = MinedValue = 0;
        ClearSent = false;
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
                    if (x != 0 || y != 0) Damage(key + new Vector2I(x, y), amount * 0.8 + 5 * Math.Pow(1.27, Math.Min(Sector - 1, 500)));
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

    public override void _Draw()
    {
        foreach (var (key, block) in Blocks)
            DrawRect(new Rect2(KeyPoint(key) - new Vector2(7, 7), new Vector2(14, 14)), BlockColor(block));
    }
}
