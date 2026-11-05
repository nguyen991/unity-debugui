using System;
using System.Collections.Generic;

namespace Ng.DebugTools
{
    /// <summary>
    /// A tree of bindings. A closed group (e.g. a collapsed foldout) skips its bindings
    /// and all of its children, so hidden controls cost nothing.
    /// </summary>
    internal sealed class BindingGroup
    {
        readonly Func<bool> isOpen;
        readonly List<IBinding> bindings = new List<IBinding>();
        readonly List<BindingGroup> children = new List<BindingGroup>();

        /// <param name="isOpen">Null means always open.</param>
        public BindingGroup(Func<bool> isOpen = null)
        {
            this.isOpen = isOpen;
        }

        public void Add(IBinding binding) => bindings.Add(binding);

        public BindingGroup AddChild(Func<bool> childIsOpen)
        {
            var child = new BindingGroup(childIsOpen);
            children.Add(child);
            return child;
        }

        public void Refresh()
        {
            if (isOpen != null && !isOpen())
                return;
            for (int i = 0; i < bindings.Count; i++)
                bindings[i].Refresh();
            for (int i = 0; i < children.Count; i++)
                children[i].Refresh();
        }

        public void InvalidateAll()
        {
            for (int i = 0; i < bindings.Count; i++)
                bindings[i].Invalidate();
            for (int i = 0; i < children.Count; i++)
                children[i].InvalidateAll();
        }
    }
}
