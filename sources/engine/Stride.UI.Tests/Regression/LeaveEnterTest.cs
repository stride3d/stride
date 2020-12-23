// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Input;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Stride.UI.Tests.Regression
{
    /// <summary>
    /// Class for tests on the <see cref="UIElement.TouchLeave"/> and <see cref="UIElement.TouchEnter"/> events.
    /// </summary>
    public class LeaveEnterTest : UITestGameBase
    {
        private Button buttonLeftTop2;

        private Button buttonLeftTop1;

        private Button buttonLeftTop0;

        private Button bottomButton;

        private Button buttonBottomLeft1;

        private Button buttonBottomLeft0;

        private Button bottonBottomRight1;

        private Button buttomBottonRight0;

        public LeaveEnterTest()
        {
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();
            
            buttonLeftTop2 = new Button { Padding = new Thickness(50, 50, 0, 50) };
            buttonLeftTop1 = new Button { Padding = new Thickness(50, 50, 0, 50), Content = buttonLeftTop2 };
            buttonLeftTop0 = new Button { Padding = new Thickness(50, 50, 0, 50), Content = buttonLeftTop1};
            ApplyButtonDefaultStyle(buttonLeftTop2);
            ApplyButtonDefaultStyle(buttonLeftTop1);
            ApplyButtonDefaultStyle(buttonLeftTop0);

            var bottomGrid = new UniformGrid { Rows = 1, Columns = 2 };
            bottomButton = new Button { Content = bottomGrid };
            ApplyButtonDefaultStyle(bottomButton);
            bottomButton.DependencyProperties.Set(GridBase.RowPropertyKey, 1);
            bottomButton.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 2);

            buttonBottomLeft1 = new Button { Margin = new Thickness(50, 50, 0, 50) };
            buttonBottomLeft0 = new Button { Margin = new Thickness(50, 0, 0, 100), Content = buttonBottomLeft1 };
            ApplyButtonDefaultStyle(buttonBottomLeft1);
            ApplyButtonDefaultStyle(buttonBottomLeft0);

            bottonBottomRight1 = new Button { Margin = new Thickness(0, 0, 50, 100) };
            buttomBottonRight0 = new Button { Margin = new Thickness(0, 0, 50, 50), Content = bottonBottomRight1 };
            ApplyButtonDefaultStyle(bottonBottomRight1);
            ApplyButtonDefaultStyle(buttomBottonRight0);
            buttomBottonRight0.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);

            bottomGrid.Children.Add(buttonBottomLeft0);
            bottomGrid.Children.Add(buttomBottonRight0);

            var mainGrid = new UniformGrid { Rows = 2, Columns = 2 };
            mainGrid.Children.Add(buttonLeftTop0);
            mainGrid.Children.Add(bottomButton);

            UIComponent.Page = new Engine.UIPage { RootElement = mainGrid };
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
            FrameGameSystem.Draw(Draw5).TakeScreenshot();
            FrameGameSystem.Draw(Draw6).TakeScreenshot();
            FrameGameSystem.Draw(Draw7).TakeScreenshot();
            FrameGameSystem.Draw(Draw8).TakeScreenshot();
            FrameGameSystem.Draw(Draw9).TakeScreenshot();
            FrameGameSystem.Draw(Draw10).TakeScreenshot();
            FrameGameSystem.Draw(Draw11).TakeScreenshot();
        }

        private void Draw1()
        {
            AddPointerEvent(PointerEventType.Pressed, new Vector2(0.125f, 0.25f));

            Input.Update(new GameTime());
            UI.Update(new GameTime());
        }
        private void Draw2()
        {
            AddPointerEvent(PointerEventType.Moved, new Vector2(0.925f, 0.25f));
            AddPointerEvent(PointerEventType.Released, new Vector2(0.925f, 0.25f));

            Input.Update(new GameTime());
            UI.Update(new GameTime());
        }
        private void Draw3()
        {
            AddPointerEvent(PointerEventType.Pressed, new Vector2(0.125f, 0.25f));

            Input.Update(new GameTime());
            UI.Update(new GameTime());
        }
        private void Draw4()
        {
            AddPointerEvent(PointerEventType.Moved, new Vector2(0.125f, 0.15f));

            Input.Update(new GameTime());
            UI.Update(new GameTime());
        }
        private void Draw5()
        {
            AddPointerEvent(PointerEventType.Moved, new Vector2(0.125f, 0.05f));

            Input.Update(new GameTime());
            UI.Update(new GameTime());
        }
        private void Draw6()
        {
            AddPointerEvent(PointerEventType.Moved, new Vector2(0.925f, 0.05f));
            AddPointerEvent(PointerEventType.Released, new Vector2(0.925f, 0.05f));

            Input.Update(new GameTime());
            UI.Update(new GameTime());
        }
        private void Draw7()
        {
            AddPointerEvent(PointerEventType.Pressed, new Vector2(0.4f, 0.65f));

            Input.Update(new GameTime());
            UI.Update(new GameTime());
        }
        private void Draw8()
        {
            AddPointerEvent(PointerEventType.Moved, new Vector2(0.6f, 0.65f));
            AddPointerEvent(PointerEventType.Released, new Vector2(0.6f, 0.65f));

            Input.Update(new GameTime());
            UI.Update(new GameTime());
        }
        private void Draw9()
        {
            AddPointerEvent(PointerEventType.Pressed, new Vector2(0.6f, 0.65f));

            Input.Update(new GameTime());
            UI.Update(new GameTime());
        }

        private void Draw10()
        {
            AddPointerEvent(PointerEventType.Moved, new Vector2(0.6f, 0.85f));

            Input.Update(new GameTime());
            UI.Update(new GameTime());
        }

        private void Draw11()
        {
            AddPointerEvent(PointerEventType.Moved, new Vector2(0.4f, 0.85f));

            Input.Update(new GameTime());
            UI.Update(new GameTime());
        }

        [Fact]
        public void RunLeaveEnterTest()
        {
            RunGameTest(new LeaveEnterTest());
        }
    }
}
