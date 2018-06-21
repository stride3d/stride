// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;

using NUnit.Framework;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Games;
using Xenko.Graphics;
using Xenko.Input;
using Xenko.UI.Controls;
using Xenko.UI.Panels;

namespace Xenko.UI.Tests.Regression
{
    /// <summary>
    /// Test the mouse over event/property/designs
    /// </summary>
    public class MouseOverTest : UITestGameBase
    {
        private Button button1;
        private Button button2;
        private EditText edit1;
        private EditText edit2;
        private Canvas canvas;
        private StackPanel stackPanel;

        private bool triggeredButton1;
        private bool triggeredButton2;
        private bool triggeredEdit1;
        private bool triggeredEdit2;
        private bool triggeredCanvas;
        private bool triggeredStackPanel;

        private MouseOverState oldValueButton1;
        private MouseOverState newValueButton1;

        public MouseOverTest()
        {
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var background = new Entity { new BackgroundComponent { Texture = Content.Load<Texture>("XenkoBackground") } };
            Scene.Entities.Add(background);

            var text1 = new TextBlock { Text = "text block button 1", Font = Content.Load<SpriteFont>("CourierNew12"), SynchronousCharacterGeneration = true };
            ApplyTextBlockDefaultStyle(text1);
            button1 = new Button { Content = text1 };
            ApplyButtonDefaultStyle(button1);
            button1.SetCanvasRelativePosition(new Vector3(0.025f, 0.05f, 0f));

            edit1 = new EditText() { Text = "Edit text 1", Font = Content.Load<SpriteFont>("CourierNew12"), SynchronousCharacterGeneration = true, };
            ApplyEditTextDefaultStyle(edit1);
            edit1.SetCanvasRelativePosition(new Vector3(0.025f, 0.15f, 0f));

            var text2 = new TextBlock { Text = "text block button 2", Font = Content.Load<SpriteFont>("MicrosoftSansSerif15"), SynchronousCharacterGeneration = true };
            ApplyTextBlockDefaultStyle(text2);
            button2 = new Button { Content = text2 };
            ApplyButtonDefaultStyle(button2);
            edit2 = new EditText() { Text = "Edit text 2", Font = Content.Load<SpriteFont>("MicrosoftSansSerif15"), };
            ApplyEditTextDefaultStyle(edit2);

            stackPanel = new StackPanel
            {
                Children = { button2, edit2 }, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                VerticalAlignment = VerticalAlignment.Center, 
                Orientation = Orientation.Horizontal
            };
            stackPanel.SetCanvasRelativePosition(new Vector3(0.5f, 0.5f, 0f));
            stackPanel.SetCanvasPinOrigin(new Vector3(.5f));

            canvas = new Canvas { Children = {button1, edit1, stackPanel}, CanBeHitByUser = true };

            button1.MouseOverStateChanged += (sender, args) => { triggeredButton1 = true; oldValueButton1 = args.OldValue; newValueButton1 = args.NewValue;};
            button2.MouseOverStateChanged += (sender, args) => { triggeredButton2 = true;};
            edit1.MouseOverStateChanged += (sender, args) => { triggeredEdit1 = true;};
            edit2.MouseOverStateChanged += (sender, args) => { triggeredEdit2 = true;};
            canvas.MouseOverStateChanged += (sender, args) => { triggeredCanvas = true;};
            stackPanel.MouseOverStateChanged += (sender, args) => { triggeredStackPanel = true;};

            canvas.UIElementServices = new UIElementServices { Services = this.Services };

            UIComponent.Page = new Engine.UIPage { RootElement = canvas };
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.DrawOrder = -1;
            FrameGameSystem.TakeScreenshot();
            FrameGameSystem.Draw(1, Test1);
            FrameGameSystem.Draw(2, PrepareTest2);
            FrameGameSystem.Draw(3, Test2);
            FrameGameSystem.Draw(4, PrepareTest3);
            FrameGameSystem.Draw(5, Test3);
            FrameGameSystem.Draw(6, PrepareTest4);
            FrameGameSystem.Draw(7, Test4);
            FrameGameSystem.Draw(8, PrepareTest5);
            FrameGameSystem.Draw(9, Test5);
            FrameGameSystem.Draw(10, PrepareTest6);
            FrameGameSystem.Draw(11, Test6);
            FrameGameSystem.Draw(12, Draw1).TakeScreenshot(12);
            FrameGameSystem.Draw(13, Draw2).TakeScreenshot(13);
        }

        private void SetMousePosition(Vector2 position)
        {
            var mouse = MouseSimulated;
            mouse.SetPosition(position);
        }

        private void Test1()
        {
            ResetStates();

            Assert.AreEqual(MouseOverState.MouseOverNone, canvas.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, stackPanel.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, button1.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, button2.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, edit1.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, edit2.MouseOverState);

            Assert.IsFalse(triggeredButton1);
            Assert.IsFalse(triggeredButton2);
            Assert.IsFalse(triggeredEdit1);
            Assert.IsFalse(triggeredEdit2);
            Assert.IsFalse(triggeredCanvas);
            Assert.IsFalse(triggeredStackPanel);
        }

