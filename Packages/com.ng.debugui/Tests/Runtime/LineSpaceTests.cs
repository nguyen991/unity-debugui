using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Ng.DebugTools.Tests
{
    public class LineSpaceTests
    {
        [SetUp]
        public void SetUp() => TestUtil.Reset();

        [TearDown]
        public void TearDown() => TestUtil.Reset();

        static VisualElement PanelRoot(string name) => DebugUIHost.Instance.Registry.Find(name).Root;

        [Test]
        public void Line_And_Space_ReturnTheBuilder()
        {
            using (var panel = new TestPanel())
            {
                IDebugBuilder b = new DebugBuilder(panel.Root, new BindingGroup());
                Assert.That(b.Line(), Is.SameAs(b));
                Assert.That(b.Space(), Is.SameAs(b));
                Assert.That(b.Space(20), Is.SameAs(b));
            }
        }

        [Test]
        public void Space_DefaultIsEightAlongTheLayoutDirection()
        {
            using (var panel = new TestPanel())
            {
                new DebugBuilder(panel.Root, new BindingGroup()).Space();
                var space = panel.Root.Q(className: "ngdbg-space");
                Assert.That(space.style.flexBasis.value.value, Is.EqualTo(8f));
                Assert.That(space.style.flexGrow.value, Is.EqualTo(0f));
                Assert.That(space.style.flexShrink.value, Is.EqualTo(0f));
            }
        }

        [UnityTest]
        public IEnumerator InVerticalPanel_LineIsThinFullWidth_SpaceAddsHeight()
        {
            DebugUI.Panel(null, "A", p => p.Label("Top").Line().Space(24).Label("Bottom"));
            DebugUI.Open();
            yield return null;
            yield return null;
            var root = PanelRoot("A");
            var line = root.Q(className: "ngdbg-line");
            var space = root.Q(className: "ngdbg-space");
            Assert.That(line.layout.height, Is.EqualTo(1f).Within(0.01f));
            Assert.That(line.layout.width, Is.GreaterThan(root.contentRect.width * 0.9f));
            Assert.That(space.layout.height, Is.EqualTo(24f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator InHorizontalRow_LineIsVertical_SpaceAddsWidth()
        {
            DebugUI.Panel(null, "A", p => p.Horizontal(h => h
                .Button("A", () => { }).Line().Space(20).Button("B", () => { })));
            DebugUI.Open();
            yield return null;
            yield return null;
            var row = PanelRoot("A").Q(className: "ngdbg-horizontal");
            var line = row.Q(className: "ngdbg-line");
            var space = row.Q(className: "ngdbg-space");
            var button = row.Q<Button>();
            Assert.That(line.layout.width, Is.EqualTo(1f).Within(0.01f));
            Assert.That(line.layout.height, Is.GreaterThan(button.layout.height * 0.5f));
            Assert.That(space.layout.width, Is.EqualTo(20f).Within(0.01f));
            // the two buttons still share the remaining width equally
            var buttons = row.Query<Button>().ToList();
            Assert.That(buttons[0].layout.width, Is.EqualTo(buttons[1].layout.width).Within(1f));
        }
    }
}
