using Godot;

namespace StarfallMiner;

/// <summary>
/// Reactive pilot: first-hit forward rays, near-field separation, damped approach/retreat,
/// aim-and-mine, and an odometry-only search when contact is lost. Never reads planet
/// center/radius, hidden ore metadata or the minimap to select a destination.
/// </summary>
public sealed class AutopilotSteering
{
    public enum Behavior { Search, Approach, Brake, Mine, Avoid, Recover, Widen }
    public const float MaxSpeed = 155;
    public const float HullPadding = 9;
    public AutopilotSettings Settings { get; set; } = null!;
    public Behavior State { get; private set; }
    public Vector2 Velocity { get; private set; }
    public float Heading { get; private set; }
    public Vector2I Target { get; private set; } = MiningPlanet.NoTarget;
    public bool CanFire { get; private set; }
    public int SensorHits { get; private set; }
    public int RecoveryCount { get; private set; }
    private bool _initialized;
    private float _scanTimer, _time, _searchDistance, _searchHeading, _blockedTime, _avoidTime;
    private int _searchLeg, _avoidSide = 1;
    private float _searchSweep;
    private float _clearanceTime;
    private Vector2 _clearanceAnchor;
    private static readonly Vector2[] EscapeAxes = [Vector2.Right, Vector2.Down, Vector2.Left, Vector2.Up,
        new(0.7071068f, 0.7071068f), new(-0.7071068f, 0.7071068f), new(-0.7071068f, -0.7071068f), new(0.7071068f, -0.7071068f)];

    public void Reset(float heading)
    {
        Heading = _searchHeading = heading;
        Velocity = Vector2.Zero; Target = MiningPlanet.NoTarget; CanFire = false;
        _scanTimer = _time = _searchDistance = _blockedTime = _avoidTime = 0;
        _searchLeg = SensorHits = RecoveryCount = 0; _avoidSide = 1; _initialized = true;
        State = Behavior.Search; _searchSweep = Mathf.Tau;
        _clearanceTime = 0;
    }

    public bool Sees(Vector2 position, Vector2 point)
    {
        var offset = point - position;
        return offset.Length() <= Settings.SensorRange + MiningPlanet.Cell &&
            Mathf.Abs(Mathf.AngleDifference(Heading, offset.Angle())) <= Mathf.DegToRad(Settings.ConeHalfAngle) + 0.05f;
    }

    private static Vector2I FirstHit(Vector2 from, Vector2 toward, MiningPlanet planet)
    {
        var offset = toward - from;
        float distance = offset.Length();
        var direction = offset / Mathf.Max(0.001f, distance);
        for (float step = 0; step <= distance; step += 4)
        {
            var key = MiningPlanet.PointKey(from + direction * step);
            if (planet.Blocks.ContainsKey(key)) return key;
        }
        return MiningPlanet.NoTarget;
    }

    private void Sense(Vector2 position, MiningPlanet planet)
    {
        float best = float.PositiveInfinity;
        var selected = MiningPlanet.NoTarget;
        SensorHits = 0;
        // 61 deterministic first-hit rays. Solid foreground blocks occlude deeper blocks.
        for (int ray = 0; ray < 61; ray++)
        {
            float angle = (ray - 30) * Mathf.DegToRad(Settings.ConeHalfAngle) / 30;
            var axis = Vector2.FromAngle(Heading + angle);
            for (float distance = 0; distance <= Settings.SensorRange; distance += 4)
            {
                var key = MiningPlanet.PointKey(position + axis * distance);
                if (!planet.Blocks.ContainsKey(key)) continue;
                // Aim at a visible center, not a sliver whose center is behind another tile.
                var face = FirstHit(position, MiningPlanet.KeyPoint(key), planet);
                if (face != MiningPlanet.NoTarget) key = face;
                SensorHits++;
                float score = position.DistanceTo(MiningPlanet.KeyPoint(key)) + Mathf.Abs(angle) * 42 - (key == Target ? 32 : 0);
                if (score < best) { best = score; selected = key; }
                break;
            }
        }
        Target = selected;
    }

