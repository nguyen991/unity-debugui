using System.Linq;
using UnityEngine.UIElements;

namespace Ng.DebugTools.Tests
{
    static class TestUtil
    {
        /// <summary>Returns the library to a clean state between tests.</summary>
        public static void Reset()
        {
            DebugUI.ForceDisabled = false;
            DebugUI.PanelWidth = null;
            DebugUIHost.DestroyInstance();
        }

        /// <summary>Activates a button the same way keyboard/gamepad submit does.</summary>
        public static void Click(Button button)
        {
            using (var e = NavigationSubmitEvent.GetPooled())
            {
                e.target = button;
                button.SendEvent(e);
            }
        }

        public static DebugUIWindow Window => DebugUIHost.Instance.Window;

        public static string[] TabTexts()
            => Window.TabBar.Query<Button>(className: "ngdbg-tab").ToList().Select(t => t.text).ToArray();

        public static Button Tab(string text)
            => Window.TabBar.Query<Button>(className: "ngdbg-tab").ToList().First(t => t.text == text);

        public static Button ButtonIn(VisualElement root, string text)
            => root.Query<Button>().ToList().First(b => b.text == text);
    }
}