        private void PrepareTest2()
        {
            ResetStates();
            SetMousePosition(new Vector2(0.1f, 0.08f));
            Input.Update(new GameTime());
        }

        private void Test2()
        {
            Assert.AreEqual(MouseOverState.MouseOverChild, canvas.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, stackPanel.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverElement, button1.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, button2.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, edit1.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, edit2.MouseOverState);

            Assert.IsTrue(triggeredButton1);
            Assert.IsTrue(triggeredCanvas);
            Assert.IsFalse(triggeredButton2);
            Assert.IsFalse(triggeredEdit1);
            Assert.IsFalse(triggeredEdit2);
            Assert.IsFalse(triggeredStackPanel);

            Assert.AreEqual(MouseOverState.MouseOverNone, oldValueButton1);
            Assert.AreEqual(MouseOverState.MouseOverElement, newValueButton1);
        }

        private void PrepareTest3()
        {
            ResetStates();
            SetMousePosition(new Vector2(0.1f, 0.18f));
            Input.Update(new GameTime());
        }

        private void Test3()
        {
            Assert.AreEqual(MouseOverState.MouseOverChild, canvas.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, stackPanel.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, button1.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, button2.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverElement, edit1.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, edit2.MouseOverState);

            Assert.IsTrue(triggeredButton1);
            Assert.IsTrue(triggeredEdit1);
            Assert.IsFalse(triggeredCanvas);
            Assert.IsFalse(triggeredButton2);
            Assert.IsFalse(triggeredEdit2);
            Assert.IsFalse(triggeredStackPanel);
        }
        
        private void PrepareTest4()
        {
            ResetStates();
            SetMousePosition(new Vector2(0.1f, 0.3f));
            Input.Update(new GameTime());
        }

        private void Test4()
        {
            Assert.AreEqual(MouseOverState.MouseOverElement, canvas.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, stackPanel.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, button1.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, button2.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, edit1.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, edit2.MouseOverState);

            Assert.IsTrue(triggeredEdit1);
            Assert.IsTrue(triggeredCanvas);
            Assert.IsFalse(triggeredButton2);
            Assert.IsFalse(triggeredButton1);
            Assert.IsFalse(triggeredEdit2);
            Assert.IsFalse(triggeredStackPanel);
        }
        
        private void PrepareTest5()
        {
            ResetStates();
            SetMousePosition(new Vector2(0.5f, 0.5f));
            Input.Update(new GameTime());
        }

        private void Test5()
        {
            Assert.AreEqual(MouseOverState.MouseOverChild, canvas.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverChild, stackPanel.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, button1.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverElement, button2.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, edit1.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, edit2.MouseOverState);

            Assert.IsTrue(triggeredCanvas);
            Assert.IsTrue(triggeredButton2);
            Assert.IsTrue(triggeredStackPanel);
            Assert.IsFalse(triggeredButton1);
            Assert.IsFalse(triggeredEdit2);
            Assert.IsFalse(triggeredEdit1);
        }

        private void PrepareTest6()
        {
            ResetStates();
            SetMousePosition(new Vector2(0.56f, 0.5f));
            Input.Update(new GameTime());
        }

        private void Test6()
        {
            Assert.AreEqual(MouseOverState.MouseOverChild, canvas.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverChild, stackPanel.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, button1.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, button2.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverNone, edit1.MouseOverState);
            Assert.AreEqual(MouseOverState.MouseOverElement, edit2.MouseOverState);

            Assert.IsTrue(triggeredEdit2);
            Assert.IsTrue(triggeredButton2);
            Assert.IsFalse(triggeredCanvas);
            Assert.IsFalse(triggeredStackPanel);
            Assert.IsFalse(triggeredButton1);
            Assert.IsFalse(triggeredEdit1);
        }

        private void ResetStates()
        {
            triggeredButton1 = false;
            triggeredButton2 = false;
            triggeredEdit1 = false;
            triggeredEdit2 = false;
            triggeredCanvas = false;
            triggeredStackPanel = false;
        }

        private void Draw1()
        {
            SetMousePosition(new Vector2(0.1f, 0.08f));
            Input.Update(new GameTime());
            UI.Update(new GameTime());
        }

        private void Draw2()
        {
            SetMousePosition(new Vector2(0.1f, 0.18f));
            Input.Update(new GameTime());
            UI.Update(new GameTime());
        }

        [Test]
        [Ignore("This test is unreliable, needs reworking")]
        public void RunMouseOversTest()
        {
            RequirePlatform(PlatformType.Windows);

            RunGameTest(new MouseOverTest());
        }

        /// <summary>
        /// Launch the Image test.
        /// </summary>
        public static void Main()
        {
            using (var game = new MouseOverTest())
                game.Run();
        }
    }
}
