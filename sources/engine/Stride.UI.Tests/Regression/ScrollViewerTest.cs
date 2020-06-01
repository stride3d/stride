// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering.Sprites;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Stride.UI.Tests.Regression
{
    /// <summary>
    /// Class for rendering tests on the <see cref="ScrollViewer"/> 
    /// </summary>
    public class ScrollViewerTest : UITestGameBase
    {
        private ScrollViewer scrollViewer;

        private UniformGrid grid;

        private StackPanel stackPanel;

        private ImageElement img3;

        private ContentDecorator contentDecorator;

        public ScrollViewerTest()
        {
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var sprites = Content.Load<SpriteSheet>("UIImages");

            var img1 = new ImageElement { Name = "UV 1 stack panel", Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")) };
            var img2 = new ImageElement { Name = "UV 2 stack panel", Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")) };
            img3 = new ImageElement { Name = "UV 3 stack panel", Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")) };

            stackPanel = new StackPanel { Orientation = Orientation.Vertical };
            stackPanel.Children.Add(img1);
            stackPanel.Children.Add(img2);
            stackPanel.Children.Add(img3);

            var img4 = new ImageElement { Name = "UV grid", Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")) };
            var img5 = new ImageElement { Name = "UV grid 2", Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")) };
            var img6 = new ImageElement { Name = "Game screen grid", Source = SpriteFromSheet.Create(sprites, "GameScreen") };

            img4.DependencyProperties.Set(GridBase.ColumnPropertyKey, 0);
            img4.DependencyProperties.Set(GridBase.RowPropertyKey, 0);
            img5.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            img5.DependencyProperties.Set(GridBase.RowPropertyKey, 0);
            img6.DependencyProperties.Set(GridBase.ColumnPropertyKey, 0);
            img6.DependencyProperties.Set(GridBase.RowPropertyKey, 1);
            img6.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 2);

            grid = new UniformGrid { Columns = 2, Rows = 2 };
            grid.Children.Add(img4);
            grid.Children.Add(img5);
            grid.Children.Add(img6);

            scrollViewer = new ScrollViewer { Content = grid, ScrollMode = ScrollingMode.HorizontalVertical};

            contentDecorator = new ContentDecorator { Content = scrollViewer };

            UIComponent.Page = new Engine.UIPage { RootElement = contentDecorator };
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            if (Input.IsKeyReleased(Keys.D1))
                scrollViewer.Content = grid;
            if (Input.IsKeyReleased(Keys.D2))
                scrollViewer.Content = stackPanel;

            if (Input.IsKeyReleased(Keys.NumPad4))
                scrollViewer.ScrollToBeginning(Orientation.Horizontal);
            if (Input.IsKeyReleased(Keys.NumPad6))
                scrollViewer.ScrollToEnd(Orientation.Horizontal);
            if (Input.IsKeyReleased(Keys.NumPad8))
                scrollViewer.ScrollToBeginning(Orientation.Vertical);
            if (Input.IsKeyReleased(Keys.NumPad2))
                scrollViewer.ScrollToEnd(Orientation.Vertical);

            if (Input.IsKeyReleased(Keys.V))
                scrollViewer.ScrollMode = ScrollingMode.Vertical;
            if (Input.IsKeyReleased(Keys.H))
                scrollViewer.ScrollMode = ScrollingMode.Horizontal;
            if (Input.IsKeyReleased(Keys.B))
                scrollViewer.ScrollMode = ScrollingMode.HorizontalVertical;

            if (Input.IsKeyReleased(Keys.Space)) // check that scroll offsets are correctly updated when content gets smaller (and we are at the end of document)
                grid.Height = float.IsNaN(grid.Height) ? 100 : float.NaN;

            if (Input.IsKeyReleased(Keys.Enter)) // check that scrolling works even when IsArrange is false (try this when ScrollMode is in Horizontal mode)
            {
                grid.Height = 1000;
                scrollViewer.ScrollMode = ScrollingMode.Vertical;
                scrollViewer.ScrollToEnd(Orientation.Vertical);
            }
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.DrawOrder = -1;
            FrameGameSystem.TakeScreenshot(5); // skip some frames in order to be sure that the picking will work
            FrameGameSystem.Draw(Draw0).TakeScreenshot();
            FrameGameSystem.Draw(Draw1).TakeScreenshot();
            FrameGameSystem.Draw(Draw2).TakeScreenshot();
            FrameGameSystem.Draw(Draw3).TakeScreenshot();
            FrameGameSystem.Draw(Draw4).TakeScreenshot();
            FrameGameSystem.Draw(Draw5).TakeScreenshot();
            FrameGameSystem.Draw(Draw6).TakeScreenshot();
            FrameGameSystem.Draw(Draw7).TakeScreenshot();
            FrameGameSystem.Draw(Draw8).TakeScreenshot();
            FrameGameSystem.Draw(Draw9).TakeScreenshot();
            FrameGameSystem.Draw(Draw10).TakeScreenshot();
            FrameGameSystem.Draw(Draw11).TakeScreenshot();
            FrameGameSystem.Draw(Draw12).TakeScreenshot();
            FrameGameSystem.Draw(Draw13).TakeScreenshot();
            FrameGameSystem.Draw(Draw14).TakeScreenshot();
        }

        private void Draw0()
        {
            // check that scrolling works before any layouting
            scrollViewer.ScrollToEnd(Orientation.Vertical);
            scrollViewer.ScrollToEnd(Orientation.Horizontal);
        }

        private void Draw1()
        {
            // show the scroll bars
            AddPointerEvent(PointerEventType.Pressed, new Vector2(0.5f, 0.5f));
            AddPointerEvent(PointerEventType.Moved, new Vector2(0.3f, 0.3f));

            Input.Update(new GameTime());
            UI.Update(new GameTime(new TimeSpan(), new TimeSpan(0, 0, 0, 0, 500)));
        }

        private void Draw2()
        {
            // check that ScrollTo works properly for a content not implementing IScrollInfo
            scrollViewer.ScrollTo(new Vector3(1000, 1000, 1000));
        }

        private void Draw3()
        {
            // check that scrolling force arrange update when scrolling is delayed to arrange function
            contentDecorator.InvalidateMeasure(); // invalidate parent layout -> invalidate scroll viewer layout but do not force update
            scrollViewer.ScrollToEnd(Orientation.Vertical); // should delay scroll in next draw and force arrange update to have correct result.
        }

        private void Draw4()
        {
            // check that changing mode correctly update layout and reset offsets
            scrollViewer.ScrollMode = ScrollingMode.Vertical;
        }

        private void Draw5()
        {
            // check that ScrollOf works properly for a content not implementing IScrollInfo
            scrollViewer.ScrollOf(new Vector3(400,400,400));
        }

        private void Draw6()
        {
            // check that changing mode correctly update layout and reset offsets
            scrollViewer.ScrollMode = ScrollingMode.Horizontal;
        }

        private void Draw7()
        {
            // check that ScrollToEnd properly works if layout is invalidated
            scrollViewer.ScrollMode = ScrollingMode.HorizontalVertical;
            scrollViewer.ScrollToEnd(Orientation.Vertical);
            scrollViewer.ScrollToEnd(Orientation.Horizontal);
        }

        private void Draw8()
        {
            // check that scroll offset are correctly updated when layout is invalidated
            grid.Height = 100;
        }

        private void Draw9()
        {
            // check that layout/offsets are correctly updated when content is changed
            scrollViewer.Content = stackPanel;
        }

        private void Draw10()
        {
            // check that ScrollToEnd works properly with children implementing  IScrollInfo
            scrollViewer.ScrollToEnd(Orientation.Horizontal);
            scrollViewer.ScrollToEnd(Orientation.Vertical);
        }

        private void Draw11()
        {
            // check that changing scroll mode update properly layout and offset with children implementing  IScrollInfo
            scrollViewer.ScrollMode = ScrollingMode.Horizontal;
        }

        private void Draw12()
        {
            // check that ScrollOf works properly for a content implementing IScrollInfo
            scrollViewer.ScrollOf(new Vector3(400, 400, 400));
        }

        private void Draw13()
        {
            // check that changing scroll mode update properly layout and offset with children implementing  IScrollInfo
            scrollViewer.ScrollMode = ScrollingMode.Vertical;
        }

        private void Draw14()
        {
            // check that ScrollTo works properly for a content implementing IScrollInfo
            scrollViewer.ScrollTo(new Vector3(300, 300, 300));
        }

        [Fact]
        public void RunScrollViewerTest()
        {
            RunGameTest(new ScrollViewerTest());
        }
    }
}
