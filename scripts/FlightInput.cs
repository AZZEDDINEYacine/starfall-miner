using Godot;

namespace StarfallMiner;

/// <summary>Hold and drag one world pointer to steer; menus retain independent finger ownership.</summary>
public partial class FlightInput : Node
{
    public Main Game { get; set; } = null!;
    public int PointerId { get; private set; } = -1;
    public Vector2 PointerPosition { get; private set; }
    private Vector2 _pressPosition, _pressWorld;
    private bool _dragged, _manualPointer;

    public void Release()
    {
        PointerId = -1;
        _dragged = _manualPointer = false;
        Game.Navigation.Stop();
        Game.ClearLaserBeams();
    }

    private bool CanNavigate(Vector2 point) => !Game.Paused && Game.WarpTimer <= 0 &&
        !Game.Hud.BlocksWorldInput && !Game.Hud.IsUiPoint(point) && Game.Hud.IsWorldPoint(point);

    public void UpdateSteering()
    {
        if (!_manualPointer || PointerId == -1 || Game.AutoMode) return;
        if (!CanNavigate(PointerPosition) || PointerPosition.DistanceTo(Game.View.WorldToScreen(Game.ShipPosition)) <= Game.FlightSettings.PointerDeadZone)
        {
            Game.Navigation.Stop();
            Game.ClearLaserBeams();
            return;
        }
        // Recompute from the held screen point every frame. Camera movement must not
        // turn a stationary thumb into a fixed waypoint that the ship eventually reaches.
        Game.Navigation.SetDestination(Game.View.ScreenToWorld(PointerPosition));
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouse && @event.Device == InputEvent.DeviceIdEmulation)
        {
            GetViewport().SetInputAsHandled();
            return;
        }
        // Releases reach the owner even over UI or outside the window.
        switch (@event)
        {
            case InputEventScreenTouch { Pressed: false } touch:
                Finish(touch.Position, touch.Index, touch.Canceled);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } mouse:
                Finish(mouse.Position, -2, false);
                break;
            case InputEventScreenDrag drag when drag.Index == PointerId:
                PointerPosition = drag.Position;
                _dragged |= drag.Position.DistanceTo(_pressPosition) > 24;
                UpdateSteering();
                break;
            case InputEventMouseMotion motion when PointerId == -2:
                PointerPosition = motion.Position;
                _dragged |= motion.Position.DistanceTo(_pressPosition) > 24;
                UpdateSteering();
                break;
        }
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
        switch (@event)
        {
            case InputEventScreenTouch { Pressed: true } touch: Begin(touch.Position, touch.Index); break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouse: Begin(mouse.Position, -2); break;
        }
    }

    private void Begin(Vector2 point, int id)
    {
        if (PointerId != -1 || !CanNavigate(point)) return;
        PointerId = id;
        _pressPosition = PointerPosition = point;
        _pressWorld = Game.View.ScreenToWorld(point);
        _manualPointer = !Game.AutoMode;
        _dragged = false;
        UpdateSteering();
    }

    private void Finish(Vector2 point, int id, bool canceled)
    {
        if (PointerId != id) return;
        PointerId = -1;
        if (_manualPointer)
        {
            _manualPointer = false;
            Game.Navigation.Stop();
            Game.ClearLaserBeams();
            return;
        }
        if (canceled || _dragged || point.DistanceTo(_pressPosition) > 24 || !CanNavigate(point)) return;
        var key = Game.RayTarget(Game.ShipPosition, _pressWorld);
        if (!Game.Planet.Blocks.ContainsKey(key)) return;
        Game.AimKey = Game.TargetKey = key;
        Game.Fire(true);
    }
}
