// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_PLATFORM_WINDOWS_DESKTOP && XENKO_GRAPHICS_API_OPENGL && (XENKO_UI_WINFORMS || XENKO_UI_WPF) && XENKO_UI_OPENTK
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xenko.Graphics;

namespace Xenko.Games
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
