#if NG_DEBUGUI_INPUT_SYSTEM
using System;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Ng.DebugTools.Tests
{
    public class ToggleKeyTests
    {
        Keyboard keyboard;
        InputSettings.EditorInputBehaviorInPlayMode savedEditorBehavior;
        InputSettings.BackgroundBehavior savedBackgroundBehavior;

        [SetUp]
        public void SetUp()
        {
            TestUtil.Reset();
            // The Editor may be unfocused while tests run; let keyboard events through anyway.
            var settings = InputSystem.settings;
            savedEditorBehavior = settings.editorInputBehaviorInPlayMode;
            savedBackgroundBehavior = settings.backgroundBehavior;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        [TearDown]
        public void TearDown()
        {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.editorInputBehaviorInPlayMode = savedEditorBehavior;
            InputSystem.settings.backgroundBehavior = savedBackgroundBehavior;
            TestUtil.Reset();
        }

        /// <summary>A real key-down event processed by an input update, as in a frame.</summary>
        void Press(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            InputSystem.Update();
        }

        [Test]
        public void Default_IsBackquote()
        {
            Assert.That(DebugUI.ToggleKey, Is.EqualTo(Key.Backquote));
        }

        [Test]
        public void DefaultKey_Pressed_IsDetected()
        {
            Press(Key.Backquote);
            Assert.That(DebugUIHost.ToggleKeyPressed(keyboard), Is.True);
        }

        [Test]
        public void CustomKey_Pressed_IsDetected_OldKeyIsNot()
        {
            DebugUI.ToggleKey = Key.F1;
            Press(Key.Backquote);
            Assert.That(DebugUIHost.ToggleKeyPressed(keyboard), Is.False);
            Press(Key.F1);
            Assert.That(DebugUIHost.ToggleKeyPressed(keyboard), Is.True);
        }

        [Test]
        public void None_DisablesTheHotkey()
        {
            DebugUI.ToggleKey = Key.None;
            Press(Key.Backquote);
            Assert.That(DebugUIHost.ToggleKeyPressed(keyboard), Is.False);
        }

        [Test]
        public void NoKeyboard_IsNotPressed()
        {
            Assert.That(DebugUIHost.ToggleKeyPressed(null), Is.False);
        }

        [Test]
        public void InvalidKey_Throws_AndKeepsPreviousKey()
        {
            DebugUI.ToggleKey = Key.F2;
            Assert.Throws<ArgumentOutOfRangeException>(() => DebugUI.ToggleKey = Key.IMESelected);
            Assert.That(DebugUI.ToggleKey, Is.EqualTo(Key.F2));
        }

        [Test]
        public void ResetSettings_RestoresBackquote()
        {
            DebugUI.ToggleKey = Key.F1;
            DebugUI.ResetSettings();
            Assert.That(DebugUI.ToggleKey, Is.EqualTo(Key.Backquote));
        }
    }
}
#endif
