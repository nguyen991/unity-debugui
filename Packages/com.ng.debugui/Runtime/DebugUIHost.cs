using UnityEngine;
using UnityEngine.UIElements;
#if NG_DEBUGUI_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Ng.DebugTools
{
    /// <summary>
    /// Owns the runtime UIDocument and drives refresh. Created lazily by <see cref="DebugUI"/>
    /// on first use, survives scene loads, needs no scene setup.
    /// </summary>
    [AddComponentMenu("")]
    internal sealed class DebugUIHost : MonoBehaviour
    {
        internal const string ThemeResourcePath = "NgDebugUI/DebugUITheme";

        PanelSettings settings;
        Rect lastSafeArea;
        Vector2Int lastScreenSize;

        internal static DebugUIHost Instance { get; private set; }

        internal PanelRegistry Registry { get; private set; }
        internal DebugUIWindow Window { get; private set; }
        internal UIDocument Document { get; private set; }
        internal bool IsOpen { get; private set; }

        internal static DebugUIHost GetOrCreate()
        {
            if (Instance != null)
                return Instance;

            var go = new GameObject("[DebugUI]") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<DebugUIHost>();
            Instance.Init();
            return Instance;
        }

        internal static void DestroyInstance()
        {
            if (Instance != null)
                DestroyImmediate(Instance.gameObject);
            Instance = null;
        }

        /// <summary>Supports Enter Play Mode Options with domain reload disabled.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetStatics() => Instance = null;

        void Init()
        {
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "DebugUI PanelSettings";
            settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>(ThemeResourcePath);
            if (settings.themeStyleSheet == null)
                Debug.LogError($"[DebugUI] Theme not found at Resources/{ThemeResourcePath}");
            settings.scaleMode = PanelScaleMode.ConstantPhysicalSize;
            settings.referenceDpi = 96;
            settings.fallbackDpi = 96;
            settings.sortingOrder = DebugUI.SortingOrder;
            settings.scale = DebugUI.Scale;

            Document = gameObject.AddComponent<UIDocument>();
            Document.panelSettings = settings;

            Registry = new PanelRegistry();
            Window = new DebugUIWindow(Document.rootVisualElement, Registry);
            Window.SetSize(DebugUI.Width, DebugUI.Height);
            Window.SetButtonPosition(DebugUI.ButtonPosition);
            foreach (var sheet in DebugUI.StyleSheets)
                Window.Root.styleSheets.Add(sheet);
            Window.ToggleRequested += Toggle;
            Window.CurrentChanged += () =>
            {
                if (IsOpen)
                    RefreshNow();
            };
        }

        internal void SetScale(float scale)
        {
            settings.scale = scale;
            lastScreenSize = default; // panel units per pixel changed: recompute safe-area insets
        }

        internal void SetSortingOrder(float order) => settings.sortingOrder = order;

        internal void Open()
        {
            IsOpen = true;
            Window.SetOpen(true);
            RefreshNow();
        }

        internal void Close()
        {
            IsOpen = false;
            Window.SetOpen(false);
        }

        internal void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        internal void Tick()
        {
            Registry.RemoveDestroyedOwners();
            UpdateSafeArea();
            if (IsOpen)
                Window.Current?.Bindings.Refresh();
        }

        void RefreshNow()
        {
            var current = Window.Current;
            if (current == null)
                return;
            current.Bindings.InvalidateAll();
            current.Bindings.Refresh();
        }

        /// <summary>True if a panel-space position hits any debug UI element (window, tab, toggle).</summary>
        internal bool IsOverUI(Vector2 panelPosition)
        {
            var panel = Document != null ? Document.rootVisualElement?.panel : null;
            return panel != null && panel.Pick(panelPosition) != null;
        }

        /// <summary>Screen position (origin bottom-left, as Input System reports it) → hit test.</summary>
        internal bool IsOverUIScreen(Vector2 screenPosition)
        {
            var panel = Document != null ? Document.rootVisualElement?.panel : null;
            if (panel == null)
                return false;
            var flipped = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            return IsOverUI(RuntimePanelUtils.ScreenToPanel(panel, flipped));
        }

#if NG_DEBUGUI_INPUT_SYSTEM
        /// <summary>True on the frame <see cref="DebugUI.ToggleKey"/> goes down on <paramref name="keyboard"/>.</summary>
        internal static bool ToggleKeyPressed(Keyboard keyboard)
        {
            var key = DebugUI.ToggleKey;
            return keyboard != null && key != Key.None && keyboard[key].wasPressedThisFrame;
        }
#endif

        void UpdateSafeArea()
        {
            var safe = Screen.safeArea;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (safe == lastSafeArea && size == lastScreenSize)
                return;
            var panel = Document.rootVisualElement.panel;
            if (panel == null || size.x <= 0 || size.y <= 0)
                return;

            lastSafeArea = safe;
            lastScreenSize = size;
            var insets = DebugUIWindow.SafeAreaInsets(safe, size);
            // Panel units per screen pixel, from the panel's own scaling.
            var panelSize = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(size.x, size.y))
                            - RuntimePanelUtils.ScreenToPanel(panel, Vector2.zero);
            float sx = panelSize.x / size.x, sy = panelSize.y / size.y;
            Window.ApplySafeArea(new Vector4(insets.x * sx, insets.y * sy, insets.z * sx, insets.w * sy));
        }

        void Update()
        {
#if NG_DEBUGUI_INPUT_SYSTEM
            if (ToggleKeyPressed(Keyboard.current))
                Toggle();
#endif
            Tick();
        }

        void OnDestroy()
        {
            if (settings != null)
                Destroy(settings);
            if (Instance == this)
                Instance = null;
        }
    }
}
