using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Ng.DebugTools.Tests
{
    public class BuilderTests
    {
        TestPanel panel;
        VisualElement root;
        BindingGroup group;
        IDebugBuilder b;

        [SetUp]
        public void SetUp()
        {
            panel = new TestPanel();
            root = new VisualElement();
            panel.Root.Add(root);
            group = new BindingGroup();
            b = new DebugBuilder(root, group);
        }

        [TearDown]
        public void TearDown() => panel.Dispose();

        /// <summary>Finds a BaseField-derived element by its label text.</summary>
        static T Find<T>(VisualElement root, string label) where T : VisualElement
            => root.Query<T>().ToList().First(e => e.Q<Label>(className: "unity-base-field__label")?.text == label);

        [Test]
        public void Label_Static_ShowsText()
        {
            b.Label("Build 1.2.3");
            Assert.That(root.Q<Label>(className: "ngdbg-text").text, Is.EqualTo("Build 1.2.3"));
        }

        [Test]
        public void LabelT_ShowsValue_UpdatesOnChange()
        {
            int hp = 87;
            b.Label("HP", () => hp);
            group.Refresh();
            var value = root.Q<Label>(className: "ngdbg-value");
            Assert.That(value.text, Is.EqualTo("87"));
            Assert.That(root.Q<Label>(className: "ngdbg-row-label").text, Is.EqualTo("HP"));
            hp = 10;
            group.Refresh();
            Assert.That(value.text, Is.EqualTo("10"));
        }

        [Test]
        public void LabelT_NullValue_ShowsNullText()
        {
            b.Label("Name", () => (string)null);
            Assert.DoesNotThrow(() => group.Refresh());
            Assert.That(root.Q<Label>(className: "ngdbg-value").text, Is.EqualTo("null"));
        }

        [Test]
        public void Slider_Float_RangeAndValue()
        {
            float speed = 5;
            b.Slider("Speed", () => speed, v => speed = v, 0, 20);
            group.Refresh();
            var s = Find<Slider>(root, "Speed");
            Assert.That(s.lowValue, Is.EqualTo(0f));
            Assert.That(s.highValue, Is.EqualTo(20f));
            Assert.That(s.value, Is.EqualTo(5f));
            Assert.That(s.showInputField, Is.True);
            Assert.That(s.ClassListContains("ngdbg-slider"), Is.True);
        }

        [Test]
        public void Slider_UserChange_CallsSetter()
        {
            float speed = 5;
            b.Slider("Speed", () => speed, v => speed = v, 0, 20);
            group.Refresh();
            Find<Slider>(root, "Speed").value = 7;
            Assert.That(speed, Is.EqualTo(7f));
        }

        [Test]
        public void Slider_GetterOutOfRange_DoesNotWriteBack()
        {
            float speed = 50;
            int setterCalls = 0;
            b.Slider("Speed", () => speed, v => { setterCalls++; speed = v; }, 0, 20);
            group.Refresh();
            group.Refresh();
            Assert.That(setterCalls, Is.EqualTo(0));
            Assert.That(speed, Is.EqualTo(50f));
        }

        [Test]
        public void SliderInt_RangeAndSetter()
        {
            int level = 3;
            b.Slider("Level", () => level, v => level = v, 1, 10);
            group.Refresh();
            var s = Find<SliderInt>(root, "Level");
            Assert.That(s.lowValue, Is.EqualTo(1));
            Assert.That(s.highValue, Is.EqualTo(10));
            Assert.That(s.value, Is.EqualTo(3));
            Assert.That(s.showInputField, Is.True);
            s.value = 8;
            Assert.That(level, Is.EqualTo(8));
        }

        [Test]
        public void Field_Int_IsDelayed_CallsSetter()
        {
            int gold = 10;
            b.Field("Gold", () => gold, v => gold = v);
            group.Refresh();
            var f = Find<IntegerField>(root, "Gold");
            Assert.That(f.isDelayed, Is.True);
            Assert.That(f.value, Is.EqualTo(10));
            Assert.That(f.ClassListContains("ngdbg-field"), Is.True);
            f.value = 120;
            Assert.That(gold, Is.EqualTo(120));
        }

        [Test]
        public void Field_Float_CallsSetter()
        {
            float g = 9.8f;
            b.Field("G", () => g, v => g = v);
            group.Refresh();
            var f = Find<FloatField>(root, "G");
            Assert.That(f.isDelayed, Is.True);
            Assert.That(f.value, Is.EqualTo(9.8f));
            f.value = 1.5f;
            Assert.That(g, Is.EqualTo(1.5f));
        }

        [Test]
        public void Field_String_NullGetter_ShowsEmpty()
        {
            string name = null;
            b.Field("Name", () => name, v => name = v);
            Assert.DoesNotThrow(() => group.Refresh());
            var f = Find<TextField>(root, "Name");
            Assert.That(f.isDelayed, Is.True);
            Assert.That(f.value, Is.EqualTo(""));
            f.value = "Bob";
            Assert.That(name, Is.EqualTo("Bob"));
        }

        [Test]
        public void Field_Bool_IsToggle_CallsSetter()
        {
            bool god = false;
            b.Field("God", () => god, v => god = v);
            group.Refresh();
            var t = Find<Toggle>(root, "God");
            Assert.That(t.value, Is.False);
            t.value = true;
            Assert.That(god, Is.True);
        }

        [Test]
        public void Field_NoSetter_IsReadOnly()
        {
            b.Field("Gold", () => 5);
            b.Field("God", () => true);
            group.Refresh();
            Assert.That(Find<IntegerField>(root, "Gold").isReadOnly, Is.True);
            Assert.That(Find<Toggle>(root, "God").enabledSelf, Is.False);
        }

        [Test]
        public void Horizontal_ContainsChildren()
        {
            b.Horizontal(h =>
            {
                h.Button("Heal", () => { });
                h.Button("Kill", () => { });
            });
            var row = root.Q(className: "ngdbg-horizontal");
            var texts = row.Query<Button>().ToList().Select(x => x.text).ToArray();
            Assert.That(texts, Is.EqualTo(new[] { "Heal", "Kill" }));
            Assert.That(row.Q<Button>().ClassListContains("ngdbg-button"), Is.True);
        }

        [Test]
        public void Vertical_ContainsChildren()
        {
            b.Vertical(v => v.Label("Inside"));
            var col = root.Q(className: "ngdbg-vertical");
            Assert.That(col.Q<Label>(className: "ngdbg-text").text, Is.EqualTo("Inside"));
        }

        [Test]
        public void Foldout_Closed_SkipsChildGetters()
        {
            int calls = 0;
            b.Foldout("Advanced", f => f.Label("X", () => { calls++; return 1; }));
            group.Refresh();
            Assert.That(calls, Is.EqualTo(0));
            var foldout = root.Q<Foldout>(className: "ngdbg-foldout");
            Assert.That(foldout.value, Is.False);
            foldout.value = true;
            group.Refresh();
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void Foldout_OpenTrue_RefreshesChildren()
        {
            int calls = 0;
            b.Foldout("Advanced", f => f.Label("X", () => { calls++; return 1; }), open: true);
            group.Refresh();
            Assert.That(root.Q<Foldout>().value, Is.True);
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void Foldout_Nested_BuildsInsideContent()
        {
            float gravity = -9.8f;
            b.Foldout("Advanced", f => f.Slider("Gravity", () => gravity, v => gravity = v, -30, 0));
            var foldout = root.Q<Foldout>();
            Assert.That(foldout.text, Is.EqualTo("Advanced"));
            Assert.That(foldout.contentContainer.Q<Slider>(), Is.Not.Null);
        }
    }
}
