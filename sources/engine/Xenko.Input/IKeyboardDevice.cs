// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Xenko.Core.Collections;

namespace Xenko.Input
{
    /// <summary>
    /// A keyboard device
    /// </summary>
    public interface IKeyboardDevice : IInputDevice
    {
        /// <summary>
        /// The keys that have been pressed since the last frame
        /// </summary>
        IReadOnlySet<Keys> PressedKeys { get; }

        /// <summary>
        /// The keys that have been released since the last frame
        /// </summary>
        IReadOnlySet<Keys> ReleasedKeys { get; }

        /// <summary>
        /// List of keys that are currently down on this keyboard
        /// </summary>
        IReadOnlySet<Keys> DownKeys { get; }
    }
}