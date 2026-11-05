using UnityEngine;
using UnityEngine.UIElements;

namespace Ng.DebugUI
{
    /// <summary>
    /// Owns the runtime UIDocument and drives refresh. Created lazily by <see cref="DebugUI"/>
    /// on first use, survives scene loads, needs no scene setup.
    /// </summary>
    [AddComponentMenu("")]
    internal sealed class DebugUIHost : MonoBehaviour
    {
        internal const string ThemeResourcePath = "NgDebugUI/DebugUITheme";
        internal const int SortingOrder = 10000;

        PanelSettings settings;

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
            settings.sortingOrder = SortingOrder;

            Document = gameObject.AddComponent<UIDocument>();
            Document.panelSettings = settings;

            Registry = new PanelRegistry();
            Window = new DebugUIWindow(Document.rootVisualElement, Registry);
            Window.ToggleRequested += Toggle;
            Window.CurrentChanged += () =>
            {
                if (IsOpen)
                    RefreshNow();
            };
        }

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

        void Update() => Tick();

        void OnDestroy()
        {
            if (settings != null)
                Destroy(settings);
            if (Instance == this)
                Instance = null;
        }
    }
}
