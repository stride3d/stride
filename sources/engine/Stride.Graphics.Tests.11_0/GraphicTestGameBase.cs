// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Games;
using Stride.Graphics.Regression;
using Stride.Input;

namespace Stride.Graphics.Tests;

/// <summary>
///   Provides a base class for graphics-based game tests on <see cref="GraphicsProfile.Level_11_0"/>,
///   configuring default graphics settings and handling common game loop functionality.
/// </summary>
/// <seealso cref="GameTestBase"/>
public class GraphicTestGameBase : GameTestBase
{
    /// <summary>
    ///   Initializes a new instance of the <see cref="GraphicTestGameBase"/> class.
    /// </summary>
    public GraphicTestGameBase()
    {
        GraphicsDeviceManager.PreferredBackBufferWidth = 800;
        GraphicsDeviceManager.PreferredBackBufferHeight = 480;
#if DEBUG
        GraphicsDeviceManager.DeviceCreationFlags = DeviceCreationFlags.Debug;
#else
        GraphicsDeviceManager.DeviceCreationFlags = DeviceCreationFlags.None;
#endif
        GraphicsDeviceManager.PreferredDepthStencilFormat = PixelFormat.D24_UNorm_S8_UInt;
        GraphicsDeviceManager.PreferredGraphicsProfile = [ GraphicsProfile.Level_11_0 ];
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
