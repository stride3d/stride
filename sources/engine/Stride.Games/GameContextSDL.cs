// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_UI_SDL
using Stride.Graphics.SDL;

namespace Stride.Games
{
    /// <summary>
    /// A <see cref="GameContext"/> to use for rendering to an existing SDL Window.
    /// </summary>
    public class GameContextSDL : GameContextDesktop<Window>
    {
        static GameContextSDL()
        {
            // Preload proper SDL native library (depending on CPU type)
            Core.NativeLibrary.PreloadLibrary("SDL2.dll", typeof(Window));
        }

        /// <inheritDoc/>
        public GameContextSDL(Window control, int requestedWidth = 0, int requestedHeight = 0, bool isUserManagingRun = false)
            : base(control ?? new GameFormSDL(), requestedWidth, requestedHeight, isUserManagingRun)
        {
            ContextType = AppContextType.DesktopSDL;
        }
    }
}
#endif
