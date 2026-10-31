using Godot;

namespace StarfallMiner;

/// <summary>Global releases and unhandled world presses have separate paths.</summary>
public partial class FlightInput : Node
{
    public Main Game { get; set; } = null!;
    public bool FireHeld { get; private set; }
    public bool JoystickActive { get; private set; }
    public Vector2 JoystickOrigin { get; private set; }
    public Vector2 JoystickPosition { get; private set; }
    public int JoystickId { get; private set; } = -1;
    public int FireId { get; private set; } = -1;
    private bool _pointerDown;
    public Vector2 StickCenter => new(108, Game.View.Size.Y - 112);
    public Vector2 FireCenter => new(Game.View.Size.X - 108, Game.View.Size.Y - 112);

    public void Release()
    {
        FireHeld = JoystickActive = _pointerDown = false;
        JoystickId = FireId = -1;
    }

    public Vector2 Movement()
    {
        if (Game.Hud.BlocksWorldInput) return Vector2.Zero;
        var movement = new Vector2(
            (Input.IsPhysicalKeyPressed(Key.D) || Input.IsPhysicalKeyPressed(Key.Right) ? 1 : 0) -
            (Input.IsPhysicalKeyPressed(Key.A) || Input.IsPhysicalKeyPressed(Key.Left) ? 1 : 0),
            (Input.IsPhysicalKeyPressed(Key.S) || Input.IsPhysicalKeyPressed(Key.Down) ? 1 : 0) -
            (Input.IsPhysicalKeyPressed(Key.W) || Input.IsPhysicalKeyPressed(Key.Up) ? 1 : 0));
        if (JoystickActive) movement += (JoystickPosition - JoystickOrigin).LimitLength(48) / 48;
        return movement.LimitLength();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouse && @event.Device == InputEvent.DeviceIdEmulation)
        {
            GetViewport().SetInputAsHandled();
            return;
        }
        // Always release, even if the finger lifts over a menu or during a transition.
        if (@event is InputEventScreenTouch { Pressed: false } touch) Pointer(touch.Position, false, touch.Index);
        else if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } mouse)
            Pointer(mouse.Position, false, -2);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouse && @event.Device == InputEvent.DeviceIdEmulation) return;
        if (@event is InputEventKey { Pressed: true, Echo: false } key)
        {
            if (key.PhysicalKeycode is Key.P or Key.Escape) Game.TogglePause();
            else if (!Game.Paused && !Game.Hud.BlocksWorldInput)
            {
                switch (key.PhysicalKeycode)
                {
                    case Key.Key1: Game.SetWeapon(0); break;
                    case Key.Key2: Game.SetWeapon(1); break;
                    case Key.Key3: Game.SetWeapon(2); break;
                    case Key.Tab: Game.SetAuto(!Game.AutoMode); break;
                    case Key.E: Game.ActivateOverdrive(); break;
                }
            }
        }
        if (Game.Paused || Game.WarpTimer > 0 || Game.TransitionLeft > 0 || Game.Hud.BlocksWorldInput) return;
        switch (@event)
        {
            case InputEventScreenTouch touch: Pointer(touch.Position, touch.Pressed, touch.Index); break;
            case InputEventScreenDrag drag when drag.Index == JoystickId: JoystickPosition = drag.Position; break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } mouse: Pointer(mouse.Position, mouse.Pressed, -2); break;
            case InputEventMouseMotion motion when _pointerDown && JoystickId == -2: JoystickPosition = motion.Position; break;
        }
    }

    private void Pointer(Vector2 point, bool pressed, int id)
    {
        if (!pressed)
        {
            if (id == JoystickId) { JoystickActive = false; JoystickId = -1; }
            if (id == FireId) { FireHeld = false; FireId = -1; }
            if (id == -2) _pointerDown = false;
            return;
        }
        if (Game.Paused || Game.TransitionLeft > 0 || Game.Hud.BlocksWorldInput || Game.Hud.IsUiPoint(point)) return;
        if (Game.AutoMode && (point.Y < 222 || point.Y > Game.View.Size.Y - 194)) return;
        if (!Game.AutoMode && point.Y < 82) return;
        if (id == -2) _pointerDown = true;
        if (!Game.AutoMode && point.X < Game.View.Size.X * 0.36f && point.Y > Game.View.Size.Y * 0.48f)
        {
            if (JoystickId != -1) return;
            JoystickId = id;
            JoystickActive = true;
            JoystickOrigin = JoystickPosition = point;
            return;
        }
        if (!Game.AutoMode && point.DistanceTo(FireCenter) < 62)
        {
            if (FireId != -1) return;
            FireHeld = true;
            FireId = id;
            Game.ShotTimer = 0;
            return;
        }
        var key = Game.RayTarget(Game.ShipPosition, Game.View.ScreenToWorld(point));
        if (!Game.Planet.Blocks.ContainsKey(key)) return;
        Game.AimKey = Game.TargetKey = key;
        Game.Fire(true);
    }
}
