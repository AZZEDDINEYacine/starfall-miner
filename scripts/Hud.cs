using Godot;
using System.Globalization;

namespace StarfallMiner;

public partial class Hud : Control
{
    private VisualPalette Palette => Game.Palette;
    private Color Ink => Palette.Ink;
    private Color MutedInk => Palette.MutedInk;
    private Color Mint => Palette.Mint;
    private Color Gold => Palette.Gold;
    private Color Lilac => Palette.Lilac;
    private Color PanelColor => Palette.PanelColor;
    private Color Edge => Palette.Edge;
    private static readonly string[] UpgradeBonuses = ["+46% DAMAGE", "+18% SPEED", "+4% YIELD"];
    public Main Game { get; set; } = null!;
    [Export] public Font RegularFont { get; set; } = null!;
    [Export] public Font MonoFont { get; set; } = null!;
    [Export] public PackedScene ButtonScene { get; set; } = null!;
    [Export] public PackedScene PanelScene { get; set; } = null!;
    [Export] public ModeTransitionStyle Motion { get; set; } = null!;
    private Control? _content;
    [Export] public PackedScene ChannelScene { get; set; } = null!;
    [Export] public HologramStyle Hologram { get; set; } = null!;
    [Export] public Texture2D Atmosphere { get; set; } = null!;
    public List<HologramChannel> Channels { get; } = new(8);
    private bool _modePending;
    private Label? _speedLabel, _headingLabel, _flightLabel;
    private HologramChannel? _telemetry;
    public SurveyMinimap? Minimap { get; private set; }
    public bool ActivationRunning => Channels.Exists(c => c.Igniting);
    public float ActivationDuration => Motion.ActivationDuration;
    private Font _regular => RegularFont;
    private Font _mono => MonoFont;
    public string PanelKind { get; private set; } = "";
    public bool CanShowToast => _toastTitle != null;
    public Dictionary<string, TouchButton> Buttons { get; } = new();
    public bool LayoutPending { get; private set; }
    public bool BlocksWorldInput => PanelKind != "" || LayoutPending;
    private readonly List<TouchButton> _weaponButtons = new(3);
    private readonly List<TouchButton> _designButtons = new(9);
    private readonly List<Label> _oreLegend = new(5);
    private Label? _designCaption;
    private readonly List<Label> _upgradeCosts = new(3), _upgradeLevels = new(3), _statValues = new(5);
    private readonly List<int> _upgradeIndices = new(3);
    private Label? _creditLabel, _energyLabel, _rateLabel, _recoveredLabel, _progressLabel;
    private TouchButton? _boostButton;
    private Panel? _toastPanel;
    private Label? _toastTitle, _toastSubtitle;
    private float _toastLeft;
    private string _noticeTitle = "", _noticeSubtitle = "";
    private Color _noticeColor;
    private bool _resetConfirmation, _buildingModal;
    private int _lastWeapon = -1;
    private bool _modalIgnitionPending;
    private bool _fabricateDrones, _fleetDrones;
    private int _fleetPage;
    private Label? _equipmentStatus;
    private Label? _workshopCurrency, _workshopMaterials;

