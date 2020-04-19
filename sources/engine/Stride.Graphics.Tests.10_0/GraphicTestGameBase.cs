// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Games;
using Stride.Graphics.Regression;
using Stride.Input;

namespace Stride.Graphics.Tests
{
    public class GraphicTestGameBase : GameTestBase
    {
        public GraphicTestGameBase()
        {
            GraphicsDeviceManager.PreferredBackBufferWidth = 800;
            GraphicsDeviceManager.PreferredBackBufferHeight = 480;
            GraphicsDeviceManager.DeviceCreationFlags = DeviceCreationFlags.None;
            GraphicsDeviceManager.PreferredDepthStencilFormat = PixelFormat.D24_UNorm_S8_UInt;
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_10_0 };
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyDown(Keys.Escape))
            {
                Exit();
            }
        }
    }
}
