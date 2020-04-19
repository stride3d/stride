// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_IOS
using UIKit;
using OpenTK.Platform.iPhoneOS;

namespace Stride.Games
{
    /// <summary>
    /// A <see cref="GameContext"/> to use for rendering in iOS.
    /// </summary>
    public partial class GameContextiOS : GameContext<iOSWindow>
    {
        /// <inheritDoc/> 
        public GameContextiOS(iOSWindow window, int requestedWidth = 0, int requestedHeight = 0)
            : base(window, requestedWidth, requestedHeight)
        {
            ContextType = AppContextType.iOS;
        }
    }

}
#endif
