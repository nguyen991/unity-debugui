using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.UIElements;

namespace Ng.DebugTools
{
    /// <summary>
    /// Conversions between game values (index, bools, enums) and the selection state of a
    /// <see cref="ToggleButtonGroup"/>. Only the (options, length) constructor, the indexer and
    /// <c>length</c> of <see cref="ToggleButtonGroupState"/> are used, so this works on Unity 6000.0.
    /// </summary>
    internal static class ToggleStates
    {
        /// <summary>The most buttons a <see cref="ToggleButtonGroupState"/> can hold.</summary>
        public const int MaxOptions = 64;

        /// <summary>Bit <paramref name="index"/> set when it is in range, otherwise nothing selected.</summary>
        public static ToggleButtonGroupState FromIndex(int index, int count)
        {
            var state = new ToggleButtonGroupState(0, count);
            if (index >= 0 && index < count)
                state[index] = true;
            return state;
        }

        /// <summary>First selected index, or -1 when nothing is selected.</summary>
        public static int ToIndex(ToggleButtonGroupState state)
        {
            for (int i = 0; i < state.length; i++)
                if (state[i])
                    return i;
            return -1;
        }

        public static ToggleButtonGroupState FromBools(Func<int, bool> isOn, int count)
        {
            var state = new ToggleButtonGroupState(0, count);
            for (int i = 0; i < count; i++)
                state[i] = isOn(i);
            return state;
        }

        /// <summary>
        /// The buttons of an enum. Plain enums get one button per distinct value (first declared
        /// name, ordered by value) and select one. <c>[Flags]</c> enums get one button per
        /// single-bit value (ordered by bit) and select several; 0 and combined values get none.
        /// </summary>
        public sealed class EnumOptions<TEnum> where TEnum : struct, Enum
        {
            static EnumOptions<TEnum> cached;

            public string[] Names { get; }
            public bool IsFlags { get; }

            readonly TEnum[] values;   // plain: the value of each button
            readonly ulong[] bits;     // flags: the bit of each button
            readonly ulong buttonMask; // flags: all bits that have a button

            public static EnumOptions<TEnum> Get() => cached ??= new EnumOptions<TEnum>();

            EnumOptions()
            {
                var type = typeof(TEnum);
                IsFlags = type.IsDefined(typeof(FlagsAttribute), false);

                // Fields come in declaration order, so for aliases (B = 1, AlsoB = 1) the first
                // declared name wins; Enum.GetName would pick one arbitrarily.
                var seen = new HashSet<long>();
                var entries = new List<(string name, long value)>();
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    long value = ToLong((TEnum)field.GetValue(null));
                    if (IsFlags && (value == 0 || (value & (value - 1)) != 0))
                        continue; // not a single bit
                    if (seen.Add(value))
                        entries.Add((field.Name, value));
                }
                // Flags: by bit (all positive after ToLong). Plain: by signed value, so Low = -1 comes first.
                entries.Sort((a, b) => a.value.CompareTo(b.value));

                if (entries.Count > MaxOptions)
                    throw new ArgumentException(
                        $"{type.Name} has {entries.Count} toggle options; Toggles supports at most {MaxOptions}.");

                Names = new string[entries.Count];
                values = new TEnum[entries.Count];
                bits = new ulong[entries.Count];
                for (int i = 0; i < entries.Count; i++)
                {
                    Names[i] = entries[i].name;
                    values[i] = FromLong(entries[i].value);
                    bits[i] = unchecked((ulong)entries[i].value);
                    buttonMask |= bits[i];
                }
            }

            public ToggleButtonGroupState ToState(TEnum value)
            {
                var state = new ToggleButtonGroupState(0, Names.Length);
                if (IsFlags)
                {
                    ulong v = unchecked((ulong)ToLong(value));
                    for (int i = 0; i < bits.Length; i++)
                        state[i] = (v & bits[i]) != 0;
                }
                else
                {
                    var comparer = EqualityComparer<TEnum>.Default;
                    for (int i = 0; i < values.Length; i++)
                        if (comparer.Equals(values[i], value))
                        {
                            state[i] = true;
                            break;
                        }
                }
                return state;
            }

            /// <summary>
            /// The enum value for <paramref name="state"/>. Plain: the selected button's value, or
            /// <paramref name="current"/> when none is selected. Flags: <paramref name="current"/>'s
            /// bits that have no button are kept.
            /// </summary>
            public TEnum ToEnum(ToggleButtonGroupState state, TEnum current)
            {
                if (!IsFlags)
                {
                    int index = ToIndex(state);
                    return index >= 0 && index < values.Length ? values[index] : current;
                }

                ulong result = unchecked((ulong)ToLong(current)) & ~buttonMask;
                for (int i = 0; i < bits.Length && i < state.length; i++)
                    if (state[i])
                        result |= bits[i];
                return FromLong(unchecked((long)result));
            }

            // Enum <-> long without boxing (the getter runs every frame while the panel is visible).
            // Signed underlying types are sign-extended, unsigned ones zero-extended.
            static readonly int Size = UnsafeUtility.SizeOf<TEnum>();
            static readonly bool Signed = IsSigned(Enum.GetUnderlyingType(typeof(TEnum)));

            static bool IsSigned(Type t) => t == typeof(sbyte) || t == typeof(short) || t == typeof(int) || t == typeof(long);

            static long ToLong(TEnum value)
            {
                switch (Size)
                {
                    case 1: return Signed ? UnsafeUtility.As<TEnum, sbyte>(ref value) : UnsafeUtility.As<TEnum, byte>(ref value);
                    case 2: return Signed ? UnsafeUtility.As<TEnum, short>(ref value) : UnsafeUtility.As<TEnum, ushort>(ref value);
                    case 4: return Signed ? UnsafeUtility.As<TEnum, int>(ref value) : UnsafeUtility.As<TEnum, uint>(ref value);
                    default: return UnsafeUtility.As<TEnum, long>(ref value);
                }
            }

            static TEnum FromLong(long value)
            {
                TEnum result = default;
                switch (Size)
                {
                    case 1: UnsafeUtility.As<TEnum, byte>(ref result) = unchecked((byte)value); break;
                    case 2: UnsafeUtility.As<TEnum, ushort>(ref result) = unchecked((ushort)value); break;
                    case 4: UnsafeUtility.As<TEnum, uint>(ref result) = unchecked((uint)value); break;
                    default: UnsafeUtility.As<TEnum, long>(ref result) = value; break;
                }
                return result;
            }
        }
    }
}
