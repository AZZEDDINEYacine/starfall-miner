using Godot;

namespace StarfallMiner;

/// <summary>
/// Ship navigation is deliberately independent of camera and HUD. Every update
/// travels at most MaxSpeed * dt, including collision recovery. No teleporting
/// to a newly computed surface radius when procedural terrain changes.
/// </summary>
public static class AutopilotSteering
{
    public const float MaxSpeed = 155f;
    private const float OrbitLookahead = 0.23f;
    private const float SurfaceClearance = 55f;
    private const float CollisionPadding = 8f;

    public static Vector2 Advance(Vector2 ship, Vector2 center, MiningPlanet planet, float dt)
    {
        if (dt <= 0) return ship;
        Vector2 outward = ship - center;
        if (outward.LengthSquared() < 0.001f) outward = Vector2.Up;
        outward = outward.Normalized();

        var key = planet.Nearest(ship - center);
        if (key == MiningPlanet.NoTarget) return ship;
        var ore = center + MiningPlanet.KeyPoint(key);

        float desiredAngle = outward.Angle() + OrbitLookahead;
        float radius = planet.SurfaceRadius(desiredAngle) + SurfaceClearance;
        Vector2 destination = center + Vector2.FromAngle(desiredAngle) * radius;
        // A sparse outer remnant may never intersect the center-based orbit's
        // lookahead ray. Orbit that ore locally instead of circling empty space.
        // Keep a range buffer so switching back cannot strand us at the firing limit.
        if (ship.DistanceTo(ore) > Main.WeaponRange ||
            destination.DistanceTo(ore) > Main.WeaponRange - SurfaceClearance)
        {
            var fromOre = ship - ore;
            outward = fromOre.LengthSquared() > 0.001f ? fromOre.Normalized() : outward;
            destination = ore + outward.Rotated(OrbitLookahead) * SurfaceClearance;
        }
        float step = MaxSpeed * dt;
        var candidate = ship.MoveToward(destination, step);
        if (!planet.ContainsPoint(candidate - center, CollisionPadding)) return candidate;

        // Prefer climbing away from an obstruction, rather than instantly setting
        // position = center + normal * surface radius (the old teleport bug).
        var escape = ship + outward * step;
        if (!planet.ContainsPoint(escape - center, CollisionPadding) ||
            planet.ContainsPoint(ship - center, CollisionPadding)) return escape;

        var tangent = new Vector2(-outward.Y, outward.X);
        var slide = ship + tangent * step;
        if (!planet.ContainsPoint(slide - center, CollisionPadding)) return slide;
        return ship;
    }
}
