using Godot;
using System.Text.Json;
using System.Text;
using StarfallMiner;

/// <summary>Opt-in localhost JSON protocol for rendered C# inspection. Never exported.</summary>
public partial class QaBridge : Node
{
    private readonly TcpServer _server = new();
    private readonly List<Client> _clients = new();
    private Main _game = null!;
    private sealed class Client(StreamPeerTcp peer)
    {
        public StreamPeerTcp Peer = peer;
        public StringBuilder Buffer = new();
        public bool Busy;
        public double Age;
    }

    public override void _Ready()
    {
        if (!OS.GetCmdlineUserArgs().Contains("--qa")) { GD.PushError("Inspection requires --qa."); GetTree().Quit(2); return; }
        _game = GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate<Main>();
        AddChild(_game);
        var error = _server.Listen(47831, "127.0.0.1");
        if (error != Error.Ok) { GD.PushError($"Inspection listen failed: {error}"); GetTree().Quit(2); return; }
        GD.Print("INSPECTION READY 127.0.0.1:47831 (C#; isolated saves)");
    }

    public override void _Process(double delta)
    {
        while (_server.IsConnectionAvailable())
        {
            var peer = _server.TakeConnection();
            if (_clients.Count >= 8) peer.DisconnectFromHost(); else _clients.Add(new Client(peer));
        }
        for (int i = _clients.Count - 1; i >= 0; i--)
        {
            var client = _clients[i];
            if (client.Busy) continue;
            client.Age += delta;
            client.Peer.Poll();
            if (client.Peer.GetStatus() != StreamPeerTcp.Status.Connected || client.Age > 10)
            {
                client.Peer.DisconnectFromHost(); client.Peer.Dispose(); _clients.RemoveAt(i); continue;
            }
            int available = client.Peer.GetAvailableBytes();
            if (available == 0) continue;
            client.Buffer.Append(client.Peer.GetUtf8String(available));
            if (client.Buffer.Length > 65536) { Respond(client, new { error = "Request too large" }); continue; }
            if (!client.Buffer.ToString().Contains('\n')) continue;
            client.Busy = true;
            Dispatch(client);
        }
    }

    private object State()
    {
        var buttons = new Dictionary<string, object>();
        foreach (var (id, button) in _game.Hud.Buttons)
        {
            if (!button.IsProcessingInput() || !button.IsVisibleInTree()) continue;
            var rect = button.GetGlobalRect();
            buttons[id] = new { x = rect.Position.X, y = rect.Position.Y, w = rect.Size.X, h = rect.Size.Y };
        }
        return new
        {
            engine = Engine.GetVersionInfo()["string"].AsString(), language = "C#", assembly = typeof(Main).Assembly.GetName().Name,
            credits = _game.Credits, flux = _game.Flux, sector = _game.Sector, weapon = _game.Weapon,
            levels = _game.Levels, auto = _game.AutoMode, paused = _game.Paused, muted = _game.Muted,
            blocks = _game.Planet.Blocks.Count, planet_id = _game.Planet.GetInstanceId(),
            ship_x = _game.ShipPosition.X, ship_y = _game.ShipPosition.Y,
            touch_steer = _game.Controls.JoystickActive, steer_id = _game.Controls.JoystickId,
            touch_fire = _game.Controls.FireHeld, fire_id = _game.Controls.FireId,
            pending_debris = _game.Debris.Count, projectiles = _game.Projectiles.Count,
            width = _game.View.Size.X, height = _game.View.Size.Y, panel = _game.Hud.PanelKind,
            transition = _game.TransitionLeft, layout_revision = _game.View.LayoutRevision, buttons,
            stick = new { x = _game.Controls.StickCenter.X, y = _game.Controls.StickCenter.Y },
            fire = new { x = _game.Controls.FireCenter.X, y = _game.Controls.FireCenter.Y },
            save_path = _game.Persistence.Path
        };
    }

    private async void Dispatch(Client client)
    {
        try
        {
            using var document = JsonDocument.Parse(client.Buffer.ToString().Trim());
            var request = document.RootElement;
            string command = request.GetProperty("command").GetString()!;
            switch (command)
            {
                case "state": break;
                case "freeze": _game.SetProcess(!request.GetProperty("value").GetBoolean()); break;
                case "resize":
                    GetWindow().Size = new Vector2I(Math.Clamp(request.GetProperty("width").GetInt32(), 320, 1920), Math.Clamp(request.GetProperty("height").GetInt32(), 320, 1920));
                    break;
                case "touch":
                    using (var touch = new InputEventScreenTouch
                    {
                        Position = Point(request), Index = request.GetProperty("finger").GetInt32(),
                        Pressed = request.GetProperty("pressed").GetBoolean(),
                        Canceled = request.TryGetProperty("canceled", out var canceled) && canceled.GetBoolean()
                    }) GetViewport().PushInput(touch, true);
                    break;
                case "drag":
                    using (var drag = new InputEventScreenDrag { Position = Point(request), Index = request.GetProperty("finger").GetInt32() })
                        GetViewport().PushInput(drag, true);
                    break;
                case "mouse":
                    using (var mouse = new InputEventMouseButton
                    {
                        Position = Point(request), GlobalPosition = Point(request), ButtonIndex = MouseButton.Left,
                        Pressed = request.GetProperty("pressed").GetBoolean(), Device = (int)InputEvent.DeviceIdEmulation
                    }) GetViewport().PushInput(mouse, true);
                    break;
                case "lifecycle":
                    _game.Notification(request.GetProperty("event").GetString() switch
                    {
                        "focus_out" => (int)NotificationWMWindowFocusOut,
                        "pause" => (int)NotificationApplicationPaused,
                        _ => throw new ArgumentException("Unknown lifecycle event")
                    });
                    break;
                case "screenshot":
                    if (DisplayServer.GetName() == "headless") throw new InvalidOperationException("Screenshot requires rendering enabled.");
                    string name = request.GetProperty("name").GetString()!;
                    if (name.GetFile() != name || !name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Screenshot requires a PNG filename.");
                    string path = "res://builds/qa/" + name;
                    DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://builds/qa"));
                    // macOS can skip display frames for occluded windows. Force an
                    // actual renderer pass so inspection also works behind the IDE.
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    RenderingServer.ForceDraw(false);
                    using (var image = GetViewport().GetTexture().GetImage())
                    {
                        var error = image.SavePng(path);
                        if (error != Error.Ok) throw new IOException($"Screenshot failed: {error}");
                    }
                    Respond(client, new { screenshot = ProjectSettings.GlobalizePath(path), state = State() });
                    return;
                case "quit":
                    Respond(client, new { ok = true });
                    Callable.From(() => GetTree().Quit()).CallDeferred();
                    return;
                default: throw new ArgumentException("Unknown inspection command");
            }
            // Deferred UI rebuilds and resize signals must settle before reporting state.
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Respond(client, State());
        }
        catch (Exception error) { Respond(client, new { error = error.Message }); }
    }

    private static Vector2 Point(JsonElement request) => new(request.GetProperty("x").GetSingle(), request.GetProperty("y").GetSingle());
    private static void Respond(Client client, object response)
    {
        client.Peer.PutData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response) + "\n"));
        client.Peer.DisconnectFromHost();
        client.Busy = false;
        client.Age = 11;
    }

    public override void _ExitTree()
    {
        _server.Stop(); _server.Dispose();
        foreach (var client in _clients) { client.Peer.DisconnectFromHost(); client.Peer.Dispose(); }
    }
}
