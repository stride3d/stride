// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_IOS
using Stride.Graphics.SDL;

namespace Stride.Games
{
    /// <summary>
    /// A <see cref="GameContext"/> to use for rendering on iOS.
    /// </summary>
    public partial class GameContextiOS : GameContextSDL
    {
        /// <inheritDoc/> 
        public GameContextiOS(Window control)
            : base(control)
        {
            ContextType = AppContextType.iOS;
        }
    }

}
#endif
