using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Ng.DebugTools.Tests
{
    public class LogPanelTests
    {
        [SetUp]
        public void SetUp() => TestUtil.Reset();

        [TearDown]
        public void TearDown() => TestUtil.Reset();

        static VisualElement Root(string name = "Logs") => DebugUIHost.Instance.Registry.Find(name).Root;
        static ListView List(string name = "Logs") => Root(name).Q<ListView>(className: "ngdbg-log-list");
        static Toggle Filter(string kind) => Root().Q<Toggle>("ngdbg-log-filter-" + kind);
        static string[] Shown() => List().itemsSource.Cast<LogEntry>().Select(e => e.Message).ToArray();

        static void OpenLogs(string name = "Logs")
        {
            DebugUI.Open();
            TestUtil.Click(TestUtil.Tab(name));
            DebugUIHost.Instance.Tick();
        }

        [Test]
        public void LogPanel_HasConditionalAttributes()
        {
            var conditions = typeof(DebugUI).GetMethod("LogPanel")
                .GetCustomAttributes(typeof(ConditionalAttribute), false)
                .Cast<ConditionalAttribute>().Select(a => a.ConditionString).OrderBy(s => s).ToArray();
            Assert.That(conditions, Is.EqualTo(new[] { "DEBUG_UI_ENABLED", "DEVELOPMENT_BUILD", "UNITY_EDITOR" }));
        }

        [Test]
        public void RegistersTab_DefaultAndCustomName()
        {
            DebugUI.LogPanel();
            DebugUI.LogPanel("Console");
            Assert.That(TestUtil.TabTexts(), Is.EqualTo(new[] { "Logs", "Console" }));
        }

        [Test]
        public void ShowsLogsOldestFirst_WithCounts()
        {
            DebugUI.LogPanel();
            Debug.Log("alpha");
            Debug.LogWarning("beta");
            LogAssert.Expect(LogType.Error, "gamma");
            Debug.LogError("gamma");
            OpenLogs();
            Assert.That(Shown(), Is.EqualTo(new[] { "alpha", "beta", "gamma" }));
            Assert.That(Filter("log").text, Is.EqualTo("Log 1"));
            Assert.That(Filter("warning").text, Is.EqualTo("Warning 1"));
            Assert.That(Filter("error").text, Is.EqualTo("Error 1"));
        }

        [Test]
        public void LogsWhileClosed_AreShownOnOpen()
        {
            DebugUI.LogPanel();
            DebugUIHost.Instance.Tick();
            Debug.Log("while closed");
            OpenLogs();
            Assert.That(Shown(), Does.Contain("while closed"));
        }

        [Test]
        public void TypeFilter_HidesThatKind()
        {
            DebugUI.LogPanel();
            Debug.Log("alpha");
            Debug.LogWarning("beta");
            OpenLogs();
            Filter("log").value = false;
            DebugUIHost.Instance.Tick();
            Assert.That(Shown(), Is.EqualTo(new[] { "beta" }));
        }

        [Test]
        public void SearchText_FiltersCaseInsensitive()
        {
            DebugUI.LogPanel();
            Debug.Log("Player spawned");
            Debug.Log("Enemy spawned");
            OpenLogs();
            Root().Q<TextField>(className: "ngdbg-log-search").value = "PLAYER";
            DebugUIHost.Instance.Tick();
            Assert.That(Shown(), Is.EqualTo(new[] { "Player spawned" }));
        }

        [Test]
        public void ClearButton_EmptiesListAndCounts()
        {
            DebugUI.LogPanel();
            Debug.Log("alpha");
            OpenLogs();
            TestUtil.Click(TestUtil.ButtonIn(Root(), "Clear"));
            DebugUIHost.Instance.Tick();
            Assert.That(Shown(), Is.Empty);
            Assert.That(Filter("log").text, Is.EqualTo("Log 0"));
        }

        [Test]
        public void SelectingAnEntry_ShowsFullMessageAndStackTrace()
        {
            DebugUI.LogPanel();
            Debug.Log("detail me\nsecond line");
            OpenLogs();
            var details = Root().Q<Label>(className: "ngdbg-log-details");
            Assert.That(details.style.display.value, Is.EqualTo(DisplayStyle.None));
            List().selectedIndex = 0;
            Assert.That(details.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(details.text, Does.Contain("detail me\nsecond line"));
            Assert.That(details.text, Does.Contain(nameof(SelectingAnEntry_ShowsFullMessageAndStackTrace)));
        }

        [Test]
        public void Capacity_KeepsOnlyNewest()
        {
            DebugUI.LogPanel(capacity: 2);
            Debug.Log("one");
            Debug.Log("two");
            Debug.Log("three");
            OpenLogs();
            Assert.That(Shown(), Is.EqualTo(new[] { "two", "three" }));
        }

        [Test]
        public void NoNewLogs_RefreshDoesNotAllocate()
        {
            DebugUI.LogPanel();
            Debug.Log("alpha");
            OpenLogs();
            var bindings = DebugUIHost.Instance.Registry.Find("Logs").Bindings;
            bindings.Refresh();
            Assert.That(() => bindings.Refresh(), Is.Not.AllocatingGCMemory());
        }

        [UnityTest]
        public IEnumerator SelectedRow_UsesThemeAccentBackground()
        {
            DebugUI.LogPanel();
            Debug.Log("alpha");
            OpenLogs();
            List().selectedIndex = 0;
            yield return null;
            yield return null;
            var selected = List().Q(className: "unity-collection-view__item--selected");
            Assert.That(selected, Is.Not.Null);
            Assert.That(selected.resolvedStyle.backgroundColor, Is.EqualTo(new Color(70f / 255f, 110f / 255f, 200f / 255f)));
        }

        [UnityTest]
        public IEnumerator RendersRowsWithKindClasses()
        {
            DebugUI.LogPanel();
            Debug.Log("alpha");
            Debug.LogWarning("beta");
            OpenLogs();
            yield return null;
            yield return null;
            var rows = List().Query<Label>(className: "ngdbg-log-row").ToList();
            Assert.That(rows.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(rows.Any(r => r.ClassListContains("ngdbg-log-row--warning") && r.text.Contains("beta")), Is.True);
        }
    }
}
