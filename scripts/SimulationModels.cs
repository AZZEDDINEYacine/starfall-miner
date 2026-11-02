using Godot;

namespace StarfallMiner;

public sealed class OreBlock
{
    public double Hp, MaxHp, Value;
    public int Kind, Shade;
    public int Material = -1;
    public long MaterialAmount;
    public OreBlock Copy() => (OreBlock)MemberwiseClone();
}

public sealed class DebrisFragment
{
    public Vector2 Position, Velocity;
    public float Age, Size;
    public double Value;
    public long Energy;
    public int Kind;
    public int Material = -1;
    public long MaterialAmount;
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

public struct ContinuousBeam
{
    public bool Active, Hit;
    public Vector2 Origin, End;
}

public struct Ring
{
    public Vector2 Position;
    public Color Color;
    public float Start, Finish, Life, MaxLife, Strength;
}

public struct Shell
{
    public Vector2 Position, Target;
    public double Damage;
    public float Speed, Life;
}

public sealed class NovaWave
{
    public Vector2 Position;
    public float Age, Radius = 104, Duration = 1.6f;
    public double Damage;
}
