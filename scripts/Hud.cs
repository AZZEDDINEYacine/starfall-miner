using Godot;
using System.Globalization;

namespace StarfallMiner;

public partial class Hud : Control
{
    public static readonly Color Ink = new("e5f1f0"), MutedInk = new("789197"), Mint = new("8ceacb"),
        Gold = new("e6ca8c"), Lilac = new("b8acf0"), PanelColor = new("0e1921"), Edge = new("294047");
    private static readonly string[] UpgradeBonuses = ["+46% DAMAGE", "+18% SPEED", "+30% YIELD"];
    public Main Game { get; set; } = null!;
    private readonly Font _regular = GD.Load<Font>("res://assets/Interface.ttf");
    private readonly Font _mono = GD.Load<Font>("res://assets/Telemetry.ttf");
    public string PanelKind { get; private set; } = "";
    public Dictionary<string, TouchButton> Buttons { get; } = new();
    public bool LayoutPending { get; private set; }
    public bool BlocksWorldInput => PanelKind != "" || LayoutPending;
    private readonly List<TouchButton> _weaponButtons = new(3);
    private readonly List<Label> _upgradeCosts = new(3), _upgradeLevels = new(3), _statValues = new(5);
    private Label? _creditLabel, _energyLabel, _rateLabel, _recoveredLabel, _progressLabel;
    private TouchButton? _boostButton;
    private Panel? _toastPanel;
    private Label? _toastTitle, _toastSubtitle;
    private float _toastLeft;
    private string _noticeTitle = "", _noticeSubtitle = "";
    private Color _noticeColor = Mint;
    private bool _resetConfirmation, _buildingModal;
    private Tween? _layoutTween;
    private int _lastWeapon = -1;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Theme = new Theme { DefaultFont = _regular, DefaultFontSize = 15 };
        Rebuild();
    }

    private static StyleBoxFlat Style(Color fill, Color edge, int line = 1)
    {
        var style = new StyleBoxFlat { BgColor = fill, BorderColor = edge, ContentMarginLeft = 12, ContentMarginRight = 12 };
        style.SetBorderWidthAll(line);
        style.SetCornerRadiusAll(3);
        return style;
    }

    private Label Label(Node parent, string text, Rect2 rect, int size = 14, Color? color = null, Font? font = null)
    {
        var label = new Label { Text = text, Position = rect.Position, Size = rect.Size, VerticalAlignment = VerticalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color ?? Ink);
        label.AddThemeFontOverride("font", font ?? _regular);
        parent.AddChild(label);
        return label;
    }

    private TouchButton Button(Node parent, string id, string text, Rect2 rect, Action callback, int size = 14, Color? accent = null)
    {
        bool modalButton = _buildingModal;
        var button = new TouchButton
        {
            Text = text, FocusMode = FocusModeEnum.None, Position = rect.Position, Size = rect.Size,
            MouseDefaultCursorShape = CursorShape.PointingHand,
            CanActivate = () => !LayoutPending && (modalButton || PanelKind == "")
        };
        button.AddThemeFontOverride("font", _mono);
        button.AddThemeFontSizeOverride("font_size", size);
        button.AddThemeColorOverride("font_color", accent ?? Ink);
        button.AddThemeColorOverride("font_hover_color", Ink);
        button.AddThemeColorOverride("font_pressed_color", Mint);
        button.AddThemeStyleboxOverride("normal", Style(PanelColor, Edge));
        button.AddThemeStyleboxOverride("hover", Style(new Color("172a31"), Mint));
        button.AddThemeStyleboxOverride("pressed", Style(new Color("25463f"), Mint));
        button.Pressed += () => { if (button.CanActivate()) callback(); };
        parent.AddChild(button);
        Buttons[id] = button;
        return button;
    }

    private static Panel Panel(Node parent, Rect2 rect)
    {
        var panel = new Panel { Position = rect.Position, Size = rect.Size, MouseFilter = MouseFilterEnum.Ignore };
        panel.AddThemeStyleboxOverride("panel", Style(PanelColor, Edge));
        parent.AddChild(panel);
        return panel;
    }

    public void CancelTouches()
    {
        foreach (var button in Buttons.Values) button.CancelTouch();
    }

    public void RequestLayout()
    {
        CancelTouches();
        if (LayoutPending) return;
        LayoutPending = true;
        Callable.From(Rebuild).CallDeferred();
    }

    public void Rebuild()
    {
        LayoutPending = false;
        _layoutTween?.Kill();
        foreach (var child in GetChildren()) { RemoveChild(child); child.QueueFree(); }
        Buttons.Clear(); _weaponButtons.Clear(); _upgradeCosts.Clear(); _upgradeLevels.Clear(); _statValues.Clear();
        _creditLabel = _energyLabel = _rateLabel = _recoveredLabel = _progressLabel = null;
        _boostButton = null;
        _lastWeapon = -1;
        var v = GetViewportRect().Size;
        _buildingModal = false;
        if (Game.AutoMode) BuildAuto(v); else BuildManual(v);
        BuildToast(v);
        if (PanelKind != "") BuildModal(v);
        Refresh();
        if (Game.TransitionLeft > 0 && PanelKind == "")
        {
            Modulate = new Color(1, 1, 1, 0.2f);
            _layoutTween = CreateTween();
            _layoutTween.TweenProperty(this, "modulate:a", 1, 0.28);
        }
        else Modulate = Colors.White;
    }

    private void BuildAuto(Vector2 v)
    {
        Label(this, "STARFALL", new Rect2(24, 22, v.X - 116, 34), 24, Ink, _mono);
        Label(this, $"SECTOR {Game.Sector:00}  /  {Game.Planet.Profile.Name}", new Rect2(24, 61, v.X - 116, 24), 11, Mint, _mono);
        if (Game.PlanetPreview)
            Label(this, "PREVIEW  ·  F7 PREVIOUS / F8 NEXT  ·  NO SAVE", new Rect2(24, 85, v.X - 48, 17), 10, Gold, _mono);
        Button(this, "pause", "II", new Rect2(v.X - 80, 25, 56, 56), Game.TogglePause, 18);
        var cargo = Button(this, "cargo_top", "", new Rect2(24, 106, v.X - 48, 70), () => OpenPanel("cargo"));
        Label(cargo, "CREDITS", new Rect2(16, 8, 230, 18), 10, MutedInk, _mono);
        _creditLabel = Label(cargo, "0", new Rect2(16, 28, 244, 34), 27, Gold, _mono);
        Label(cargo, "ENERGY", new Rect2(v.X - 184, 8, 120, 18), 10, MutedInk, _mono);
        _energyLabel = Label(cargo, "0", new Rect2(v.X - 184, 28, 120, 34), 25, Lilac, _mono);
        _progressLabel = Label(this, "", new Rect2(24, 190, v.X - 230, 24), 11, MutedInk, _mono);
        _rateLabel = Label(this, "", new Rect2(v.X - 202, 190, 178, 24), 11, Mint, _mono);
        _rateLabel.HorizontalAlignment = HorizontalAlignment.Right;
        var mode = Button(this, "mode", "", new Rect2(24, v.Y - 188, v.X - 48, 82), () => Game.SetAuto(false), 18, Mint);
        mode.AddThemeStyleboxOverride("normal", Style(new Color("122c28"), Mint));
        Label(mode, "TAKE CONTROL   →", new Rect2(20, 11, v.X - 90, 33), 20, Mint, _mono);
        Label(mode, "MANUAL FLIGHT  /  ROTATE TO LANDSCAPE", new Rect2(20, 46, v.X - 90, 23), 10, MutedInk, _mono);
        float dockWidth = (v.X - 64) * 0.5f;
        Button(this, "systems", "SHIP SYSTEMS", new Rect2(24, v.Y - 88, dockWidth, 62), () => OpenPanel("systems"), 13);
        Button(this, "cargo", "CARGO LOG", new Rect2(40 + dockWidth, v.Y - 88, dockWidth, 62), () => OpenPanel("cargo"), 13);
    }

    private void BuildManual(Vector2 v)
    {
        Button(this, "mode", "←  AUTO PILOT", new Rect2(24, 18, 190, 62), () => Game.SetAuto(true), 15, Mint);
        if (v.X > 830) Label(this, $"{Game.Planet.Profile.Name}", new Rect2(238, 25, 196, 42), 12, MutedInk, _mono);
        if (Game.PlanetPreview)
            Label(this, "PREVIEW  ·  F7 / F8  ·  NO SAVE", new Rect2(238, 72, v.X - 260, 20), 10, Gold, _mono);
        float statsX = Mathf.Max(230, v.X - 450), statsW = Mathf.Max(210, v.X - 106 - statsX);
        var cargo = Button(this, "cargo", "", new Rect2(statsX, 18, statsW, 62), () => OpenPanel("cargo"));
        Label(cargo, "CR", new Rect2(12, 7, 40, 16), 9, MutedInk, _mono);
        _creditLabel = Label(cargo, "", new Rect2(12, 24, statsW * 0.62f, 25), 20, Gold, _mono);
        Label(cargo, "ENERGY", new Rect2(statsW * 0.68f, 7, statsW * 0.3f, 16), 9, MutedInk, _mono);
        _energyLabel = Label(cargo, "", new Rect2(statsW * 0.68f, 24, statsW * 0.3f, 25), 20, Lilac, _mono);
        Button(this, "pause", "II", new Rect2(v.X - 80, 18, 56, 62), Game.TogglePause, 17);
        string[] names = ["LANCE", "NOVA", "ION"];
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            _weaponButtons.Add(Button(this, $"weapon_{i}", names[i], new Rect2(v.X * 0.5f - 129 + 90 * i, v.Y - 88, 78, 62), () => Game.SetWeapon(index), 13, Main.WeaponColors[i]));
        }
        _recoveredLabel = Label(this, "", new Rect2(v.X * 0.5f - 204, v.Y - 124, 408, 24), 12, MutedInk, _mono);
        _recoveredLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _boostButton = Button(this, "boost", "BOOST", new Rect2(v.X - 166, v.Y - 251, 116, 62), Game.ActivateOverdrive, 13, Lilac);
        if (v.X < v.Y) Label(this, "ROTATE PHONE FOR FLIGHT", new Rect2(60, 94, v.X - 120, 28), 11, Mint, _mono).HorizontalAlignment = HorizontalAlignment.Center;
    }

    private void BuildToast(Vector2 v)
    {
        float width = Mathf.Min(v.X - 72, 460), y = Game.AutoMode ? 230 : v.Y - 184;
        _toastPanel = Panel(this, new Rect2((v.X - width) * 0.5f, y, width, 76));
        _toastPanel.ZIndex = 15;
        _toastTitle = Label(_toastPanel, _noticeTitle, new Rect2(14, 7, width - 28, 22), 12, _noticeColor, _mono);
        _toastSubtitle = Label(_toastPanel, _noticeSubtitle, new Rect2(14, 29, width - 28, 42), 11, MutedInk);
        _toastSubtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _toastPanel.Visible = _toastLeft > 0;
    }

    public void OpenPanel(string kind)
    {
        Game.ReleaseControls();
        PanelKind = kind;
        _resetConfirmation = false;
        RequestLayout();
    }
    public void ClosePanel() { PanelKind = ""; _resetConfirmation = false; RequestLayout(); }
    private void CloseAction() { if (Game.Paused) Game.TogglePause(); else ClosePanel(); }
    public void SetPauseVisible(bool value) { PanelKind = value ? "pause" : ""; _resetConfirmation = false; RequestLayout(); }

    public bool IsUiPoint(Vector2 point)
    {
        if (BlocksWorldInput) return true;
        foreach (var button in Buttons.Values)
            if (button.IsVisibleInTree() && button.GetGlobalRect().HasPoint(point)) return true;
        return false;
    }

    private void BuildModal(Vector2 v)
    {
        foreach (var button in Buttons.Values) { button.CancelTouch(); button.SetProcessInput(false); button.MouseFilter = MouseFilterEnum.Ignore; }
        var modal = new Control { Size = v, MouseFilter = MouseFilterEnum.Stop, ZIndex = 30 };
        AddChild(modal);
        modal.AddChild(new ColorRect { Size = v, Color = new Color(0.02f, 0.035f, 0.05f, 0.94f), MouseFilter = MouseFilterEnum.Ignore });
        float width = Mathf.Min(v.X - 48, 560), height = Mathf.Min(v.Y - 48, PanelKind == "systems" ? 656 : 480);
        var card = Panel(modal, new Rect2((v.X - width) * 0.5f, (v.Y - height) * 0.5f, width, height));
        _buildingModal = true;
        Label(card, PanelKind switch { "systems" => "SHIP SYSTEMS", "cargo" => "EXTRACTION LOG", _ => "FLIGHT PAUSED" }, new Rect2(22, 20, width - 110, 34), 20, Ink, _mono);
        Button(card, "close", "×", new Rect2(width - 74, 15, 52, 48), CloseAction, 23);
        if (PanelKind == "systems") BuildSystems(card, width, height);
        else if (PanelKind == "cargo") BuildCargo(card, width, height);
        else BuildPause(card, width, height);
        _buildingModal = false;
    }

    private void BuildSystems(Panel card, float width, float height)
    {
        bool compact = height < 600;
        Label(card, "LOADOUT", new Rect2(22, 75, width - 44, 24), 11, MutedInk, _mono);
        float slotW = (width - 64) / 3;
        string[] names = ["PHOTON", "NOVA", "ION"], modes = ["PIERCE", "BLAST", "CHAIN"];
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            var button = Button(card, $"weapon_{i}", "", new Rect2(22 + i * (slotW + 10), 108, slotW, compact ? 72 : 90), () => Game.SetWeapon(index), 12);
            Label(button, names[i], new Rect2(12, 12, slotW - 24, 23), 13, Main.WeaponColors[i], _mono);
            Label(button, $"{modes[i]}  /  MK {Game.WeaponTiers[i] + 1}", new Rect2(12, 39, slotW - 24, 20), 10, MutedInk, _mono);
            _weaponButtons.Add(button);
        }
        float titleY = compact ? 190 : 220, rowHeight = compact ? 58 : 82, top = titleY + 34;
        Label(card, "UPGRADES", new Rect2(22, titleY, width - 44, 24), 11, MutedInk, _mono);
        string[] upgrades = ["FIREPOWER", "FIRE RATE", "ORE VALUE"];
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            var button = Button(card, $"upgrade_{i}", "", new Rect2(22, top + i * (rowHeight + 12), width - 44, rowHeight), () => Game.BuyUpgrade(index));
            Label(button, upgrades[i], new Rect2(14, 8, width - 210, 25), 14, Ink, _mono);
            _upgradeLevels.Add(Label(button, "", new Rect2(14, 32, width - 210, 22), 10, MutedInk, _mono));
            _upgradeCosts.Add(Label(button, "", new Rect2(width - 188, 8, 126, rowHeight - 16), 17, Gold, _mono));
        }
        Button(card, "done", "BACK TO MINING", new Rect2(22, height - 70, width - 44, 48), ClosePanel, 13, Mint);
    }

    private void BuildCargo(Panel card, float width, float height)
    {
        Label(card, "RECOVERED BY THE SHIP", new Rect2(22, 76, width - 44, 22), 11, Mint, _mono);
        string[] labels = ["THIS FLIGHT / CREDITS", "THIS FLIGHT / ENERGY", "BLOCKS MINED", "LIFETIME / CREDITS", "LIFETIME / ENERGY", "IRON FROM SECTORS"];
        for (int i = 0; i < labels.Length; i++)
        {
            float y = 108 + i * 48;
            Label(card, labels[i], new Rect2(22, y, width - 196, 31), 11, MutedInk, _mono);
            var value = Label(card, "", new Rect2(width - 178, y, 154, 31), 21, i is 0 or 3 ? Gold : i is 1 or 4 ? Lilac : Ink, _mono);
            value.HorizontalAlignment = HorizontalAlignment.Right;
            _statValues.Add(value);
        }
        Button(card, "done", "CLOSE LOG", new Rect2(22, height - 70, width - 44, 48), ClosePanel, 13, Mint);
    }

    private void BuildPause(Panel card, float width, float height)
    {
        const string guide = "AUTO  /  Mines and collects on its own.\nMANUAL  /  Left thumb steers, right thumb fires.\n\nGold pays credits. Blue ore powers the boost.\nDamaged blocks grow brighter before breaking.\n\nDesktop: WASD + Space · 1/2/3 weapons · Tab mode";
        var label = Label(card, guide, new Rect2(22, 90, width - 44, height - 282), 13, MutedInk);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.VerticalAlignment = VerticalAlignment.Top;
        Button(card, "resume", "RESUME", new Rect2(22, height - 176, width - 44, 54), Game.TogglePause, 15, Mint);
        float half = (width - 56) * 0.5f;
        Button(card, "sound", Game.Muted ? "SOUND ON" : "SOUND OFF", new Rect2(22, height - 104, half, 54), () => { Game.ToggleSound(); RequestLayout(); }, 11);
        Button(card, "reset", "RESET DEMO", new Rect2(34 + half, height - 104, half, 54), ResetAction, 11);
    }

    private void ResetAction()
    {
        if (!_resetConfirmation)
        {
            _resetConfirmation = true;
            Buttons["reset"].Text = "TAP TO CONFIRM";
            Buttons["reset"].AddThemeColorOverride("font_color", new Color("f2a998"));
        }
        else Game.ResetProgress();
    }

    public void Refresh()
    {
        if (_creditLabel != null) _creditLabel.Text = Main.Compact(Game.Credits);
        if (_energyLabel != null) _energyLabel.Text = Main.Compact(Game.Flux);
        if (_rateLabel != null) _rateLabel.Text = (Game.IncomeRate < 1000 ? Game.IncomeRate.ToString("0.0", CultureInfo.InvariantCulture) : Main.Compact(Game.IncomeRate)) + " CR / S";
        if (_progressLabel != null) _progressLabel.Text = $"{(int)(Game.Planet.Progress * 100)}% EXTRACTED / {Game.Sector:00}";
        if (_recoveredLabel != null) _recoveredLabel.Text = $"RECOVERED  +{Main.Compact(Game.SessionCredits)} CR  /  +{Main.Compact(Game.SessionEnergy)} E";
        if (_lastWeapon != Game.Weapon)
        {
            for (int i = 0; i < _weaponButtons.Count; i++)
                _weaponButtons[i].AddThemeStyleboxOverride("normal", Style(Game.Weapon == i ? new Color("172d31") : PanelColor, Game.Weapon == i ? Main.WeaponColors[i] : Edge));
            _lastWeapon = Game.Weapon;
        }
        for (int i = 0; i < _upgradeCosts.Count; i++)
        {
            double cost = Game.UpgradeCost(i);
            _upgradeCosts[i].Text = Main.Compact(cost) + " CR";
            _upgradeCosts[i].AddThemeColorOverride("font_color", Game.Credits >= cost ? Gold : MutedInk);
            _upgradeLevels[i].Text = $"LV {Game.Levels[i]} / {UpgradeBonuses[i]}";
        }
        if (_statValues.Count == 6)
        {
            _statValues[0].Text = Main.Compact(Game.SessionCredits);
            _statValues[1].Text = Main.Compact(Game.SessionEnergy);
            _statValues[2].Text = Main.Compact(Game.TotalMined);
            _statValues[3].Text = Main.Compact(Game.TotalEarned);
            _statValues[4].Text = Main.Compact(Game.LifetimeEnergy);
            _statValues[5].Text = Main.Compact(Game.Iron);
        }
        if (_boostButton != null) _boostButton.Text = Game.OverdriveLeft > 0 ? $"BOOST {Mathf.CeilToInt(Game.OverdriveLeft)}s" : Game.OverdriveCooldown > 0 ? $"{Mathf.CeilToInt(Game.OverdriveCooldown)}s" : "BOOST / 5 E";
        QueueRedraw();
    }

    public void ShowToast(string title, string subtitle, Color color, float duration)
    {
        _noticeTitle = title; _noticeSubtitle = subtitle; _noticeColor = color; _toastLeft = duration;
        if (_toastTitle == null) return;
        _toastTitle.Text = title;
        _toastSubtitle!.Text = subtitle;
        _toastTitle.AddThemeColorOverride("font_color", color);
        _toastPanel!.Visible = true;
    }

    public override void _Process(double delta)
    {
        if (_toastPanel != null)
        {
            _toastLeft = Mathf.Max(0, _toastLeft - (float)delta);
            _toastPanel.Visible = _toastLeft > 0 && PanelKind == "";
            _toastPanel.Modulate = new Color(1, 1, 1, Mathf.Min(1, _toastLeft * 4));
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        var v = GetViewportRect().Size;
        if (Game.AutoMode)
        {
            DrawRect(new Rect2(24, 222, v.X - 48, 2), Edge);
            DrawRect(new Rect2(24, 222, (v.X - 48) * Game.Planet.Progress, 2), Mint);
            return;
        }
        var controls = Game.Controls;
        var origin = controls.JoystickActive ? controls.JoystickOrigin : controls.StickCenter;
        var stick = controls.JoystickActive ? origin + (controls.JoystickPosition - origin).LimitLength(56) : origin;
        DrawArc(origin, 62, 0, Mathf.Tau, 48, new Color(0.48f, 0.73f, 0.73f, 0.24f), 1.2f, true);
        DrawCircle(stick, 24, new Color(0.48f, 0.73f, 0.73f, 0.09f));
        DrawArc(stick, 24, 0, Mathf.Tau, 32, new Color(0.59f, 0.86f, 0.8f, 0.6f), 1.2f, true);
        var fire = controls.FireCenter;
        var color = Main.WeaponColors[Game.Weapon];
        DrawCircle(fire, 52, new Color(color, controls.FireHeld ? 0.1f : 0.035f));
        DrawArc(fire, 52, 0, Mathf.Tau, 48, new Color(color, 0.8f), 1.5f, true);
        DrawString(_mono, fire + new Vector2(-18, 5), "FIRE", HorizontalAlignment.Left, -1, 15, color);
    }
}
