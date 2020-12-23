// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering.Compositing;
using Stride.Rendering.Sprites;
using Stride.UI.Controls;
using Stride.UI.Events;
using Stride.UI.Panels;

namespace Stride.UI.Tests.Regression
{
    /// <summary>
    /// Class for rendering tests on the <see cref="ModalElement"/>
    /// </summary>
    public class ModalElementTest : UITestGameBase
    {
        private UniformGrid uniformGrid;

        private ModalElement modal1;
        private ModalElement modal2;

        private TextBlock modalButton1Text;

        private TextBlock modalButton2Text;

        private SpriteSheet sprites;

        public ModalElementTest()
        {
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            sprites = Content.Load<SpriteSheet>("UIImages");

            // Also draw a texture during the clear renderer
            // TODO: Use a custom compositor as soon as we have visual scripting?
            var topChildRenderer = ((SceneCameraRenderer)SceneSystem.GraphicsCompositor.Game).Child;
            var forwardRenderer = (topChildRenderer as SceneRendererCollection)?.Children.OfType<ForwardRenderer>().FirstOrDefault() ?? (ForwardRenderer)topChildRenderer;
            forwardRenderer.Clear = new ClearAndDrawTextureRenderer { Color = forwardRenderer.Clear.Color, Texture = sprites["GameScreen"].Texture };

            var lifeBar = new ImageElement { Source = SpriteFromSheet.Create(sprites, "Logo"), HorizontalAlignment = HorizontalAlignment.Center };
            lifeBar.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 3);

            var quitText = new TextBlock { Text = "Quit Game", Font = Content.Load<SpriteFont>("MicrosoftSansSerif15") };
            ApplyTextBlockDefaultStyle(quitText);
            var quitGameButton = new Button
            {
                Content = quitText,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Left,
                Padding = Thickness.UniformRectangle(10),
            };
            ApplyButtonDefaultStyle(quitGameButton);
            quitGameButton.DependencyProperties.Set(GridBase.ColumnPropertyKey, 0);
            quitGameButton.DependencyProperties.Set(GridBase.RowPropertyKey, 2);
            quitGameButton.Click += (sender, args) => Exit();

            modalButton1Text = new TextBlock { Text = "Close Modal window 1", Font = Content.Load<SpriteFont>("MicrosoftSansSerif15") };
            ApplyTextBlockDefaultStyle(modalButton1Text);
            var modalButton1 = new Button
            {
                Name = "Button Modal 1",
                Content = modalButton1Text,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Padding = Thickness.UniformRectangle(10),
            };
            ApplyButtonDefaultStyle(modalButton1);
            modalButton1.Click += ModalButton1OnClick;
            modal1 = new ModalElement { Content = modalButton1, Name = "Modal 1"};
            modal1.DependencyProperties.Set(Panel.ZIndexPropertyKey, 1);
            modal1.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            modal1.DependencyProperties.Set(GridBase.RowPropertyKey, 1);
            modal1.OutsideClick += Modal1OnOutsideClick;

            modalButton2Text = new TextBlock { Text = "Close Modal window 2", Font = Content.Load<SpriteFont>("MicrosoftSansSerif15") };
            ApplyTextBlockDefaultStyle(modalButton2Text);
            var modalButton2 = new Button
            {
                Name = "Button Modal 2",
                Content = modalButton2Text,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Padding = Thickness.UniformRectangle(10),
            };
            ApplyButtonDefaultStyle(modalButton2);
            modalButton2.Click += ModalButton2OnClick;
            modal2 = new ModalElement { Content = modalButton2, Name = "Modal 2" };
            modal2.DependencyProperties.Set(Panel.ZIndexPropertyKey, 2);
            modal2.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            modal2.DependencyProperties.Set(GridBase.RowPropertyKey, 2);
            modal2.OutsideClick += Modal2OnOutsideClick;

            uniformGrid = new UniformGrid { Columns = 3, Rows = 3 };
            uniformGrid.Children.Add(modal1);
            uniformGrid.Children.Add(modal2);
            uniformGrid.Children.Add(lifeBar);
            uniformGrid.Children.Add(quitGameButton);

            UIComponent.Page = new Engine.UIPage { RootElement = uniformGrid };
        }

        private void Modal1OnOutsideClick(object sender, RoutedEventArgs routedEventArgs)
        {
            modalButton1Text.Text = "Click on the Button, please";
        }

        private void Modal2OnOutsideClick(object sender, RoutedEventArgs routedEventArgs)
        {
            modalButton2Text.Text = "Click on the Button, please";
        }

        private void ModalButton1OnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            uniformGrid.Children.Remove(modal1);
        }

        private void ModalButton2OnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            uniformGrid.Children.Remove(modal2);
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyReleased(Keys.D1))
                uniformGrid.Children.Add(modal1);
            if (Input.IsKeyReleased(Keys.D2))
                uniformGrid.Children.Add(modal2);
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();
            FrameGameSystem.DrawOrder = -1;
            FrameGameSystem.TakeScreenshot(5); // skip some frames in order to be sure that the picking will work
            FrameGameSystem.Draw(Draw1).TakeScreenshot();
            FrameGameSystem.Draw(Draw2).TakeScreenshot();
            FrameGameSystem.Draw(Draw3).TakeScreenshot();
            FrameGameSystem.Draw(Draw4).TakeScreenshot();
        }

        private void Draw1()
        {
            AddPointerEvent(PointerEventType.Pressed, new Vector2(0.125f, 0.15f));
            AddPointerEvent(PointerEventType.Released,   new Vector2(0.125f, 0.15f));

            Input.Update(new GameTime());
            UI.Update(new GameTime());
        }

        private void Draw2()
        {
            AddPointerEvent(PointerEventType.Pressed, new Vector2(0.5f, 0.85f));
            AddPointerEvent(PointerEventType.Released, new Vector2(0.5f, 0.85f));

            Input.Update(new GameTime());
            UI.Update(new GameTime());
        }

        private void Draw3()
        {
            AddPointerEvent(PointerEventType.Pressed, new Vector2(0.05f, 0.95f));
            AddPointerEvent(PointerEventType.Released, new Vector2(0.05f, 0.95f));

            Input.Update(new GameTime());
            UI.Update(new GameTime());
        }

        private void Draw4()
        {
            AddPointerEvent(PointerEventType.Pressed, new Vector2(0.5f, 0.5f));
            AddPointerEvent(PointerEventType.Released, new Vector2(0.5f, 0.5f));

            Input.Update(new GameTime());
            UI.Update(new GameTime());
        }

        [Fact]
        public void RunModalElementTest()
        {
            RunGameTest(new ModalElementTest());
        }

        private class ClearAndDrawTextureRenderer : ClearRenderer
        {
            public Texture Texture { get; set; }

            protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
            {
                base.DrawCore(context, drawContext);

                drawContext.GraphicsContext.DrawTexture(Texture);
            }
        }
    }
}
