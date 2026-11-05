namespace Ng.DebugUI
{
    /// <summary>A live link between a game value and a UI element.</summary>
    internal interface IBinding
    {
        /// <summary>Reads the game value and pushes it to the UI if it changed.</summary>
        void Refresh();

        /// <summary>Forces the next <see cref="Refresh"/> to push the value even if unchanged.</summary>
        void Invalidate();

        bool HasError { get; }
    }
}
