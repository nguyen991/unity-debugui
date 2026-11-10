using System;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Ng.DebugTools.Tests
{
    public class ToggleStatesTests
    {
        static ToggleButtonGroupState State(string bits)
        {
            var s = new ToggleButtonGroupState(0, bits.Length);
            for (int i = 0; i < bits.Length; i++)
                s[i] = bits[i] == '1';
            return s;
        }

        static string Bits(ToggleButtonGroupState s)
        {
            var c = new char[s.length];
            for (int i = 0; i < s.length; i++)
                c[i] = s[i] ? '1' : '0';
            return new string(c);
        }

        // ---------- Index / bools ----------

        [Test]
        public void FromIndex_SetsOneBit()
        {
            Assert.That(Bits(ToggleStates.FromIndex(1, 3)), Is.EqualTo("010"));
        }

        [Test]
        public void FromIndex_OutOfRange_Empty()
        {
            Assert.That(Bits(ToggleStates.FromIndex(-1, 3)), Is.EqualTo("000"));
            Assert.That(Bits(ToggleStates.FromIndex(3, 3)), Is.EqualTo("000"));
        }

        [Test]
        public void ToIndex_FirstActive_OrMinusOne()
        {
            Assert.That(ToggleStates.ToIndex(State("010")), Is.EqualTo(1));
            Assert.That(ToggleStates.ToIndex(State("000")), Is.EqualTo(-1));
        }

        [Test]
        public void FromBools_ReadsEachIndex()
        {
            Assert.That(Bits(ToggleStates.FromBools(i => i != 1, 3)), Is.EqualTo("101"));
        }

        // ---------- Enums ----------

        [Test]
        public void EnumOptions_Plain_NamesInValueOrder()
        {
            var o = ToggleStates.EnumOptions<Difficulty>.Get();
            Assert.That(o.Names, Is.EqualTo(new[] { "Easy", "Normal", "Hard" }));
            Assert.That(o.IsFlags, Is.False);
        }

        [Test]
        public void EnumOptions_Alias_FirstNameOnly()
        {
            Assert.That(ToggleStates.EnumOptions<Alias>.Get().Names, Is.EqualTo(new[] { "A", "B", "C" }));
        }

        [Test]
        public void EnumOptions_Flags_SingleBitsOnly()
        {
            var o = ToggleStates.EnumOptions<Cheats>.Get();
            Assert.That(o.Names, Is.EqualTo(new[] { "God", "Ammo", "Fly" }));
            Assert.That(o.IsFlags, Is.True);
        }

        [Test]
        public void EnumOptions_NegativeValues_Work()
        {
            var o = ToggleStates.EnumOptions<Signed>.Get();
            Assert.That(o.Names, Is.EqualTo(new[] { "Low", "Mid", "High" }));
            Assert.That(o.ToState(Signed.Low)[0], Is.True);
            Assert.That(o.ToEnum(State("001"), Signed.Mid), Is.EqualTo(Signed.High));
        }

        [Test]
        public void ToState_Plain_UnknownValue_Empty()
        {
            Assert.That(Bits(ToggleStates.EnumOptions<Difficulty>.Get().ToState((Difficulty)99)), Is.EqualTo("000"));
        }

        [Test]
        public void ToState_Flags()
        {
            Assert.That(Bits(ToggleStates.EnumOptions<Cheats>.Get().ToState(Cheats.God | Cheats.Fly)), Is.EqualTo("101"));
        }

        [Test]
        public void ToEnum_Plain()
        {
            var o = ToggleStates.EnumOptions<Difficulty>.Get();
            Assert.That(o.ToEnum(State("001"), Difficulty.Easy), Is.EqualTo(Difficulty.Hard));
            Assert.That(o.ToEnum(State("000"), Difficulty.Normal), Is.EqualTo(Difficulty.Normal));
        }

        [Test]
        public void ToEnum_Flags_KeepsBitsWithoutButton()
        {
            var o = ToggleStates.EnumOptions<Cheats>.Get();
            Assert.That(o.ToEnum(State("010"), (Cheats)(8 | 1)), Is.EqualTo((Cheats)(8 | 2)));
        }

        [Test]
        public void Flags_SignedTopBit_HasButton()
        {
            var o = ToggleStates.EnumOptions<IntTop>.Get();
            Assert.That(o.Names, Is.EqualTo(new[] { "A", "Top" }));
            Assert.That(Bits(o.ToState(IntTop.Top)), Is.EqualTo("01"));
            Assert.That(o.ToEnum(State("11"), 0), Is.EqualTo(IntTop.A | IntTop.Top));
        }

        [Test]
        public void Flags_ULongBit63_LastAndRoundTrips()
        {
            var o = ToggleStates.EnumOptions<ULongTop>.Get();
            Assert.That(o.Names, Is.EqualTo(new[] { "A", "Top" }));
            Assert.That(o.ToEnum(State("01"), 0), Is.EqualTo(ULongTop.Top));
        }

        [Test]
        public void Plain_UnsignedHugeValue_SortsLast()
        {
            var o = ToggleStates.EnumOptions<UBig>.Get();
            Assert.That(o.Names, Is.EqualTo(new[] { "Small", "Huge" }));
            Assert.That(o.ToEnum(State("01"), UBig.Small), Is.EqualTo(UBig.Huge));
        }

        [Test]
        public void EnumOptions_MoreThan64_Throws()
        {
            Assert.Throws<ArgumentException>(() => ToggleStates.EnumOptions<Big>.Get());
        }
    }
}
