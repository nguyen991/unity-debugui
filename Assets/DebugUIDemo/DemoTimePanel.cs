using Ng.DebugTools;
using UnityEngine;

/// <summary>
/// A panel owned by no object (owner = null): it lives until DebugUI.Remove is called.
/// Typical for static systems that are not MonoBehaviours.
/// </summary>
public static class DemoTimePanel
{
    public static void Register()
    {
        // Built-in log panel: one line, captures logs from here on.
        DebugUI.LogPanel();

        DebugUI.Panel(null, "Time", p => p
            .Slider("Time scale", () => Time.timeScale, v => Time.timeScale = v, 0, 2)
            .Label("FPS", () => Mathf.RoundToInt(1f / Mathf.Max(Time.smoothDeltaTime, 0.0001f)))
            .Slider("Window width", () => DebugUI.Width, v => DebugUI.Width = v, 0.5f, 1f)
            .Slider("Window height", () => DebugUI.Height, v => DebugUI.Height = v, 0.5f, 1f)
            .Slider("UI scale", () => DebugUI.Scale, v => DebugUI.Scale = v, 0.5f, 3f)
            .Horizontal(h => h
                .Button("Log", () => Debug.Log("Hello from the demo\nwith a second line"))
                .Button("Warning", () => Debug.LogWarning("Something looks off"), new Color(0.8f, 0.65f, 0.2f))
                .Button("Error", () => Debug.LogError("Something broke"), new Color(0.75f, 0.2f, 0.2f)))
            .Button("Remove me", () => DebugUI.Remove("Time")));
    }
}
