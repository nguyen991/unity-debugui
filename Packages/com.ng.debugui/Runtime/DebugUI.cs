using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;
#if NG_DEBUGUI_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Debug = UnityEngine.Debug;

namespace Ng.DebugTools
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

        /// <summary>
        /// Registers the built-in log panel: Log / Warning / Error filters with counts, Clear, text
        /// search, and a details view with the stack trace. Logs are captured from this call on
        /// (from any thread), keeping the newest <paramref name="capacity"/> entries — call it early,
        /// e.g. in the first scene's Awake.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("DEBUG_UI_ENABLED")]
        public static void LogPanel(string name = "Logs", int capacity = 500)
        {
            if (!Enabled)
                return;
            var store = LogStore.GetShared(Mathf.Clamp(capacity, 1, 100000));
            Panel(null, name, p => LogView.Build((DebugBuilder)p, store));
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

        /// <summary>
        /// Width of the debug window as a fraction of the screen (safe area) width: 0.5 = half,
        /// 1 = full width (default). Clamped to [0.5, 1]. The window is anchored top-right.
        /// Can be set before the UI exists.
        /// </summary>
        public static float Width
        {
            get => width;
            set
            {
                width = ClampSize(value);
                ApplySize();
            }
        }

        /// <summary>
        /// Height of the debug window as a fraction of the screen (safe area) height: 0.5 = top
        /// half, 1 = full height (default). Clamped to [0.5, 1].
        /// </summary>
        public static float Height
        {
            get => height;
            set
            {
                height = ClampSize(value);
                ApplySize();
            }
        }

        internal const float MinSize = 0.5f;
        static float width = 1f, height = 1f;

        static float ClampSize(float v) => float.IsNaN(v) ? 1f : Mathf.Clamp(v, MinSize, 1f);

        static void ApplySize()
        {
            var host = DebugUIHost.Instance;
            if (host != null)
                host.Window.SetSize(width, height);
        }

        /// <summary>
        /// Scales the whole debug UI — text, controls and touch targets together — on top of the
        /// automatic DPI scaling. 1 = default, 1.5 = 50% bigger. Clamped to [0.5, 3].
        /// </summary>
        public static float Scale
        {
            get => scale;
            set
            {
                scale = float.IsNaN(value) ? 1f : Mathf.Clamp(value, MinScale, MaxScale);
                var host = DebugUIHost.Instance;
                if (host != null)
                    host.SetScale(scale);
            }
        }

#if NG_DEBUGUI_INPUT_SYSTEM
        /// <summary>
        /// Keyboard key that opens/closes the debug UI. Default <c>Key.Backquote</c>;
        /// <c>Key.None</c> turns the shortcut off (the DBG button still works).
        /// Only available with the Input System package.
        /// </summary>
        public static Key ToggleKey
        {
            get => toggleKey;
            set
            {
                if (value != Key.None && ((int)value < 1 || (int)value > Keyboard.KeyCount))
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Not a keyboard key.");
                toggleKey = value;
            }
        }

        static Key toggleKey = Key.Backquote;
#endif

        internal const float MinScale = 0.5f, MaxScale = 3f;
        static float scale = 1f;

        /// <summary>
        /// Adds a USS stylesheet on top of the built-in theme, e.g. to change fonts or colors.
        /// Target the "ngdbg-*" classes listed in the README. Can be called before the UI exists.
        /// </summary>
        public static void AddStyleSheet(StyleSheet styleSheet)
        {
            if (styleSheet == null || styleSheets.Contains(styleSheet))
                return;
            styleSheets.Add(styleSheet);
            var host = DebugUIHost.Instance;
            if (host != null)
                host.Window.Root.styleSheets.Add(styleSheet);
        }

        public static void RemoveStyleSheet(StyleSheet styleSheet)
        {
            if (styleSheet == null || !styleSheets.Remove(styleSheet))
                return;
            var host = DebugUIHost.Instance;
            if (host != null)
                host.Window.Root.styleSheets.Remove(styleSheet);
        }

        static readonly List<StyleSheet> styleSheets = new List<StyleSheet>();

        internal static IReadOnlyList<StyleSheet> StyleSheets => styleSheets;

        /// <summary>
        /// Back to defaults: full size, scale 1, no extra stylesheets, backquote shortcut. Also runs on entering play
        /// mode so Enter Play Mode Options with domain reload disabled starts clean.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetSettings()
        {
            var host = DebugUIHost.Instance;
            if (host != null)
                foreach (var sheet in styleSheets)
                    host.Window.Root.styleSheets.Remove(sheet);
            styleSheets.Clear();
            width = height = 1f;
            ApplySize();
            Scale = 1f;
#if NG_DEBUGUI_INPUT_SYSTEM
            toggleKey = Key.Backquote;
#endif
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
