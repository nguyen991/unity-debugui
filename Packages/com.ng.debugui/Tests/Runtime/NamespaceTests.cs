using NUnit.Framework;
using Ng.DebugTools;

// Deliberately outside the library's namespace but under "Ng.*", like a game by the same author.
// The API must resolve as written in the README: `DebugUI.Panel(...)` after a single using.
namespace Ng.SampleGame
{
    public class NamespaceTests
    {
        [TearDown]
        public void TearDown() => Ng.DebugTools.Tests.TestUtil.Reset();

        [Test]
        public void CallerInNgNamespace_ResolvesDebugUIClass()
        {
            bool built = false;
            DebugUI.Panel(null, "Game", p => built = true);
            Assert.That(built, Is.True);
            Assert.That(DebugUI.IsOpen, Is.False);
        }
    }
}
