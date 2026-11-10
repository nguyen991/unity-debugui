using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Ng.DebugTools.Tests
{
    public class TogglesTests
    {
        static readonly string[] Weapons = { "Sword", "Bow", "Staff" };
        static readonly string[] Shows = { "FPS", "Grid", "Hitbox" };

        TestPanel panel;
        VisualElement root;
        BindingGroup group;
        IDebugBuilder b;

        [SetUp]
        public void SetUp()
        {
            TestUtil.Reset();
            panel = new TestPanel();
            root = new VisualElement();
            panel.Root.Add(root);
            group = new BindingGroup();
            b = new DebugBuilder(root, group);
        }

        [TearDown]
        public void TearDown()
        {
            panel.Dispose();
            TestUtil.Reset();
        }

        ToggleButtonGroup Group => root.Q<ToggleButtonGroup>();

        List<Button> Buttons => Group.Query<Button>().ToList();

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

        // ---------- Single select by index ----------

        [Test]
        public void Toggles_Index_BuildsOneButtonPerOption()
        {
            int weapon = 0;
            b.Toggles("Weapon", Weapons, () => weapon, v => weapon = v);
            Assert.That(Buttons.Select(x => x.text), Is.EqualTo(Weapons));
            Assert.That(Group.ClassListContains("ngdbg-toggles"), Is.True);
            Assert.That(Group.label, Is.EqualTo("Weapon"));
            Assert.That(Group.isMultipleSelection, Is.False);
            Assert.That(Group.allowEmptySelection, Is.False);
        }

        [Test]
        public void Toggles_Index_ShowsGameValue()
        {
            int weapon = 1;
            b.Toggles("Weapon", Weapons, () => weapon, v => weapon = v);
            group.Refresh();
            Assert.That(Bits(Group.value), Is.EqualTo("010"));
        }

        [Test]
        public void Toggles_Index_FollowsGameValue()
        {
            int weapon = 1;
            b.Toggles("Weapon", Weapons, () => weapon, v => weapon = v);
            group.Refresh();
            weapon = 2;
            group.Refresh();
            Assert.That(Bits(Group.value), Is.EqualTo("001"));
        }

        [Test]
        public void Toggles_Index_UserChange_CallsSetter()
        {
            int weapon = 0;
            b.Toggles("Weapon", Weapons, () => weapon, v => weapon = v);
            group.Refresh();
            Group.value = State("001");
            Assert.That(weapon, Is.EqualTo(2));
        }

        [Test]
        public void Toggles_Index_OutOfRange_ShowsNothing_NoSetterCall()
        {
            int weapon = 7, calls = 0;
            b.Toggles("Weapon", Weapons, () => weapon, v => { calls++; weapon = v; });
            Assert.DoesNotThrow(() => group.Refresh());
            Assert.That(Bits(Group.value), Is.EqualTo("000"));
            Assert.That(calls, Is.EqualTo(0));
        }

        [Test]
        public void Toggles_Index_AllowNone_TapSelectedClears()
        {
            int weapon = 1;
            b.Toggles("Weapon", Weapons, () => weapon, v => weapon = v, allowNone: true);
            group.Refresh();
            TestUtil.Click(Buttons[1]);
            Assert.That(weapon, Is.EqualTo(-1));
        }

        [Test]
        public void Toggles_Index_NoAllowNone_TapSelectedKeeps()
        {
            int weapon = 1;
            b.Toggles("Weapon", Weapons, () => weapon, v => weapon = v);
            group.Refresh();
            TestUtil.Click(Buttons[1]);
            Assert.That(weapon, Is.EqualTo(1));
        }

        [Test]
        public void Toggles_Index_TapOther_Selects()
        {
            int weapon = 1;
            b.Toggles("Weapon", Weapons, () => weapon, v => weapon = v);
            group.Refresh();
            TestUtil.Click(Buttons[2]);
            Assert.That(weapon, Is.EqualTo(2));
        }

        // ---------- Multi select by bool ----------

        [Test]
        public void Toggles_Multi_ShowsEachBool()
        {
            var show = new[] { true, false, true };
            b.Toggles("Show", Shows, i => show[i], (i, on) => show[i] = on);
            group.Refresh();
            Assert.That(Bits(Group.value), Is.EqualTo("101"));
            Assert.That(Group.isMultipleSelection, Is.True);
            Assert.That(Group.allowEmptySelection, Is.True);
        }

        [Test]
        public void Toggles_Multi_UserChange_CallsSetterOnlyForChanged()
        {
            var show = new[] { true, false, true };
            var calls = new List<(int, bool)>();
            b.Toggles("Show", Shows, i => show[i], (i, on) => { calls.Add((i, on)); show[i] = on; });
            group.Refresh();
            Group.value = State("111");
            Assert.That(calls, Is.EqualTo(new List<(int, bool)> { (1, true) }));
        }

        [Test]
        public void Toggles_Multi_TapOnStaleDisplay_KeepsGameChangeOfOtherOptions()
        {
            var show = new[] { true, false, false };
            b.Toggles("Show", Shows, i => show[i], (i, on) => show[i] = on);
            group.Refresh();               // shows 100
            show[0] = false;               // the game turns FPS off during the press (no refresh yet)
            Group.value = State("110");    // the user taps Grid on the stale display
            Assert.That(show, Is.EqualTo(new[] { false, true, false }));
        }

        [Test]
        public void Toggles_Multi_NoAllowNone_LastCannotBeCleared()
        {
            var show = new[] { false, true, false };
            b.Toggles("Show", Shows, i => show[i], (i, on) => show[i] = on, allowNone: false);
            group.Refresh();
            TestUtil.Click(Buttons[1]);
            Assert.That(show[1], Is.True);
        }

        // ---------- Enums ----------

        [Test]
        public void Toggles_Enum_Plain_SingleSelect_NeverEmpty()
        {
            var difficulty = Difficulty.Normal;
            b.Toggles("Difficulty", () => difficulty, v => difficulty = v, allowNone: true);
            Assert.That(Buttons.Select(x => x.text), Is.EqualTo(new[] { "Easy", "Normal", "Hard" }));
            Assert.That(Group.isMultipleSelection, Is.False);
            Assert.That(Group.allowEmptySelection, Is.False);
        }

        [Test]
        public void Toggles_Enum_Plain_UserChange()
        {
            var difficulty = Difficulty.Easy;
            b.Toggles("Difficulty", () => difficulty, v => difficulty = v);
            group.Refresh();
            Group.value = State("001");
            Assert.That(difficulty, Is.EqualTo(Difficulty.Hard));
        }

        [Test]
        public void Toggles_Flags_MultiSelect()
        {
            var cheats = Cheats.None;
            b.Toggles("Cheats", () => cheats, v => cheats = v);
            Assert.That(Buttons.Select(x => x.text), Is.EqualTo(new[] { "God", "Ammo", "Fly" }));
            Assert.That(Group.isMultipleSelection, Is.True);
            Assert.That(Group.allowEmptySelection, Is.True);
        }

        [Test]
        public void Toggles_Flags_AllowNoneFalse()
        {
            var cheats = Cheats.God;
            b.Toggles("Cheats", () => cheats, v => cheats = v, allowNone: false);
            Assert.That(Group.allowEmptySelection, Is.False);
        }

        [Test]
        public void Toggles_Flags_FollowsGameValue()
        {
            var cheats = Cheats.God;
            b.Toggles("Cheats", () => cheats, v => cheats = v);
            group.Refresh();
            cheats = Cheats.God | Cheats.Fly;
            group.Refresh();
            Assert.That(Bits(Group.value), Is.EqualTo("101"));
        }

        [Test]
        public void Toggles_Flags_UserChange_KeepsUnknownBits()
        {
            var cheats = (Cheats)(8 | 1);
            b.Toggles("Cheats", () => cheats, v => cheats = v);
            group.Refresh();
            Group.value = State("010");
            Assert.That(cheats, Is.EqualTo((Cheats)(8 | 2)));
        }

        [Test]
        public void Toggles_Flags_TapOnStaleDisplay_KeepsGameChangeOfOtherBits()
        {
            var cheats = Cheats.God;
            b.Toggles("Cheats", () => cheats, v => cheats = v);
            group.Refresh();               // shows 100
            cheats = Cheats.None;          // the game clears God during the press
            Group.value = State("101");    // the user taps Fly on the stale display
            Assert.That(cheats, Is.EqualTo(Cheats.Fly));
        }

        [Test]
        public void Toggles_Enum_TooManyValues_Throws()
        {
            var big = Big.V0;
            Assert.Throws<ArgumentException>(() => b.Toggles("Big", () => big, v => big = v));
            Assert.That(root.childCount, Is.EqualTo(0));
        }

        // ---------- Common ----------

        [Test]
        public void Toggles_ReadOnly_WhenSetterNull()
        {
            int weapon = 1;
            b.Toggles("Weapon", Weapons, () => weapon);
            Assert.That(Group.enabledSelf, Is.False);
        }

        [Test]
        public void Toggles_ButtonWidth_AppliesToEveryButton()
        {
            int weapon = 0;
            b.Toggles("Weapon", Weapons, () => weapon, v => weapon = v, buttonWidth: 80);
            foreach (var button in Buttons)
                Assert.That(button.style.width.value, Is.EqualTo(new Length(80)));
        }

        [Test]
        public void Toggles_ButtonWidth_Zero_LeavesWidthAuto()
        {
            int weapon = 0;
            b.Toggles("Weapon", Weapons, () => weapon, v => weapon = v);
            foreach (var button in Buttons)
                Assert.That(button.style.width.keyword, Is.EqualTo(StyleKeyword.Null));
        }

        [Test]
        public void Toggles_NullOptions_Throws()
        {
            int weapon = 0;
            Assert.Throws<ArgumentNullException>(() => b.Toggles("Weapon", null, () => weapon, v => weapon = v));
            Assert.That(root.childCount, Is.EqualTo(0));
        }

        [Test]
        public void Toggles_MoreThan64Options_Throws()
        {
            var options = Enumerable.Range(0, 65).Select(i => "O" + i).ToArray();
            int weapon = 0;
            Assert.Throws<ArgumentException>(() => b.Toggles("Weapon", options, () => weapon, v => weapon = v));
            Assert.That(root.childCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Toggles_Theme_ButtonsMatchTheDebugTheme()
        {
            int weapon = 1;
            DebugUI.Panel(null, "A", p => p.Toggles("Weapon", Weapons, () => weapon, v => weapon = v));
            DebugUI.Open();
            yield return null;
            yield return null;

            var buttons = TestUtil.Window.Content.Q<ToggleButtonGroup>().Query<Button>().ToList();
            var off = buttons[0].resolvedStyle;
            var on = buttons[1].resolvedStyle;
            // Off: the theme's button colors (dark background, light text), like Button().
            Assert.That(off.backgroundColor, Is.EqualTo((Color)new Color32(58, 58, 70, 255)));
            Assert.That(off.color, Is.EqualTo((Color)new Color32(235, 235, 240, 255)));
            // On: the selected-tab accent with white text.
            Assert.That(on.backgroundColor, Is.EqualTo((Color)new Color32(70, 110, 200, 255)));
            Assert.That(on.color, Is.EqualTo(Color.white));
        }

        [UnityTest]
        public IEnumerator Toggles_ButtonsWrap()
        {
            DebugUI.Scale = 2f;
            var options = Enumerable.Range(1, 20).Select(i => "Option " + i).ToArray();
            int pick = 0;
            DebugUI.Panel(null, "A", p => p.Toggles("Pick", options, () => pick, v => pick = v));
            DebugUI.Open();
            yield return null;
            yield return null;

            var buttons = TestUtil.Window.Content.Q<ToggleButtonGroup>().Query<Button>().ToList();
            float firstY = buttons[0].worldBound.y;
            Assert.That(buttons.Any(x => x.worldBound.y > firstY + 1f), Is.True, "no button wrapped to a new line");
            float right = TestUtil.Window.Content.worldBound.xMax;
            foreach (var button in buttons)
                Assert.That(button.worldBound.xMax, Is.LessThanOrEqualTo(right + 1f), button.text + " overflows");
        }
    }
}
