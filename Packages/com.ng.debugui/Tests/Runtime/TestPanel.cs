using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ng.DebugUI.Tests
{
    /// <summary>
    /// A throwaway runtime UIDocument so test elements live in a real panel.
    /// UI Toolkit only dispatches ChangeEvent (and other events) to elements attached to a panel.
    /// </summary>
    sealed class TestPanel : IDisposable
    {
        readonly GameObject go;
        readonly PanelSettings settings;

        public TestPanel()
        {
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            go = new GameObject("[TestPanel]");
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = settings;
            Root = doc.rootVisualElement;
        }

        public VisualElement Root { get; }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(settings);
        }
    }
}
