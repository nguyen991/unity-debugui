using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ng.DebugTools
{
    /// <summary>
    /// A section built once per item of a list. On refresh it compares the list with the items it
    /// was built from (count and Equals, i.e. reference identity for classes) and rebuilds the
    /// section only when they differ; otherwise it just refreshes the items' own bindings.
    /// The comparison walks the list by index, so an unchanged list costs no allocations.
    /// </summary>
    internal sealed class EachBinding<T> : IBinding
    {
        static readonly EqualityComparer<T> Comparer = EqualityComparer<T>.Default;

        readonly VisualElement container;
        readonly Func<IReadOnlyList<T>> items;
        readonly Action<T, IDebugBuilder> build;
        readonly List<T> builtFrom = new List<T>();

        BindingGroup itemBindings = new BindingGroup();
        bool built;
        bool logged;

        public EachBinding(VisualElement container, Func<IReadOnlyList<T>> items, Action<T, IDebugBuilder> build)
        {
            this.container = container;
            this.items = items;
            this.build = build;
        }

        public bool HasError { get; private set; }

        public void Refresh()
        {
            IReadOnlyList<T> list;
            try
            {
                list = items?.Invoke();
            }
            catch (Exception e)
            {
                MarkError(e);
                return;
            }
            ClearError();

            if (!built || Changed(list))
                Rebuild(list);

            itemBindings.Refresh();
        }

        public void Invalidate() => itemBindings.InvalidateAll();

        bool Changed(IReadOnlyList<T> list)
        {
            int count = list?.Count ?? 0;
            if (count != builtFrom.Count)
                return true;
            for (int i = 0; i < count; i++)
                if (!Comparer.Equals(list[i], builtFrom[i]))
                    return true;
            return false;
        }

        void Rebuild(IReadOnlyList<T> list)
        {
            built = true;
            container.Clear();
            builtFrom.Clear();
            itemBindings = new BindingGroup();

            int count = list?.Count ?? 0;
            var builder = new DebugBuilder(container, itemBindings);
            for (int i = 0; i < count; i++)
            {
                var item = list[i];
                builtFrom.Add(item);
                try
                {
                    build?.Invoke(item, builder);
                }
                catch (Exception e)
                {
                    // Keep the other items; show where it failed.
                    var error = new Label("Item failed: " + e.Message);
                    error.AddToClassList(UssClasses.Error);
                    container.Add(error);
                    Debug.LogException(e);
                }
            }
        }

        void MarkError(Exception e)
        {
            if (!HasError)
            {
                HasError = true;
                container.AddToClassList(UssClasses.Error);
            }
            if (!logged)
            {
                logged = true;
                Debug.LogException(e);
            }
        }

        void ClearError()
        {
            if (!HasError)
                return;
            HasError = false;
            container.RemoveFromClassList(UssClasses.Error);
        }
    }
}
