using System;
using System.Diagnostics;
using UnityEngine;
#if NG_DEBUGUI_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Debug = UnityEngine.Debug;

namespace Ng.DebugUI
{
    /// <summary>
    /// Entry point for building in-game debug panels.
    /// <code>
    /// DebugUI.Panel(this, "Player", p =>
    /// {
    ///     p.Label("HP", () => hp);
    ///     p.Slider("Speed", () => speed, v => speed = v, 0, 20);
    /// });
    /// </code>
    /// <see cref="Panel"/> and <see cref="Remove"/> are compiled out (call sites included) unless
    /// UNITY_EDITOR, DEVELOPMENT_BUILD or DEBUG_UI_ENABLED is defined. Define DEBUG_UI_DISABLED to
    /// turn the library into a no-op everywhere.
    /// </summary>
    // UAC0009: Unity 6.6 deprecates DEVELOPMENT_BUILD in favour of managed-code-variant defines that
    // do not exist on 6000.0–6.5. DEVELOPMENT_BUILD is still the one define that marks a development
    // player on every supported version, so it is kept on purpose.
#pragma warning disable UAC0009
    public static class DebugUI
    {
        /// <summary>Test hook: behaves as if DEBUG_UI_DISABLED were defined.</summary>
        internal static bool ForceDisabled;

        /// <summary>
        /// Whether this build may show the debug UI at all. False in release builds unless
        /// DEBUG_UI_ENABLED is defined, so Open()/Toggle() can never create UI for players.
        /// Settable only so tests can simulate a release build.
        /// </summary>
        internal static bool BuildAllowsDebugUI =
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG_UI_ENABLED
            true;
#else
            false;
#endif

        static bool Enabled
        {
            get
            {
#if DEBUG_UI_DISABLED
                return false;
#else
                return BuildAllowsDebugUI && !ForceDisabled && Application.isPlaying;
#endif
            }
        }

        /// <summary>
        /// Registers (or replaces) a panel. The panel is removed automatically once
        /// <paramref name="owner"/> is destroyed; pass null to keep it until <see cref="Remove"/>.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("DEBUG_UI_ENABLED")]
        public static void Panel(UnityEngine.Object owner, string name, Action<IDebugBuilder> build)
        {
            if (!Enabled)
                return;
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("[DebugUI] Panel name must not be empty.");
                return;
            }
            DebugUIHost.GetOrCreate().Registry.AddOrReplace(owner, name, build);
        }

        /// <summary>Removes a panel by name.</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("DEBUG_UI_ENABLED")]
        public static void Remove(string name)
        {
            if (!Enabled)
                return;
            var host = DebugUIHost.Instance;
            if (host != null)
                host.Registry.Remove(name);
        }

        public static bool IsOpen
        {
            get
            {
                if (!Enabled)
                    return false;
                var host = DebugUIHost.Instance;
                return host != null && host.IsOpen;
            }
        }

        /// <summary>
        /// True when the mouse / touch pointer is over the debug UI. Check it before handling a
        /// click in gameplay code so taps on the debug UI don't also hit the game. Requires the
        /// Input System package; always false without it.
        /// </summary>
        public static bool IsPointerOverUI
        {
            get
            {
#if NG_DEBUGUI_INPUT_SYSTEM
                if (!Enabled)
                    return false;
                var host = DebugUIHost.Instance;
                var pointer = Pointer.current;
                return host != null && pointer != null && host.IsOverUIScreen(pointer.position.ReadValue());
#else
                return false;
#endif
            }
        }

        public static void Open()
        {
            if (Enabled)
                DebugUIHost.GetOrCreate().Open();
        }

        public static void Close()
        {
            if (!Enabled)
                return;
            var host = DebugUIHost.Instance;
            if (host != null)
                host.Close();
        }

        public static void Toggle()
        {
            if (Enabled)
                DebugUIHost.GetOrCreate().Toggle();
        }
    }
#pragma warning restore UAC0009
}
