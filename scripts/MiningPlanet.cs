using Godot;

namespace StarfallMiner;

/// <summary>Destructible tile state, collisions and mining events. Visuals are drawn by PlanetRenderer.</summary>
public partial class MiningPlanet : Node2D
{
    public const float Cell = 16;
    public const int MaxGridExtent = 30;
    public const int GridCapacity = (MaxGridExtent * 2 + 1) * (MaxGridExtent * 2 + 1);
    public const double BaseRadius = 27;
    public const int Stone = 0, Gold = 1, Crystal = 2, Volatile = 3, Core = 4;
    public static readonly Vector2I NoTarget = new(999, 999);
    [Export] public PlanetGenerationSettings GenerationSettings { get; set; } = null!;

    /// <summary>Raised whenever the visible tile data changes, including after loading a save.</summary>
    public event Action? TilesChanged;

    public event Action<OreBlock, Vector2>? BlockMined;
    public event Action? PlanetCleared;
    public event Action<Vector2>? BlastTriggered;
    public Dictionary<Vector2I, OreBlock> Blocks { get; } = new(GridCapacity);
    public int Sector { get; private set; } = 1;
    public int TotalBlocks { get; set; }
    public double InitialValue { get; private set; }
    public double MinedValue { get; private set; }
    public bool ClearSent { get; set; }
    private readonly PlanetGenerator _generator = new();
    // Nested volatile cascades mutate Blocks. Reuse one snapshot for area damage.
    private readonly List<Vector2I> _areaKeys = new(GridCapacity);

    public void Generate(int sector)
    {
        Sector = sector;
        InitialValue = _generator.Generate(sector, GenerationSettings, Blocks);
        MinedValue = 0;
        ClearSent = false;
        TotalBlocks = Blocks.Count;
        NotifyTilesChanged();
    }

    // Explicit notification supports save restoration without coupling persistence to the renderer.
    public void NotifyTilesChanged() => TilesChanged?.Invoke();

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

    public bool ContainsPoint(Vector2 point, float padding = 0) => CollisionKey(point, padding) != NoTarget;

    public Vector2I CollisionKey(Vector2 point, float padding = 0)
    {
        var key = PointKey(point);
        for (int y = -1; y <= 1; y++)
        for (int x = -1; x <= 1; x++)
        {
            var neighbor = key + new Vector2I(x, y);
            if (Blocks.ContainsKey(neighbor) && new Rect2(KeyPoint(neighbor) - Vector2.One * (Cell * 0.5f + padding),
                    Vector2.One * (Cell + padding * 2)).HasPoint(point)) return neighbor;
        }
        return NoTarget;
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
            BlockMined?.Invoke(block, p + Position);
            if (block.Kind == Volatile)
            {
                BlastTriggered?.Invoke(p + Position);
                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                    if (x != 0 || y != 0) Damage(key + new Vector2I(x, y), amount * 0.25 + 0.7 * GenerationSettings.Toughness(Sector));
            }
        }
        if (Blocks.Count == 0 && !ClearSent)
        {
            ClearSent = true;
            PlanetCleared?.Invoke();
        }
        NotifyTilesChanged();
    }

    public void DamageAnnulus(Vector2 center, float inner, float outer, float radius, double amount)
    {
        _areaKeys.Clear(); _areaKeys.AddRange(Blocks.Keys);
        foreach (var key in _areaKeys)
        {
            float distance = KeyPoint(key).DistanceTo(center);
            if ((distance > inner || inner == 0 && distance == 0) && distance <= outer)
                Damage(key, amount * Mathf.Lerp(1, 0.15f, distance / radius));
        }
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
}
