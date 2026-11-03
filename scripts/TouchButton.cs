using Godot;

namespace StarfallMiner;

/// <summary>Menus own real fingers independently of touch-to-mouse emulation.</summary>
public partial class TouchButton : Button
{
    public int TouchFinger { get; private set; } = -1;
    public Func<bool>? CanActivate { get; set; }
    public bool Holographic { get; set; }
    public bool TelemetryLink { get; set; }
    private bool _selected;
    public bool Selected
    {
        get => _selected;
        set { if (_selected == value) return; _selected = value; RedrawSurface(); }
    }
    public Color Accent { get; set; } = Colors.White;
    public HologramStyle? Ink { get; set; }
    private GlassSurface? _glass;
    private readonly StyleBoxFlat _edge = new() { DrawCenter = false, CornerRadiusTopLeft = 10,
        CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10 };

    public override void _Ready()
    {
        if (Holographic && !TelemetryLink && Ink != null)
        {
            _glass = new GlassSurface { Ink = Ink, Button = this };
            AddChild(_glass);
        }
        MouseEntered += RedrawSurface;
        MouseExited += RedrawSurface;
        ButtonDown += RedrawSurface;
        ButtonUp += RedrawSurface;
        FocusEntered += RedrawSurface;
        FocusExited += RedrawSurface;
    }

    private void RedrawSurface() { QueueRedraw(); _glass?.QueueRedraw(); }

    // Direct touch input must respect scroll clipping as well as the original hit box.
    private bool ContainsVisiblePoint(Vector2 point)
    {
        if (!GetGlobalRect().HasPoint(point)) return false;
        for (Node? node = GetParent(); node != null; node = node.GetParent())
            if (node is Control { ClipContents: true } clip && !clip.GetGlobalRect().HasPoint(point)) return false;
        return true;
    }

    public void CancelTouch()
    {
        TouchFinger = -1;
        RedrawSurface();
    }

    public override void _Input(InputEvent @event)
    {
        if (!IsVisibleInTree() || Disabled || CanActivate?.Invoke() == false)
        {
            CancelTouch();
            return;
        }
        if (@event is InputEventScreenTouch touch)
        {
            if (touch.Pressed && TouchFinger < 0 && ContainsVisiblePoint(touch.Position))
            {
                TouchFinger = touch.Index;
                RedrawSurface();
                GetViewport().SetInputAsHandled();
            }
            else if (!touch.Pressed && touch.Index == TouchFinger)
            {
                bool activate = !touch.Canceled && ContainsVisiblePoint(touch.Position);
                CancelTouch();
                GetViewport().SetInputAsHandled();
                if (activate) EmitSignal(SignalName.Pressed);
            }
        }
        else if (@event is InputEventScreenDrag drag && drag.Index == TouchFinger)
            GetViewport().SetInputAsHandled();
    }

    public override void _Draw()
    {
        _glass?.QueueRedraw(); // Native disabled/mouse state changes also refresh the underlay.
        if (!Holographic)
        {
            if (TouchFinger >= 0) DrawStyleBox(GetThemeStylebox("pressed"), new Rect2(Vector2.Zero, Size));
            return;
        }
        bool pressed = !Disabled && (TouchFinger >= 0 || ButtonPressed || GetDrawMode() is DrawMode.Pressed or DrawMode.HoverPressed);
        bool hovered = !Disabled && IsHovered();
        var ink = Disabled ? Ink?.Disabled ?? new Color("69758e") : pressed ? Colors.White : Accent;
        float alpha = Disabled ? 0.22f : pressed ? 1 : Selected ? 0.9f : hovered ? 0.75f : 0.32f;
        if (TelemetryLink)
        {
            // Keep telemetry open, with a quiet link underline and a clear response.
            DrawLine(new Vector2(12, Size.Y - 2), new Vector2(Size.X - 12, Size.Y - 2),
                new Color(ink, hovered || pressed ? alpha : 0.15f), pressed ? 2 : 1, true);
            return;
        }
        _edge.BorderColor = new Color(ink, alpha);
        _edge.SetBorderWidthAll(pressed ? 2 : 1);
        _edge.ShadowColor = new Color(ink, !Disabled && (hovered || Selected || pressed) ? 0.09f : 0);
        _edge.ShadowSize = Disabled ? 0 : 5;
        DrawStyleBox(_edge, new Rect2(Vector2.One, Size - Vector2.One * 2));
        if (!Disabled)
            DrawLine(new Vector2(12, 2), new Vector2(Size.X - 12, 2), new Color(ink, pressed ? 0.9f : hovered ? 0.65f : 0.3f), 1, true);
        if (Selected && !Disabled)
            DrawLine(new Vector2(10, Size.Y - 5), new Vector2(Mathf.Min(Size.X - 10, 38), Size.Y - 5), Accent, 2, true);
        if (!Disabled && HasFocus())
            DrawRect(new Rect2(Vector2.One * 4, Size - Vector2.One * 8), new Color(Ink?.Focus ?? Colors.White, 0.9f), false, 1, true);
    }

    public override void _ExitTree() => _edge.Dispose();
}
