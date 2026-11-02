using Godot;

namespace StarfallMiner;

public partial class Main
{
    public void ClearLaserBeams(bool stopAudio = true)
    {
        Array.Clear(LaserBeams);
        if (stopAudio) Sound?.SetLaserActive(false);
    }

    private void SyncLaserSound()
    {
        bool active = false;
        if (!Paused && WarpTimer <= 0 && !Hud.BlocksWorldInput)
            for (int i = 0; i < LaserBeams.Length; i++) active |= LaserBeams[i].Active;
        Sound.SetLaserActive(active);
    }

    // Grid traversal returns the actual entry point, never the block center. A grazing
    // hit therefore cannot pull the beam away from the player's pointer or skip a wall.
    public Vector2I TraceLaser(Vector2 origin, Vector2 direction, out Vector2 endpoint)
    {
        direction = direction.Normalized();
        var local = origin - Center;
        var key = new Vector2I(Mathf.FloorToInt((local.X + 8) / 16), Mathf.FloorToInt((local.Y + 8) / 16));
        int sx = direction.X >= 0 ? 1 : -1, sy = direction.Y >= 0 ? 1 : -1;
        float dx = Mathf.Abs(direction.X) < 0.000001f ? float.PositiveInfinity : 16 / Mathf.Abs(direction.X);
        float dy = Mathf.Abs(direction.Y) < 0.000001f ? float.PositiveInfinity : 16 / Mathf.Abs(direction.Y);
        float tx = float.IsPositiveInfinity(dx) ? dx : (key.X * 16 + sx * 8 - local.X) / direction.X;
        float ty = float.IsPositiveInfinity(dy) ? dy : (key.Y * 16 + sy * 8 - local.Y) / direction.Y;
        float distance = 0;
        while (distance <= WeaponRange)
        {
            if (Planet.Blocks.ContainsKey(key)) { endpoint = origin + direction * distance; return key; }
            if (tx < ty) { distance = tx; tx += dx; key.X += sx; }
            else if (ty < tx) { distance = ty; ty += dy; key.Y += sy; }
            else { distance = tx; tx += dx; ty += dy; key.X += sx; key.Y += sy; }
        }
        endpoint = origin + direction * WeaponRange;
        return MiningPlanet.NoTarget;
    }

    private void UpdateShipLaser(int slot, CraftedWeapon item, Vector2 origin, float dt)
    {
        if (Paused || WarpTimer > 0 || Hud.BlocksWorldInput) return;
        Vector2 direction;
        if (AutoMode)
        {
            if (!Autopilot.CanFire || !Planet.Blocks.ContainsKey(TargetKey)) return;
            direction = Center + MiningPlanet.KeyPoint(TargetKey) - origin;
        }
        else
        {
            if (!Navigation.Active) return;
            // Navigation may aim toward a grazing obstacle for safe movement. The
            // laser must instead pass precisely through the held world pointer.
            direction = Navigation.Destination - origin;
        }
        UpdateLaser(slot, origin, direction, ShipDamageMultiplier * WeaponPower(item.Level),
            BaseCooldown[ContinuousLaser] / WeaponCooldown(item.Type, item.Level, item.RateLevel) * (AutoMode ? 1 : ManualFireRateMultiplier), dt);
    }

    private void UpdateLaser(int slot, Vector2 origin, Vector2 direction, double power, float cadence, float dt)
    {
        if (direction.LengthSquared() < 0.0001f || dt <= 0) return;
        direction = direction.Normalized();
        var key = TraceLaser(origin, direction, out var end);
        bool hit = Planet.Blocks.ContainsKey(key);
        double budget = Progression.WeaponBaseDamage[ContinuousLaser] * power * cadence * dt;
        // Spend remaining frame energy on newly exposed faces. Large upgrades must
        // not lose most of their output to overkill or become frame-rate limited.
        for (int cut = 0; cut < 32 && budget > 0 && Planet.Blocks.TryGetValue(key, out var block); cut++)
        {
            double spent = Math.Min(budget, block.Hp);
            Planet.Damage(key, spent);
            budget -= spent;
            if (WarpTimer > 0 || Planet.Blocks.ContainsKey(key) || budget <= 0) break;
            key = TraceLaser(origin, direction, out end);
            hit = Planet.Blocks.ContainsKey(key);
        }
        if (WarpTimer > 0) return;
        LaserBeams[slot] = new ContinuousBeam { Active = true, Hit = hit, Origin = origin + direction * Mathf.Min(19, origin.DistanceTo(end)), End = end };
    }
}
