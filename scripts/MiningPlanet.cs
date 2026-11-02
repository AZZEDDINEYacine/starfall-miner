using Godot;

namespace StarfallMiner;

/// <summary>Destructible tile state, collisions and mining events. Visuals are drawn by PlanetRenderer.</summary>
public partial class MiningPlanet : Node2D
{
    public const float Cell = 16;
    public const int MaxGridExtent = 256;
    public const int GridCapacity = 12769;
    public const double BaseRadius = 54;
    public const int Stone = 0, Gold = 1, Crystal = 2, Volatile = 3, Core = 4;
    public static readonly Vector2I NoTarget = new(999, 999);
    [Export] public PlanetGenerationSettings GenerationSettings { get; set; } = null!;

    /// <summary>Raised whenever the visible tile data changes, including after loading a save.</summary>
    public event Action? TilesChanged;

    public event Action<OreBlock, Vector2>? BlockMined;
    public event Action? PlanetCleared;
    public event Action<Vector2>? BlastTriggered;
    public Dictionary<Vector2I, OreBlock> Blocks { get; } = new(GridCapacity);
    public ulong TopologyRevision { get; private set; }
    public float MapRadius { get; private set; } = (float)BaseRadius * Cell;
    public int Sector { get; private set; } = 1;
    public int TotalBlocks { get; set; }
    public double InitialValue { get; private set; }
    public double MinedValue { get; private set; }
    public bool ClearSent { get; set; }
    private readonly PlanetGenerator _generator = new();
    // Nested volatile cascades mutate Blocks. Reuse one snapshot for area damage.
    private readonly List<Vector2I> _areaKeys = new(GridCapacity);
    private readonly Queue<Vector2I> _blastCenters = new(32);
    private bool _resolvingDamage;
    public static bool IsExplosive(OreBlock block) => block.Kind == Volatile && block.Material < 0;

    public void Generate(int sector)
    {
        Sector = sector;
        MapRadius = (float)GenerationSettings.RadiusForSector(sector) * Cell;
        InitialValue = _generator.Generate(sector, GenerationSettings, Blocks);
        MinedValue = 0;
        ClearSent = false;
        TotalBlocks = Blocks.Count;
        NotifyTilesChanged();
    }

    // Explicit notification supports save restoration without coupling persistence to the renderer.
    public void NotifyTilesChanged(bool topologyChanged = true)
    {
        if (topologyChanged) TopologyRevision++;
        TilesChanged?.Invoke();
    }

    public void RestoreMapExtent()
    {
        float cells = Mathf.Sqrt(TotalBlocks / Mathf.Pi);
        foreach (var key in Blocks.Keys) cells = Mathf.Max(cells, key.Length() + 1);
        MapRadius = Mathf.Max(16, cells * Cell);
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
        if (!double.IsFinite(amount) || amount <= 0 || !Blocks.ContainsKey(key)) return;
        if (_resolvingDamage) { DamageOne(key, amount); return; }
        _resolvingDamage = true;
        try
        {
            DamageOne(key, amount);
            while (_blastCenters.TryDequeue(out var origin))
            {
                BlastTriggered?.Invoke(KeyPoint(origin) + Position);
                double blast = GenerationSettings.ExplosiveDamage * GenerationSettings.Toughness(Sector);
                float radius = GenerationSettings.ExplosiveRadius;
                int reach = Mathf.CeilToInt(radius / Cell);
                for (int y = -reach; y <= reach; y++)
                for (int x = -reach; x <= reach; x++)
                {
                    float distance = new Vector2(x, y).Length() * Cell;
                    if (distance > 0 && distance <= radius)
                        DamageOne(origin + new Vector2I(x, y), blast * Mathf.Lerp(1, 0.45f, distance / radius));
                }
            }
        }
        finally { _resolvingDamage = false; }
        if (Blocks.Count == 0 && !ClearSent)
        {
            ClearSent = true;
            PlanetCleared?.Invoke();
        }
        NotifyTilesChanged();
    }

    private void DamageOne(Vector2I key, double amount)
    {
        if (!Blocks.TryGetValue(key, out var block)) return;
        bool explosive = IsExplosive(block);
        block.Hp = explosive ? 0 : block.Hp - amount;
        if (block.Hp > 0) return;
        // Remove before publishing rewards/queuing neighbors: each block pays and detonates once.
        Blocks.Remove(key);
        MinedValue += block.Value;
        if (explosive) _blastCenters.Enqueue(key);
        BlockMined?.Invoke(block, KeyPoint(key) + Position);
    }

    private void GatherArea(Vector2 center, float radius)
    {
        _areaKeys.Clear();
        int left = Mathf.FloorToInt((center.X - radius) / Cell), right = Mathf.CeilToInt((center.X + radius) / Cell);
        int top = Mathf.FloorToInt((center.Y - radius) / Cell), bottom = Mathf.CeilToInt((center.Y + radius) / Cell);
        for (int y = top; y <= bottom; y++)
        for (int x = left; x <= right; x++)
        {
            var key = new Vector2I(x, y);
            if (Blocks.ContainsKey(key)) _areaKeys.Add(key);
        }
    }

    public void DamageAnnulus(Vector2 center, float inner, float outer, float radius, double amount)
    {
        GatherArea(center, outer);
        foreach (var key in _areaKeys)
        {
            float distance = KeyPoint(key).DistanceTo(center);
            if ((distance > inner || inner == 0 && distance == 0) && distance <= outer)
                Damage(key, amount * Mathf.Lerp(1, 0.15f, distance / radius));
        }
    }

    public void DamageRadius(Vector2 center, float radius, double amount)
    {
        GatherArea(center, radius);
        foreach (var key in _areaKeys)
        {
            float distance = KeyPoint(key).DistanceTo(center);
            if (distance <= radius) Damage(key, amount * Mathf.Lerp(1, 0.55f, distance / radius));
        }
    }
}
