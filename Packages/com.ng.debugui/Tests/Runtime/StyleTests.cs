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

        // ---------- Reference resolution ----------

        [Test]
        public void Defaults_ScaleWithScreenSize540x960_Match05()
        {
            Assert.That(DebugUI.ReferenceResolution, Is.EqualTo(new Vector2Int(540, 960)));
            Assert.That(DebugUI.MatchWidthOrHeight, Is.EqualTo(0.5f));
            DebugUI.Panel(null, "A", p => { });
            var settings = DebugUIHost.Instance.Document.panelSettings;
            Assert.That(settings.scaleMode, Is.EqualTo(PanelScaleMode.ScaleWithScreenSize));
            Assert.That(settings.screenMatchMode, Is.EqualTo(PanelScreenMatchMode.MatchWidthOrHeight));
            Assert.That(settings.scale, Is.EqualTo(1f));
        }

        [Test]
        public void ReferenceResolutionAndMatch_AreClamped()
        {
            DebugUI.ReferenceResolution = new Vector2Int(0, -5);
            Assert.That(DebugUI.ReferenceResolution, Is.EqualTo(new Vector2Int(1, 1)));
            DebugUI.MatchWidthOrHeight = -1f;
            Assert.That(DebugUI.MatchWidthOrHeight, Is.EqualTo(0f));
            DebugUI.MatchWidthOrHeight = 2f;
            Assert.That(DebugUI.MatchWidthOrHeight, Is.EqualTo(1f));
            DebugUI.MatchWidthOrHeight = float.NaN;
            Assert.That(DebugUI.MatchWidthOrHeight, Is.EqualTo(0.5f));
        }

        [Test]
        public void ReferenceResolutionAndMatch_AppliedToPanelSettings_BeforeAndAfterHost()
        {
            DebugUI.ReferenceResolution = new Vector2Int(1920, 1080);
            DebugUI.MatchWidthOrHeight = 1f;
            DebugUI.Panel(null, "A", p => { });
            var settings = DebugUIHost.Instance.Document.panelSettings;
            Assert.That(settings.referenceResolution, Is.EqualTo(new Vector2Int(1920, 1080)));
            Assert.That(settings.match, Is.EqualTo(1f));

            DebugUI.ReferenceResolution = new Vector2Int(800, 600);
            DebugUI.MatchWidthOrHeight = 0f;
            Assert.That(settings.referenceResolution, Is.EqualTo(new Vector2Int(800, 600)));
            Assert.That(settings.match, Is.EqualTo(0f));
        }

        IEnumerator OpenAndLayout()
        {
            DebugUI.Panel(null, "A", p => { });
            DebugUI.Open();
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator MatchWidth_PanelIsReferenceWidthWide()
        {
            DebugUI.ReferenceResolution = new Vector2Int(500, 1000);
            DebugUI.MatchWidthOrHeight = 0f;
            yield return OpenAndLayout();
            // Whatever the screen size, the panel is the reference width wide.
            Assert.That(TestUtil.Window.Root.layout.width, Is.EqualTo(500f).Within(2f));
        }

        [UnityTest]
        public IEnumerator MatchHeight_PanelIsReferenceHeightHigh()
        {
            DebugUI.ReferenceResolution = new Vector2Int(1000, 800);
            DebugUI.MatchWidthOrHeight = 1f;
            yield return OpenAndLayout();
            Assert.That(TestUtil.Window.Root.layout.height, Is.EqualTo(800f).Within(2f));
        }

        [UnityTest]
        public IEnumerator ReferenceResolution_ChangeWhileOpen_AppliesImmediately()
        {
            DebugUI.MatchWidthOrHeight = 0f;
            yield return OpenAndLayout();
            DebugUI.ReferenceResolution = new Vector2Int(640, 640);
            yield return null;
            yield return null;
            Assert.That(TestUtil.Window.Root.layout.width, Is.EqualTo(640f).Within(2f));
        }

        [UnityTest]
        public IEnumerator HalfTheReference_MakesEverythingTwiceAsBigOnScreen()
        {
            DebugUI.ReferenceResolution = new Vector2Int(1080, 1920);
            yield return OpenAndLayout();
            float widthAtFull = TestUtil.Window.Root.layout.width;
            DebugUI.ReferenceResolution = new Vector2Int(540, 960);
            yield return null;
            yield return null;
            // Same screen, half the reference → half as many panel units across.
            Assert.That(TestUtil.Window.Root.layout.width, Is.EqualTo(widthAtFull / 2f).Within(2f));
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
