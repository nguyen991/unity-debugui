using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Ng.DebugTools.Tests
{
    public class WindowSizeTests
    {
        [SetUp]
        public void SetUp() => TestUtil.Reset();

        [TearDown]
        public void TearDown() => TestUtil.Reset();

        static VisualElement Win => TestUtil.Window.Window;
        static Rect RootRect => TestUtil.Window.Root.layout;

        IEnumerator OpenAndLayout()
        {
            DebugUI.Panel(null, "A", p => p.Label("Hello"));
            DebugUI.Open();
            yield return null;
            yield return null;
        }

        [Test]
        public void Defaults_AreFullWidthAndHeight()
        {
            Assert.That(DebugUI.Width, Is.EqualTo(1f));
            Assert.That(DebugUI.Height, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator Defaults_FillTheWholeRoot()
        {
            yield return OpenAndLayout();
            Assert.That(Win.layout.width, Is.EqualTo(RootRect.width).Within(1f));
            Assert.That(Win.layout.height, Is.EqualTo(RootRect.height).Within(1f));
        }

        [UnityTest]
        public IEnumerator HalfWidth_AnchoredRight()
        {
            DebugUI.Width = 0.5f;
            yield return OpenAndLayout();
            Assert.That(Win.layout.width, Is.EqualTo(RootRect.width * 0.5f).Within(1f));
            Assert.That(Win.layout.xMax, Is.EqualTo(RootRect.width).Within(1f));
        }

        [UnityTest]
        public IEnumerator HalfHeight_AnchoredTop()
        {
            DebugUI.Height = 0.5f;
            yield return OpenAndLayout();
            Assert.That(Win.layout.height, Is.EqualTo(RootRect.height * 0.5f).Within(1f));
            Assert.That(Win.layout.yMin, Is.EqualTo(0f).Within(1f));
        }

        [UnityTest]
        public IEnumerator ChangeWhileOpen_AppliesImmediately()
        {
            yield return OpenAndLayout();
            DebugUI.Width = 0.75f;
            DebugUI.Height = 0.6f;
            yield return null;
            yield return null;
            Assert.That(Win.layout.width, Is.EqualTo(RootRect.width * 0.75f).Within(1f));
            Assert.That(Win.layout.height, Is.EqualTo(RootRect.height * 0.6f).Within(1f));
        }

        [UnityTest]
        public IEnumerator NarrowScreen_UsesTheSameFractions()
        {
            DebugUI.Width = 0.5f;
            yield return OpenAndLayout();
            TestUtil.Window.Root.AddToClassList("ngdbg-narrow");
            yield return null;
            yield return null;
            Assert.That(Win.layout.width, Is.EqualTo(RootRect.width * 0.5f).Within(1f));
        }

        [Test]
        public void OutOfRange_IsClamped()
        {
            DebugUI.Width = 0.2f;
            DebugUI.Height = 0.1f;
            Assert.That(DebugUI.Width, Is.EqualTo(0.5f));
            Assert.That(DebugUI.Height, Is.EqualTo(0.5f));
            DebugUI.Width = 3f;
            DebugUI.Height = float.NaN;
            Assert.That(DebugUI.Width, Is.EqualTo(1f));
            Assert.That(DebugUI.Height, Is.EqualTo(1f));
        }

        [Test]
        public void SetBeforeHostExists_AppliedWhenCreated()
        {
            DebugUI.Width = 0.75f;
            DebugUI.Height = 0.5f;
            DebugUI.Panel(null, "A", p => { });
            Assert.That(Win.style.width.value, Is.EqualTo(Length.Percent(75)));
            Assert.That(Win.style.height.value, Is.EqualTo(Length.Percent(50)));
        }
    }
}
