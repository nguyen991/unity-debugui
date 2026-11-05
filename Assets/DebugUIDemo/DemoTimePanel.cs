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
        DebugUI.Panel(null, "Time", p =>
        {
            p.Slider("Time scale", () => Time.timeScale, v => Time.timeScale = v, 0, 2);
            p.Label("FPS", () => Mathf.RoundToInt(1f / Mathf.Max(Time.smoothDeltaTime, 0.0001f)));
            p.Button("Remove me", () => DebugUI.Remove("Time"));
        });
    }
}
