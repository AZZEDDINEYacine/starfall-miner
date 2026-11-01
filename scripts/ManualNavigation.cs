using Godot;

namespace StarfallMiner;

/// <summary>Advance toward the currently held steering point, clearing the hull's corridor.</summary>
public sealed class ManualNavigation
{
    public const float HullPadding = 9;
    public bool Active { get; private set; }
    public Vector2 Destination { get; private set; }
    public Vector2 AimDirection { get; private set; } = Vector2.Right;
    public Vector2I Obstacle { get; private set; } = MiningPlanet.NoTarget;
    public bool ClearingPath => Active && Obstacle != MiningPlanet.NoTarget;

    public void SetDestination(Vector2 destination)
    {
        Destination = destination;
        Active = true;
        Obstacle = MiningPlanet.NoTarget;
    }

    public void Stop()
    {
        Active = false;
        Obstacle = MiningPlanet.NoTarget;
    }

    public Vector2 Advance(Vector2 position, MiningPlanet planet, ManualFlightSettings settings, float delta)
    {
        Obstacle = MiningPlanet.NoTarget;
        if (!Active) return position;
        var offset = Destination - position;
        float distance = offset.Length();
        if (distance <= 1) { Stop(); return position; }
        var direction = offset / distance;
        AimDirection = direction;
        // Scan the hull's width: grazing tiles must be mined as well.
        float lookAhead = Mathf.Min(distance, Main.WeaponRange - MiningPlanet.Cell);
        float speed = settings.CruiseSpeed;
        float safeTravel = distance;
        for (float along = 0; along <= lookAhead; along += 2)
        {
            var key = planet.CollisionKey(position + direction * along - Main.Center, HullPadding);
            if (key == MiningPlanet.NoTarget) continue;
            Obstacle = key;
            AimDirection = (Main.Center + MiningPlanet.KeyPoint(key) - position).Normalized();
            // The scan already includes hull padding. Leave additional breathing room
            // and brake smoothly instead of driving the hull up against solid terrain.
            safeTravel = Mathf.Max(0, along - 2 - settings.MiningStandOff);
            speed = Mathf.Min(speed, safeTravel * settings.ApproachResponse);
            break;
        }
        // Swept steps prevent crossing a tile even during low-frame-rate updates.
        float travel = Mathf.Min(speed * delta, Mathf.Min(distance, safeTravel));
        for (float moved = 0; moved < travel;)
        {
            float step = Mathf.Min(2, travel - moved);
            var next = position + direction * step;
            if (planet.ContainsPoint(next - Main.Center, HullPadding)) break;
            position = next;
            moved += step;
        }
        if (Obstacle != MiningPlanet.NoTarget)
            AimDirection = (Main.Center + MiningPlanet.KeyPoint(Obstacle) - position).Normalized();
        if (position.DistanceTo(Destination) <= 1) Stop();
        return position;
    }
}
