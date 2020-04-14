// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Input;
using Stride.UI.Events;

namespace Stride.UI
{
    /// <summary>
    /// The arguments associated to an key event.
    /// </summary>
    internal class KeyEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// The key that triggered the event.
        /// </summary>
        public Keys Key { get; internal set; }

        /// <summary>
        /// A reference to the input system that can be used to check the status of the other keys.
        /// </summary>
        public InputManager Input { get; internal set; }
    }
}
