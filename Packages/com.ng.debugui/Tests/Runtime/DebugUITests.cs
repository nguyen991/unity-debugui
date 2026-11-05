using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Ng.DebugTools.Tests
{
    public class DebugUITests
    {
        [SetUp]
        public void SetUp() => TestUtil.Reset();

        [TearDown]
        public void TearDown() => TestUtil.Reset();

        static DebugUIHost Host => DebugUIHost.Instance;

        [Test]
        public void Panel_FirstCall_CreatesHostLazily()
        {
            Assert.That(DebugUIHost.Instance, Is.Null);
            DebugUI.Panel(null, "A", p => { });
            Assert.That(DebugUIHost.Instance, Is.Not.Null);
        }

        [Test]
        public void Host_UsesThemedPanelSettings()
        {
            DebugUI.Panel(null, "A", p => { });
            var settings = Host.Document.panelSettings;
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.themeStyleSheet, Is.Not.Null);
            Assert.That(settings.sortingOrder, Is.EqualTo(DebugUI.DefaultSortingOrder));
        }

        [Test]
        public void Panel_BuildRunsOnce_GettersNotCalledWhileClosed()
        {
            int buildCalls = 0, getterCalls = 0;
            DebugUI.Panel(null, "A", p =>
            {
                buildCalls++;
                p.Label("X", () => { getterCalls++; return 1; });
            });
            Host.Tick();
            Host.Tick();
            Host.Tick();
            Assert.That(buildCalls, Is.EqualTo(1));
            Assert.That(getterCalls, Is.EqualTo(0));
        }

        [Test]
        public void Open_RefreshesCurrentPanelImmediately()
        {
            int getterCalls = 0;
            DebugUI.Panel(null, "A", p => p.Label("X", () => { getterCalls++; return 1; }));
            DebugUI.Open();
            Assert.That(DebugUI.IsOpen, Is.True);
            Assert.That(getterCalls, Is.EqualTo(1));
        }

        [Test]
        public void Tick_WhenOpen_RefreshesOnlyCurrentPanel()
        {
            int a = 0, b = 0, valueA = 0;
            DebugUI.Panel(null, "A", p => p.Label("X", () => { a++; return valueA; }));
            DebugUI.Panel(null, "B", p => p.Label("Y", () => { b++; return 1; }));
            DebugUI.Open();
            valueA = 5;
            Host.Tick();
            Assert.That(a, Is.EqualTo(2));
            Assert.That(b, Is.EqualTo(0));
        }

        [Test]
        public void Tabs_FollowRegistrationOrder()
        {
            DebugUI.Panel(null, "Player", p => { });
            DebugUI.Panel(null, "Enemy", p => { });
            Assert.That(TestUtil.TabTexts(), Is.EqualTo(new[] { "Player", "Enemy" }));
            Assert.That(TestUtil.Window.Current.Name, Is.EqualTo("Player"));
        }

        [Test]
        public void ClickTab_SelectsPanelAndRefreshes()
        {
            int enemyCalls = 0;
            DebugUI.Panel(null, "Player", p => { });
            DebugUI.Panel(null, "Enemy", p => p.Label("HP", () => { enemyCalls++; return 3; }));
            DebugUI.Open();
            TestUtil.Click(TestUtil.Tab("Enemy"));
            var w = TestUtil.Window;
            Assert.That(w.Current.Name, Is.EqualTo("Enemy"));
            Assert.That(w.Current.Root.parent, Is.Not.Null);
            Assert.That(w.Content.Contains(w.Current.Root), Is.True);
            Assert.That(TestUtil.Tab("Enemy").ClassListContains("ngdbg-tab--selected"), Is.True);
            Assert.That(TestUtil.Tab("Player").ClassListContains("ngdbg-tab--selected"), Is.False);
            Assert.That(enemyCalls, Is.EqualTo(1));
        }

        [Test]
        public void Panel_SameNameTwice_ReplacesInPlace()
        {
            DebugUI.Panel(null, "A", p => p.Label("old"));
            DebugUI.Panel(null, "B", p => { });
            DebugUI.Panel(null, "A", p => p.Label("new"));
            Assert.That(TestUtil.TabTexts(), Is.EqualTo(new[] { "A", "B" }));
            var entry = Host.Registry.Find("A");
            Assert.That(entry.Root.Q<Label>().text, Is.EqualTo("new"));
            Assert.That(TestUtil.Window.Current, Is.SameAs(entry));
            Assert.That(TestUtil.Window.Content.Contains(entry.Root), Is.True);
        }

        [Test]
        public void Remove_CurrentPanel_SelectsFirstRemaining()
        {
            DebugUI.Panel(null, "A", p => { });
            DebugUI.Panel(null, "B", p => { });
            DebugUI.Panel(null, "C", p => { });
            TestUtil.Click(TestUtil.Tab("B"));
            DebugUI.Remove("B");
            Assert.That(TestUtil.TabTexts(), Is.EqualTo(new[] { "A", "C" }));
            Assert.That(TestUtil.Window.Current.Name, Is.EqualTo("A"));
        }

        [UnityTest]
        public IEnumerator Owner_Destroyed_RemovedOnNextTick()
        {
            var go = new GameObject("owner");
            DebugUI.Panel(go, "A", p => { });
            Object.Destroy(go);
            yield return null;
            Host.Tick();
            Assert.That(Host.Registry.Find("A"), Is.Null);
            Assert.That(TestUtil.TabTexts(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator Owner_DestroyedWhileCurrent_SelectsOther()
        {
            var go = new GameObject("owner");
            DebugUI.Panel(go, "A", p => { });
            DebugUI.Panel(null, "B", p => { });
            DebugUI.Open();
            Assert.That(TestUtil.Window.Current.Name, Is.EqualTo("A"));
            Object.Destroy(go);
            yield return null;
            Assert.DoesNotThrow(() => Host.Tick());
            Assert.That(TestUtil.Window.Current.Name, Is.EqualTo("B"));
        }

        [Test]
        public void Owner_Null_SurvivesUntilRemove()
        {
            DebugUI.Panel(null, "A", p => { });
            Host.Tick();
            Assert.That(Host.Registry.Find("A"), Is.Not.Null);
            DebugUI.Remove("A");
            Assert.That(Host.Registry.Find("A"), Is.Null);
        }

        [UnityTest]
        public IEnumerator Owner_AlreadyDestroyedAtRegistration_RemovedOnTick()
        {
            var go = new GameObject("owner");
            Object.Destroy(go);
            yield return null;
            DebugUI.Panel(go, "A", p => { });
            Host.Tick();
            Assert.That(Host.Registry.Find("A"), Is.Null);
        }

        [Test]
        public void Build_Throws_KeepsPartialPanelAndLogs()
        {
            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            DebugUI.Panel(null, "A", p =>
            {
                p.Label("Before");
                throw new InvalidOperationException("boom");
            });
            DebugUI.Panel(null, "B", p => p.Label("Fine"));
            var a = Host.Registry.Find("A");
            Assert.That(a, Is.Not.Null);
            Assert.That(a.Root.Q<Label>(className: "ngdbg-text").text, Is.EqualTo("Before"));
            Assert.That(a.Root.Q(className: "ngdbg-error"), Is.Not.Null);
            Assert.That(Host.Registry.Find("B").Root.Q<Label>().text, Is.EqualTo("Fine"));
        }

        [Test]
        public void Button_Click_InvokesAction()
        {
            bool healed = false;
            DebugUI.Panel(null, "A", p => p.Button("Heal", () => healed = true));
            DebugUI.Open();
            TestUtil.Click(TestUtil.ButtonIn(Host.Registry.Find("A").Root, "Heal"));
            Assert.That(healed, Is.True);
        }

        [Test]
        public void Button_ActionThrows_IsCaughtAndLogged()
        {
            DebugUI.Panel(null, "A", p => p.Button("Kill", () => throw new InvalidOperationException("boom")));
            DebugUI.Open();
            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            Assert.DoesNotThrow(() => TestUtil.Click(TestUtil.ButtonIn(Host.Registry.Find("A").Root, "Kill")));
        }

        [Test]
        public void ToggleButton_Click_OpensWindow()
        {
            DebugUI.Panel(null, "A", p => { });
            Assert.That(TestUtil.Window.Window.style.display.value, Is.EqualTo(DisplayStyle.None));
            TestUtil.Click(TestUtil.Window.ToggleButton);
            Assert.That(DebugUI.IsOpen, Is.True);
            Assert.That(TestUtil.Window.Window.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(TestUtil.Window.ToggleButton.text, Is.EqualTo("DBG"));
        }

        [Test]
        public void Close_HidesWindow_StopsRefresh()
        {
            int calls = 0, value = 0;
            DebugUI.Panel(null, "A", p => p.Label("X", () => { calls++; return value; }));
            DebugUI.Open();
            DebugUI.Close();
            value = 1;
            Host.Tick();
            Assert.That(DebugUI.IsOpen, Is.False);
            Assert.That(TestUtil.Window.Window.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(calls, Is.EqualTo(1));
            DebugUI.Toggle();
            Assert.That(DebugUI.IsOpen, Is.True);
        }

        [Test]
        public void Disabled_NoHostNoBuild()
        {
            DebugUI.ForceDisabled = true;
            int buildCalls = 0;
            DebugUI.Panel(null, "A", p => buildCalls++);
            DebugUI.Open();
            DebugUI.Toggle();
            DebugUI.Remove("A");
            Assert.That(buildCalls, Is.EqualTo(0));
            Assert.That(DebugUIHost.Instance, Is.Null);
            Assert.That(DebugUI.IsOpen, Is.False);
        }

        [Test]
        public void PanelAndRemove_HaveConditionalAttributes()
        {
            var expected = new[] { "DEBUG_UI_ENABLED", "DEVELOPMENT_BUILD", "UNITY_EDITOR" };
            foreach (var name in new[] { "Panel", "Remove" })
            {
                var conditions = typeof(DebugUI).GetMethod(name)
                    .GetCustomAttributes(typeof(ConditionalAttribute), false)
                    .Cast<ConditionalAttribute>()
                    .Select(a => a.ConditionString)
                    .OrderBy(s => s)
                    .ToArray();
                Assert.That(conditions, Is.EqualTo(expected), name);
            }
        }

        [Test]
        public void ResetStatics_ClearsInstance()
        {
            DebugUI.Panel(null, "A", p => { });
            var old = DebugUIHost.Instance;
            DebugUIHost.ResetStatics();
            Assert.That(DebugUIHost.Instance, Is.Null);
            DebugUI.Panel(null, "B", p => { });
            Assert.That(DebugUIHost.Instance, Is.Not.SameAs(old));
            Object.DestroyImmediate(old.gameObject);
        }
    }
}
