using System;
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Ng.DebugUI.Tests
{
    /// <summary>Regression tests for findings from the final code review.</summary>
    public class ReviewFixTests
    {
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
            DebugUI.BuildAllowsDebugUI = true;
            TestUtil.Reset();
        }

        // Finding 1: release builds must never create the debug UI, even via Open()/Toggle().
        [Test]
        public void ReleaseBuild_OpenAndToggle_CreateNothing()
        {
            DebugUI.BuildAllowsDebugUI = false;
            DebugUI.Open();
            DebugUI.Toggle();
            Assert.That(DebugUIHost.Instance, Is.Null);
            Assert.That(DebugUI.IsOpen, Is.False);
            Assert.That(DebugUI.IsPointerOverUI, Is.False);
        }

        // Finding 2: a focused text field keeps the user's input even after the pointer capture
        // used by text selection is released.
        [Test]
        public void Field_FocusedThenCaptureReleased_NotOverwritten()
        {
            int gold = 1;
            b.Field("Gold", () => gold, v => gold = v);
            group.Refresh();
            var field = root.Q<IntegerField>();
            field.Focus();
            using (var e = PointerCaptureOutEvent.GetPooled(field, null, PointerId.mousePointerId))
                field.SendEvent(e);
            field.SetValueWithoutNotify(999); // what the tester is typing
            gold = 5;
            group.Refresh();
            Assert.That(field.value, Is.EqualTo(999));
        }

        // Finding 2: a slider that merely keeps focus after a drag must keep following the game.
        [Test]
        public void Slider_FocusedNotDragging_FollowsGame()
        {
            float speed = 1;
            b.Slider("Speed", () => speed, v => speed = v, 0, 10);
            group.Refresh();
            var slider = root.Q<Slider>();
            slider.Focus();
            speed = 4;
            group.Refresh();
            Assert.That(slider.value, Is.EqualTo(4f));
        }

        // Finding 2: while the slider has pointer capture (dragging) the game must not move it.
        [Test]
        public void Slider_WhileCaptured_NotOverwritten()
        {
            float speed = 1;
            b.Slider("Speed", () => speed, v => speed = v, 0, 10);
            group.Refresh();
            var slider = root.Q<Slider>();
            using (var e = PointerCaptureEvent.GetPooled(slider, null, PointerId.mousePointerId))
                slider.SendEvent(e);
            speed = 4;
            group.Refresh();
            Assert.That(slider.value, Is.EqualTo(1f));
        }

        // Finding 3: when the setter rejects/clamps, the UI shows the real game value again.
        [Test]
        public void Field_SetterClamps_UIResyncsToGameValue()
        {
            int gold = 0;
            b.Field("Gold", () => gold, v => gold = Mathf.Max(0, v));
            group.Refresh();
            var field = root.Q<IntegerField>();
            field.value = -5;
            group.Refresh();
            Assert.That(gold, Is.EqualTo(0));
            Assert.That(field.value, Is.EqualTo(0));
        }

        class Exploding
        {
            public override string ToString() => throw new InvalidOperationException("boom");
        }

        // Finding 4: an exception while pushing a value to the UI is contained to that control.
        [Test]
        public void LabelT_ToStringThrows_OtherControlsStillRefresh()
        {
            int hp = 1;
            b.Label("Bad", () => new Exploding());
            b.Label("HP", () => hp);
            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            Assert.DoesNotThrow(() => group.Refresh());
            hp = 2;
            Assert.DoesNotThrow(() => group.Refresh());
            var values = root.Query<Label>(className: "ngdbg-value").ToList();
            Assert.That(values[1].text, Is.EqualTo("2"));
            Assert.That(root.Q(className: "ngdbg-row").ClassListContains("ngdbg-error"), Is.True);
        }

        // Finding 9: on a phone-width window the slider track stays wide enough to drag.
        [UnityTest]
        public IEnumerator Narrow_PhoneWidth_SliderTrackIsWideEnough()
        {
            float speed = 1;
            DebugUI.Panel(null, "A", p => p.Slider("Speed", () => speed, v => speed = v, 0, 10));
            DebugUI.Open();
            var w = TestUtil.Window;
            // Let the first layout pass (whose GeometryChangedEvent applies the real width) finish
            // before forcing a phone-sized window.
            yield return null;
            yield return null;
            w.Root.AddToClassList("ngdbg-narrow");
            w.Window.style.right = StyleKeyword.Auto;
            w.Window.style.width = 260;
            yield return null;
            yield return null;
            Assert.That(w.Root.ClassListContains("ngdbg-narrow"), Is.True);
            var track = w.Content.Q<Slider>().Q(className: "unity-base-slider__drag-container");
            Assert.That(track.layout.width, Is.GreaterThanOrEqualTo(150f));
        }
    }
}
