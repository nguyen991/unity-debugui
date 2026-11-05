using System;
using UnityEngine;

namespace Ng.DebugTools
{
    /// <summary>
    /// Declares the content of a debug panel. Every value control binds to the game with a
    /// getter (polled while visible) and an optional setter (called when the user edits).
    /// Passing a null setter makes the control read-only.
    /// Every method returns the same builder, so calls can be chained:
    /// <c>p.Label("Build 1.2.3").Label("HP", () => hp).Button("Heal", Heal);</c>
    /// Layout methods return the outer builder, so the chain continues at the same level.
    /// </summary>
    public interface IDebugBuilder
    {
        // Layout

        /// <summary>Stacks children top to bottom.</summary>
        IDebugBuilder Vertical(Action<IDebugBuilder> build);

        /// <summary>Lays children out left to right, sharing the width.</summary>
        IDebugBuilder Horizontal(Action<IDebugBuilder> build);

        /// <summary>A collapsible section. Getters inside are not called while it is closed.</summary>
        IDebugBuilder Foldout(string title, Action<IDebugBuilder> build, bool open = false);

        /// <summary>A thin separator line: horizontal in a column, vertical inside <see cref="Horizontal"/>.</summary>
        IDebugBuilder Line();

        /// <summary>Empty space along the layout direction: height in a column, width inside <see cref="Horizontal"/>.</summary>
        IDebugBuilder Space(float size = 8f);

        // Controls

        /// <summary>Static text.</summary>
        IDebugBuilder Label(string text);

        /// <summary>A read-only "label: value" row that follows the getter.</summary>
        IDebugBuilder Label<T>(string label, Func<T> getter);

        /// <summary>
        /// A button. Pass <paramref name="color"/> to tint its background (text turns dark or light
        /// automatically to stay readable); leave it out for the theme color.
        /// </summary>
        IDebugBuilder Button(string text, Action onClick, Color? color = null);

        IDebugBuilder Slider(string label, Func<float> get, Action<float> set, float min, float max);
        IDebugBuilder Slider(string label, Func<int> get, Action<int> set, int min, int max);

        /// <summary>Editable number. The setter is called when the user presses Enter or leaves the field.</summary>
        IDebugBuilder Field(string label, Func<int> get, Action<int> set = null);
        IDebugBuilder Field(string label, Func<float> get, Action<float> set = null);
        IDebugBuilder Field(string label, Func<string> get, Action<string> set = null);

        /// <summary>Shown as a toggle.</summary>
        IDebugBuilder Field(string label, Func<bool> get, Action<bool> set = null);
    }
}
