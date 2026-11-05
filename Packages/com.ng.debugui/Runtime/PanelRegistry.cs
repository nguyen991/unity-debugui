using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ng.DebugTools
{
    /// <summary>One registered panel: its built UI and the bindings that keep it live.</summary>
    internal sealed class PanelEntry
    {
        public string Name;
        public UnityEngine.Object Owner;

        /// <summary>True when an owner was given (even one that is already destroyed).</summary>
        public bool HasOwner;

        public VisualElement Root;
        public BindingGroup Bindings;

        /// <summary>Unity's overloaded == reports destroyed objects as null.</summary>
        public bool IsOwnerDestroyed => HasOwner && Owner == null;
    }

    /// <summary>Ordered list of panels, keyed by name. Registering a name again replaces it in place.</summary>
    internal sealed class PanelRegistry
    {
        readonly List<PanelEntry> panels = new List<PanelEntry>();

        public IReadOnlyList<PanelEntry> Panels => panels;

        public event Action Changed;

        public PanelEntry AddOrReplace(UnityEngine.Object owner, string name, Action<IDebugBuilder> build)
        {
            var entry = new PanelEntry
            {
                Name = name,
                Owner = owner,
                HasOwner = !ReferenceEquals(owner, null),
                Root = new VisualElement(),
                Bindings = new BindingGroup(),
            };
            entry.Root.AddToClassList(UssClasses.Panel);

            try
            {
                build?.Invoke(new DebugBuilder(entry.Root, entry.Bindings));
            }
            catch (Exception e)
            {
                var error = new Label("Build failed: " + e.Message);
                error.AddToClassList(UssClasses.Error);
                entry.Root.Add(error);
                Debug.LogException(e);
            }

            int index = IndexOf(name);
            if (index >= 0)
                panels[index] = entry;
            else
                panels.Add(entry);

            Changed?.Invoke();
            return entry;
        }

        public bool Remove(string name)
        {
            int index = IndexOf(name);
            if (index < 0)
                return false;
            panels.RemoveAt(index);
            Changed?.Invoke();
            return true;
        }

        public int RemoveDestroyedOwners()
        {
            int removed = 0;
            for (int i = panels.Count - 1; i >= 0; i--)
            {
                if (panels[i].IsOwnerDestroyed)
                {
                    panels.RemoveAt(i);
                    removed++;
                }
            }
            if (removed > 0)
                Changed?.Invoke();
            return removed;
        }

        public PanelEntry Find(string name)
        {
            int index = IndexOf(name);
            return index >= 0 ? panels[index] : null;
        }

        int IndexOf(string name)
        {
            for (int i = 0; i < panels.Count; i++)
                if (panels[i].Name == name)
                    return i;
            return -1;
        }
    }
}
