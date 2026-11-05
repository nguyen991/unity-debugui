using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ng.DebugTools
{
    /// <summary>Builds UI Toolkit elements into a container and registers their bindings in a group.</summary>
    internal sealed class DebugBuilder : IDebugBuilder
    {
        readonly VisualElement container;
        readonly BindingGroup group;

        public DebugBuilder(VisualElement container, BindingGroup group)
        {
            this.container = container;
            this.group = group;
        }

        // ---------- Layout ----------

        public IDebugBuilder Vertical(Action<IDebugBuilder> build) => Nest(UssClasses.Vertical, build);

        public IDebugBuilder Horizontal(Action<IDebugBuilder> build) => Nest(UssClasses.Horizontal, build);

        public IDebugBuilder Foldout(string title, Action<IDebugBuilder> build, bool open = false)
        {
            var foldout = new Foldout { text = title, value = open };
            foldout.AddToClassList(UssClasses.Foldout);
            container.Add(foldout);
            var child = group.AddChild(() => foldout.value);
            build?.Invoke(new DebugBuilder(foldout.contentContainer, child));
            return this;
        }

        public IDebugBuilder Line()
        {
            var line = new VisualElement { pickingMode = PickingMode.Ignore };
            line.AddToClassList(UssClasses.Line);
            container.Add(line);
            return this;
        }

        public IDebugBuilder Space(float size = 8f)
        {
            // flex-basis is the size along the parent's direction, so one element works as vertical
            // space in a column and horizontal space in a row. Inline values beat the
            // ".ngdbg-horizontal > *" rule that makes row children share the width.
            var space = new VisualElement { pickingMode = PickingMode.Ignore };
            space.AddToClassList(UssClasses.Space);
            space.style.flexBasis = Mathf.Max(0f, size);
            space.style.flexGrow = 0f;
            space.style.flexShrink = 0f;
            container.Add(space);
            return this;
        }

        IDebugBuilder Nest(string className, Action<IDebugBuilder> build)
        {
            var element = new VisualElement();
            element.AddToClassList(className);
            container.Add(element);
            build?.Invoke(new DebugBuilder(element, group));
            return this;
        }

        // ---------- Controls ----------

        public IDebugBuilder Label(string text)
        {
            var label = new Label(text);
            label.AddToClassList(UssClasses.Text);
            container.Add(label);
            return this;
        }

        public IDebugBuilder Label<T>(string label, Func<T> getter)
        {
            var row = new VisualElement();
            row.AddToClassList(UssClasses.Row);
            var name = new Label(label);
            name.AddToClassList(UssClasses.RowLabel);
            var value = new Label();
            value.AddToClassList(UssClasses.Value);
            row.Add(name);
            row.Add(value);
            container.Add(row);

            group.Add(new ValueBinding<T>(row, getter, null,
                v => value.text = v == null ? "null" : v.ToString()));
            return this;
        }

        public IDebugBuilder Button(string text, Action onClick, Color? color = null)
        {
            var button = new Button(() =>
            {
                try { onClick?.Invoke(); }
                catch (Exception e) { Debug.LogException(e); }
            }) { text = text };
            button.AddToClassList(UssClasses.Button);
            if (color.HasValue)
            {
                // Inline color beats the theme's :hover/:active backgrounds, so the
                // "--colored" modifier gives hover/press feedback through opacity instead.
                button.AddToClassList(UssClasses.ButtonColored);
                button.style.backgroundColor = color.Value;
                button.style.color = ReadableTextColor(color.Value);
            }
            container.Add(button);
            return this;
        }

        /// <summary>Dark text on light backgrounds, white text on dark ones.</summary>
        static Color ReadableTextColor(Color background)
        {
            float luminance = 0.2126f * background.linear.r + 0.7152f * background.linear.g + 0.0722f * background.linear.b;
            return luminance > 0.35f ? new Color(0.08f, 0.08f, 0.1f) : Color.white;
        }

        public IDebugBuilder Slider(string label, Func<float> get, Action<float> set, float min, float max)
            => AddField(new Slider(label, min, max) { showInputField = true }, UssClasses.Slider, get, set, null);

        public IDebugBuilder Slider(string label, Func<int> get, Action<int> set, int min, int max)
            => AddField(new SliderInt(label, min, max) { showInputField = true }, UssClasses.Slider, get, set, null);

        public IDebugBuilder Field(string label, Func<int> get, Action<int> set = null)
            => AddField(new IntegerField(label) { isDelayed = true }, UssClasses.Field, get, set, null);

        public IDebugBuilder Field(string label, Func<float> get, Action<float> set = null)
            => AddField(new FloatField(label) { isDelayed = true }, UssClasses.Field, get, set, null);

        public IDebugBuilder Field(string label, Func<string> get, Action<string> set = null)
            => AddField(new TextField(label) { isDelayed = true }, UssClasses.Field, get, set, v => v ?? string.Empty);

        public IDebugBuilder Field(string label, Func<bool> get, Action<bool> set = null)
            => AddField(new Toggle(label), UssClasses.Field, get, set, null);

        /// <summary>
        /// Shared wiring for every value control: getter → SetValueWithoutNotify (never echoes back
        /// into the setter), user change → setter, and "editing" while focused or dragging.
        /// </summary>
        IDebugBuilder AddField<T>(BaseField<T> field, string className, Func<T> get, Action<T> set, Func<T, T> toDisplay)
        {
            field.AddToClassList(className);
            container.Add(field);

            var binding = new ValueBinding<T>(field, get, set,
                v => field.SetValueWithoutNotify(toDisplay != null ? toDisplay(v) : v));
            group.Add(binding);

            if (set == null)
            {
                if (field is TextInputBaseField<T> input)
                    input.isReadOnly = true;
                else
                    field.SetEnabled(false);
                return this;
            }

            field.RegisterValueChangedCallback(e => binding.Write(e.newValue));

            // "Editing" = a text input inside this control has focus (typing), or the control holds
            // pointer capture (dragging). Tracked separately: text selection captures and releases
            // the pointer while the field keeps focus, and a slider keeps focus after a drag even
            // though the user is no longer changing it.
            bool typing = false, dragging = false;
            void Update() => binding.IsEditing = typing || dragging;
            field.RegisterCallback<FocusInEvent>(e => { typing = IsInTextInput(e.target as VisualElement, field); Update(); }, TrickleDown.TrickleDown);
            field.RegisterCallback<FocusOutEvent>(_ => { typing = false; Update(); }, TrickleDown.TrickleDown);
            field.RegisterCallback<PointerCaptureEvent>(_ => { dragging = true; Update(); }, TrickleDown.TrickleDown);
            field.RegisterCallback<PointerCaptureOutEvent>(_ => { dragging = false; Update(); }, TrickleDown.TrickleDown);
            return this;
        }

        const string TextInputClass = "unity-base-text-field"; // TextInputBaseField<T>.ussClassName

        /// <summary>True if <paramref name="target"/> is, or is inside, a text input within <paramref name="field"/>.</summary>
        static bool IsInTextInput(VisualElement target, VisualElement field)
        {
            for (var e = target; e != null; e = e.parent)
            {
                if (e.ClassListContains(TextInputClass))
                    return true;
                if (e == field)
                    break;
            }
            return false;
        }
    }
}
