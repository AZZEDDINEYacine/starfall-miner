using Godot;

namespace StarfallMiner;

/// <summary>Static edge framing; the entire decoration is transparent to input.</summary>
public partial class ObservatoryFrame : Control
{
    public HologramStyle Ink { get; set; } = null!;
    private readonly StyleBoxFlat _frame = new() { DrawCenter = false, ShadowSize = 5 };

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _frame.SetCornerRadiusAll(22);
        _frame.SetBorderWidthAll(1);
        _frame.BorderColor = new Color(Ink.Cyan, 0.18f);
        _frame.ShadowColor = new Color(Ink.Cyan, 0.035f);
        Resized += QueueRedraw;
        SetProcess(false);
    }

    public override void _Draw()
    {
        if (Size.X < 80 || Size.Y < 80) return;
        DrawStyleBox(_frame, new Rect2(8, 8, Size.X - 16, Size.Y - 16));
        DrawLine(new Vector2(32, 8), new Vector2(Mathf.Min(Size.X * 0.34f, 196), 8), new Color(Ink.Cyan, 0.6f), 1, true);
        DrawLine(new Vector2(Size.X - 128, Size.Y - 8), new Vector2(Size.X - 32, Size.Y - 8), new Color(Ink.Focus, 0.5f), 1, true);
        for (int i = 0; i < 7; i++)
        {
            float x = Size.X * 0.5f + (i - 3) * 11;
            DrawLine(new Vector2(x, 9), new Vector2(x + 5, 9), new Color(Ink.Dim, 0.28f), 1, true);
        }
    }

    public override void _ExitTree() => _frame.Dispose();
}
