using Godot;

namespace StarfallMiner;

public sealed class OreBlock
{
    public double Hp, MaxHp, Value;
    public int Kind, Shade;
    public OreBlock Copy() => (OreBlock)MemberwiseClone();
}

public sealed class DebrisFragment
{
    public Vector2 Position, Velocity;
    public float Age, Size;
    public double Value;
    public long Energy;
    public int Kind;
}

public struct Particle
{
    public Vector2 Position, Velocity;
    public Color Color;
    public float Life, MaxLife, Size;
}

public struct Beam
{
    public Vector2[] Points;
    public Color Color;
    public float Life, MaxLife;
}

public struct Ring
{
    public Vector2 Position;
    public Color Color;
    public float Start, Finish, Life, MaxLife;
}

public struct Shell
{
    public Vector2 Position, Target;
    public double Damage;
    public float Speed, Life;
}
