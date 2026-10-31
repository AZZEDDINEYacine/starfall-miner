using Godot;

namespace StarfallMiner;

/// <summary>Menus own real fingers independently of touch-to-mouse emulation.</summary>
public partial class TouchButton : Button
{
    public int TouchFinger { get; private set; } = -1;
    public Func<bool>? CanActivate { get; set; }

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
        if (TouchFinger >= 0) DrawStyleBox(GetThemeStylebox("pressed"), new Rect2(Vector2.Zero, Size));
    }
}
