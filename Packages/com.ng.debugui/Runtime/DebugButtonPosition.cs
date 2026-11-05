namespace Ng.DebugTools
{
    /// <summary>Where the round DBG button sits; <see cref="None"/> hides it.</summary>
    public enum DebugButtonPosition
    {
        /// <summary>No button: open the debug UI with the keyboard shortcut or DebugUI.Open().</summary>
        None,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
    }
}
