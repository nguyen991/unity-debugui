using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UIElements;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Ng.DebugTools.Tests
{
    public class EachTests
    {
        class Enemy
        {
            public string Name;
            public int Hp;
            public Enemy(string name, int hp) { Name = name; Hp = hp; }
        }

        TestPanel panel;
        VisualElement root;
        BindingGroup group;
        IDebugBuilder b;
        List<Enemy> enemies;

        [SetUp]
        public void SetUp()
        {
            TestUtil.Reset();
            panel = new TestPanel();
            root = new VisualElement();
            panel.Root.Add(root);
            group = new BindingGroup();
            b = new DebugBuilder(root, group);
            enemies = new List<Enemy> { new Enemy("Slime", 10), new Enemy("Bat", 5) };
        }

        [TearDown]
        public void TearDown()
        {
            panel.Dispose();
            TestUtil.Reset();
        }

        VisualElement Section => root.Q(className: "ngdbg-each");

        string[] RowNames() => Section.Query<Label>(className: "ngdbg-row-label").ToList().Select(l => l.text).ToArray();

        void BuildEnemies() => b.Each(() => enemies, (e, item) => item.Label(e.Name, () => e.Hp));

        [Test]
        public void ReturnsTheBuilder()
        {
            Assert.That(b.Each(() => enemies, (e, item) => { }), Is.SameAs(b));
        }

        [Test]
        public void BuildsOneEntryPerItem_InOrder()
        {
            BuildEnemies();
            group.Refresh();
            Assert.That(RowNames(), Is.EqualTo(new[] { "Slime", "Bat" }));
        }

        [Test]
        public void ItemAdded_ShowsNewItemOnNextRefresh()
        {
            BuildEnemies();
            group.Refresh();
            enemies.Add(new Enemy("Orc", 30));
            group.Refresh();
            Assert.That(RowNames(), Is.EqualTo(new[] { "Slime", "Bat", "Orc" }));
            Assert.That(Section.Query<Label>(className: "ngdbg-value").ToList().Last().text, Is.EqualTo("30"));
        }

        [Test]
        public void ItemRemoved_DisappearsOnNextRefresh()
        {
            BuildEnemies();
            group.Refresh();
            enemies.RemoveAt(0);
            group.Refresh();
            Assert.That(RowNames(), Is.EqualTo(new[] { "Bat" }));
        }

        [Test]
        public void ItemReplacedAtSameCount_Rebuilds()
        {
            BuildEnemies();
            group.Refresh();
            enemies[1] = new Enemy("Ghost", 1);
            group.Refresh();
            Assert.That(RowNames(), Is.EqualTo(new[] { "Slime", "Ghost" }));
        }

        [Test]
        public void Unchanged_KeepsTheSameElements()
        {
            BuildEnemies();
            group.Refresh();
            var first = Section.Q(className: "ngdbg-row");
            group.Refresh();
            Assert.That(Section.Q(className: "ngdbg-row"), Is.SameAs(first));
        }

        [Test]
        public void ItemFieldChanges_UpdateWithoutRebuild()
        {
            BuildEnemies();
            group.Refresh();
            var first = Section.Q(className: "ngdbg-row");
            enemies[0].Hp = 3;
            group.Refresh();
            Assert.That(Section.Q(className: "ngdbg-row"), Is.SameAs(first));
            Assert.That(first.Q<Label>(className: "ngdbg-value").text, Is.EqualTo("3"));
        }

        [Test]
        public void KeepsItsPositionAmongSiblings()
        {
            b.Label("Before");
            BuildEnemies();
            b.Label("After");
            group.Refresh();
            enemies.Add(new Enemy("Orc", 30));
            group.Refresh();
            var children = root.Children().ToList();
            Assert.That(((Label)children[0]).text, Is.EqualTo("Before"));
            Assert.That(children[1].ClassListContains("ngdbg-each"), Is.True);
            Assert.That(((Label)children[2]).text, Is.EqualTo("After"));
        }

        [Test]
        public void NullList_ShowsNothing()
        {
            enemies = null;
            BuildEnemies();
            Assert.DoesNotThrow(() => group.Refresh());
            Assert.That(Section.childCount, Is.EqualTo(0));
        }

        [Test]
        public void ArraySource_Works()
        {
            var names = new[] { "a", "b", "c" };
            b.Each(() => names, (n, item) => item.Label(n));
            group.Refresh();
            Assert.That(Section.Query<Label>(className: "ngdbg-text").ToList().Count, Is.EqualTo(3));
        }

        [Test]
        public void InsideClosedFoldout_ListGetterNotCalled()
        {
            int calls = 0;
            b.Foldout("Enemies", f => f.Each(() => { calls++; return enemies; }, (e, item) => item.Label(e.Name)));
            group.Refresh();
            Assert.That(calls, Is.EqualTo(0));
        }

        [Test]
        public void ListGetterThrows_MarksErrorAndLogsOnce()
        {
            b.Each<Enemy>(() => throw new InvalidOperationException("boom"), (e, item) => item.Label(e.Name));
            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            group.Refresh();
            group.Refresh();
            Assert.That(Section.ClassListContains("ngdbg-error"), Is.True);
        }

        [Test]
        public void OneItemBuildThrows_OtherItemsStillBuilt()
        {
            LogAssert.Expect(LogType.Exception, new Regex("bad item"));
            b.Each(() => enemies, (e, item) =>
            {
                if (e.Name == "Slime") throw new InvalidOperationException("bad item");
                item.Label(e.Name, () => e.Hp);
            });
            group.Refresh();
            Assert.That(RowNames(), Is.EqualTo(new[] { "Bat" }));
            Assert.That(Section.Q(className: "ngdbg-error"), Is.Not.Null);
        }

        [Test]
        public void Unchanged_DoesNotAllocate()
        {
            BuildEnemies();
            group.Refresh();
            group.Refresh();
            Assert.That(() => group.Refresh(), Is.Not.AllocatingGCMemory());
        }

        [UnityTest]
        public IEnumerator InHostPanel_ListChange_ShowsUpAfterTick()
        {
            DebugUI.Panel(null, "Enemies", p => p
                .Each(() => enemies, (e, item) => item.Label(e.Name, () => e.Hp))
                .Horizontal(h => h.Each(() => enemies, (e, item) => item.Button(e.Name, () => { }))));
            DebugUI.Open();
            yield return null;
            enemies.Add(new Enemy("Orc", 30));
            DebugUIHost.Instance.Tick();
            yield return null;
            yield return null;
            var panelRoot = DebugUIHost.Instance.Registry.Find("Enemies").Root;
            Assert.That(panelRoot.Q(className: "ngdbg-each").Query<Label>(className: "ngdbg-row-label").ToList().Count, Is.EqualTo(3));
            // inside Horizontal, items lay out side by side
            var buttons = panelRoot.Q(className: "ngdbg-horizontal").Query<Button>().ToList();
            Assert.That(buttons.Count, Is.EqualTo(3));
            Assert.That(buttons[1].layout.x, Is.GreaterThan(buttons[0].layout.x));
            Assert.That(buttons[1].worldBound.y, Is.EqualTo(buttons[0].worldBound.y).Within(0.5f));
        }
    }
}
