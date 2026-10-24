// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Input;
using Stride.UI.Events;

namespace Stride.UI
{
    /// <summary>
    /// The arguments associated to an key event.
    /// </summary>
    public class KeyEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// The key that triggered the event.
        /// </summary>
        public Keys Key { get; init; }

        /// <summary>
        /// A reference to the input system.
        /// </summary>
        /// <remarks>
        /// While the UI holds the keyboard, the input system hides the keyboard from game-facing reads. Use
        /// <see cref="DownKeys"/> or <see cref="IsKeyDown"/> to check the status of the other keys.
        /// </remarks>
        public InputManager Input { get; init; }

        /// <summary>
        /// The keys that are held, as the UI sees them.
        /// </summary>
        public Core.Collections.IReadOnlySet<Keys> DownKeys { get; init; }

        /// <summary>
        /// Determines whether a key is held, as the UI sees it.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if the key is held; otherwise, <c>false</c>.</returns>
        public bool IsKeyDown(Keys key) => DownKeys?.Contains(key) ?? false;
    }
}
