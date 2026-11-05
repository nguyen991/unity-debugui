using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Ng.DebugTools.Tests
{
    public class ButtonSizeTests
    {
        [SetUp]
        public void SetUp() => TestUtil.Reset();

        [TearDown]
        public void TearDown() => TestUtil.Reset();

        static Button ToggleButton => TestUtil.Window.ToggleButton;

        static IEnumerator Layout()
        {
            yield return null;
            yield return null;
        }

        [Test]
        public void Default_Is48()
        {
            Assert.That(DebugUI.ButtonSize, Is.EqualTo(48f));
        }

        [Test]
        public void OutOfRange_IsClamped_NaNIsDefault()
        {
            DebugUI.ButtonSize = 10f;
            Assert.That(DebugUI.ButtonSize, Is.EqualTo(24f));
            DebugUI.ButtonSize = 1000f;
            Assert.That(DebugUI.ButtonSize, Is.EqualTo(200f));
            DebugUI.ButtonSize = float.NaN;
            Assert.That(DebugUI.ButtonSize, Is.EqualTo(48f));
        }

        [UnityTest]
        public IEnumerator Size_ResizesTheRoundButton()
        {
            DebugUI.Panel(null, "A", p => { });
            DebugUI.ButtonSize = 80f;
            yield return Layout();
            Assert.That(ToggleButton.layout.width, Is.EqualTo(80f).Within(0.5f));
            Assert.That(ToggleButton.layout.height, Is.EqualTo(80f).Within(0.5f));
            Assert.That(ToggleButton.resolvedStyle.borderTopLeftRadius, Is.EqualTo(40f).Within(0.5f));
        }

        [UnityTest]
        public IEnumerator BigButtonInTopCorner_TabsAndContentStayClear()
        {
            DebugUI.ButtonSize = 96f;
            DebugUI.Panel(null, "A", p => p.Label("First row"));
            DebugUI.Open();
            foreach (var position in new[] { DebugButtonPosition.TopLeft, DebugButtonPosition.TopRight })
            {
                DebugUI.ButtonPosition = position;
                yield return Layout();
                var button = ToggleButton.worldBound;
                var firstRow = TestUtil.Window.Content.Q<Label>(className: "ngdbg-text").worldBound;
                Assert.That(firstRow.yMin, Is.GreaterThanOrEqualTo(button.yMax - 0.5f), position + " content");
                var tab = TestUtil.Tab("A").worldBound;
                Assert.That(tab.Overlaps(button), Is.False, position + " tab " + tab + " button " + button);
            }
        }

        [UnityTest]
        public IEnumerator BigButtonAtBottom_ContentCanScrollClearOfIt()
        {
            DebugUI.ButtonSize = 96f;
            DebugUI.Panel(null, "A", p => p.Label("Row"));
            DebugUI.Open();
            yield return Layout();
            var container = TestUtil.Window.Content.contentContainer;
            Assert.That(container.resolvedStyle.paddingBottom, Is.GreaterThanOrEqualTo(96f));
        }

        [UnityTest]
        public IEnumerator NoButton_NoReservedSpace()
        {
            DebugUI.ButtonPosition = DebugButtonPosition.None;
            DebugUI.Panel(null, "A", p => p.Label("Row"));
            DebugUI.Open();
            yield return Layout();
            Assert.That(TestUtil.Window.Content.contentContainer.resolvedStyle.paddingBottom, Is.EqualTo(0f));
        }

        [Test]
        public void SetBeforeHostExists_AppliedWhenCreated()
        {
            DebugUI.ButtonSize = 64f;
            DebugUI.Panel(null, "A", p => { });
            Assert.That(ToggleButton.style.width.value.value, Is.EqualTo(64f));
        }

        [Test]
        public void ResetSettings_Restores48()
        {
            DebugUI.ButtonSize = 64f;
            DebugUI.ResetSettings();
            Assert.That(DebugUI.ButtonSize, Is.EqualTo(48f));
        }
    }
}
