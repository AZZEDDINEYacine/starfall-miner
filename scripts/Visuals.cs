using Godot;

namespace StarfallMiner;

/// <summary>Flat geometric ship, restrained effects and collectible debris.</summary>
public partial class Visuals : Node2D
{
    public Main Game { get; set; } = null!;
    private VisualPalette Palette => Game.Palette;
    private static readonly Vector2[] Hull = [new(15, 0), new(-11, -8), new(-6, 0), new(-11, 8), new(15, 0)];
    private static readonly Vector2[] Fill = [new(15, 0), new(-11, -8), new(-6, 0), new(-11, 8)];
    private static readonly Vector2[] Directions = [new(-1, -1), new(1, -1), new(-1, 1), new(1, 1)];
    private readonly Vector2[] _novaFill = new Vector2[64], _novaRim = new Vector2[65];

    public override void _Draw()
    {
        var g = Game;
        foreach (var ring in g.Rings)
        {
            float t = 1 - ring.Life / ring.MaxLife;
            float strength = ring.Strength > 0 ? ring.Strength : 0.28f;
            float expansion = strength > 0.5f ? Mathf.Min(t * 3, 1) : t;
            float radius = Mathf.Lerp(ring.Start, ring.Finish, expansion);
            if (strength > 0.5f) DrawCircle(ring.Position, radius, new Color(ring.Color, (1 - t) * 0.12f));
            DrawArc(ring.Position, radius, 0, Mathf.Tau, 40, new Color(ring.Color, (1 - t) * strength), strength > 0.5f ? 2 : 1, true);
        }
        foreach (var wave in g.Waves)
        {
            float t = Mathf.Clamp(wave.Age / wave.Duration, 0, 1);
            float radius = wave.Radius * t;
            var ink = Palette.Weapon(3);
            if (radius < 0.1f) continue;
            for (int point = 0; point < 64; point++)
            {
                float angle = point * Mathf.Tau / 64;
                float roughness = 0.96f + 0.02f * Mathf.Sin(angle * 11 + wave.Age * 6) + 0.02f * Mathf.Sin(angle * 23 - wave.Age * 9);
                _novaFill[point] = _novaRim[point] = wave.Position + Vector2.FromAngle(angle) * radius * roughness;
            }
            _novaRim[64] = _novaRim[0];
            DrawColoredPolygon(_novaFill, new Color(ink, (1 - t) * 0.16f));
            DrawPolyline(_novaRim, new Color(ink, (1 - t) * 0.16f), 6, true);
            DrawPolyline(_novaRim, new Color(ink.Lightened(0.35f), (1 - t) * 0.9f), 1.5f, true);
            for (int echo = 1; echo <= 3; echo++)
            {
                float echoRadius = wave.Radius * (t - echo * 0.13f);
                if (echoRadius > 0) DrawArc(wave.Position, echoRadius, 0, Mathf.Tau, 64, new Color(ink, (1 - t) * (0.55f - echo * 0.1f)), 1, true);
            }
        }
        if (!g.Paused && !g.Hud.BlocksWorldInput)
        for (int slot = 0; slot < g.LaserBeams.Length; slot++)
        {
            var laser = g.LaserBeams[slot];
            if (!laser.Active || slot < 3 && !g.AutoMode && !g.Navigation.Active) continue;
            var ink = Palette.Weapon(Main.ContinuousLaser);
            DrawLine(laser.Origin, laser.End, new Color(ink, 0.10f), 8, true);
            DrawLine(laser.Origin, laser.End, new Color(ink, 0.5f), 3, true);
            DrawLine(laser.Origin, laser.End, ink.Lightened(0.7f), 1, true);
            if (laser.Hit)
            {
                DrawCircle(laser.End, 4, new Color(ink, 0.14f));
                DrawCircle(laser.End, 1.5f, ink.Lightened(0.7f));
            }
        }
        foreach (var beam in g.Beams)
            DrawPolyline(beam.Points, new Color(beam.Color, Mathf.Clamp(beam.Life / beam.MaxLife, 0, 1) * 0.8f), 1.5f, true);
        foreach (var shell in g.Projectiles)
            DrawRect(new Rect2(shell.Position - new Vector2(2, 2), new Vector2(4, 4)), Palette.Projectile);
        foreach (var particle in g.Particles)
            DrawRect(new Rect2(particle.Position, Vector2.One * Mathf.Min(2, particle.Size)),
                new Color(particle.Color, Mathf.Clamp(particle.Life / particle.MaxLife, 0, 1) * 0.5f));
        foreach (var fragment in g.Debris)
        {
            var art = g.CurrentVisualStyle;
            var color = art.Treatment == PlanetVisualStyle.Surface.FlatGrid ? Palette.Debris(fragment.Kind) : art.DamagedColor(fragment.Kind, Palette);
            if (fragment.Kind == MiningPlanet.Stone) color = PlanetRenderer.TerrainColor(color, g.Planet.Sector);
            if (fragment.Material >= 0) color = g.Catalog.MaterialColors[fragment.Material];
            float size = fragment.Size;
            if (fragment.Material >= 0)
            {
                DrawCircle(fragment.Position, size * 1.4f, new Color(color, 0.12f));
                DrawRect(new Rect2(fragment.Position - Vector2.One * 1.4f, Vector2.One * 2.8f), new Color(color, 0.95f));
            }
            else if (art.Treatment == PlanetVisualStyle.Surface.NeonFrontier)
            {
                DrawCircle(fragment.Position, size * 1.3f, new Color(color, art.GlowStrength * 0.3f));
                DrawCircle(fragment.Position, size * 0.6f, new Color(color, 0.9f));
                if (art.Treatment == PlanetVisualStyle.Surface.NeonFrontier && fragment.Value > 0)
                    DrawArc(fragment.Position, size + 1, 0, Mathf.Tau, 10, new Color(color, 0.55f), 0.7f, true);
            }
            else DrawRect(new Rect2(fragment.Position - Vector2.One * size * 0.5f, Vector2.One * size), new Color(color, 0.8f));
            if (fragment.Age > 0.22f) DrawLine(fragment.Position, fragment.Position - fragment.Velocity.Normalized() * 4, new Color(color, 0.18f), 1, true);
        }
        if (!g.AutoMode && g.Navigation.Active && g.WarpTimer <= 0)
        {
            var endpoint = g.Navigation.Destination;
            var color = g.Navigation.ClearingPath ? Palette.Gold : Palette.Mint;
            var direction = (endpoint - g.ShipPosition).Normalized();
            DrawLine(g.ShipPosition + direction * 22, endpoint, new Color(color, 0.3f), 1, true);
            DrawArc(endpoint, 9, 0, Mathf.Tau, 24, new Color(color, 0.85f), 1.2f, true);
            DrawLine(endpoint - Vector2.Right * 4, endpoint + Vector2.Right * 4, color, 1, true);
            DrawLine(endpoint - Vector2.Down * 4, endpoint + Vector2.Down * 4, color, 1, true);
        }
        if (g.Planet.Blocks.ContainsKey(g.TargetKey) && g.WarpTimer <= 0)
        {
            var target = Main.Center + MiningPlanet.KeyPoint(g.TargetKey);
            var c = new Color(Palette.Weapon(g.Weapon), 0.6f);
            if (MiningPlanet.IsExplosive(g.Planet.Blocks[g.TargetKey]))
            {
                float radius = g.Planet.GenerationSettings.ExplosiveRadius;
                var warning = new Color("ff7161");
                DrawCircle(target, radius, new Color(warning, 0.055f));
                for (int dash = 0; dash < 12; dash++)
                    DrawArc(target, radius, dash * Mathf.Tau / 12, (dash + 0.65f) * Mathf.Tau / 12, 4, new Color(warning, 0.6f), 1, true);
            }
            foreach (var direction in Directions)
            {
                var p = target + direction * 9;
                DrawLine(p, p - new Vector2(direction.X * 3.5f, 0), c, 1, true);
                DrawLine(p, p - new Vector2(0, direction.Y * 3.5f), c, 1, true);
            }
        }
        foreach (var drone in g.Equipment.Drones)
        {
            if (drone.Docked) continue;
            var mount = g.Equipment.FindWeapon(drone.WeaponId);
            var tint = mount == null ? Palette.MutedInk : Palette.Weapon(mount.Type);
            DrawSetTransform(drone.Position, drone.Angle);
            DrawArc(Vector2.Zero, 7, 0, Mathf.Tau, 6, new Color(tint, 0.65f), 1.2f, true);
            DrawLine(new Vector2(-3, -3), new Vector2(6, 0), tint, 1.2f, true);
            DrawLine(new Vector2(6, 0), new Vector2(-3, 3), tint, 1.2f, true);
            DrawCircle(Vector2.Zero, 1.5f, tint);
            DrawSetTransform(Vector2.Zero);
        }
        DrawShip();
    }

    private void DrawShip()
    {
        var color = Palette.Weapon(Game.Weapon);
        DrawSetTransform(Game.ShipPosition, Game.ShipAngle);
        DrawColoredPolygon(Fill, Palette.ShipFill);
        DrawPolyline(Hull, Palette.ShipOutline, 1.35f, true);
        DrawLine(new Vector2(2, 0), new Vector2(11, 0), color, 1.6f, true);
        for (int i = 0; i < 2; i++)
        {
            var mount = Game.Equipment.FindWeapon(Game.Equipment.ShipAuxWeaponIds[i]);
            if (mount == null) continue;
            float side = i == 0 ? -9 : 9;
            DrawLine(new Vector2(-6, side), new Vector2(4, side), Palette.Weapon(mount.Type), 2.5f, true);
        }
        if (Game.ShipVelocity.Length() > 4)
        {
            float length = 7 + Mathf.Sin(Game.Clock * 32) * 2;
            DrawLine(new Vector2(-10, 0), new Vector2(-10 - length, 0), new Color(color, 0.55f), 1.4f, true);
        }
        DrawSetTransform(Vector2.Zero);
    }
}
