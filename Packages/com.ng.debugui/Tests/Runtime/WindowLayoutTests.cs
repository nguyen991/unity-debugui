using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ng.DebugTools.Tests
{
    public class WindowLayoutTests
    {
        [SetUp]
        public void SetUp() => TestUtil.Reset();

        [TearDown]
        public void TearDown() => TestUtil.Reset();

        static DebugUIHost Host => DebugUIHost.Instance;

        [Test]
        public void ApplyWidth_Narrow_AddsClass()
        {
            DebugUI.Panel(null, "A", p => { });
            TestUtil.Window.ApplyWidth(400);
            Assert.That(TestUtil.Window.Root.ClassListContains("ngdbg-narrow"), Is.True);
        }

        [Test]
        public void ApplyWidth_Wide_RemovesClass()
        {
            DebugUI.Panel(null, "A", p => { });
            TestUtil.Window.ApplyWidth(400);
            TestUtil.Window.ApplyWidth(1000);
            Assert.That(TestUtil.Window.Root.ClassListContains("ngdbg-narrow"), Is.False);
        }

        [Test]
        public void SafeAreaInsets_NotchedPortrait()
        {
            var insets = DebugUIWindow.SafeAreaInsets(new Rect(0, 100, 1000, 1800), new Vector2(1000, 2000));
            Assert.That(insets, Is.EqualTo(new Vector4(0, 100, 0, 100)));
        }

        [Test]
        public void SafeAreaInsets_LandscapeNotchLeft()
        {
            var insets = DebugUIWindow.SafeAreaInsets(new Rect(80, 0, 1900, 1000), new Vector2(2000, 1000));
            Assert.That(insets, Is.EqualTo(new Vector4(80, 0, 20, 0)));
        }

        [Test]
        public void SafeAreaInsets_FullScreen_IsZero()
        {
            var insets = DebugUIWindow.SafeAreaInsets(new Rect(0, 0, 1000, 2000), new Vector2(1000, 2000));
            Assert.That(insets, Is.EqualTo(Vector4.zero));
        }

        [UnityTest]
        public IEnumerator Theme_ImportsDebugUss()
        {
            DebugUI.Panel(null, "A", p => { });
            DebugUI.Open();
            yield return null;
            yield return null;
            var tab = TestUtil.Tab("A");
            Assert.That(tab.resolvedStyle.minHeight.value, Is.EqualTo(32f));
        }

        [UnityTest]
        public IEnumerator IsOverUI_InsideOpenWindow_True()
        {
            DebugUI.Panel(null, "A", p => p.Label("Hello"));
            DebugUI.Open();
            yield return null;
            yield return null;
            var center = TestUtil.Window.Window.worldBound.center;
            Assert.That(Host.IsOverUI(center), Is.True);
        }

        [UnityTest]
        public IEnumerator IsOverUI_ClosedWindowArea_False()
        {
            DebugUI.Panel(null, "A", p => p.Label("Hello"));
            DebugUI.Open();
            yield return null;
            yield return null;
            var center = TestUtil.Window.Window.worldBound.center;
            DebugUI.Close();
            yield return null;
            yield return null;
            Assert.That(Host.IsOverUI(center), Is.False);
        }

        [Test]
        public void IsPointerOverUI_NoHost_False()
        {
            Assert.That(DebugUI.IsPointerOverUI, Is.False);
        }
    }
}
