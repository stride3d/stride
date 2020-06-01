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
    /// Class for rendering tests on the <see cref="ImageElement"/> 
    /// </summary>
    public class ComplexLayoutTest : UITestGameBase
    {
        private StackPanel stackPanel;

        private ToggleButton toggle;

        private ScrollViewer scrollViewer;

        private ScrollingText scrollingText;

        public ComplexLayoutTest()
        {
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var resolution = (Vector3)UIComponent.Resolution;

            var canvas = new Canvas();
            var imgElt = new ImageElement { Name = "UV image", Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")), Width = resolution.X / 5, Height = resolution.Y / 5, StretchType = StretchType.Fill };
            imgElt.DependencyProperties.Set(Canvas.PinOriginPropertyKey, 0.5f * Vector3.One);
            imgElt.DependencyProperties.Set(Canvas.AbsolutePositionPropertyKey, new Vector3(resolution.X / 10, resolution.Y / 10, 0));
            imgElt.DependencyProperties.Set(Panel.ZIndexPropertyKey, -1);

            stackPanel = new StackPanel { Orientation = Orientation.Vertical };

            scrollViewer = new ScrollViewer { ScrollMode = ScrollingMode.Vertical };
            scrollViewer.DependencyProperties.Set(Canvas.AbsolutePositionPropertyKey, new Vector3(resolution.X / 4, 0, 0));
            scrollViewer.Content = stackPanel;

            var button1 = new Button { Margin = Thickness.UniformRectangle(5), Padding = Thickness.UniformRectangle(5), LocalMatrix = Matrix.Scaling(2, 2, 2) };
            ApplyButtonDefaultStyle(button1);
            var textOnly = new TextBlock { Text = "Text only button", Font = Content.Load<SpriteFont>("MicrosoftSansSerif15"), TextColor = new Color(1f, 0, 0, 0.5f) };
            button1.Content = textOnly;

            var button2 = new Button { Name = "Button2", Margin = Thickness.UniformRectangle(5), Padding = Thickness.UniformRectangle(5) };
            ApplyButtonDefaultStyle(button2);
            var imageContent = new ImageElement { Name = "Image Button2", Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")), StretchType = StretchType.FillOnStretch, MaximumHeight = 50 };
            button2.Content = imageContent;

            var button3 = new Button { Margin = Thickness.UniformRectangle(5), Padding = Thickness.UniformRectangle(5) };
            ApplyButtonDefaultStyle(button3);
            var stackContent = new StackPanel { Orientation = Orientation.Horizontal };
            var stackImage = new ImageElement { Name = "Image stack panel", Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")), MaximumHeight = 50 };
            var stackText = new TextBlock { Text = "button text", Font = Content.Load<SpriteFont>("MicrosoftSansSerif15"), Margin = Thickness.UniformRectangle(5) };
            ApplyTextBlockDefaultStyle(stackText);
            stackContent.Children.Add(stackImage);
            stackContent.Children.Add(stackText);
            button3.Content = stackContent;

            var button4 = new Button { Margin = Thickness.UniformRectangle(5), HorizontalAlignment = HorizontalAlignment.Right, Padding = Thickness.UniformRectangle(5) };
            ApplyButtonDefaultStyle(button4);
            var imageContent2 = new ImageElement { Name = "button 4 uv image", Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")), StretchType = StretchType.FillOnStretch, MaximumHeight = 40, Opacity = 0.5f };
            button4.Content = imageContent2;

            var button5 = new Button { Margin = Thickness.UniformRectangle(5), HorizontalAlignment = HorizontalAlignment.Left, Padding = Thickness.UniformRectangle(5) };
            ApplyButtonDefaultStyle(button5);
            var textOnly2 = new TextBlock { Text = "Left aligned", Font = Content.Load<SpriteFont>("MicrosoftSansSerif15") };
            ApplyTextBlockDefaultStyle(textOnly2);
            button5.Content = textOnly2;

            var button6 = new Button
            {
                Height = 50,
                Margin = Thickness.UniformRectangle(5),
                HorizontalAlignment = HorizontalAlignment.Center,
                PressedImage = (SpriteFromTexture)new Sprite(Content.Load<Texture>("ImageButtonPressed")),
                NotPressedImage = (SpriteFromTexture)new Sprite(Content.Load<Texture>("ImageButtonNotPressed")),
                SizeToContent = false,
            };

            var toggleButtonText = new TextBlock { Text = "Toggle button test", Font = Content.Load<SpriteFont>("MicrosoftSansSerif15") };
            ApplyTextBlockDefaultStyle(toggleButtonText);
            toggle = new ToggleButton
            {
                IsThreeState = true,
                Content = toggleButtonText
            };
            ApplyToggleButtonBlockDefaultStyle(toggle);

            scrollingText = new ScrollingText { Font = Content.Load<SpriteFont>("MicrosoftSansSerif15"), Text = "<<<--- Scrolling text in a button ", IsEnabled = ForceInteractiveMode };
            ApplyScrollingTextDefaultStyle(scrollingText);
            var button7 = new Button { Margin = Thickness.UniformRectangle(5), Content = scrollingText };
            ApplyButtonDefaultStyle(button7);

            var uniformGrid = new UniformGrid { Rows = 2, Columns = 2 };
            var gridText = new TextBlock { Text = "Uniform grid", Font = Content.Load<SpriteFont>("MicrosoftSansSerif15"), TextAlignment = TextAlignment.Center};
            ApplyTextBlockDefaultStyle(gridText);
            gridText.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 2);

            var buttonLeftText = new TextBlock { Text = "unif-grid left", Font = Content.Load<SpriteFont>("MicrosoftSansSerif15"), TextAlignment = TextAlignment.Center };
            ApplyTextBlockDefaultStyle(buttonLeftText);
            var buttonLeft = new Button { Content = buttonLeftText };
            ApplyButtonDefaultStyle(buttonLeft);
            buttonLeft.DependencyProperties.Set(GridBase.RowPropertyKey, 1);

            var buttonRightText = new TextBlock { Text = "unif-grid right", Font = Content.Load<SpriteFont>("MicrosoftSansSerif15"), TextAlignment = TextAlignment.Center };
            ApplyTextBlockDefaultStyle(buttonRightText);
            var buttonRight = new Button { Content = buttonRightText };
            ApplyButtonDefaultStyle(buttonRight);
            buttonRight.DependencyProperties.Set(GridBase.RowPropertyKey, 1);
            buttonRight.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            uniformGrid.Children.Add(gridText);
            uniformGrid.Children.Add(buttonLeft);
            uniformGrid.Children.Add(buttonRight);

            stackPanel.Children.Add(button1);
            stackPanel.Children.Add(button2);
            stackPanel.Children.Add(button3);
            stackPanel.Children.Add(button4);
            stackPanel.Children.Add(button5);
            stackPanel.Children.Add(button6);
            stackPanel.Children.Add(toggle);
            stackPanel.Children.Add(button7);
            stackPanel.Children.Add(uniformGrid);

            canvas.Children.Add(imgElt);
            canvas.Children.Add(scrollViewer);

            UIComponent.Page = new Engine.UIPage { RootElement = canvas };
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyPressed(Keys.B))
                scrollViewer.ClipToBounds = !scrollViewer.ClipToBounds;

            if (Input.IsKeyDown(Keys.Down))
                stackPanel.Opacity *= 0.95f;
            if (Input.IsKeyDown(Keys.Up))
                stackPanel.Opacity /= 0.95f;

            if (Input.IsKeyPressed(Keys.H))
                toggle.Visibility = Visibility.Hidden;
            if (Input.IsKeyPressed(Keys.V))
                toggle.Visibility = Visibility.Visible;
            if (Input.IsKeyPressed(Keys.C))
                toggle.Visibility = Visibility.Collapsed;

            if (Input.IsKeyPressed(Keys.E))
                scrollViewer.IsEnabled = !scrollViewer.IsEnabled;
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();
            FrameGameSystem.DrawOrder = -1;
            FrameGameSystem.Draw(DrawTest0).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest1).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest2).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest3).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest4).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest5).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest6).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest7).TakeScreenshot();
        }

        private void DrawTest0()
        {
            scrollViewer.ClipToBounds = false;
        }

        private void DrawTest1()
        {
            scrollViewer.ClipToBounds = true;
        }

        private void DrawTest2()
        {
            stackPanel.Opacity = 0.5f;
        }

        private void DrawTest3()
        {
            stackPanel.Opacity = 0f;
        }

        private void DrawTest4()
        {
            stackPanel.Opacity = 1f;
            // set the scrolling text to a fixed position
            scrollingText.IsEnabled = true;
            ((IUIElementUpdate)scrollingText).Update(new GameTime(new TimeSpan(), new TimeSpan(0, 0, 0, 10)));
            scrollingText.IsEnabled = false;
            scrollViewer.ScrollTo(new Vector3(50, 100, 0));
        }

        private void DrawTest5()
        {
            toggle.Visibility = Visibility.Hidden;
        }

        private void DrawTest6()
        {
            toggle.Visibility = Visibility.Collapsed;
        }

        private void DrawTest7()
        {
            toggle.Visibility = Visibility.Visible;
        }

        [Fact]
        public void RunComplexLayoutTest()
        {
            RunGameTest(new ComplexLayoutTest());
        }
    }
}