    public void RefreshInventory() { _modalIgnitionPending = false; RequestLayout(); }
    public void OpenWeaponRecipes() { _fabricateDrones = false; OpenPanel("fabricator"); }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ApplyPaletteToTheme();
        // Main builds the HUD after loading the simulation; the scene owns our theme and prefabs.
    }

    // Theme margins and native state tints sit over Hologram's shared glass artwork.
    // Duplicate before modifying it so the authored .tres is never changed at runtime.
    private void ApplyPaletteToTheme()
    {
        Theme = (Theme)Theme.Duplicate(true);
        SetThemeStyleColors("Button", "normal", Palette.PanelColor, Palette.Edge);
        SetThemeStyleColors("Button", "hover", Palette.ButtonHover, Palette.Mint);
        SetThemeStyleColors("Button", "pressed", Palette.ButtonPressed, Palette.Mint);
        SetThemeStyleColors("Button", "hover_pressed", Palette.ButtonPressed, Palette.Mint);
        SetThemeStyleColors("Button", "disabled", Palette.PanelColor, Hologram.Disabled.Darkened(0.7f));
        SetThemeStyleColors("Button", "focus", Colors.Transparent, Hologram.Focus);
        SetThemeStyleColors("Panel", "panel", Palette.PanelColor, Palette.Edge);
        Theme.SetColor("font_color", "Button", Palette.Ink);
        Theme.SetColor("font_hover_color", "Button", Palette.Ink);
        Theme.SetColor("font_pressed_color", "Button", Palette.Mint);
        Theme.SetColor("font_disabled_color", "Button", Hologram.Disabled);
        Theme.SetColor("font_focus_color", "Button", Hologram.Text);
    }

    private void SetThemeStyleColors(string control, string state, Color background, Color border)
    {
        var style = (StyleBoxFlat)Theme.GetStylebox(state, control).Duplicate();
        // Nine-slice glass draws beneath native controls; keep native text/focus
        // and the palette's state tint without covering the gradient surface.
        style.BgColor = new Color(background, control == "Button" ? 0.08f : 0.04f);
        style.BorderColor = border;
        Theme.SetStylebox(state, control, style);
    }

    private static StyleBoxFlat Style(Color fill, Color edge, int line = 1)
    {
        var style = new StyleBoxFlat { BgColor = fill, BorderColor = edge, ContentMarginLeft = 12, ContentMarginRight = 12 };
        style.SetBorderWidthAll(line);
        style.SetCornerRadiusAll(10);
        return style;
    }

    private Label Label(Node parent, string text, Rect2 rect, int size = 14, Color? color = null, Font? font = null)
    {
        var label = new Label { ClipText = true, Text = text, Position = rect.Position, Size = rect.Size, VerticalAlignment = VerticalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", FittedFontSize(font ?? _regular, text, rect.Size.X, size));
        label.AddThemeColorOverride("font_color", color ?? Ink);
        label.AddThemeFontOverride("font", font ?? _regular);
        label.AddThemeColorOverride("font_shadow_color", new Color(Hologram.Shadow, 0.95f));
        label.AddThemeConstantOverride("shadow_offset_x", 0);
        label.AddThemeConstantOverride("shadow_offset_y", 1);
        label.AddThemeConstantOverride("shadow_outline_size", 0);
        label.AddThemeConstantOverride("outline_size", 0);
        parent.AddChild(label);
        label.Size = rect.Size;
        return label;
    }

    private static int FittedFontSize(Font font, string text, float width, int requested)
    {
        if (text.Length == 0) return requested;
        float measured = font.GetMultilineStringSize(text, HorizontalAlignment.Left, -1, requested).X;
        return measured <= width ? requested : Math.Clamp((int)Mathf.Floor(requested * width / Mathf.Max(1, measured)), Math.Min(9, requested), requested);
    }

    private TouchButton Button(Node parent, string id, string text, Rect2 rect, Action callback, int size = 14, Color? accent = null)
    {
        bool modalButton = _buildingModal;
        var button = ButtonScene.Instantiate<TouchButton>();
        button.Text = text;
        button.Position = rect.Position;
        button.Size = rect.Size;
        button.ClipText = true;
        // Tab keeps its existing flight-mode shortcut. Inside a modal it can focus
        // controls without handing a key through to world input.
        button.FocusMode = modalButton ? FocusModeEnum.All : FocusModeEnum.None;
        button.CanActivate = () => !LayoutPending && (modalButton || PanelKind == "");
        button.AddThemeFontOverride("font", _mono);
        button.AddThemeFontSizeOverride("font_size", FittedFontSize(_mono, text, rect.Size.X - 20, size));
        button.AddThemeColorOverride("font_color", accent ?? Ink);
        HoloButton(button, accent ?? Hologram.Cyan);
        button.Pressed += () => { if (button.CanActivate()) callback(); };
        parent.AddChild(button);
        Buttons[id] = button;
        return button;
    }

    private Panel Panel(Node parent, Rect2 rect)
    {
        var panel = PanelScene.Instantiate<Panel>();
        panel.Position = rect.Position;
        panel.Size = rect.Size;
        parent.AddChild(panel);
        panel.AddChild(new GlassSurface { Ink = Hologram, Nebula = Palette.NebulaArtwork });
        return panel;
    }

    public void CancelTouches()
    {
        foreach (var button in Buttons.Values) button.CancelTouch();
    }

    private void HoloButton(TouchButton button, Color accent, bool transparent = false)
    {
        button.Holographic = true;
        button.TelemetryLink = transparent;
        button.Accent = accent;
        button.Ink = Hologram;
        if (transparent)
            foreach (string state in new[] { "normal", "hover", "pressed", "hover_pressed", "focus", "disabled" })
                button.AddThemeStyleboxOverride(state, new StyleBoxEmpty());
        button.AddThemeColorOverride("font_color", Hologram.Text);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", Colors.White);
        button.AddThemeColorOverride("font_disabled_color", Hologram.Disabled);
        button.AddThemeConstantOverride("outline_size", 0);
    }

    private HologramChannel Channel(string name, Rect2 rect, HologramChannel.Geometry geometry, float order)
    {
        var channel = ChannelScene.Instantiate<HologramChannel>();
        channel.Name = name;
        channel.Position = rect.Position;
        channel.Size = rect.Size;
        channel.Decoration = geometry;
        channel.Order = order;
        _content!.AddChild(channel);
        Channels.Add(channel);
        return channel;
    }

    public void BeginModeTransition(bool toAuto) => _modePending = true;

    public void RequestLayout()
    {
        CancelTouches();
        if (LayoutPending) return;
        LayoutPending = true;
        Callable.From(Rebuild).CallDeferred();
    }

    public void Rebuild()
    {
        bool animate = _content == null || _modePending;
        LayoutPending = false;
        _modePending = false;
        CancelTouches();
        if (_content != null)
        {
            if (_workshopScroll != null) _workshopScrollOffsets[_workshopScrollIndex] = _workshopScroll.ScrollVertical;
            _workshopScroll = null;
            RemoveChild(_content); // Old controls leave the input tree immediately.
            _content.QueueFree();
        }
        _content = new Control { Size = GetViewportRect().Size, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_content);
        Channels.Clear();
        Buttons.Clear(); _weaponButtons.Clear(); _upgradeCosts.Clear(); _upgradeLevels.Clear(); _statValues.Clear();
        _upgradeIndices.Clear();
        _designButtons.Clear(); _oreLegend.Clear(); _designCaption = null;
        _equipmentStatus = null; _shipSchematic = null;
        _workshopCurrency = _workshopMaterials = null;
        _creditLabel = _energyLabel = _rateLabel = _recoveredLabel = _progressLabel = null;
        _speedLabel = _headingLabel = _flightLabel = null;
        _telemetry = null; Minimap = null;
        _boostButton = null;
        _lastWeapon = -1;
        var v = GetViewportRect().Size;
        _content.AddChild(new TextureRect { Texture = Atmosphere, Size = v,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, MouseFilter = MouseFilterEnum.Ignore });
        _content.AddChild(new ObservatoryFrame { Ink = Hologram, MouseFilter = MouseFilterEnum.Ignore });
        _buildingModal = false;
        BuildFlightHeader(v);
        if (Game.View.LayoutAutoMode) BuildAuto(v); else BuildManual(v);
        float mapSize = Mathf.Clamp((v.Y - 464) * 0.22f, 80, 112);
        var survey = Channel("Survey", new Rect2(v.X - 24 - mapSize, 232, mapSize, mapSize), HologramChannel.Geometry.None, 0.35f);
        Minimap = new SurveyMinimap { Game = Game, Font = _mono, Ink = Hologram, Size = survey.Size };
        survey.AddChild(Minimap);
        BuildToast(v);
        if (PanelKind != "") BuildModal(v);
        Refresh();
        foreach (var channel in Channels) channel.Activate(animate || channel.Name == "Modal" && _modalIgnitionPending);
        _modalIgnitionPending = false;
        Modulate = Colors.White;
    }

    private void BuildFlightHeader(Vector2 v)
    {
        bool wide = v.X >= 1000;
        float width = wide ? 400 : v.X - 48;
        var header = Channel("Identity", new Rect2(24, 18, width, 66), HologramChannel.Geometry.Header, 0);
        Label(header, "Starfall Miner", new Rect2(45, 0, width - 118, 30), 24, Ink, _mono);
        Label(header, $"Mining vessel  /  Sector {Game.Sector:00}", new Rect2(45, 32, width - 118, 20), 11, Hologram.Dim);
        Button(header, "pause", "Ⅱ", new Rect2(width - 54, 0, 54, 52), Game.TogglePause, 17, Hologram.Cyan);

        float telemetryWidth = wide ? 460 : v.X - 48;
        _telemetry = Channel("Telemetry", new Rect2(wide ? v.X - 24 - telemetryWidth : 24, wide ? 24 : 96, telemetryWidth, 110), HologramChannel.Geometry.Telemetry, 0.22f);
        string cargoId = Game.View.LayoutAutoMode ? "cargo_top" : "cargo";
        var cargo = Button(_telemetry, cargoId, "", new Rect2(0, 0, telemetryWidth, 84), () => OpenPanel("cargo"));
        HoloButton(cargo, Hologram.Cyan, true);
        Label(cargo, "Extracted capital · CR  ›", new Rect2(14, 0, telemetryWidth * 0.6f, 20), 11, Hologram.Dim);
        _creditLabel = Label(cargo, "", new Rect2(12, 20, telemetryWidth * 0.61f, 51), 40, Ink, _mono);
        float reactor = telemetryWidth * 0.66f;
        Label(cargo, "Reactor · Flux", new Rect2(reactor, 0, telemetryWidth * 0.34f, 20), 11, Hologram.Amber);
        _energyLabel = Label(cargo, "", new Rect2(reactor, 24, telemetryWidth * 0.34f, 38), 27, Hologram.Amber, _mono);
        _progressLabel = Label(_telemetry, "", new Rect2(12, 88, telemetryWidth * 0.5f, 20), 11, Hologram.Dim);
        _rateLabel = Label(_telemetry, "", new Rect2(telemetryWidth * 0.57f, 88, telemetryWidth * 0.43f, 20), 11, Hologram.Cyan);
        _rateLabel.HorizontalAlignment = HorizontalAlignment.Right;
    }

    private void BuildAuto(Vector2 v)
    {
        float width = Mathf.Min(v.X - 48, 640);
        var command = Channel("AutopilotCommand", new Rect2((v.X - width) * 0.5f, v.Y - 188, width, 162), HologramChannel.Geometry.Command, 1);
        var mode = Button(command, "mode", "", new Rect2(0, 0, width, 78), () => Game.SetAuto(false), 18, Hologram.Cyan);
        Label(mode, "Take control   ›", new Rect2(18, 9, width - 36, 30), 21, Hologram.Cyan, _mono);
        Label(mode, "Hold + drag · 2.4× ship mining output", new Rect2(18, 43, width - 36, 23), 12, Hologram.Dim);
        float half = (width - 16) * 0.5f;
        Button(command, "systems", "Ship systems", new Rect2(0, 100, half, 62), () => OpenPanel("systems"), 14);
        Button(command, "cargo", "Extraction log", new Rect2(half + 16, 100, half, 62), () => OpenPanel("cargo"), 14);
    }

    private void BuildManual(Vector2 v)
    {
        var nav = Channel("Navigation", new Rect2(0, 230, v.X, v.Y - 464), HologramChannel.Geometry.Navigation, 0.5f);
        Label(nav, "VEL", new Rect2(28, nav.Size.Y * 0.28f, 54, 18), 8, Hologram.Dim, _mono);
        _speedLabel = Label(nav, "", new Rect2(28, nav.Size.Y * 0.28f + 18, 65, 28), 16, Hologram.Cyan, _mono);
        Label(nav, "HDG", new Rect2(v.X - 85, nav.Size.Y * 0.28f, 62, 18), 8, Hologram.Dim, _mono);
        _headingLabel = Label(nav, "", new Rect2(v.X - 85, nav.Size.Y * 0.28f + 18, 65, 28), 16, Hologram.Cyan, _mono);
        _flightLabel = Label(nav, "", new Rect2(28, nav.Size.Y - 34, v.X - 56, 22), 9, Hologram.Dim, _mono);
        _flightLabel.HorizontalAlignment = HorizontalAlignment.Center;

        float width = Mathf.Min(v.X - 48, 640), left = (v.X - width) * 0.5f;
        var recovery = Channel("Recovery", new Rect2(left, v.Y - 222, width, 54), HologramChannel.Geometry.Recovery, 0.7f);
        Label(recovery, "SESSION / RECOVERED", new Rect2(0, 10, 182, 17), 8, Hologram.Dim, _mono);
        _recoveredLabel = Label(recovery, "", new Rect2(0, 27, width, 24), 12, Hologram.Cyan, _mono);

        var loadout = Channel("Loadout", new Rect2(left, v.Y - 156, width, 52), HologramChannel.Geometry.Loadout, 0.85f);
        float slot = (width - 24) / 4;
        string[] names = ["01 LANCE", "02 NOVA", "03 ION"];
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            _weaponButtons.Add(Button(loadout, $"weapon_{i}", names[i],
                new Rect2(i * (slot + 8), 0, slot, 52), () => Game.SetWeapon(index == 2 && Game.Weapon >= 2 ? Game.Weapon : index), 11, Palette.Weapon(i)));
        }
        _boostButton = Button(loadout, "boost", "BOOST", new Rect2(3 * (slot + 8), 0, slot, 52),
            Game.ActivateOverdrive, 11, Hologram.Amber);
        var command = Channel("ManualCommand", new Rect2(left, v.Y - 88, width, 62), HologramChannel.Geometry.Command, 1);
        Button(command, "mode", "‹  Return to autopilot", new Rect2(0, 0, width, 62), () => Game.SetAuto(true), 14, Hologram.Cyan);
    }

    private void BuildToast(Vector2 v)
    {
        float width = Mathf.Min(v.X - 72, 460), y = 236;
        _toastPanel = Panel(_content!, new Rect2((v.X - width) * 0.5f, y, width, 62));
        _toastPanel.ZIndex = 15;
        var noticeStyle = Style(new Color(Hologram.Shadow, 0.9f), Hologram.Cyan, 0);
        noticeStyle.BorderWidthLeft = 2;
        _toastPanel.AddThemeStyleboxOverride("panel", noticeStyle);
        _toastTitle = Label(_toastPanel, _noticeTitle, new Rect2(14, 7, width - 28, 22), 12, _noticeColor, _mono);
        _toastSubtitle = Label(_toastPanel, _noticeSubtitle, new Rect2(14, 30, width - 28, 25), 11, MutedInk);
        _toastSubtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _toastPanel.Visible = _toastLeft > 0;
    }

    public void OpenPanel(string kind)
    {
        if (PanelKind == kind) return;
        Game.ReleaseControls();
        // The four tabs share a single workshop visit, not four power-on events.
        _modalIgnitionPending = !(IsWorkshop(PanelKind) && IsWorkshop(kind));
        PanelKind = kind;
        _resetConfirmation = false;
        RequestLayout();
    }
    public void ClosePanel() { PanelKind = ""; _modalIgnitionPending = false; _resetConfirmation = false; RequestLayout(); }
    private void CloseAction()
    {
        if (PanelKind == "designs") OpenPanel("pause");
        else if (Game.Paused && IsWorkshop(PanelKind)) OpenPanel("pause");
        else if (Game.Paused) Game.TogglePause();
        else ClosePanel();
    }
    public void SetPauseVisible(bool value)
    {
        _modalIgnitionPending = value && PanelKind != "pause";
        PanelKind = value ? "pause" : "";
        _resetConfirmation = false;
        RequestLayout();
    }

    public bool IsWorldPoint(Vector2 point) => new Rect2(0, 230, Game.View.Size.X, Mathf.Max(0, Game.View.Size.Y - 464)).HasPoint(point);

    public bool IsUiPoint(Vector2 point)
    {
        if (BlocksWorldInput) return true;
        if (Minimap?.GetGlobalRect().HasPoint(point) == true) return true;
        foreach (var button in Buttons.Values)
            if (button.IsVisibleInTree() && button.GetGlobalRect().HasPoint(point)) return true;
        return false;
    }

    private void BuildModal(Vector2 v)
    {
        foreach (var button in Buttons.Values) { button.CancelTouch(); button.SetProcessInput(false); button.MouseFilter = MouseFilterEnum.Ignore; }
        var modal = new Control { Size = v, MouseFilter = MouseFilterEnum.Stop, ZIndex = 30 };
        _content!.AddChild(modal);
        bool comparing = PanelKind == "designs";
        modal.AddChild(new ColorRect { Size = v, Color = new Color(Palette.ModalScrim, comparing ? 0.08f : 0.76f), MouseFilter = MouseFilterEnum.Ignore });
        float width = Mathf.Min(v.X - 48, IsWorkshop(PanelKind) && v.X >= 1000 ? 1080 : 560);
        float height = Mathf.Min(v.Y - 32, comparing ? 300 : IsWorkshop(PanelKind) ? 820 : PanelKind == "pause" ? 536 : 480);
        float top = comparing ? v.Y - height - 20 : (v.Y - height) * 0.5f;
        var popup = Channel("Modal", new Rect2((v.X - width) * 0.5f, top, width, height), HologramChannel.Geometry.None, 0);
        popup.ZIndex = 31;
        var card = Panel(popup, new Rect2(0, 0, width, height));
        _buildingModal = true;
        Label(card, PanelKind switch { "systems" => "Ship drydock", "fabricator" => "Fabricator", "fleet" => "Fleet loadout", "market" => "Intergalactic market", "cargo" => "Extraction log", "designs" => "World designs", _ => "Flight paused" }, new Rect2(22, 20, width - 110, 34), comparing ? 20 : 23, Ink, _mono);
        Button(card, "close", "×", new Rect2(width - 74, 15, 52, 48), CloseAction, 23);
        if (IsWorkshop(PanelKind)) BuildWorkshop(card, width, height);
        else if (PanelKind == "cargo") BuildCargo(card, width, height);
        else if (comparing) BuildDesigns(card, width, height);
        else BuildPause(card, width, height);
        _buildingModal = false;
    }

    private void BuildCargo(Panel card, float width, float height)
    {
        Label(card, "Recovered by the ship", new Rect2(22, 76, width - 44, 22), 12, Mint);
        string[] labels = ["This flight · Credits", "This flight · Energy", "Blocks mined", "Lifetime · Credits", "Lifetime · Energy"];
        for (int i = 0; i < labels.Length; i++)
        {
            float y = 118 + i * 52;
            Label(card, labels[i], new Rect2(22, y, width - 196, 31), 13, MutedInk);
            var value = Label(card, "", new Rect2(width - 178, y, 154, 31), 21, i is 0 or 3 ? Gold : i is 1 or 4 ? Lilac : Ink, _mono);
            value.HorizontalAlignment = HorizontalAlignment.Right;
            _statValues.Add(value);
        }
        Button(card, "done", "Close log", new Rect2(22, height - 70, width - 44, 48), ClosePanel, 14, Mint);
    }

    private void BuildPause(Panel card, float width, float height)
    {
        const string guide = "Hold + drag to steer. Release to stop.\nThe ship mines blocking rocks from a safe distance.\nDesktop: hold left mouse · 1/2/3 weapons · E boost";
        var label = Label(card, guide, new Rect2(22, 82, width - 44, 54), 11, MutedInk);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.VerticalAlignment = VerticalAlignment.Top;
        Button(card, "workshop", "Fabricator / Fleet / Market  ›", new Rect2(22, 146, width - 44, 60), () => OpenPanel("fabricator"), 14, Hologram.Cyan);
        var designs = Button(card, "designs", "", new Rect2(22, 220, width - 44, 60), () => OpenPanel("designs"));
        Label(designs, "Compare world designs  ›", new Rect2(14, 4, width - 72, 26), 14, Hologram.Cyan, _mono);
        Label(designs, Game.CurrentVisualStyle.Title, new Rect2(14, 31, width - 72, 20), 10, MutedInk, _mono);
        Button(card, "test_credits", "", new Rect2(22, height - 230, width - 44, 40), Game.GrantTestCredits, 11, Hologram.Amber);
        Button(card, "resume", "Resume", new Rect2(22, height - 176, width - 44, 54), Game.TogglePause, 16, Mint);
        float half = (width - 56) * 0.5f;
        Button(card, "sound", Game.Muted ? "Sound on" : "Sound off", new Rect2(22, height - 104, half, 54), () => { Game.ToggleSound(); RequestLayout(); }, 13);
        Button(card, "reset", "Reset demo", new Rect2(34 + half, height - 104, half, 54), ResetAction, 13, Palette.ResetWarning);
    }

    private void BuildDesigns(Panel card, float width, float height)
    {
        _designCaption = Label(card, "", new Rect2(22, 55, width - 44, 18), 9, MutedInk, _mono);
        float cell = (width - 52) * 0.5f;
        for (int row = 0; row < Game.VisualStyles.Count; row++)
        {
            int index = row;
            if (index >= Game.VisualStyles.Count) break;
            var art = Game.VisualStyles[index];
            var choice = Button(card, $"design_{index}", "", new Rect2(22 + row % 2 * (cell + 8), 84 + row / 2 * 58, cell, 50),
                () => { Game.SetVisualStyle(index); RefreshDesignPicker(); });
            HoloButton(choice, Hologram.Cyan);
            Label(choice, art.Title, new Rect2(7, 5, cell - 14, 20), 11, Hologram.Cyan, _mono);
            Label(choice, art.Description, new Rect2(7, 27, cell - 14, 16), 9, MutedInk);
            _designButtons.Add(choice);
        }
        var legend = Label(card, "COLOR-CODED CELLS / REACTOR CORES\nRED CHARGES / AREA BLAST ON IMPACT", new Rect2(22, 156, width - 44, 54), 10, Hologram.Amber, _mono);
        legend.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        float half = (width - 56) * 0.5f;
        Button(card, "back", "‹ Pause", new Rect2(22, height - 58, half, 42), () => OpenPanel("pause"), 13);
        Button(card, "resume", "Resume", new Rect2(34 + half, height - 58, half, 42), Game.TogglePause, 13, Hologram.Cyan);
        RefreshDesignPicker();
    }

    private void RefreshDesignPicker()
    {
        if (_designCaption != null) _designCaption.Text = "PAUSED PREVIEW / " + Game.CurrentVisualStyle.Title;
        for (int i = 0; i < _designButtons.Count; i++)
        {
            _designButtons[i].Selected = i == Game.VisualStyleIndex;
            _designButtons[i].QueueRedraw();
        }
    }

    private void ResetAction()
    {
        if (!_resetConfirmation)
        {
            _resetConfirmation = true;
            Buttons["reset"].Text = "Tap to confirm";
            Buttons["reset"].AddThemeColorOverride("font_color", Palette.ResetWarning);
        }
        else Game.ResetProgress();
    }

    public void Refresh()
    {
        if (Buttons.TryGetValue("test_credits", out var funds)) funds.Text = $"TEST / +1B CR   ·   WALLET {Main.Compact(Game.Credits)}";
        RefreshService();
        if (_workshopCurrency != null) _workshopCurrency.Text = $"RANK {Game.EngineeringRank}  /  {Game.Equipment.Drones.Count}/{Game.DroneBays} DRONE BAYS  /  {Main.Compact(Game.Credits)} CR";
        if (_workshopMaterials != null) _workshopMaterials.Text = $"TITANIUM {Game.Equipment.Materials[0]}    IRIDIUM {Game.Equipment.Materials[1]}    AETHER {Game.Equipment.Materials[2]}";
        if (_creditLabel != null) _creditLabel.Text = Main.Compact(Game.Credits);
        if (_energyLabel != null) _energyLabel.Text = Main.Compact(Game.Flux);
        if (_rateLabel != null) _rateLabel.Text = (Game.IncomeRate < 1000 ? Game.IncomeRate.ToString("0.0", CultureInfo.InvariantCulture) : Main.Compact(Game.IncomeRate)) + " CR / S";
        if (_progressLabel != null) _progressLabel.Text = $"EXTRACTION / {(int)(Game.Planet.Progress * 100):00}%";
        if (_recoveredLabel != null) _recoveredLabel.Text = $"+{Main.Compact(Game.SessionCredits)} CR   /   +{Main.Compact(Game.SessionEnergy)} FLUX";
        if (_speedLabel != null) _speedLabel.Text = Game.ShipVelocity.Length().ToString("000", CultureInfo.InvariantCulture);
        if (_headingLabel != null) _headingLabel.Text = $"{Mathf.PosMod(Mathf.RadToDeg(Game.ShipAngle) + 90, 360):000}°";
        if (_flightLabel != null) _flightLabel.Text = Game.Navigation.ClearingPath ? "PRECISION MINING / 2.4× SHIP OUTPUT" :
            Game.Controls.PointerId != -1 ? "VECTOR LINK / 2.4× SHIP OUTPUT WHEN MINING" : "HOLD + DRAG TO STEER / RELEASE TO STOP / 2.4× OUTPUT";
        if (_telemetry != null) _telemetry.Meter = Mathf.Clamp(Game.Flux / 5f, 0, 1);
        if (_lastWeapon != Game.Weapon)
        {
            for (int i = 0; i < _weaponButtons.Count; i++)
            {
                var button = _weaponButtons[i];
                int type = i == 2 && Game.Weapon >= 2 ? Game.Weapon : i;
                button.Selected = Game.Weapon == type;
                string[] loadoutNames = ["LANCE", "NOVA", "ION", "NOVA WAVE", "BORE", "FLUX BEAM"];
                button.Text = loadoutNames[type] + (Game.HasShipWeaponType(type) ? "" : "\nCRAFT / FIT");
                if (!button.Holographic)
                    button.AddThemeStyleboxOverride("normal", Style(Game.Weapon == i ? Palette.WeaponSelectedBackground : PanelColor, Game.Weapon == i ? Palette.Weapon(i) : Edge));
                button.QueueRedraw();
            }
            _lastWeapon = Game.Weapon;
        }
        for (int i = 0; i < _upgradeCosts.Count; i++)
        {
            int index = _upgradeIndices[i];
            double cost = Game.UpgradeCost(index);
            _upgradeCosts[i].Text = Main.Compact(cost) + " CR";
            _upgradeCosts[i].AddThemeColorOverride("font_color", Game.Credits >= cost ? Gold : MutedInk);
            _upgradeLevels[i].Text = $"LV {Game.Levels[index]} / {UpgradeBonuses[index]}";
        }
        if (_statValues.Count == 5)
        {
            _statValues[0].Text = Main.Compact(Game.SessionCredits);
            _statValues[1].Text = Main.Compact(Game.SessionEnergy);
            _statValues[2].Text = Main.Compact(Game.TotalMined);
            _statValues[3].Text = Main.Compact(Game.TotalEarned);
            _statValues[4].Text = Main.Compact(Game.LifetimeEnergy);
        }
        if (_boostButton != null) _boostButton.Text = Game.OverdriveLeft > 0 ? $"BOOST {Mathf.CeilToInt(Game.OverdriveLeft)}s" : Game.OverdriveCooldown > 0 ? $"{Mathf.CeilToInt(Game.OverdriveCooldown)}s" : "BOOST / 5 E";
        foreach (var channel in Channels) channel.QueueRedraw();
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
    }

}
