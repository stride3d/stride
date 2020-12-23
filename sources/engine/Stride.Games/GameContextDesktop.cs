// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_WINDOWS || STRIDE_PLATFORM_UNIX

using System;

namespace Stride.Games
{
    /// <summary>
    /// Common ancestor to all game contexts on the Windows platform.
    /// </summary>
    /// <typeparam name="TK"></typeparam>
    public abstract class GameContextDesktop<TK> : GameContext<TK>
    {
        /// <inheritDoc/>
        protected GameContextDesktop(TK control, int requestedWidth = 0, int requestedHeight = 0, bool isUserManagingRun = false)
            : base(control, requestedWidth, requestedHeight, isUserManagingRun)
        {
        }
    }
}
#endif
