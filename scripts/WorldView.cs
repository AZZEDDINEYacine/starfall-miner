using Godot;

namespace StarfallMiner;

/// <summary>Orientation and camera transforms never mutate the simulated world.</summary>
public partial class WorldView : Node
{
    public Main Game { get; set; } = null!;
    public Node2D Stage { get; } = new() { Name = "PersistentWorld" };
    public Vector2 Size { get; private set; } = new(540, 960);
    public float CameraScale { get; private set; } = 1;
    public float TargetScale { get; private set; } = 1;
    public int LayoutRevision { get; private set; }
    private Vector2 _cameraFocus = Main.Center;

    public override void _Ready()
    {
        AddChild(Stage);
        GetViewport().SizeChanged += OnResize;
    }

    private void OnResize()
    {
        LayoutRevision++;
        Game.ReleaseControls();
        PlaceWorld();
        Game.Hud?.RequestLayout();
    }

    public void PlaceWorld(bool snap = false, float delta = 0)
    {
        Size = Game.GetViewportRect().Size;
        Vector2 center;
        if (Game.AutoMode)
        {
            const float top = 218;
            float bottom = Size.Y - 194;
            center = new Vector2(Size.X * 0.5f, (top + bottom) * 0.5f);
            TargetScale = Mathf.Min((Size.X - 48) / 450, (bottom - top - 12) / 450);
        }
        else
        {
            center = Size * 0.5f;
            TargetScale = Mathf.Min((Size.X - 180) / 450, (Size.Y - 106) / 450);
        }
        TargetScale = Mathf.Clamp(TargetScale, 0.6f, 1.35f);
        CameraScale = snap ? TargetScale : Mathf.Lerp(CameraScale, TargetScale, 1 - Mathf.Exp(-delta * 8));
        // Keep the ship on screen and show the nearby mining surface. A full
        // planet no longer fits, so following its center would hide the pilot.
        var desiredFocus = Main.Center + (Game.ShipPosition - Main.Center) * 0.68f;
        _cameraFocus = snap ? desiredFocus : _cameraFocus.Lerp(desiredFocus, 1 - Mathf.Exp(-delta * 5));
        Stage.Scale = Vector2.One * CameraScale;
        Stage.Position = center - _cameraFocus * CameraScale;
    }

    public Vector2 ScreenToWorld(Vector2 point) => (point - Stage.Position) / Mathf.Max(CameraScale, 0.01f);
    public Vector2 WorldToScreen(Vector2 point) => point * CameraScale + Stage.Position;
    public Rect2 ManualBounds
    {
        get
        {
            var start = ScreenToWorld(new Vector2(22, 94));
            return new Rect2(start, ScreenToWorld(Size - new Vector2(22, 22)) - start);
        }
    }

    public void ApplyOrientation()
    {
        if (OS.HasFeature("android") || OS.HasFeature("ios"))
            DisplayServer.ScreenSetOrientation(Game.AutoMode ? DisplayServer.ScreenOrientation.Portrait : DisplayServer.ScreenOrientation.Landscape);
        else if ((!Game.IsTest || OS.GetCmdlineUserArgs().Contains("--qa")) && DisplayServer.GetName() != "headless")
            GetWindow().Size = Game.AutoMode ? new Vector2I(540, 960) : new Vector2I(960, 540);
    }
}
