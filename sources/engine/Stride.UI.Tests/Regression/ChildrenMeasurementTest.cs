// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Xunit;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Stride.UI.Tests.Regression
{
    /// <summary>
    /// Test to see if the children of a panel's panel get rendered correctly the root panel size is manually set.
    /// </summary>
    public class ChildrenMeasurementTest : UITestGameBase
    {
        private Canvas canvas;
        private StackPanel stackPanel;

        public ChildrenMeasurementTest()
        {
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();


            var font = Content.Load<SpriteFont>("MicrosoftSansSerif15");

            // root panel (any kind of panel could be used for this test)
            canvas = new Canvas
            {
                BackgroundColor = Color.LightBlue,
            };
            // child panel with children to be rendered
            stackPanel = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Some text.", Font = font, TextColor = Color.Black },
                    new TextBlock { Text = "Some other text.", Font = font, TextColor = Color.Black }
                },
                Orientation = Orientation.Vertical
            };

            canvas.Children.Add(stackPanel);

            UIComponent.Page = new Engine.UIPage { RootElement = canvas };
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();
            FrameGameSystem.DrawOrder = -1;
            FrameGameSystem.Draw(DrawTest0).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest1).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest2).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest3).TakeScreenshot();
        }

        /// <summary>
        /// Sets manually the size of the panel (all dimensions are set).
        /// </summary>
        private void DrawTest0()
        {
            var resolution = UIComponent.Resolution;
            resolution.Z = 0;
            canvas.Size = resolution;
        }

        /// <summary>
        /// Changes the child StackPanel background color, to visualize its rendered size.
        /// </summary>
        private void DrawTest1()
        {
            stackPanel.BackgroundColor = Color.Gray;
        }

        /// <summary>
        /// Resets root panel size to undetermined (float.NaN).
        /// Also changes the child StackPanel background color to generate a different image.
        /// </summary>
        private void DrawTest2()
        {
            canvas.Size = new Vector3(float.NaN);
            stackPanel.BackgroundColor = Color.LightGray;
        }

        /// <summary>
        /// Changes orientation of the child StackPanel.
        /// </summary>
        private void DrawTest3()
        {
            stackPanel.Orientation = Orientation.Horizontal;
        }

        [Fact]
        public void RunChildrenMeasurementTest()
        {
            RunGameTest(new ChildrenMeasurementTest());
        }
    }
}
