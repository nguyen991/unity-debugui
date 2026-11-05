using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Ng.DebugTools.Tests
{
    public class LogStoreTests
    {
        [TearDown]
        public void TearDown() => TestUtil.Reset();

        static List<LogEntry> All(LogStore s)
        {
            var list = new List<LogEntry>();
            s.CopyTo(list, _ => true);
            return list;
        }

        [Test]
        public void Add_CountsByKind_ErrorsIncludeExceptionAndAssert()
        {
            var s = new LogStore(10);
            s.Add("a", "", LogType.Log);
            s.Add("b", "", LogType.Warning);
            s.Add("c", "", LogType.Error);
            s.Add("d", "", LogType.Exception);
            s.Add("e", "", LogType.Assert);
            Assert.That(s.CountOf(LogKind.Log), Is.EqualTo(1));
            Assert.That(s.CountOf(LogKind.Warning), Is.EqualTo(1));
            Assert.That(s.CountOf(LogKind.Error), Is.EqualTo(3));
        }

        [Test]
        public void Capacity_DropsOldest_AndCountsFollow()
        {
            var s = new LogStore(3);
            s.Add("w1", "", LogType.Warning);
            s.Add("l1", "", LogType.Log);
            s.Add("l2", "", LogType.Log);
            s.Add("l3", "", LogType.Log);
            var all = All(s);
            Assert.That(all.ConvertAll(e => e.Message), Is.EqualTo(new[] { "l1", "l2", "l3" }));
            Assert.That(s.CountOf(LogKind.Warning), Is.EqualTo(0));
            Assert.That(s.CountOf(LogKind.Log), Is.EqualTo(3));
        }

        [Test]
        public void Clear_EmptiesAndBumpsVersion()
        {
            var s = new LogStore(10);
            s.Add("a", "", LogType.Log);
            int v = s.Version;
            s.Clear();
            Assert.That(All(s), Is.Empty);
            Assert.That(s.CountOf(LogKind.Log), Is.EqualTo(0));
            Assert.That(s.Version, Is.Not.EqualTo(v));
        }

        [Test]
        public void Add_BumpsVersion()
        {
            var s = new LogStore(10);
            int v = s.Version;
            s.Add("a", "", LogType.Log);
            Assert.That(s.Version, Is.Not.EqualTo(v));
        }

        [Test]
        public void CopyTo_AppliesFilter_OldestFirst()
        {
            var s = new LogStore(10);
            s.Add("one", "", LogType.Log);
            s.Add("two", "", LogType.Warning);
            s.Add("three", "", LogType.Log);
            var list = new List<LogEntry>();
            s.CopyTo(list, e => e.Kind == LogKind.Log);
            Assert.That(list.ConvertAll(e => e.Message), Is.EqualTo(new[] { "one", "three" }));
        }

        [Test]
        public void Add_FromAnotherThread_IsCaptured()
        {
            var s = new LogStore(100);
            Task.Run(() => { for (int i = 0; i < 50; i++) s.Add("bg" + i, "", LogType.Log); }).Wait();
            Assert.That(All(s).Count, Is.EqualTo(50));
        }

        [Test]
        public void Attach_CapturesDebugLog_DetachStops()
        {
            var s = new LogStore(10);
            s.Attach();
            Debug.Log("captured-xyz");
            s.Detach();
            Debug.Log("not-captured-xyz");
            var messages = All(s).ConvertAll(e => e.Message);
            Assert.That(messages, Does.Contain("captured-xyz"));
            Assert.That(messages, Does.Not.Contain("not-captured-xyz"));
        }

        [Test]
        public void Entry_HasTimeAndFirstLine()
        {
            var s = new LogStore(10);
            s.Add("first line\nsecond line", "trace", LogType.Log);
            var e = All(s)[0];
            Assert.That(e.Summary, Does.Contain("first line"));
            Assert.That(e.Summary, Does.Not.Contain("second line"));
            Assert.That(e.Summary, Does.Match(@"^\d\d:\d\d:\d\d "));
            Assert.That(e.StackTrace, Is.EqualTo("trace"));
        }
    }
}
