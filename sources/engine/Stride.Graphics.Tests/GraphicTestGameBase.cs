// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;

using Stride.Games;
using Stride.Graphics.Regression;
using Stride.Input;

namespace Stride.Graphics.Tests
{
    /// <summary>
    ///   Serves as a base class for graphics-related game tests, providing default configurations
    ///   for the Graphics Device and common functionality for loading content and handling input.
    /// </summary>
    /// <remarks>
    ///   This class by default loads a default Texture (<see cref="UVTexture"/>) that can be used
    ///   to quickly set up graphics tests.
    /// </remarks>
    public class GraphicTestGameBase : GameTestBase
    {
        public Texture UVTexture { get; private set; }


        /// <summary>
        ///   Initializes a new instance of the <see cref="GraphicTestGameBase"/> class with default graphics settings.
        /// </summary>
        public GraphicTestGameBase()
        {
            GraphicsDeviceManager.PreferredBackBufferWidth = 800;
            GraphicsDeviceManager.PreferredBackBufferHeight = 480;
            GraphicsDeviceManager.DeviceCreationFlags = DeviceCreationFlags.Debug;
            GraphicsDeviceManager.PreferredDepthStencilFormat = PixelFormat.D24_UNorm_S8_UInt;
            GraphicsDeviceManager.PreferredGraphicsProfile = [ GraphicsProfile.Level_9_1 ];
        }


        /// <inheritdoc/>
        protected override async Task LoadContent()
        {
            await base.LoadContent();

            UVTexture = Content.Load<Texture>("uv");
        }

        /// <inheritdoc/>
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
