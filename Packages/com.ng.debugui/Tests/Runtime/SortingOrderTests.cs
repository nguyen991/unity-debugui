using NUnit.Framework;

namespace Ng.DebugTools.Tests
{
    public class SortingOrderTests
    {
        [SetUp]
        public void SetUp() => TestUtil.Reset();

        [TearDown]
        public void TearDown() => TestUtil.Reset();

        static float Applied => DebugUIHost.Instance.Document.panelSettings.sortingOrder;

        [Test]
        public void Default_IsAboveEveryCanvasSortingOrder()
        {
            // uGUI Canvas.sortingOrder is capped at 32767 (short.MaxValue).
            Assert.That(DebugUI.DefaultSortingOrder, Is.GreaterThan((float)short.MaxValue));
            Assert.That(DebugUI.SortingOrder, Is.EqualTo(DebugUI.DefaultSortingOrder));
        }

        [Test]
        public void Default_IsAppliedToThePanel()
        {
            DebugUI.Panel(null, "A", p => { });
            Assert.That(Applied, Is.EqualTo(DebugUI.DefaultSortingOrder));
        }

        [Test]
        public void SetBeforeHostExists_AppliedWhenCreated()
        {
            DebugUI.SortingOrder = 50000f;
            DebugUI.Panel(null, "A", p => { });
            Assert.That(Applied, Is.EqualTo(50000f));
        }

        [Test]
        public void SetWhileRunning_AppliesImmediately()
        {
            DebugUI.Panel(null, "A", p => { });
            DebugUI.SortingOrder = 5f;
            Assert.That(Applied, Is.EqualTo(5f));
        }

        [Test]
        public void NaN_FallsBackToDefault()
        {
            DebugUI.SortingOrder = 7f;
            DebugUI.SortingOrder = float.NaN;
            Assert.That(DebugUI.SortingOrder, Is.EqualTo(DebugUI.DefaultSortingOrder));
        }

        [Test]
        public void ResetSettings_RestoresDefault()
        {
            DebugUI.SortingOrder = 7f;
            DebugUI.ResetSettings();
            Assert.That(DebugUI.SortingOrder, Is.EqualTo(DebugUI.DefaultSortingOrder));
        }
    }
}
