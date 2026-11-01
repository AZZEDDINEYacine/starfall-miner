using Godot;

namespace StarfallMiner;

/// <summary>A bounded, staggered power-on pulse. Settled channels stop processing entirely.</summary>
public partial class HologramChannel : Control
{
    public enum Geometry { None, Header, Telemetry, Navigation, Recovery, Loadout, Command }
    [Export] public HologramStyle Style { get; set; } = null!;
    [Export] public ModeTransitionStyle Motion { get; set; } = null!;
    public Geometry Decoration { get; set; }
    public float Order { get; set; }
    public float Meter { get; set; }
    public float Brightness { get; private set; } = 1;
    public bool Igniting { get; private set; }
    public float Age { get; private set; }
    private Vector2 _home;
    private bool _placed;

    public void Activate(bool animate)
    {
        if (!_placed) { _home = Position; _placed = true; }
        Seek(animate ? 0 : Motion.ActivationDuration);
    }

    // Neon starter: abrupt failed strikes, short blackouts and brief contact chatter.
    // Times are normalized across three seconds; flashes themselves last 20–90 ms.
    public static float Envelope(float t, float peak)
    {
        if (t <= 0) return 0;
        if (t >= 1) return 1;
        if (t < 0.018f) return 0.08f;
        if (t < 0.033f) return peak;
        if (t < 0.046f) return 0;
        if (t < 0.065f) return peak;
        if (t < 0.10f) return 0.22f;
        if (t < 0.117f) return 1.25f;
        if (t < 0.15f) return 0.06f;
        if (t < 0.19f) return 0.8f;
        if (t < 0.223f) return 1 + (peak - 1) * 0.55f;
        if (t < 0.242f) return 0.15f;
        if (t < 0.276f) return 1.1f;
        if (t < 0.304f) return 0.52f;
        if (t < 0.37f) return 0.97f;
        if (t < 0.402f) return 1 + (peak - 1) * 0.22f;
        if (t < 0.414f) return 0.68f;
        if (t < 0.57f) return 1;
        if (t < 0.583f) return 0.65f;
        if (t < 0.71f) return 1.02f;
        if (t < 0.72f) return 0.86f;
        if (t < 0.89f) return 1;
        if (t < 0.90f) return 0.94f;
        return 1;
    }

    public static Vector2 IgnitionJitter(float phase, float order, float pixels)
    {
        if (phase <= 0 || phase >= 0.9f) return Vector2.Zero;
        int tick = (int)(phase * 167 + order * 31);
        uint hash = unchecked((uint)(tick * 374761393 + 668265263));
        hash = (hash ^ (hash >> 13)) * 1274126177;
        float x = ((hash & 255) / 127.5f - 1);
        float y = (((hash >> 8) & 255) / 127.5f - 1) * 0.35f;
        float strength = phase < 0.31f ? 1 : phase < 0.42f ? 0.65f : Envelope(phase, 1.72f) < 0.95f ? 0.5f : 0;
        return new Vector2(x, y) * pixels * strength;
    }

    public void Seek(float age)
    {
        Age = Mathf.Clamp(age, 0, Motion.ActivationDuration);
        float delay = Order * Motion.StaggerFraction * Motion.ActivationDuration;
        float phase = Mathf.Clamp((Age - delay) / Mathf.Max(0.01f, Motion.ActivationDuration - delay), 0, 1);
        Brightness = Envelope(phase, Motion.IgnitionPeak);
        Igniting = Age < Motion.ActivationDuration;
        Position = _home + IgnitionJitter(phase, Order, Motion.JitterPixels);
        // Before ignition, hide the ink outright rather than drawing black ghosts.
        // Once energized alpha stays at one; the fluctuations are luminance only.
        Modulate = new Color(Brightness, Brightness, Brightness, Brightness > 0 ? 1 : 0);
        SetProcess(Igniting);
        QueueRedraw();
    }

    public override void _Process(double delta) => Seek(Age + (float)delta);

