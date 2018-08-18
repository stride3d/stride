// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;

using Xunit;

using Xenko.Core.Mathematics;
using Xenko.Games;
using Xenko.Graphics;
using Xenko.Input;
using Xenko.UI.Controls;
using Xenko.UI.Panels;

namespace Xenko.UI.Tests.Regression
{
    /// <summary>
    /// Class for rendering tests on the <see cref="ScrollViewer"/> 
    /// </summary>
    public class ScrollViewerAnchorTest : UITestGameBase
    {
        private class TestScrollViewer : ScrollViewer
        {
            public bool SkipUpdates;

            protected override void Update(GameTime time)
            {
                if (!SkipUpdates)
                    base.Update(time);
            }

            public void ManualUpdates(double elapsedSeconds, int updateTimes)
            {
                var elapsedSpan = TimeSpan.FromSeconds(elapsedSeconds);
                var gameTime = new GameTime(elapsedSpan, elapsedSpan);

                for (int i = 0; i < updateTimes; i++)
                    base.Update(gameTime);
            }
        }

        private Grid grid;
        private StackPanel randomStackPanel;
        private StackPanel virtualizedStackPanel;
        private UniformGrid uniformGrid;

        private TestScrollViewer scrollViewer;

        private readonly Random random = new Random(0);

        private readonly Vector3 scrollValue = new Vector3(300, 400, 500);

        public ScrollViewerAnchorTest()
        {
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            // build the randomStackPanel elements
            randomStackPanel = new StackPanel { Orientation = Orientation.Vertical };
            for (int i = 0; i < 30; i++)
                randomStackPanel.Children.Add(CreateButton(0, i, 50, 1200, true));

            // build the randomStackPanel elements
            virtualizedStackPanel = new StackPanel { Orientation = Orientation.Vertical, ItemVirtualizationEnabled = true };
            for (int i = 0; i < 30; i++)
                virtualizedStackPanel.Children.Add(CreateButton(0, i, 75, 1200));

            // build the uniform grid
            uniformGrid = new UniformGrid { Columns = 15, Rows = 20 };
            for (int c = 0; c < uniformGrid.Columns; ++c)
            {
                for (int r = 0; r < uniformGrid.Rows; ++r)
                    uniformGrid.Children.Add(CreateButton(c,r, 175, 300));
            }
                
            // build the grid
            const int gridColumns = 10;
            const int gridRows = 10;
            grid = new Grid();
            grid.LayerDefinitions.Add(new StripDefinition(StripType.Auto));
            for (int i = 0; i < gridColumns; i++)
                grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto));
            for (int i = 0; i < gridRows; i++)
                grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));
            for (int c = 0; c < gridColumns; ++c)
            {
                for (int r = 0; r < gridRows; ++r)
                    grid.Children.Add(CreateButton(c, r, 50 + r * 30, 100 + c * 40));
            }

            // build the scroll viewer
            scrollViewer = new TestScrollViewer { Name = "sv", Content = randomStackPanel, ScrollMode = ScrollingMode.HorizontalVertical, SnapToAnchors = true };

            // set the scroll viewer as the root
            UIComponent.Page = new Engine.UIPage { RootElement = scrollViewer };
        }

        private Button CreateButton(int col, int row, float minimumHeight = 0, float minimumWidth = 0, bool randomMinHeight = false, bool randowMinWidth = false)
        {
            var textBlock = new TextBlock { Text = "Col " + col + " - Row " + row, Font = Content.Load<SpriteFont>("MicrosoftSansSerif15"), TextAlignment = TextAlignment.Center};
            ApplyTextBlockDefaultStyle(textBlock);
            var button =  new Button
            {
                Name = "Button at col " + col + " - row " + row,
                MinimumHeight = minimumHeight,
                MinimumWidth = minimumWidth,
                Content = textBlock
            };
            ApplyButtonDefaultStyle(button);

            if (randomMinHeight)
                button.MinimumHeight = minimumHeight + 3 * (float)random.NextDouble() * minimumHeight;

            if (randowMinWidth)
                button.MinimumWidth = minimumWidth + 3 * (float)random.NextDouble() * minimumWidth;

            button.DependencyProperties.Set(GridBase.ColumnPropertyKey, col);
            button.DependencyProperties.Set(GridBase.RowPropertyKey, row);

            return button;
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyReleased(Keys.Enter))
                scrollViewer.SnapToAnchors = !scrollViewer.SnapToAnchors;

            if (Input.IsKeyReleased(Keys.D1))
                scrollViewer.Content = randomStackPanel;
            if (Input.IsKeyReleased(Keys.D2))
                scrollViewer.Content = virtualizedStackPanel;
            if (Input.IsKeyReleased(Keys.D3))
                scrollViewer.Content = uniformGrid;
            if (Input.IsKeyReleased(Keys.D4))
                scrollViewer.Content = grid;
            
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
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Update(() => scrollViewer.SkipUpdates = true); // we don't want automatic random updates

            FrameGameSystem.DrawOrder = -1; // perform the draw action before the UI draw calls

            RegisterTestsForContent(randomStackPanel);
            RegisterTestsForContent(virtualizedStackPanel);
            RegisterTestsForContent(uniformGrid);
            RegisterTestsForContent(grid);
        }

        private void RegisterTestsForContent(UIElement content)
        {
            FrameGameSystem.Draw(() => SetContentTo(content));
            FrameGameSystem.Draw(ScrollWithoutSnapping).TakeScreenshot();
            FrameGameSystem.Draw(PerformManualUpdates).TakeScreenshot();
            FrameGameSystem.Draw(ScrollWithSnapping).TakeScreenshot();
            FrameGameSystem.Draw(PerformManualUpdates).TakeScreenshot();
        }

        private void PerformManualUpdates()
        {
            scrollViewer.ManualUpdates(0.016, 200);
            scrollViewer.HideScrollBars();
        }

        private void ScrollWithoutSnapping()
        {
            scrollViewer.SnapToAnchors = false;
            scrollViewer.ScrollTo(scrollValue);
        }

        private void ScrollWithSnapping()
        {
            scrollViewer.SnapToAnchors = true;
            scrollViewer.ScrollTo(scrollValue);
        }

        private void SetContentTo(UIElement element)
        {
            scrollViewer.Content = element;
        }
        
        [Fact]
        public void RunScrollViewerAnchorTests()
        {
            RunGameTest(new ScrollViewerAnchorTest());
        }

        /// <summary>
        /// Launch the Image test.
        /// </summary>
        internal static void Main()
        {
            using (var game = new ScrollViewerAnchorTest())
                game.Run();
        }
    }
}
