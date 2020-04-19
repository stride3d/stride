// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_WINDOWS_DESKTOP && STRIDE_GRAPHICS_API_OPENGL && (STRIDE_UI_WINFORMS || STRIDE_UI_WPF) && STRIDE_UI_OPENTK
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Stride.Graphics;

namespace Stride.Games
{
    internal class GamePlatformOpenTK : GamePlatformWindows, IGraphicsDeviceFactory
    {
        public GamePlatformOpenTK(GameBase game) : base(game)
        {
        }

        public virtual void DeviceChanged(GraphicsDevice currentDevice, GraphicsDeviceInformation deviceInformation)
        {
            // TODO: Check when it needs to be disabled on iOS (OpenGL)?
            // Force to resize the gameWindow
            //gameWindow.Resize(deviceInformation.PresentationParameters.BackBufferWidth, deviceInformation.PresentationParameters.BackBufferHeight);
        }
    }
}
#endif
