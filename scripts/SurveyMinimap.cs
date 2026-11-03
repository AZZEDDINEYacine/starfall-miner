using Godot;

namespace StarfallMiner;

/// <summary>Cached coarse terrain survey. Its data is presentation-only, never pilot input.</summary>
public partial class SurveyMinimap : Control
{
    public Main Game { get; set; } = null!;
    public Font Font { get; set; } = null!;
    public ulong RasterRevision { get; private set; }
    public int RasterUpdates { get; private set; }
    public float HalfExtent { get; private set; }
    private Image _image = null!;
    private ImageTexture _texture = null!;
    private float _refreshLeft;
    private readonly Vector2[] _ship = new Vector2[4];
    private const int Resolution = 112;
    private Rect2 Plot => new(10, 18, Size.X - 20, Size.X - 20);

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _image = Image.CreateEmpty(Resolution, Resolution, false, Image.Format.Rgba8);
        _texture = ImageTexture.CreateFromImage(_image);
        RefreshMap(true);
    }

    public void RefreshMap(bool force = false)
    {
        if (!force && RasterRevision == Game.Planet.TopologyRevision) return;
        HalfExtent = Game.Planet.MapRadius + 120;
        _image.Fill(new Color("061118"));
        var rock = new Color("49707a");
        foreach (var key in Game.Planet.Blocks.Keys)
        {
            var p = MiningPlanet.KeyPoint(key);
            int x = Math.Clamp(Mathf.RoundToInt((p.X / HalfExtent * 0.5f + 0.5f) * (Resolution - 1)), 0, Resolution - 1);
            int y = Math.Clamp(Mathf.RoundToInt((p.Y / HalfExtent * 0.5f + 0.5f) * (Resolution - 1)), 0, Resolution - 1);
            _image.SetPixel(x, y, rock);
        }
        _texture.Update(_image);
        RasterRevision = Game.Planet.TopologyRevision; RasterUpdates++;
    }

    public Vector2 MapPoint(Vector2 world)
    {
        var plot = Plot;
        var p = plot.GetCenter() + (world - Main.Center) * (plot.Size.X / (HalfExtent * 2));
        return p.Clamp(plot.Position + Vector2.One * 3, plot.End - Vector2.One * 3);
    }

    public override void _Process(double delta)
    {
        _refreshLeft -= (float)delta;
        if (_refreshLeft <= 0) { _refreshLeft = 0.2f; RefreshMap(); }
        QueueRedraw(); // Cheap ship/drone markers remain fluid independently of the cached terrain.
    }

    public override void _Draw()
    {
        if (_texture == null) return;
        var ink = new Color("83d5df"); var plot = Plot;
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("040c12"));
        DrawString(Font, new Vector2(4, 11), "SURVEY", HorizontalAlignment.Left, -1, 9, new Color(ink, 0.65f));
        DrawLine(new Vector2(0, 14), new Vector2(Size.X - 8, 14), new Color(ink, 0.3f), 1);
        DrawLine(new Vector2(Size.X, 22), new Vector2(Size.X, Size.Y), new Color(ink, 0.3f), 1);
        DrawTextureRect(_texture, plot, false);
        var a = MapPoint(Game.View.ScreenToWorld(new Vector2(0, 230)));
        var b = MapPoint(Game.View.ScreenToWorld(new Vector2(Game.View.Size.X, Game.View.Size.Y - 234)));
        DrawRect(new Rect2(a, b - a), new Color(ink, 0.35f), false, 0.7f);
        foreach (var drone in Game.Equipment.Drones)
            if (!drone.Docked) DrawCircle(MapPoint(drone.Position), 1.8f, drone.Recalling ? new Color("e6c087") : new Color("b3a1df"));
        var point = MapPoint(Game.ShipPosition); var direction = Vector2.FromAngle(Game.ShipAngle);
        var side = new Vector2(-direction.Y, direction.X);
        _ship[0] = point + direction * 4; _ship[1] = point - direction * 3 + side * 2.3f;
        _ship[2] = point - direction; _ship[3] = point - direction * 3 - side * 2.3f;
        DrawColoredPolygon(_ship, new Color("c4ffff"));
    }

    public override void _ExitTree()
    {
        _texture?.Dispose(); _image?.Dispose();
    }
}
