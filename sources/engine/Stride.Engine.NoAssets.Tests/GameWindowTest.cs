// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.Regression;
using Stride.Rendering.Sprites;
using Stride.Core;
using Stride.Games;

namespace Stride.Engine.Tests
{
    public class GameWindowTest : GameTestBase
    {
        [Theory]
        [InlineData(AppContextType.Desktop)]
        [InlineData(AppContextType.DesktopSDL)]
        public void RenderToWindow(AppContextType contextType)
        {
            PerformTest(game =>
            {
                var context = GameContextFactory.NewGameContext(contextType, isUserManagingRun: true);
                var windowRenderer = new GameWindowRenderer(game.Services, context)
                {
                    PreferredBackBufferWidth = 640,
                    PreferredBackBufferHeight = 480,
                };
                windowRenderer.Initialize();
                ((IContentable)windowRenderer).LoadContent();

                var messageLoop = windowRenderer.Window.CreateUserManagedMessageLoop();
                messageLoop.NextFrame();

                windowRenderer.BeginDraw();
                game.GraphicsContext.CommandList.Clear(windowRenderer.Presenter.BackBuffer, Color.Blue);
                windowRenderer.EndDraw();

                game.SaveImage(windowRenderer.Presenter.BackBuffer, "Clear");

                windowRenderer.Dispose();
            });
        }
    }
}
