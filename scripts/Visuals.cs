using Godot;

namespace StarfallMiner;

/// <summary>Flat geometric ship, restrained effects and collectible debris.</summary>
public partial class Visuals : Node2D
{
    public Main Game { get; set; } = null!;
    private static readonly Color[] DebrisColors = [new("aad6cf"), new("e8cd94"), new("a1deee"), new("dba7a3"), new("ccbbef")];
    private static readonly Vector2[] Hull = [new(15, 0), new(-11, -8), new(-6, 0), new(-11, 8), new(15, 0)];
    private static readonly Vector2[] Fill = [new(15, 0), new(-11, -8), new(-6, 0), new(-11, 8)];
    private static readonly Vector2[] Directions = [new(-1, -1), new(1, -1), new(-1, 1), new(1, 1)];

    public override void _Draw()
    {
        var g = Game;
        foreach (var ring in g.Rings)
        {
            float t = 1 - ring.Life / ring.MaxLife;
            DrawArc(ring.Position, Mathf.Lerp(ring.Start, ring.Finish, t), 0, Mathf.Tau, 40, new Color(ring.Color, (1 - t) * 0.28f), 1, true);
        }
        foreach (var beam in g.Beams)
            DrawPolyline(beam.Points, new Color(beam.Color, Mathf.Clamp(beam.Life / beam.MaxLife, 0, 1) * 0.8f), 1.5f, true);
        foreach (var shell in g.Projectiles)
            DrawRect(new Rect2(shell.Position - new Vector2(2, 2), new Vector2(4, 4)), new Color("edc391"));
        foreach (var particle in g.Particles)
            DrawRect(new Rect2(particle.Position, Vector2.One * Mathf.Min(2, particle.Size)),
                new Color(particle.Color, Mathf.Clamp(particle.Life / particle.MaxLife, 0, 1) * 0.5f));
        foreach (var fragment in g.Debris)
        {
            var color = DebrisColors[fragment.Kind];
            DrawRect(new Rect2(fragment.Position - Vector2.One * fragment.Size * 0.5f, Vector2.One * fragment.Size), new Color(color, 0.72f));
            if (fragment.Age > 0.22f) DrawLine(fragment.Position, fragment.Position - fragment.Velocity.Normalized() * 4, new Color(color, 0.18f), 1, true);
        }
        if (g.Planet.Blocks.ContainsKey(g.TargetKey) && g.WarpTimer <= 0)
        {
            var target = Main.Center + MiningPlanet.KeyPoint(g.TargetKey);
            var c = new Color(Main.WeaponColors[g.Weapon], 0.6f);
            foreach (var direction in Directions)
            {
                var p = target + direction * 9;
                DrawLine(p, p - new Vector2(direction.X * 3.5f, 0), c, 1, true);
                DrawLine(p, p - new Vector2(0, direction.Y * 3.5f), c, 1, true);
            }
        }
        DrawShip();
    }

    private void DrawShip()
    {
        var color = Main.WeaponColors[Game.Weapon];
        DrawSetTransform(Game.ShipPosition, Game.ShipAngle);
        DrawColoredPolygon(Fill, new Color("101e27"));
        DrawPolyline(Hull, new Color("d7edeb"), 1.35f, true);
        DrawLine(new Vector2(2, 0), new Vector2(11, 0), color, 1.6f, true);
        if (Game.ShipVelocity.Length() > 4)
        {
            float length = 7 + Mathf.Sin(Game.Clock * 32) * 2;
            DrawLine(new Vector2(-10, 0), new Vector2(-10 - length, 0), new Color(color, 0.55f), 1.4f, true);
        }
        DrawSetTransform(Vector2.Zero);
    }
}
