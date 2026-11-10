using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Ng.DebugTools
{
    internal sealed partial class DebugBuilder
    {
        public IDebugBuilder Toggles(string label, string[] options, Func<int> get, Action<int> set = null,
                                     bool allowNone = false, float buttonWidth = 0)
        {
            var toggles = CreateToggleGroup(label, options, multiple: false, allowNone, buttonWidth);
            int count = options.Length;
            return AddField(toggles, UssClasses.Toggles,
                () => ToggleStates.FromIndex(get(), count),
                set == null ? null : s => set(ToggleStates.ToIndex(s)),
                null);
        }

        public IDebugBuilder Toggles(string label, string[] options, Func<int, bool> isOn, Action<int, bool> set = null,
                                     bool allowNone = true, float buttonWidth = 0)
        {
            var toggles = CreateToggleGroup(label, options, multiple: true, allowNone, buttonWidth);
            int count = options.Length;
            return AddField(toggles, UssClasses.Toggles,
                () => ToggleStates.FromBools(isOn, count),
                set == null ? null : s =>
                {
                    // Compare with the game's current values, so only the options the user changed
                    // reach the setter.
                    for (int i = 0; i < count; i++)
                        if (isOn(i) != s[i])
                            set(i, s[i]);
                },
                null);
        }

        public IDebugBuilder Toggles<TEnum>(string label, Func<TEnum> get, Action<TEnum> set = null,
                                            bool allowNone = true, float buttonWidth = 0) where TEnum : struct, Enum
        {
            var options = ToggleStates.EnumOptions<TEnum>.Get();
            var toggles = CreateToggleGroup(label, options.Names, options.IsFlags, options.IsFlags && allowNone, buttonWidth);
            return AddField(toggles, UssClasses.Toggles,
                () => options.ToState(get()),
                set == null ? null : s => set(options.ToEnum(s, get())),
                null);
        }

        /// <summary>
        /// A <see cref="ToggleButtonGroup"/> with one button per name. Validates before anything is
        /// added to the panel, so a bad call leaves only the "Build failed" line.
        /// </summary>
        static ToggleButtonGroup CreateToggleGroup(string label, IReadOnlyList<string> names, bool multiple,
                                                   bool allowNone, float buttonWidth)
        {
            if (names == null)
                throw new ArgumentNullException("options");
            if (names.Count > ToggleStates.MaxOptions)
                throw new ArgumentException(
                    $"Toggles \"{label}\" has {names.Count} options; at most {ToggleStates.MaxOptions} are supported.", "options");

            var toggles = new ToggleButtonGroup(label)
            {
                isMultipleSelection = multiple,
                allowEmptySelection = allowNone,
            };
            foreach (var name in names)
            {
                var button = new Button { text = name };
                if (buttonWidth > 0)
                    button.style.width = buttonWidth;
                toggles.Add(button);
            }
            // Start from "nothing selected" with the right length (the default state is 64 long
            // with the first option on); the first refresh then shows the game's value.
            toggles.SetValueWithoutNotify(new ToggleButtonGroupState(0, names.Count));
            return toggles;
        }
    }
}
