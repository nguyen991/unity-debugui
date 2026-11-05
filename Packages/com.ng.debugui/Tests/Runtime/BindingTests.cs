using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UIElements;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Ng.DebugUI.Tests
{
    public class BindingTests
    {
        static ValueBinding<T> Make<T>(Func<T> getter, List<T> applied, Action<T> setter = null)
            => new ValueBinding<T>(new VisualElement(), getter, setter, v => applied.Add(v));

        [Test]
        public void Refresh_FirstCall_AppliesGetterValue()
        {
            var applied = new List<int>();
            var b = Make(() => 5, applied);
            b.Refresh();
            Assert.That(applied, Is.EqualTo(new[] { 5 }));
        }

        [Test]
        public void Refresh_Unchanged_DoesNotReapply()
        {
            var applied = new List<int>();
            var b = Make(() => 5, applied);
            b.Refresh();
            b.Refresh();
            Assert.That(applied.Count, Is.EqualTo(1));
        }

        [Test]
        public void Refresh_Changed_AppliesNewValue()
        {
            var applied = new List<int>();
            int v = 5;
            var b = Make(() => v, applied);
            b.Refresh();
            v = 6;
            b.Refresh();
            Assert.That(applied, Is.EqualTo(new[] { 5, 6 }));
        }

        [Test]
        public void Refresh_WhileEditing_DoesNotCallGetter()
        {
            int getterCalls = 0;
            var b = Make(() => { getterCalls++; return 1; }, new List<int>());
            b.IsEditing = true;
            b.Refresh();
            Assert.That(getterCalls, Is.EqualTo(0));
        }

        [Test]
        public void Refresh_GetterThrows_MarksErrorAndLogsOnce()
        {
            var element = new VisualElement();
            var b = new ValueBinding<int>(element, () => throw new InvalidOperationException("boom"), null, _ => { });
            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            b.Refresh();
            b.Refresh();
            b.Refresh();
            Assert.That(b.HasError, Is.True);
            Assert.That(element.ClassListContains(UssClasses.Error), Is.True);
        }

        [Test]
        public void Refresh_GetterRecovers_ClearsErrorClass()
        {
            var element = new VisualElement();
            bool fail = true;
            var b = new ValueBinding<int>(element, () => fail ? throw new InvalidOperationException("boom") : 1, null, _ => { });
            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            b.Refresh();
            fail = false;
            b.Refresh();
            Assert.That(b.HasError, Is.False);
            Assert.That(element.ClassListContains(UssClasses.Error), Is.False);
        }

        [Test]
        public void Write_SetterThrows_MarksError()
        {
            var b = Make(() => 1, new List<int>(), _ => throw new InvalidOperationException("boom"));
            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            b.Write(2);
            b.Write(3);
            Assert.That(b.HasError, Is.True);
        }

        [Test]
        public void Write_NullSetter_DoesNothing()
        {
            var b = Make(() => 1, new List<int>());
            Assert.DoesNotThrow(() => b.Write(2));
            Assert.That(b.HasError, Is.False);
        }

        [Test]
        public void Invalidate_ForcesReapply()
        {
            var applied = new List<int>();
            var b = Make(() => 5, applied);
            b.Refresh();
            b.Invalidate();
            b.Refresh();
            Assert.That(applied.Count, Is.EqualTo(2));
        }

        [Test]
        public void Group_Closed_SkipsBindingsAndChildren()
        {
            int calls = 0;
            var group = new BindingGroup(() => false);
            group.Add(Make(() => { calls++; return 1; }, new List<int>()));
            var child = group.AddChild(null);
            child.Add(Make(() => { calls++; return 1; }, new List<int>()));
            group.Refresh();
            Assert.That(calls, Is.EqualTo(0));
        }

        [Test]
        public void Group_ChildClosed_ParentStillRefreshes()
        {
            int parentCalls = 0, childCalls = 0;
            var group = new BindingGroup();
            group.Add(Make(() => { parentCalls++; return 1; }, new List<int>()));
            var child = group.AddChild(() => false);
            child.Add(Make(() => { childCalls++; return 1; }, new List<int>()));
            group.Refresh();
            Assert.That(parentCalls, Is.EqualTo(1));
            Assert.That(childCalls, Is.EqualTo(0));
        }

        [Test]
        public void Group_Unchanged_DoesNotAllocate()
        {
            int i = 3;
            float f = 1.5f;
            var group = new BindingGroup();
            group.Add(new ValueBinding<int>(new VisualElement(), () => i, null, _ => { }));
            var child = group.AddChild(() => true);
            child.Add(new ValueBinding<float>(new VisualElement(), () => f, null, _ => { }));
            group.Refresh();
            Assert.That(() => group.Refresh(), Is.Not.AllocatingGCMemory());
        }
    }
}
