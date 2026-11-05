using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Ng.DebugTools
{
    /// <summary>
    /// The built-in log panel: kind filters with counts, Clear, text search, a virtualized list
    /// (oldest first, follows new entries while scrolled to the bottom) and a details view with the
    /// full message and stack trace. Refreshes only when the store or a filter changed.
    /// </summary>
    internal sealed class LogView : IBinding
    {
        static readonly string[] KindNames = { "Log", "Warning", "Error" };
        const string RowClass = "ngdbg-log-row";
        const string RowWarningClass = "ngdbg-log-row--warning";
        const string RowErrorClass = "ngdbg-log-row--error";

        readonly LogStore store;
        readonly List<LogEntry> shown = new List<LogEntry>();
        readonly bool[] showKind = { true, true, true };
        readonly int[] shownCounts = { -1, -1, -1 };
        readonly Toggle[] filters = new Toggle[3];
        readonly Predicate<LogEntry> matches;
        readonly VisualElement root;
        readonly ListView list;
        readonly Label details;
        ScrollView listScroll;

        string search = string.Empty;
        int seenVersion = -1;
        bool dirty = true;
        LogEntry selected;

        public static void Build(DebugBuilder builder, LogStore store)
        {
            var view = new LogView(store);
            builder.AddElement(view.root, view);
        }

        LogView(LogStore store)
        {
            this.store = store;
            matches = Matches;

            root = new VisualElement();
            root.AddToClassList("ngdbg-log");

            var toolbar = new VisualElement();
            toolbar.AddToClassList("ngdbg-log-toolbar");
            for (int i = 0; i < filters.Length; i++)
            {
                int kind = i;
                var toggle = new Toggle { name = "ngdbg-log-filter-" + KindNames[i].ToLowerInvariant(), value = true, text = KindNames[i] };
                toggle.AddToClassList("ngdbg-log-filter");
                toggle.RegisterValueChangedCallback(e => { showKind[kind] = e.newValue; dirty = true; });
                filters[i] = toggle;
                toolbar.Add(toggle);
            }
            var clear = new Button(Clear) { text = "Clear" };
            clear.AddToClassList(UssClasses.Button);
            toolbar.Add(clear);
            root.Add(toolbar);

            var searchField = new TextField();
            searchField.AddToClassList("ngdbg-log-search");
            searchField.textEdition.placeholder = "Filter";
            searchField.RegisterValueChangedCallback(e => { search = e.newValue ?? string.Empty; dirty = true; });
            root.Add(searchField);

            list = new ListView(shown, 26, MakeRow, BindRow) { selectionType = SelectionType.Single };
            list.AddToClassList("ngdbg-log-list");
            list.selectionChanged += _ => OnSelectionChanged();
            root.Add(list);

            details = new Label();
            details.AddToClassList("ngdbg-log-details");
            details.selection.isSelectable = true;
            details.style.display = DisplayStyle.None;
            root.Add(details);
        }

        public bool HasError => false;

        public void Invalidate() => dirty = true;

        public void Refresh()
        {
            int version = store.Version;
            if (version == seenVersion && !dirty)
                return;
            seenVersion = version;
            dirty = false;

            bool followNewest = IsScrolledToBottom();
            store.CopyTo(shown, matches);
            list.RefreshItems();
            RestoreSelection();
            if (followNewest && shown.Count > 0)
                list.ScrollToItem(shown.Count - 1);
            UpdateCounts();
        }

        bool Matches(LogEntry e)
        {
            if (!showKind[(int)e.Kind])
                return false;
            return search.Length == 0 || e.Message.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        void UpdateCounts()
        {
            for (int i = 0; i < filters.Length; i++)
            {
                int c = store.CountOf((LogKind)i);
                if (c == shownCounts[i])
                    continue;
                shownCounts[i] = c;
                filters[i].text = KindNames[i] + " " + c;
            }
        }

        void Clear()
        {
            store.Clear();
            selected = null;
            list.ClearSelection();
            dirty = true;
        }

        bool IsScrolledToBottom()
        {
            listScroll ??= list.Q<ScrollView>();
            if (listScroll == null)
                return true;
            var scroller = listScroll.verticalScroller;
            return scroller.value >= scroller.highValue - 1f;
        }

        void RestoreSelection()
        {
            if (selected == null)
                return;
            int index = shown.IndexOf(selected);
            if (index < 0)
                list.ClearSelection();
            else if (list.selectedIndex != index)
                list.SetSelectionWithoutNotify(new[] { index });
        }

        void OnSelectionChanged()
        {
            selected = list.selectedItem as LogEntry;
            if (selected == null)
            {
                details.style.display = DisplayStyle.None;
                return;
            }
            details.text = selected.StackTrace.Length > 0 ? selected.Message + "\n\n" + selected.StackTrace : selected.Message;
            details.style.display = DisplayStyle.Flex;
        }

        static VisualElement MakeRow()
        {
            var row = new Label();
            row.AddToClassList(RowClass);
            return row;
        }

        void BindRow(VisualElement element, int index)
        {
            var entry = shown[index];
            var row = (Label)element;
            row.text = entry.Summary;
            row.EnableInClassList(RowWarningClass, entry.Kind == LogKind.Warning);
            row.EnableInClassList(RowErrorClass, entry.Kind == LogKind.Error);
        }
    }
}
