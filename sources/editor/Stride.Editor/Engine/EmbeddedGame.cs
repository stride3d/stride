// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets;
using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Graphics;

namespace Stride.Editor.Engine
{
    /// <summary>
    /// Represents a Game that is embedded in a external window.
    /// </summary>
    public class EmbeddedGame : Game
    {
        public EmbeddedGame()
        {
            GraphicsDeviceManager.PreferredGraphicsProfile = new [] { GraphicsProfile.Level_11_0, GraphicsProfile.Level_10_1, GraphicsProfile.Level_10_0 };
            GraphicsDeviceManager.PreferredBackBufferWidth = 64;
            GraphicsDeviceManager.PreferredBackBufferHeight = 64;
            GraphicsDeviceManager.PreferredDepthStencilFormat = PixelFormat.D24_UNorm_S8_UInt;
            GraphicsDeviceManager.DeviceCreationFlags = StrideConfig.GraphicsDebugMode ? DeviceCreationFlags.Debug : DeviceCreationFlags.None;

            AutoLoadDefaultSettings = false;
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            base.Initialize();

            Window.IsBorderLess = true;
            Window.IsMouseVisible = true;
        }

        /// <inheritdoc />
        protected sealed override LogListener GetLogListener()
        {
            // We don't want the embedded games to log in the console
            return null;
        }
    }
}
