using Godot;

namespace StarfallMiner;

/// <summary>Live workshop view of the same hull and fitted hardpoints used in flight.</summary>
public partial class ShipSchematic : Control
{
    public Main Game { get; set; } = null!;
    public Font Font { get; set; } = null!;
    public HologramStyle Ink { get; set; } = null!;
    private static readonly string[] WeaponNames = ["PHOTON", "CANNON", "ION", "NOVA WAVE", "BORE", "FLUX BEAM"];
    private static readonly Vector2[] Hull = [new(15, 0), new(-11, -8), new(-6, 0), new(-11, 8), new(15, 0)];
    private static readonly Vector2[] Fill = [new(15, 0), new(-11, -8), new(-6, 0), new(-11, 8)];
    private void Text(string text, Vector2 p, Color color, int size = 9) => DrawString(Font, p, text, HorizontalAlignment.Left, -1, size, color);
    public override void _Draw()
    {
        var cyan = Ink.Cyan; var dim = Ink.Dim;
        var center = new Vector2(Size.X * 0.5f, Mathf.Min(108, Size.Y * 0.48f));
        float radius = Mathf.Min(49, Size.Y * 0.29f);
        if (Game.Palette.NebulaArtwork != null)
            DrawTextureRect(Game.Palette.NebulaArtwork, new Rect2(Vector2.Zero, Size), false, new Color(1, 1, 1, 0.12f));
        // Analytic dock rings share one transform and remain behind the fitted hull.
        DrawSetTransform(center, -0.24f, new Vector2(1, 0.38f));
        for (int orbit = 0; orbit < 3; orbit++)
            DrawArc(Vector2.Zero, radius * (1.45f + orbit * 0.25f), 0, Mathf.Tau, 64,
                new Color(orbit == 1 ? Ink.Focus : cyan, 0.18f - orbit * 0.035f), 1, true);
        DrawSetTransform(Vector2.Zero);
        DrawCircle(center, radius, new Color(cyan, 0.035f), true, -1, true);
        DrawLine(new Vector2(0, 0), new Vector2(Size.X, 0), new Color(cyan, 0.18f), 1);
        Text("DRYDOCK / LIVE FIT", new Vector2(0, 17), dim);
        Text($"{Game.Equipment.MountedShipCount}/{Game.ShipHardpoints} MOUNTS", new Vector2(Size.X - 88, 17), cyan);
        DrawArc(center, radius, -2.6f, -0.55f, 24, new Color(cyan, 0.22f), 1, true);
        DrawArc(center, radius, 0.55f, 2.6f, 24, new Color(cyan, 0.22f), 1, true);
        DrawLine(center - new Vector2(0, radius + 5), center + new Vector2(0, radius + 5), new Color(cyan, 0.07f), 1);
        DrawSetTransform(center, -Mathf.Pi * 0.5f, Vector2.One * (Size.Y < 140 ? 2.25f : 3.25f));
        DrawColoredPolygon(Fill, Ink.Shadow.Lightened(0.06f));
        DrawPolyline(Hull, Game.Palette.ShipOutline, 0.6f, true);
        DrawPolyline(Hull, new Color(cyan, 0.12f), 1.8f, true);
        DrawLine(new Vector2(2, 0), new Vector2(11, 0), Game.Palette.Weapon(Game.Weapon), 0.9f, true);
        for (int i = 0; i < 2; i++)
        {
            var item = Game.Equipment.FindWeapon(Game.Equipment.ShipAuxWeaponIds[i]);
            float side = i == 0 ? -9 : 9;
            var ink = item != null ? Game.Palette.Weapon(item.Type) : new Color(dim, 0.45f);
            if (i < Game.Equipment.HardpointLevel) DrawLine(new Vector2(-6, side), new Vector2(4, side), ink, 1.1f, true);
        }
        DrawSetTransform(Vector2.Zero);
        for (int i = 0; i < 3; i++)
        {
            int id = i == 0 ? Game.Equipment.ShipWeaponId : Game.Equipment.ShipAuxWeaponIds[i - 1];
            var item = Game.Equipment.FindWeapon(id);
            float x = i == 2 ? Size.X - 94 : 0;
            float y = i == 0 ? 42 : Mathf.Min(Size.Y - 50, center.Y + 30);
            var ink = item == null ? dim : Game.Palette.Weapon(item.Type);
            string name = item == null ? i > Game.Equipment.HardpointLevel ? "LOCKED" : "EMPTY" : WeaponNames[item.Type];
            Text($"0{i + 1} / {name}", new Vector2(x, y), ink, 9);
            if (item != null) Text($"POWER {item.Level} / RATE {item.RateLevel}", new Vector2(x, y + 14), dim, 8);
            float start = i == 2 ? x - 4 : x + 88;
            float end = i == 2 ? center.X + 32 : center.X - (i == 0 ? 9 : 32);
            DrawLine(new Vector2(start, y - 3), new Vector2(end, y - 3), new Color(ink, 0.3f), 0.8f, true);
        }
        for (int i = 0; i < Game.Equipment.Drones.Count; i++)
        {
            var drone = Game.Equipment.Drones[i];
            float x = 4 + i * (Size.X / 3);
            var ink = drone.Docked ? cyan : Ink.Amber;
            DrawArc(new Vector2(x + 5, Size.Y - 15), 4, 0, Mathf.Tau, 6, ink, 1, true);
            Text($"D{i + 1} / {(drone.Docked ? "DOCKED" : drone.Recalling ? "RETURN" : "MINING")}", new Vector2(x + 14, Size.Y - 12), ink, 8);
        }
        DrawLine(new Vector2(0, Size.Y - 1), new Vector2(Size.X, Size.Y - 1), new Color(cyan, 0.18f), 1);
    }
}
