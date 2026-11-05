using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Ng.DebugTools.Tests
{
    public class ButtonPositionTests
    {
        [SetUp]
        public void SetUp() => TestUtil.Reset();

        [TearDown]
        public void TearDown() => TestUtil.Reset();

        static Button ToggleButton => TestUtil.Window.ToggleButton;
        static Rect RootRect => TestUtil.Window.Root.layout;

        IEnumerator Layout()
        {
            yield return null;
            yield return null;
        }

        [Test]
        public void Default_IsBottomRight()
        {
            Assert.That(DebugUI.ButtonPosition, Is.EqualTo(DebugButtonPosition.BottomRight));
        }

        [UnityTest]
        public IEnumerator EachCorner_PlacesTheButtonThere()
        {
            DebugUI.Panel(null, "A", p => { });
            var cases = new[]
            {
                DebugButtonPosition.TopLeft, DebugButtonPosition.TopRight,
                DebugButtonPosition.BottomLeft, DebugButtonPosition.BottomRight,
            };
            foreach (var position in cases)
            {
                DebugUI.ButtonPosition = position;
                yield return Layout();
                var r = ToggleButton.layout;
                bool top = position == DebugButtonPosition.TopLeft || position == DebugButtonPosition.TopRight;
                bool left = position == DebugButtonPosition.TopLeft || position == DebugButtonPosition.BottomLeft;
                Assert.That(ToggleButton.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), position.ToString());
                Assert.That(top ? r.yMin < RootRect.height / 2 : r.yMax > RootRect.height / 2, Is.True, position + " vertical " + r);
                Assert.That(left ? r.xMin < RootRect.width / 2 : r.xMax > RootRect.width / 2, Is.True, position + " horizontal " + r);
            }
        }

        [UnityTest]
        public IEnumerator None_HidesTheButton_AndItCatchesNoInput()
        {
            DebugUI.Panel(null, "A", p => { });
            yield return Layout();
            var center = ToggleButton.worldBound.center;
            DebugUI.ButtonPosition = DebugButtonPosition.None;
            yield return Layout();
            Assert.That(ToggleButton.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(DebugUIHost.Instance.IsOverUI(center), Is.False);
        }

        [UnityTest]
        public IEnumerator None_UIStillOpensFromCode()
        {
            DebugUI.ButtonPosition = DebugButtonPosition.None;
            DebugUI.Panel(null, "A", p => { });
            DebugUI.Open();
            yield return Layout();
            Assert.That(DebugUI.IsOpen, Is.True);
            Assert.That(TestUtil.Window.Window.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
        }

        [UnityTest]
        public IEnumerator TopRight_TabsAreNotUnderTheButton()
        {
            DebugUI.ButtonPosition = DebugButtonPosition.TopRight;
            DebugUI.Panel(null, "A", p => { });
            DebugUI.Open();
            yield return Layout();
            var tabBar = TestUtil.Window.TabBar;
            var tabArea = tabBar.contentRect;
            // the tab bar keeps the button's width free on the right
            Assert.That(tabBar.worldBound.xMax - tabBar.LocalToWorld(tabArea).xMax,
                Is.GreaterThanOrEqualTo(ToggleButton.worldBound.width - 1f));
        }

        [UnityTest]
        public IEnumerator TopCorners_ContentStartsBelowTheButton()
        {
            DebugUI.Panel(null, "A", p => p.Label("First row"));
            DebugUI.Open();
            foreach (var position in new[] { DebugButtonPosition.TopLeft, DebugButtonPosition.TopRight })
            {
                DebugUI.ButtonPosition = position;
                yield return Layout();
                var firstRow = TestUtil.Window.Content.Q<Label>(className: "ngdbg-text");
                Assert.That(firstRow.worldBound.yMin, Is.GreaterThanOrEqualTo(ToggleButton.worldBound.yMax - 0.5f), position.ToString());
            }
        }

        [Test]
        public void SetBeforeHostExists_AppliedWhenCreated()
        {
            DebugUI.ButtonPosition = DebugButtonPosition.TopLeft;
            DebugUI.Panel(null, "A", p => { });
            Assert.That(ToggleButton.ClassListContains("ngdbg-toggle--top-left"), Is.True);
        }

        [Test]
        public void ResetSettings_RestoresBottomRight()
        {
            DebugUI.ButtonPosition = DebugButtonPosition.None;
            DebugUI.ResetSettings();
            Assert.That(DebugUI.ButtonPosition, Is.EqualTo(DebugButtonPosition.BottomRight));
        }
    }
}
