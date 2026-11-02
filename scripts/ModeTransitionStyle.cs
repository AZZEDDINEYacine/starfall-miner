using Godot;

namespace StarfallMiner;

/// <summary>Inspector-editable presentation timing and zoom. Does not affect physics.</summary>
[GlobalClass]
public partial class ModeTransitionStyle : Resource
{
    [ExportGroup("Camera")]
    [Export(PropertyHint.Range, "1.3,3.5,0.05")] public float ManualZoom { get; set; } = 2.3f;
    [Export(PropertyHint.Range, "1,20,0.5")] public float CameraResponse { get; set; } = 6.5f;
    [ExportGroup("Holographic ignition")]
    [Export(PropertyHint.Range, "0.25,6,0.05")] public float ActivationDuration { get; set; } = 3.0f;
    [Export(PropertyHint.Range, "0,0.35,0.01")] public float StaggerFraction { get; set; } = 0.24f;
    [Export(PropertyHint.Range, "1.1,2,0.05")] public float IgnitionPeak { get; set; } = 1.72f;
    [Export(PropertyHint.Range, "0,4,0.05")] public float JitterPixels { get; set; } = 2.4f;
}
