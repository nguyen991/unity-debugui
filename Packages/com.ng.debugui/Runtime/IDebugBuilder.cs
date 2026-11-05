using System;

namespace Ng.DebugUI
{
    /// <summary>
    /// Declares the content of a debug panel. Every value control binds to the game with a
    /// getter (polled while visible) and an optional setter (called when the user edits).
    /// Passing a null setter makes the control read-only.
    /// </summary>
    public interface IDebugBuilder
    {
        // Layout

        /// <summary>Stacks children top to bottom.</summary>
        void Vertical(Action<IDebugBuilder> build);

        /// <summary>Lays children out left to right, sharing the width.</summary>
        void Horizontal(Action<IDebugBuilder> build);

        /// <summary>A collapsible section. Getters inside are not called while it is closed.</summary>
        void Foldout(string title, Action<IDebugBuilder> build, bool open = false);

        // Controls

        /// <summary>Static text.</summary>
        void Label(string text);

        /// <summary>A read-only "label: value" row that follows the getter.</summary>
        void Label<T>(string label, Func<T> getter);

        void Button(string text, Action onClick);

        void Slider(string label, Func<float> get, Action<float> set, float min, float max);
        void Slider(string label, Func<int> get, Action<int> set, int min, int max);

        /// <summary>Editable number. The setter is called when the user presses Enter or leaves the field.</summary>
        void Field(string label, Func<int> get, Action<int> set = null);
        void Field(string label, Func<float> get, Action<float> set = null);
        void Field(string label, Func<string> get, Action<string> set = null);

        /// <summary>Shown as a toggle.</summary>
        void Field(string label, Func<bool> get, Action<bool> set = null);
    }
}
