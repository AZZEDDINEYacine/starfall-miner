using Godot;

namespace StarfallMiner;

/// <summary>Compact link-status readout sharing the HUD's bounded ignition envelope.</summary>
public partial class ModeStatusOverlay : Control
{
    [Export] public HologramStyle Hologram { get; set; } = null!;
    [Export] public ModeTransitionStyle Motion { get; set; } = null!;
    private Label _title = null!, _subtitle = null!;
    private float _age;
    public bool Igniting { get; private set; }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _title = GetNode<Label>("Title");
        _subtitle = GetNode<Label>("Subtitle");
        _title.AddThemeColorOverride("font_color", Hologram.Cyan);
        _title.AddThemeColorOverride("font_outline_color", new Color(Hologram.Cyan, 0.12f));
        _title.AddThemeConstantOverride("outline_size", 2);
        _subtitle.AddThemeColorOverride("font_color", Hologram.Dim);
        GetViewport().SizeChanged += PlaceElements;
        PlaceElements();
        Visible = false;
        SetProcess(false);
    }

    public void Play(bool auto)
    {
        _title.Text = auto ? "AUTO / MINING ONLINE" : "MANUAL / LINK ESTABLISHED";
        _subtitle.Text = "SYSTEMS NOMINAL";
        Visible = true;
        Seek(0);
    }

    private void PlaceElements()
    {
        float width = GetViewportRect().Size.X - 48;
        _title.Position = new Vector2(24, 210);
        _title.Size = new Vector2(width * 0.65f, 18);
        _subtitle.Position = new Vector2(24 + width * 0.65f, 210);
        _subtitle.Size = new Vector2(width * 0.35f, 18);
    }

    public void Seek(float age)
    {
        _age = Mathf.Clamp(age, 0, Motion.ActivationDuration);
        Igniting = _age < Motion.ActivationDuration;
        float phase = _age / Motion.ActivationDuration;
        float b = HologramChannel.Envelope(phase, Motion.IgnitionPeak);
        Modulate = new Color(b, b, b, b > 0 ? 1 : 0);
        Position = HologramChannel.IgnitionJitter(phase, 0.4f, Motion.JitterPixels);
        SetProcess(Igniting);
    }

    public override void _Process(double delta) => Seek(_age + (float)delta);
}
