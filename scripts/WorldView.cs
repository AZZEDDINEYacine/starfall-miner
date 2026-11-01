using Godot;

namespace StarfallMiner;

/// <summary>Portrait framing and ship-following transforms never mutate the simulated world.</summary>
public partial class WorldView : Node
{
    public Main Game { get; set; } = null!;
    [Export] public ModeTransitionStyle Motion { get; set; } = null!;
    public Node2D Stage { get; private set; } = null!;
    public Vector2 Size { get; private set; } = new(540, 960);
    public float CameraScale { get; private set; } = 1;
    public float TargetScale { get; private set; } = 1;
    public int LayoutRevision { get; private set; }
    public bool LayoutAutoMode { get; private set; } = true;
    public const float ModeChangeDuration = 0.85f;
    private Vector2 _cameraCenter;
    public Vector2 CameraFocus { get; private set; } = Main.Center;
    private bool _cameraInitialized;

    public override void _Ready()
    {
        Stage = GetNode<Node2D>("PersistentWorld");
        GetViewport().SizeChanged += OnResize;
    }

    private void OnResize()
    {
        LayoutRevision++;
        Game.ReleaseControls();
        PlaceWorld();
        Game.Hud?.RequestLayout();
    }

    public void BeginModeChange()
    {
        LayoutAutoMode = Game.AutoMode;
        Game.TransitionLeft = ModeChangeDuration;
    }

    public void PlaceWorld(bool snap = false, float delta = 0)
    {
        var newSize = Game.GetViewportRect().Size;
        if (_cameraInitialized) _cameraCenter += (newSize - Size) * 0.5f;
        Size = newSize;
        if (snap) LayoutAutoMode = Game.AutoMode;
        const float top = 230;
        float bottom = Size.Y - 234;
        var center = new Vector2(Size.X * 0.5f, (top + bottom) * 0.5f);
        TargetScale = LayoutAutoMode
            ? Mathf.Min((Size.X - 48) / 450, (bottom - top - 12) / 450)
            : Motion.ManualZoom;
        TargetScale = Mathf.Clamp(TargetScale, 0.6f, 3.5f);
        float blend = 1 - Mathf.Exp(-delta * Motion.CameraResponse);
        CameraScale = snap ? TargetScale : Mathf.Lerp(CameraScale, TargetScale, blend);
        _cameraCenter = snap || !_cameraInitialized ? center : _cameraCenter.Lerp(center, blend);
        CameraFocus = snap || !_cameraInitialized ? Game.ShipPosition : CameraFocus.Lerp(Game.ShipPosition, blend);
        _cameraInitialized = true;
        var scale = Vector2.One * CameraScale;
        var position = _cameraCenter - CameraFocus * CameraScale;
        bool changed = Stage.Position != position || Stage.Scale != scale;
        Stage.Scale = scale;
        Stage.Position = position;
        if (changed) Game.Planet?.NotifyTilesChanged();
    }

    public Vector2 ScreenToWorld(Vector2 point) => (point - Stage.Position) / Mathf.Max(CameraScale, 0.01f);
    public Vector2 WorldToScreen(Vector2 point) => point * CameraScale + Stage.Position;

    // Called once on startup. Switching modes never rotates or resizes the native window.
    public void ApplyOrientation()
    {
        if (OS.HasFeature("android") || OS.HasFeature("ios"))
            DisplayServer.ScreenSetOrientation(DisplayServer.ScreenOrientation.Portrait);
    }
}
