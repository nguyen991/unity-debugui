using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ng.DebugUI
{
    /// <summary>
    /// The visual shell: a floating toggle button and a window with a tab per panel.
    /// One panel is shown at a time, tabs follow registration order.
    /// </summary>
    internal sealed class DebugUIWindow
    {
        /// <summary>Panel widths below this use the full-width (phone) layout.</summary>
        internal const float NarrowBreakpoint = 600f;

        readonly PanelRegistry registry;

        public DebugUIWindow(VisualElement documentRoot, PanelRegistry registry)
        {
            this.registry = registry;

            Root = new VisualElement { pickingMode = PickingMode.Ignore };
            Root.AddToClassList(UssClasses.Root);

            Window = new VisualElement();
            Window.AddToClassList(UssClasses.Window);

            TabBar = new ScrollView(ScrollViewMode.Horizontal);
            TabBar.AddToClassList(UssClasses.TabBar);

            Content = new ScrollView(ScrollViewMode.Vertical);
            Content.AddToClassList(UssClasses.Content);

            ToggleButton = new Button(() => ToggleRequested?.Invoke()) { text = "DBG" };
            ToggleButton.AddToClassList(UssClasses.Toggle);

            Window.Add(TabBar);
            Window.Add(Content);
            Root.Add(Window);
            Root.Add(ToggleButton);
            documentRoot.Add(Root);

            Root.RegisterCallback<GeometryChangedEvent>(e => ApplyWidth(e.newRect.width));

            registry.Changed += Rebuild;
            Rebuild();
            SetOpen(false);
        }

        public VisualElement Root { get; }
        public VisualElement Window { get; }
        public ScrollView TabBar { get; }
        public ScrollView Content { get; }
        public Button ToggleButton { get; }

        public PanelEntry Current { get; private set; }

        public event Action ToggleRequested;
        public event Action CurrentChanged;

        public void SetOpen(bool open)
            => Window.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;

        internal void ApplyWidth(float panelWidth)
            => Root.EnableInClassList(UssClasses.Narrow, panelWidth < NarrowBreakpoint);

        /// <summary>
        /// Insets (left, top, right, bottom) in screen pixels between the screen edges and the safe
        /// area. Screen coordinates have their origin at the bottom-left.
        /// </summary>
        internal static Vector4 SafeAreaInsets(Rect safeArea, Vector2 screenSize)
            => new Vector4(
                safeArea.xMin,
                screenSize.y - safeArea.yMax,
                screenSize.x - safeArea.xMax,
                safeArea.yMin);

        /// <summary>
        /// Shrinks the (absolutely positioned) root to the safe area so nothing is drawn under
        /// notches or rounded corners. Offsets rather than padding, because the window and toggle
        /// are absolutely positioned children.
        /// </summary>
        internal void ApplySafeArea(Vector4 insetsInPanelUnits)
        {
            Root.style.left = insetsInPanelUnits.x;
            Root.style.top = insetsInPanelUnits.y;
            Root.style.right = insetsInPanelUnits.z;
            Root.style.bottom = insetsInPanelUnits.w;
        }

        public void Select(string name)
        {
            var entry = name != null ? registry.Find(name) : null;
            if (!ReferenceEquals(entry, Current))
            {
                Content.Clear();
                Current = entry;
                if (entry != null)
                    Content.Add(entry.Root);
                UpdateTabClasses();
                CurrentChanged?.Invoke();
            }
            else
            {
                UpdateTabClasses();
            }
        }

        void Rebuild()
        {
            TabBar.Clear();
            var panels = registry.Panels;
            for (int i = 0; i < panels.Count; i++)
            {
                string name = panels[i].Name;
                var tab = new Button(() => Select(name)) { text = name };
                tab.AddToClassList(UssClasses.Tab);
                TabBar.Add(tab);
            }

            // Keep the current tab if it still exists (possibly replaced by a new entry with the
            // same name); otherwise fall back to the first panel.
            if (Current != null && registry.Find(Current.Name) != null)
                Select(Current.Name);
            else
                Select(panels.Count > 0 ? panels[0].Name : null);
        }

        void UpdateTabClasses()
        {
            foreach (var child in TabBar.Children())
            {
                if (child is Button tab)
                    tab.EnableInClassList(UssClasses.TabSelected, Current != null && tab.text == Current.Name);
            }
        }
    }
}
