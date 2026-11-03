using Godot;

namespace StarfallMiner;

/// <summary>Static nine-slice glass beneath native label ink; never owns input.</summary>
public partial class GlassSurface : Control
{
    public HologramStyle Ink { get; set; } = null!;
    public TouchButton? Button { get; set; }
    public Texture2D? Nebula { get; set; }
    private readonly StyleBoxTexture _surface = new();

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ShowBehindParent = true;
        TextureFilter = TextureFilterEnum.Linear;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _surface.Texture = Button != null ? Ink.ControlSurface : Ink.PanelSurface;
        float margin = Button != null ? 12 : 20;
        _surface.TextureMarginLeft = _surface.TextureMarginRight = margin;
        _surface.TextureMarginTop = _surface.TextureMarginBottom = margin;
        Resized += QueueRedraw;
        SetProcess(false);
    }

    public override void _Draw()
    {
        if (_surface.Texture == null || Size.X <= 0 || Size.Y <= 0) return;
        bool disabled = Button?.Disabled == true;
        bool pressed = Button != null && !disabled &&
            (Button.TouchFinger >= 0 || Button.GetDrawMode() is BaseButton.DrawMode.Pressed or BaseButton.DrawMode.HoverPressed);
        bool selected = Button?.Selected == true;
        _surface.ModulateColor = disabled ? new Color(0.42f, 0.47f, 0.54f, 0.72f) :
            pressed ? new Color(0.65f, 0.83f, 0.96f, 1) :
            selected || Button?.IsHovered() == true ? Colors.White : new Color(0.78f, 0.87f, 1, 1);
        DrawStyleBox(_surface, new Rect2(Vector2.Zero, Size));
        if (Button == null && Nebula != null && Size.X > 80 && Size.Y > 80)
            DrawTextureRect(Nebula, new Rect2(Vector2.One * 12, Size - Vector2.One * 24), false, new Color(1, 1, 1, 0.055f));
    }

    public override void _ExitTree() => _surface.Dispose();
}
