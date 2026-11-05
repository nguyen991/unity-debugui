using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Ng.DebugTools.Tests
{
    public class PanelWidthTests
    {
        [SetUp]
        public void SetUp() => TestUtil.Reset();

        [TearDown]
        public void TearDown() => TestUtil.Reset();

        static VisualElement Win => TestUtil.Window.Window;

        [Test]
        public void Default_IsNull_KeepsAutomaticWidth()
        {
            DebugUI.Panel(null, "A", p => { });
            Assert.That(DebugUI.PanelWidth, Is.Null);
            Assert.That(Win.style.width.keyword, Is.EqualTo(StyleKeyword.Null));
        }

        [Test]
        public void Half_SetsFiftyPercentWidth()
        {
            DebugUI.Panel(null, "A", p => { });
            DebugUI.PanelWidth = 0.5f;
            Assert.That(Win.style.width.value, Is.EqualTo(Length.Percent(50)));
        }

        [Test]
        public void OutOfRange_IsClamped()
        {
            DebugUI.PanelWidth = 0.2f;
            Assert.That(DebugUI.PanelWidth, Is.EqualTo(0.5f));
            DebugUI.PanelWidth = 3f;
            Assert.That(DebugUI.PanelWidth, Is.EqualTo(1f));
            DebugUI.PanelWidth = float.NaN;
            Assert.That(DebugUI.PanelWidth, Is.EqualTo(1f));
        }

        [Test]
        public void SetBeforeHostExists_AppliedWhenCreated()
        {
            DebugUI.PanelWidth = 0.75f;
            DebugUI.Panel(null, "A", p => { });
            Assert.That(Win.style.width.value, Is.EqualTo(Length.Percent(75)));
        }

        [Test]
        public void SetNull_RestoresAutomaticWidth()
        {
            DebugUI.Panel(null, "A", p => { });
            DebugUI.PanelWidth = 0.6f;
            DebugUI.PanelWidth = null;
            Assert.That(Win.style.width.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(Win.style.left.keyword, Is.EqualTo(StyleKeyword.Null));
        }

        [UnityTest]
        public IEnumerator Half_LaysOutAtHalfTheScreen_AnchoredRight()
        {
            DebugUI.Panel(null, "A", p => { });
            DebugUI.PanelWidth = 0.5f;
            DebugUI.Open();
            yield return null;
            yield return null;
            var root = TestUtil.Window.Root.layout;
            Assert.That(Win.layout.width, Is.EqualTo(root.width * 0.5f).Within(1f));
            Assert.That(Win.layout.xMax, Is.EqualTo(root.width).Within(1f));
        }

        [UnityTest]
        public IEnumerator Full_AlsoOnNarrowScreens_FillsWidth()
        {
            DebugUI.Panel(null, "A", p => { });
            DebugUI.Open();
            yield return null;
            yield return null;
            TestUtil.Window.Root.AddToClassList("ngdbg-narrow");
            DebugUI.PanelWidth = 1f;
            yield return null;
            yield return null;
            var root = TestUtil.Window.Root.layout;
            Assert.That(Win.layout.width, Is.EqualTo(root.width).Within(1f));
            Assert.That(Win.layout.xMin, Is.EqualTo(0f).Within(1f));
        }
    }
}
