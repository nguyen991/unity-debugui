using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Ng.DebugTools.Tests
{
    public class ButtonColorTests
    {
        TestPanel panel;
        VisualElement root;
        IDebugBuilder b;

        [SetUp]
        public void SetUp()
        {
            TestUtil.Reset();
            panel = new TestPanel();
            root = new VisualElement();
            panel.Root.Add(root);
            b = new DebugBuilder(root, new BindingGroup());
        }

        [TearDown]
        public void TearDown()
        {
            panel.Dispose();
            TestUtil.Reset();
        }

        [Test]
        public void WithColor_SetsBackgroundAndColoredClass()
        {
            b.Button("Kill", () => { }, Color.red);
            var button = root.Q<Button>();
            Assert.That(button.style.backgroundColor.value, Is.EqualTo(Color.red));
            Assert.That(button.ClassListContains("ngdbg-button--colored"), Is.True);
        }

        [Test]
        public void WithoutColor_KeepsThemeColor()
        {
            b.Button("Reset", () => { });
            var button = root.Q<Button>();
            Assert.That(button.style.backgroundColor.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(button.ClassListContains("ngdbg-button--colored"), Is.False);
        }

        [Test]
        public void DarkColor_UsesLightText()
        {
            b.Button("Dark", () => { }, new Color(0.1f, 0.1f, 0.4f));
            Assert.That(root.Q<Button>().style.color.value, Is.EqualTo(Color.white));
        }

        [Test]
        public void LightColor_UsesDarkText()
        {
            b.Button("Light", () => { }, Color.yellow);
            var text = root.Q<Button>().style.color.value;
            Assert.That(text.grayscale, Is.LessThan(0.3f));
        }

        [UnityTest]
        public IEnumerator WithColor_RendersThatColorInsideTheThemedWindow()
        {
            var green = new Color(0.2f, 0.6f, 0.3f);
            DebugUI.Panel(null, "A", p => p.Button("Heal", () => { }, green));
            DebugUI.Open();
            yield return null;
            yield return null;
            var button = DebugUIHost.Instance.Registry.Find("A").Root.Q<Button>();
            Assert.That(button.resolvedStyle.backgroundColor, Is.EqualTo(green));
        }
    }
}
