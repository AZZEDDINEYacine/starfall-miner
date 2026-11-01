using Godot;

namespace StarfallMiner;

/// <summary>Menus own real fingers independently of touch-to-mouse emulation.</summary>
public partial class TouchButton : Button
{
    public int TouchFinger { get; private set; } = -1;
    public Func<bool>? CanActivate { get; set; }
    public bool Holographic { get; set; }
    public bool Selected { get; set; }
    public Color Accent { get; set; } = Colors.White;

    public void CancelTouch()
    {
        TouchFinger = -1;
        QueueRedraw();
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
            if (touch.Pressed && TouchFinger < 0 && GetGlobalRect().HasPoint(touch.Position))
            {
                TouchFinger = touch.Index;
                QueueRedraw();
                GetViewport().SetInputAsHandled();
            }
            else if (!touch.Pressed && touch.Index == TouchFinger)
            {
                bool activate = !touch.Canceled && GetGlobalRect().HasPoint(touch.Position);
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
        if (!Holographic)
        {
            if (TouchFinger >= 0) DrawStyleBox(GetThemeStylebox("pressed"), new Rect2(Vector2.Zero, Size));
            return;
        }
        float w = Size.X, h = Size.Y;
        bool active = Selected || TouchFinger >= 0 || IsHovered() || ButtonPressed;
        var color = new Color(Accent, active ? 0.9f : 0.35f);
        if (active) DrawRect(new Rect2(0, 0, w, h), new Color(Accent, 0.055f));
        DrawLine(new Vector2(0, 12), new Vector2(0, h - 1), color, active ? 2 : 1, true);
        DrawLine(new Vector2(0, 12), new Vector2(10, 2), color, 1, true);
        DrawLine(new Vector2(10, 2), new Vector2(Mathf.Min(w - 12, 32), 2), color, 1, true);
        DrawLine(new Vector2(0, h - 1), new Vector2(w - 10, h - 1), color, 1, true);
        DrawLine(new Vector2(w - 10, h - 1), new Vector2(w, h - 11), color, 1, true);
        if (active) DrawLine(new Vector2(4, h + 1), new Vector2(w - 14, h + 1), new Color(Accent, 0.08f), 4, true);
    }
}
