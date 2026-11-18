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
        /// Size of the debug UI: the screen size, in pixels, at which one panel px is one screen pixel.
        /// The UI keeps the same proportion of the screen on every device, whatever its DPI; on other
        /// screens it is scaled by the side chosen with <see cref="MatchWidthOrHeight"/>. A smaller
        /// reference makes everything bigger (text, controls, touch targets): 360×640 is about 1.5× the
        /// default, 1080×1920 half of it. Default 540×960. Each side is at least 1.
        /// Can be set before the UI exists.
        /// </summary>
        public static Vector2Int ReferenceResolution
        {
            get => referenceResolution;
            set
            {
                referenceResolution = new Vector2Int(Mathf.Max(1, value.x), Mathf.Max(1, value.y));
                ApplyScaling();
            }
        }

        /// <summary>
        /// Which side of <see cref="ReferenceResolution"/> the UI fits: 0 = width, 1 = height, values
        /// between blend the two (default 0.5, which keeps the same size when the device rotates).
        /// Clamped to [0, 1].
        /// </summary>
        public static float MatchWidthOrHeight
        {
            get => matchWidthOrHeight;
            set
            {
                matchWidthOrHeight = float.IsNaN(value) ? DefaultMatch : Mathf.Clamp01(value);
                ApplyScaling();
            }
        }

        internal const float DefaultMatch = 0.5f;
        internal static readonly Vector2Int DefaultReferenceResolution = new Vector2Int(540, 960);
        static Vector2Int referenceResolution = DefaultReferenceResolution;
        static float matchWidthOrHeight = DefaultMatch;

        static void ApplyScaling()
        {
            var host = DebugUIHost.Instance;
            if (host != null)
                host.ApplyScaling();
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

        /// <summary>
        /// Default <see cref="SortingOrder"/>: above every uGUI Canvas (whose sortingOrder is capped at
        /// 32767), so no Screen Space - Overlay canvas can cover the debug UI.
        /// </summary>
        public const float DefaultSortingOrder = 40000f;

        /// <summary>
        /// Draw order of the debug UI among UI Toolkit panels and Screen Space - Overlay canvases: higher
        /// draws on top. Default <see cref="DefaultSortingOrder"/>. Lower it if a game popup must appear
        /// above the debug UI; raise it if a UI Toolkit panel of the game uses an even higher value.
        /// </summary>
        public static float SortingOrder
        {
            get => sortingOrder;
            set
            {
                sortingOrder = float.IsNaN(value) ? DefaultSortingOrder : value;
                var host = DebugUIHost.Instance;
                if (host != null)
                    host.SetSortingOrder(sortingOrder);
            }
        }

        static float sortingOrder = DefaultSortingOrder;

        /// <summary>
        /// Corner of the round DBG button (default bottom-right). <see cref="DebugButtonPosition.None"/>
        /// hides it — then the UI opens only via the keyboard shortcut or <see cref="Open"/>, so on
        /// touch devices open it from your own code.
        /// </summary>
        public static DebugButtonPosition ButtonPosition
        {
            get => buttonPosition;
            set
            {
                buttonPosition = value;
                var host = DebugUIHost.Instance;
                if (host != null)
                    host.Window.SetButtonPosition(value);
            }
        }

        static DebugButtonPosition buttonPosition = DebugButtonPosition.BottomRight;

        /// <summary>
        /// Diameter of the round DBG button in panel px (default 48, clamped to 24 – 200; scales with
        /// the rest of the UI). Tabs and content keep clear of it at any size.
        /// </summary>
        public static float ButtonSize
        {
            get => buttonSize;
            set
            {
                buttonSize = float.IsNaN(value) ? DefaultButtonSize : Mathf.Clamp(value, MinButtonSize, MaxButtonSize);
                var host = DebugUIHost.Instance;
                if (host != null)
                    host.Window.SetButtonSize(buttonSize);
            }
        }

        internal const float DefaultButtonSize = 48f, MinButtonSize = 24f, MaxButtonSize = 200f;
        static float buttonSize = DefaultButtonSize;

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
        /// Back to defaults: full size, 540×960 reference resolution, match 0.5, default sorting order, button bottom-right and 48px, no extra stylesheets, backquote shortcut. Also runs on entering play
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
            referenceResolution = DefaultReferenceResolution;
            matchWidthOrHeight = DefaultMatch;
            ApplyScaling();
            SortingOrder = DefaultSortingOrder;
            ButtonPosition = DebugButtonPosition.BottomRight;
            ButtonSize = DefaultButtonSize;
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
