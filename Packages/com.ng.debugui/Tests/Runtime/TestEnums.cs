using System;

namespace Ng.DebugTools.Tests
{
    // Enums shared by the Toggles tests.
    enum Difficulty { Easy, Normal, Hard }

    enum Alias { A = 0, B = 1, AlsoB = 1, C = 2 }

    [Flags]
    enum Cheats { None = 0, God = 1, Ammo = 2, Fly = 4, All = God | Ammo | Fly }

    enum Signed : sbyte { Low = -1, Mid = 0, High = 1 }

    [Flags]
    enum IntTop { A = 1, Top = int.MinValue }

    [Flags]
    enum ULongTop : ulong { A = 1, Top = 1UL << 63 }

    enum UBig : ulong { Small = 1, Huge = ulong.MaxValue }

    /// <summary>65 values: one more than ToggleButtonGroup supports.</summary>
    enum Big { V0, V1, V2, V3, V4, V5, V6, V7, V8, V9, V10, V11, V12, V13, V14, V15, V16, V17, V18, V19, V20, V21, V22, V23, V24, V25, V26, V27, V28, V29, V30, V31, V32, V33, V34, V35, V36, V37, V38, V39, V40, V41, V42, V43, V44, V45, V46, V47, V48, V49, V50, V51, V52, V53, V54, V55, V56, V57, V58, V59, V60, V61, V62, V63, V64 }
}