    private void Trace(Vector2 a, Vector2 b, Color color, float alpha = 1, float width = 1)
    {
        // Local vector halos approximate bloom without HDR, blur textures, or a full-screen pass.
        float halo = Style.HaloStrength * (1 + Mathf.Max(0, Brightness - 1) * 2);
        DrawLine(a, b, new Color(color, alpha * halo * 0.45f), width + 5, true);
        DrawLine(a, b, new Color(color, alpha * halo), width + 2, true);
        DrawLine(a, b, new Color(color, alpha), width, true);
    }

    private void Arc(Vector2 center, float radius, float from, float to, float alpha)
    {
        DrawArc(center, radius, from, to, 36, new Color(Style.Cyan, alpha * Style.HaloStrength), 4, true);
        DrawArc(center, radius, from, to, 36, new Color(Style.Cyan, alpha), 1, true);
    }

    public override void _Draw()
    {
        var cyan = Style.Cyan;
        float w = Size.X, h = Size.Y;
        switch (Decoration)
        {
            case Geometry.Header:
                Arc(new Vector2(18, 23), 14, -2.4f, 2.3f, 0.8f);
                Arc(new Vector2(18, 23), 9, -0.7f, 3.8f, 0.4f);
                Trace(new Vector2(18, 7), new Vector2(18, 14), cyan);
                Trace(new Vector2(7, 23), new Vector2(14, 23), cyan);
                Trace(new Vector2(30, 48), new Vector2(w - 72, 48), cyan, 0.18f);
                break;
            case Geometry.Telemetry:
                Trace(new Vector2(0, 18), new Vector2(0, 64), cyan, 0.8f, 2);
                Trace(new Vector2(0, 72), new Vector2(10, 82), cyan, 0.6f);
                Trace(new Vector2(10, 82), new Vector2(w * 0.5f, 82), cyan, 0.5f);
                Trace(new Vector2(w * 0.5f, 82), new Vector2(w * 0.5f + 14, 68), cyan, 0.3f);
                for (int i = 0; i < 5; i++)
                    Trace(new Vector2(w * 0.66f + i * 22, 69), new Vector2(w * 0.66f + i * 22 + 15, 69), Style.Amber, Meter >= (i + 1) / 5f ? 0.9f : 0.15f, 2);
                break;
            case Geometry.Navigation:
                var center = Size * 0.5f;
                float radius = Mathf.Min(w * 0.37f, h * 0.44f);
                Arc(center, radius, -0.55f, 0.55f, 0.25f);
                Arc(center, radius, Mathf.Pi - 0.55f, Mathf.Pi + 0.55f, 0.25f);
                Arc(center, radius + 7, -0.4f, 0.3f, 0.10f);
                for (int i = -5; i <= 5; i++)
                {
                    float x = w * 0.5f + i * 17;
                    Trace(new Vector2(x, 12), new Vector2(x, i % 5 == 0 ? 22 : 17), cyan, i == 0 ? 0.65f : 0.18f);
                }
                for (int side = 0; side < 2; side++)
                {
                    float x = side == 0 ? 15 : w - 15;
                    float inward = side == 0 ? 1 : -1;
                    Trace(new Vector2(x, h * 0.30f), new Vector2(x, h * 0.70f), cyan, 0.22f);
                    Trace(new Vector2(x, h * 0.30f), new Vector2(x + 8 * inward, h * 0.30f - 8), cyan, 0.4f);
                    Trace(new Vector2(x, h * 0.70f), new Vector2(x + 8 * inward, h * 0.70f + 8), cyan, 0.4f);
                }
                break;
            case Geometry.Recovery:
                Trace(new Vector2(0, 5), new Vector2(21, 5), cyan, 0.7f);
                Trace(new Vector2(26, 5), new Vector2(72, 5), cyan, 0.2f);
                Trace(new Vector2(w - 60, h - 2), new Vector2(w, h - 2), cyan, 0.25f);
                break;
            case Geometry.Loadout:
                Trace(new Vector2(0, 0), new Vector2(w * 0.35f, 0), cyan, 0.3f);
                Trace(new Vector2(w * 0.35f + 8, 0), new Vector2(w, 0), cyan, 0.12f);
                break;
            case Geometry.Command:
                Trace(new Vector2(0, 0), new Vector2(10, -7), cyan, 0.4f);
                Trace(new Vector2(10, -7), new Vector2(w - 10, -7), cyan, 0.12f);
                break;
        }
    }
}
