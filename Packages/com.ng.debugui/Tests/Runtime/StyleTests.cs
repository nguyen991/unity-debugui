using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Ng.DebugTools.Tests
{
    public class StyleTests
    {
        [SetUp]
        public void SetUp() => TestUtil.Reset();

        [TearDown]
        public void TearDown() => TestUtil.Reset();

        static StyleSheet Override => Resources.Load<StyleSheet>("NgDebugUITests/Override");

        // ---------- Scale ----------

        [Test]
        public void Scale_DefaultIsOne()
        {
            Assert.That(DebugUI.Scale, Is.EqualTo(1f));
        }

        [Test]
        public void Scale_IsClamped()
        {
            DebugUI.Scale = 0.1f;
            Assert.That(DebugUI.Scale, Is.EqualTo(0.5f));
            DebugUI.Scale = 10f;
            Assert.That(DebugUI.Scale, Is.EqualTo(3f));
            DebugUI.Scale = float.NaN;
            Assert.That(DebugUI.Scale, Is.EqualTo(1f));
        }

        [Test]
        public void Scale_AppliedToPanelSettings_BeforeAndAfterHost()
        {
            DebugUI.Scale = 1.5f;
            DebugUI.Panel(null, "A", p => { });
            Assert.That(DebugUIHost.Instance.Document.panelSettings.scale, Is.EqualTo(1.5f));
            DebugUI.Scale = 2f;
            Assert.That(DebugUIHost.Instance.Document.panelSettings.scale, Is.EqualTo(2f));
        }

        [UnityTest]
        public IEnumerator Scale_Two_MakesEverythingTwiceAsBigOnScreen()
        {
            DebugUI.Panel(null, "A", p => { });
            DebugUI.Open();
            yield return null;
            yield return null;
            float widthAtOne = TestUtil.Window.Root.layout.width;
            DebugUI.Scale = 2f;
            yield return null;
            yield return null;
            // Same screen, twice the scale → half as many panel units across.
            Assert.That(TestUtil.Window.Root.layout.width, Is.EqualTo(widthAtOne / 2f).Within(2f));
        }

        // ---------- Style sheets ----------

        [Test]
        public void Override_TestAssetLoads()
        {
            Assert.That(Override, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator AddStyleSheet_OverridesThemeRules()
        {
            DebugUI.AddStyleSheet(Override);
            DebugUI.Panel(null, "A", p =>
            {
                p.Label("Text");
                p.Button("Red", () => { });
            });
            DebugUI.Open();
            yield return null;
            yield return null;
            var root = DebugUIHost.Instance.Registry.Find("A").Root;
            Assert.That(root.Q<Label>(className: "ngdbg-text").resolvedStyle.fontSize, Is.EqualTo(22f));
            Assert.That(root.Q<Button>(className: "ngdbg-button").resolvedStyle.backgroundColor, Is.EqualTo(Color.red));
        }

        [Test]
        public void AddStyleSheet_BeforeHost_AttachedOnCreate()
        {
            DebugUI.AddStyleSheet(Override);
            DebugUI.Panel(null, "A", p => { });
            Assert.That(TestUtil.Window.Root.styleSheets.Contains(Override), Is.True);
        }

        [Test]
        public void AddStyleSheet_NullAndDuplicates_Ignored()
        {
            DebugUI.Panel(null, "A", p => { });
            int before = TestUtil.Window.Root.styleSheets.count;
            DebugUI.AddStyleSheet(null);
            DebugUI.AddStyleSheet(Override);
            DebugUI.AddStyleSheet(Override);
            Assert.That(TestUtil.Window.Root.styleSheets.count, Is.EqualTo(before + 1));
        }

        [Test]
        public void RemoveStyleSheet_Detaches()
        {
            DebugUI.Panel(null, "A", p => { });
            DebugUI.AddStyleSheet(Override);
            DebugUI.RemoveStyleSheet(Override);
            Assert.That(TestUtil.Window.Root.styleSheets.Contains(Override), Is.False);
            // and it is not re-attached when the host is recreated
            DebugUIHost.DestroyInstance();
            DebugUI.Panel(null, "B", p => { });
            Assert.That(TestUtil.Window.Root.styleSheets.Contains(Override), Is.False);
        }
    }
}
