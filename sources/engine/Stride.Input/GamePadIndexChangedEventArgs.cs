// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Input
{
    /// <summary>
    /// Event for when a <see cref="IGamePadDevice"/>'s index changed
    /// </summary>
    public class GamePadIndexChangedEventArgs : EventArgs
    {
        /// <summary>
        /// New device index
        /// </summary>
        public int Index;

        /// <summary>
        /// if <c>true</c>, this change was initiate by the device
        /// </summary>
        public bool IsDeviceSideChange;
    }
}