using Godot;

namespace StarfallMiner;

/// <summary>Per-window Android rotation policy; no global animation settings or plugin.</summary>
public static class AndroidOrientation
{
    // Public Android API constants: WindowManager.LayoutParams and ActivityInfo.
    private const int JumpCut = 2, Portrait = 1, Landscape = 0;

    public static bool Request(bool portrait)
    {
        if (!OS.HasFeature("android") || !Engine.HasSingleton("AndroidRuntime")) return false;
        try
        {
            var runtime = Engine.GetSingleton("AndroidRuntime");
            var activity = CallObject(runtime, "getActivity");
            var action = Callable.From(() =>
            {
                try
                {
                    var window = CallObject(activity, "getWindow");
                    var attributes = CallObject(window, "getAttributes");
                    // JavaObject exposes methods and constants, not mutable instance
                    // fields. Use public Java reflection to set this public int field.
                    var type = CallObject(attributes, "getClass");
                    var field = CallObject(type, "getField", "rotationAnimation");
                    Call(field, "setInt", attributes, JumpCut);
                    Call(window, "setAttributes", attributes);
                }
                catch (Exception error)
                {
                    GD.PushWarning($"Android jump-cut request failed: {error.Message}");
                }
                // Apply policy and orientation in the same UI-thread operation so
                // rotation cannot start before its animation setting takes effect.
                try { Call(activity, "setRequestedOrientation", portrait ? Portrait : Landscape); }
                catch (Exception error) { GD.PushWarning($"Android orientation request failed: {error.Message}"); }
            });
            var runnable = CallObject(runtime, "createRunnableFromGodotCallable", action);
            Call(activity, "runOnUiThread", runnable);
            return true;
        }
        catch (Exception error)
        {
            GD.PushWarning($"Android runtime bridge unavailable; using engine rotation: {error.Message}");
            return false;
        }
    }

    private static Variant Call(GodotObject target, StringName method, params Variant[] args)
    {
        var result = target.Call(method, args);
        if (JavaClassWrapper.GetException() != null) throw new InvalidOperationException($"Java method {method} threw an exception.");
        return result;
    }

    private static JavaObject CallObject(GodotObject target, StringName method, params Variant[] args) =>
        Call(target, method, args).As<JavaObject>() ?? throw new InvalidOperationException($"Java method {method} returned no object.");
}