    private Vector2 Separation(Vector2 local, MiningPlanet planet)
    {
        var origin = MiningPlanet.PointKey(local);
        var force = Vector2.Zero;
        for (int y = -4; y <= 4; y++)
        for (int x = -4; x <= 4; x++)
        {
            var key = origin + new Vector2I(x, y);
            if (!planet.Blocks.ContainsKey(key)) continue;
            var away = local - MiningPlanet.KeyPoint(key);
            float distance = away.Length();
            if (distance < 0.01f || distance >= Settings.MinimumDistance) continue;
            float strength = 1 - distance / Settings.MinimumDistance;
            force += away / distance * strength * strength * 170;
        }
        return force.LimitLength(MaxSpeed);
    }

    private static float Penetration(Vector2 local, MiningPlanet planet)
    {
        var origin = MiningPlanet.PointKey(local);
        float cost = 0, radius = MiningPlanet.Cell * 0.5f + HullPadding;
        for (int y = -2; y <= 2; y++)
        for (int x = -2; x <= 2; x++)
        {
            var key = origin + new Vector2I(x, y);
            if (!planet.Blocks.ContainsKey(key)) continue;
            var offset = local - MiningPlanet.KeyPoint(key);
            float dx = radius - Mathf.Abs(offset.X), dy = radius - Mathf.Abs(offset.Y);
            if (dx > 0 && dy > 0) cost += Mathf.Min(dx, dy);
        }
        return cost;
    }

    private Vector2 Recover(Vector2 position, MiningPlanet planet, float distance)
    {
        float best = Penetration(position, planet);
        var result = position;
        for (int i = 0; i < EscapeAxes.Length; i++)
        {
            var candidate = position + EscapeAxes[(i + _avoidSide + 8) % 8] * Mathf.Min(2, distance);
            float cost = Penetration(candidate, planet);
            if (cost < best - 0.001f) { best = cost; result = candidate; }
        }
        return result;
    }

    private Vector2 Sweep(Vector2 local, Vector2 movement, MiningPlanet planet)
    {
        float remaining = movement.Length();
        if (remaining < 0.00001f) return local;
        var direction = movement / remaining;
        while (remaining > 0.0001f)
        {
            float step = Mathf.Min(2, remaining);
            var next = local + direction * step;
            if (planet.ContainsPoint(next, HullPadding)) break;
            local = next; remaining -= step;
        }
        return local;
    }

    private void CheckClearance(Vector2 position, MiningPlanet planet, float dt)
    {
        // Retreat first. In a pocket narrower than our preferred stand-off, waiting
        // for that distance forever is a deadlock. Cut only an exposed forward face.
        if (!planet.Blocks.ContainsKey(Target)) return; // A blocked rescan must not erase the stall watchdog.
        if (position.DistanceTo(MiningPlanet.KeyPoint(Target)) >= Settings.MiningDistance - Settings.MiningTolerance)
        { _clearanceTime = 0; return; }
        if (_clearanceTime == 0 || position.DistanceTo(_clearanceAnchor) > Settings.ClearanceProgress)
        { _clearanceAnchor = position; _clearanceTime = 0; }
        _clearanceTime += dt;
        if (_clearanceTime < Settings.ClearanceStallTime) return;
        var target = MiningPlanet.KeyPoint(Target);
        State = Behavior.Widen;
        Heading = Mathf.RotateToward(Heading, (target - position).Angle(), Settings.TurnSpeed * dt);
        CanFire = Sees(position, target) && FirstHit(position, target, planet) == Target &&
            Mathf.Abs(Mathf.AngleDifference(Heading, (target - position).Angle())) < 0.15f;
    }

