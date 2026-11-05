using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ng.DebugUI
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

        public void Vertical(Action<IDebugBuilder> build) => Nest(UssClasses.Vertical, build);

        public void Horizontal(Action<IDebugBuilder> build) => Nest(UssClasses.Horizontal, build);

        public void Foldout(string title, Action<IDebugBuilder> build, bool open = false)
        {
            var foldout = new Foldout { text = title, value = open };
            foldout.AddToClassList(UssClasses.Foldout);
            container.Add(foldout);
            var child = group.AddChild(() => foldout.value);
            build?.Invoke(new DebugBuilder(foldout.contentContainer, child));
        }

        void Nest(string className, Action<IDebugBuilder> build)
        {
            var element = new VisualElement();
            element.AddToClassList(className);
            container.Add(element);
            build?.Invoke(new DebugBuilder(element, group));
        }

        // ---------- Controls ----------

        public void Label(string text)
        {
            var label = new Label(text);
            label.AddToClassList(UssClasses.Text);
            container.Add(label);
        }

        public void Label<T>(string label, Func<T> getter)
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
        }

        public void Button(string text, Action onClick)
        {
            var button = new Button(() =>
            {
                try { onClick?.Invoke(); }
                catch (Exception e) { Debug.LogException(e); }
            }) { text = text };
            button.AddToClassList(UssClasses.Button);
            container.Add(button);
        }

        public void Slider(string label, Func<float> get, Action<float> set, float min, float max)
            => AddField(new Slider(label, min, max) { showInputField = true }, UssClasses.Slider, get, set, null);

        public void Slider(string label, Func<int> get, Action<int> set, int min, int max)
            => AddField(new SliderInt(label, min, max) { showInputField = true }, UssClasses.Slider, get, set, null);

        public void Field(string label, Func<int> get, Action<int> set = null)
            => AddField(new IntegerField(label) { isDelayed = true }, UssClasses.Field, get, set, null);

        public void Field(string label, Func<float> get, Action<float> set = null)
            => AddField(new FloatField(label) { isDelayed = true }, UssClasses.Field, get, set, null);

        public void Field(string label, Func<string> get, Action<string> set = null)
            => AddField(new TextField(label) { isDelayed = true }, UssClasses.Field, get, set, v => v ?? string.Empty);

        public void Field(string label, Func<bool> get, Action<bool> set = null)
            => AddField(new Toggle(label), UssClasses.Field, get, set, null);

        /// <summary>
        /// Shared wiring for every value control: getter → SetValueWithoutNotify (never echoes back
        /// into the setter), user change → setter, and "editing" while focused or dragging.
        /// </summary>
        void AddField<T>(BaseField<T> field, string className, Func<T> get, Action<T> set, Func<T, T> toDisplay)
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
                return;
            }

            field.RegisterValueChangedCallback(e => binding.Write(e.newValue));
            field.RegisterCallback<FocusInEvent>(_ => binding.IsEditing = true, TrickleDown.TrickleDown);
            field.RegisterCallback<FocusOutEvent>(_ => binding.IsEditing = false, TrickleDown.TrickleDown);
            field.RegisterCallback<PointerCaptureEvent>(_ => binding.IsEditing = true, TrickleDown.TrickleDown);
            field.RegisterCallback<PointerCaptureOutEvent>(_ => binding.IsEditing = false, TrickleDown.TrickleDown);
        }
    }
}
