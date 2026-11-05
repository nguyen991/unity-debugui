using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ng.DebugTools.Tests
{
    public class ChainingTests
    {
        TestPanel panel;
        VisualElement root;
        IDebugBuilder b;

        [SetUp]
        public void SetUp()
        {
            panel = new TestPanel();
            root = new VisualElement();
            panel.Root.Add(root);
            b = new DebugBuilder(root, new BindingGroup());
        }

        [TearDown]
        public void TearDown() => panel.Dispose();

        [Test]
        public void EveryMethod_ReturnsTheSameBuilder()
        {
            float f = 0; int i = 0; string s = ""; bool t = false;
            Assert.That(b.Label("a"), Is.SameAs(b));
            Assert.That(b.Label("hp", () => 1), Is.SameAs(b));
            Assert.That(b.Button("x", () => { }), Is.SameAs(b));
            Assert.That(b.Button("x", () => { }, Color.red), Is.SameAs(b));
            Assert.That(b.Slider("f", () => f, v => f = v, 0, 1), Is.SameAs(b));
            Assert.That(b.Slider("i", () => i, v => i = v, 0, 1), Is.SameAs(b));
            Assert.That(b.Field("i", () => i, v => i = v), Is.SameAs(b));
            Assert.That(b.Field("f", () => f, v => f = v), Is.SameAs(b));
            Assert.That(b.Field("s", () => s, v => s = v), Is.SameAs(b));
            Assert.That(b.Field("t", () => t, v => t = v), Is.SameAs(b));
            Assert.That(b.Vertical(v => { }), Is.SameAs(b));
            Assert.That(b.Horizontal(h => { }), Is.SameAs(b));
            Assert.That(b.Foldout("fo", fo => { }), Is.SameAs(b));
        }

        [Test]
        public void Chain_BuildsControlsInOrder_AtTheSameLevel()
        {
            int hp = 7;
            b.Label("Build 1.2.3")
             .Label("HP", () => hp)
             .Horizontal(h => h.Button("Heal", () => { }).Button("Kill", () => { }))
             .Foldout("Advanced", f => f.Label("Inside"))
             .Button("After", () => { });

            var children = root.Children().ToList();
            Assert.That(children.Count, Is.EqualTo(5));
            Assert.That(((Label)children[0]).text, Is.EqualTo("Build 1.2.3"));
            Assert.That(children[1].ClassListContains("ngdbg-row"), Is.True);
            Assert.That(children[2].Query<Button>().ToList().Select(x => x.text), Is.EqualTo(new[] { "Heal", "Kill" }));
            Assert.That(((Foldout)children[3]).contentContainer.Q<Label>().text, Is.EqualTo("Inside"));
            Assert.That(((Button)children[4]).text, Is.EqualTo("After"));
        }

        [Test]
        public void ExpressionLambda_WithChain_CompilesAsPanelBuilder()
        {
            // The common one-liner: p => p.A().B() used where an Action<IDebugBuilder> is expected.
            System.Action<IDebugBuilder> build = p => p.Label("A").Label("B");
            build(b);
            Assert.That(root.Query<Label>(className: "ngdbg-text").ToList().Count, Is.EqualTo(2));
        }
    }
}