    public Vector2 Advance(Vector2 ship, Vector2 center, MiningPlanet planet, float dt)
    {
        if (dt <= 0) return ship;
        if (!_initialized) Reset(Heading);
        _time += dt; _scanTimer -= dt; _avoidTime = Mathf.Max(0, _avoidTime - dt);
        var local = ship - center;
        CanFire = false;
        if (_scanTimer <= 0 || Target != MiningPlanet.NoTarget && !planet.Blocks.ContainsKey(Target))
        {
            _scanTimer = Settings.ScanInterval;
            Sense(local, planet);
        }
        if (planet.ContainsPoint(local, HullPadding))
        {
            State = Behavior.Recover; RecoveryCount++;
            var next = local;
            float remaining = MaxSpeed * dt;
            while (remaining > 0)
            {
                var escaped = Recover(next, planet, remaining);
                if (escaped == next) break;
                remaining -= escaped.DistanceTo(next); next = escaped;
                if (!planet.ContainsPoint(next, HullPadding)) break;
            }
            Velocity = (next - local) / dt;
            CheckClearance(next, planet, dt);
            return center + next;
        }
        Vector2 desired;
        float desiredHeading;
        if (planet.Blocks.ContainsKey(Target))
        {
            var offset = MiningPlanet.KeyPoint(Target) - local;
            float distance = offset.Length();
            var toward = offset / Mathf.Max(distance, 0.01f);
            desiredHeading = toward.Angle();
            _searchDistance = 0; _searchLeg = 0; _searchHeading = desiredHeading; _searchSweep = Mathf.Tau;
            float speed = Mathf.Clamp((distance - Settings.MiningDistance) * 2.2f, -65, MaxSpeed);
            State = distance < Settings.MinimumDistance ? Behavior.Avoid : distance > Settings.MiningDistance + Settings.MiningTolerance ? Behavior.Approach : Behavior.Brake;
            if (Mathf.Abs(distance - Settings.MiningDistance) <= Settings.MiningTolerance)
            {
                State = Behavior.Mine;
                // Small, slowly changing lateral corrections; no permanent orbit direction.
                speed = (distance - Settings.MiningDistance) * 1.3f;
                var side = new Vector2(-toward.Y, toward.X);
                desired = toward * speed + side * (Mathf.Sin(_time * 0.73f) * 6);
            }
            else desired = toward * speed;
        }
        else
        {
            State = Behavior.Search;
            if (_searchSweep > 0)
            {
                // Look around locally before cruising out of an empty forward view.
                float turn = Mathf.Min(_searchSweep, Settings.TurnSpeed * dt);
                _searchSweep -= turn;
                desiredHeading = Heading + turn;
                desired = Vector2.Zero;
            }
            else
            {
                // Expanding survey legs use travelled distance from last contact only.
                float legLength = _searchLeg == 0 ? Settings.SensorRange * 4 : Settings.SensorRange * (2 + _searchLeg / 2);
                if (_searchDistance >= legLength) { _searchLeg++; _searchDistance = 0; _searchHeading += Mathf.Pi * 0.5f; }
                desiredHeading = _searchHeading;
                desired = Vector2.FromAngle(desiredHeading) * Settings.SearchSpeed;
            }
        }
        desired += Separation(local, planet);
        Heading = Mathf.RotateToward(Heading, desiredHeading, Settings.TurnSpeed * dt);
        Velocity = Velocity.MoveToward(desired.LimitLength(MaxSpeed), Settings.Acceleration * dt).LimitLength(MaxSpeed);
        var result = Sweep(local, Velocity * dt, planet);
        bool blocked = result.DistanceTo(local) + 0.1f < Velocity.Length() * dt;
        if (blocked)
        {
            State = Behavior.Avoid; _blockedTime += dt;
            if (_avoidTime == 0) { _avoidSide = -_avoidSide; _avoidTime = 0.8f; }
            var axis = Velocity.LengthSquared() > 1 ? Velocity.Normalized() : Vector2.FromAngle(Heading);
            var tangent = new Vector2(-axis.Y, axis.X) * _avoidSide;
            var slide = Sweep(local, tangent * Mathf.Min(MaxSpeed * dt, 45 * dt), planet);
            if (slide == local) slide = Sweep(local, -tangent * 45 * dt, planet);
            if (slide.DistanceSquaredTo(local) > result.DistanceSquaredTo(local)) result = slide;
            Velocity = (result - local) / dt;
            if (_blockedTime > 0.6f) { Target = MiningPlanet.NoTarget; _scanTimer = 0; _blockedTime = 0; }
        }
        else _blockedTime = 0;
        if (State == Behavior.Search) _searchDistance += result.DistanceTo(local);
        if (planet.Blocks.ContainsKey(Target) && Sees(result, MiningPlanet.KeyPoint(Target)))
        {
            var offset = MiningPlanet.KeyPoint(Target) - result;
            float distance = offset.Length();
            CanFire = FirstHit(result, MiningPlanet.KeyPoint(Target), planet) == Target &&
                Mathf.Abs(distance - Settings.MiningDistance) <= Settings.MiningTolerance + 3 &&
                Mathf.Abs(Mathf.AngleDifference(Heading, offset.Angle())) < 0.15f;
        }
        CheckClearance(result, planet, dt);
        return center + result;
    }
}
