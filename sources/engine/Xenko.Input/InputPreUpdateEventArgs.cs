// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Games;

namespace Xenko.Input
{
    /// <summary>
    /// Arguments for input pre update event
    /// </summary>
    public class InputPreUpdateEventArgs : EventArgs
    {
        /// <summary>
        /// The game time passed to <see cref="InputManager.Update"/>
        /// </summary>
        public GameTime GameTime;
    }
}