// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Sprites;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace SpaceEscape
{
    /// <summary>
    /// UIScript manages UIElements using in the game.
    /// At one time UI.RootElement is one of each root which corresponding to each state of the game.
    /// 
    /// It provides a ButtonClickedEvent Action that could be subscribed by its user.
    /// This action provides the name of Button element that is clicked,
    ///  which is one of {StartButton, MenuBotton and RestartButton}
    /// </summary>
    public class UIScript : StartupScript
    {
        internal Button StartButton { get; private set; }
        internal Button MenuButton { get; private set; }
        internal Button RetryButton { get; private set; }

        public SpriteFont Font;
        public SpriteSheet UIImages;

        private ModalElement mainMenuRoot;
        private Canvas gameRoot;
        private ModalElement gameOverRoot;

        private TextBlock distanceTextBlock;
        private ISpriteProvider buttonImage;

        /// <summary>
        /// Load resource and construct ui components
        /// </summary>
        public override void Start()
        {
            base.Start();
            
            // Load resources shared by different UI screens
            buttonImage = SpriteFromSheet.Create(UIImages, "button");

            // Load and create specific UI screens.
            CreateMainMenuUI();
            CreateGameUI();
            CreateGameOverUI();
        }

        private void CreateMainMenuUI()
        {
            var strideLogo = new ImageElement { Source = SpriteFromSheet.Create(UIImages, "sd_logo") };

            strideLogo.SetCanvasPinOrigin(new Vector3(0.5f, 0.5f, 1f));
            strideLogo.SetCanvasRelativeSize(new Vector3(0.8f, 0.5f, 1f));
            strideLogo.SetCanvasRelativePosition(new Vector3(0.5f, 0.3f, 1f));

            StartButton = new Button
            {
                Content = new TextBlock { Font = Font, Text = "Touch to Start", TextColor = Color.Black, 
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center},
                NotPressedImage = buttonImage,
                PressedImage = buttonImage,
                MouseOverImage = buttonImage,
                Padding = new Thickness(80, 27, 25, 35),
                MinimumWidth = 250f,
            };

            StartButton.SetCanvasPinOrigin(new Vector3(0.5f, 0.5f, 1f));
            StartButton.SetCanvasRelativePosition(new Vector3(0.5f, 0.8f, 0f));

            var mainMenuCanvas = new Canvas();
            mainMenuCanvas.Children.Add(strideLogo);
            mainMenuCanvas.Children.Add(StartButton);

            mainMenuRoot = new ModalElement
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Content = mainMenuCanvas
            };
        }

        private void CreateGameUI()
        {
            distanceTextBlock = new TextBlock { Font = Font, TextColor = Color.Gold, VerticalAlignment = VerticalAlignment.Center };
            distanceTextBlock.SetCanvasPinOrigin(new Vector3(0.5f, 0.5f, 1f));
            distanceTextBlock.SetCanvasRelativePosition(new Vector3(0.2f, 0.05f, 0f));

            var scoreBoard = new ContentDecorator
            {
                BackgroundImage = SpriteFromSheet.Create(UIImages, "distance_bg"),
                Content = distanceTextBlock,
                Padding = new Thickness(60, 31, 25, 35),
                MinimumWidth = 290f // Set the minimum width of score button so that it wont modify when the content (text) changes, and less than minimum.
            };

            gameRoot = new Canvas();
            gameRoot.Children.Add(scoreBoard);
        }

        private void CreateGameOverUI()
        {
            MenuButton = new Button
            {
                Content = new TextBlock { Font = Font, Text = "Menu", TextColor = Color.Black, 
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center},
                PressedImage = buttonImage,
                NotPressedImage = buttonImage,
                MouseOverImage = buttonImage,
                Padding = new Thickness(77, 29, 25, 35),
                MinimumWidth = 190f,
            };

            MenuButton.SetCanvasPinOrigin(new Vector3(0.5f, 0.5f, 1f));
            MenuButton.SetCanvasRelativePosition(new Vector3(0.70f, 0.7f, 0f));

            RetryButton = new Button
            {
                Content = new TextBlock { Font = Font, Text = "Retry", TextColor = Color.Black, 
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center},
                Padding = new Thickness(74, 29, 25, 35),
                MinimumWidth = 190f,
                PressedImage = buttonImage,
                MouseOverImage = buttonImage,
                NotPressedImage = buttonImage
            };

            RetryButton.SetCanvasPinOrigin(new Vector3(0.5f, 0.5f, 1f));
            RetryButton.SetCanvasRelativePosition(new Vector3(0.3f, 0.7f, 0f));

            var gameOverCanvas = new Canvas();
            gameOverCanvas.Children.Add(MenuButton);
            gameOverCanvas.Children.Add(RetryButton);

            gameOverRoot = new ModalElement
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Content = gameOverCanvas,
                MinimumWidth = 200f,
            };
        }

        /// <summary>
        /// Change UI mode to main menu
        /// </summary>
        public void StartMainMenuMode()
        {
            Entity.Get<UIComponent>().Page = new UIPage { RootElement = mainMenuRoot };
        }

        /// <summary>
        /// Change UI mode to game
        /// </summary>
        public void StartPlayMode()
        {
            Entity.Get<UIComponent>().Page = new UIPage { RootElement = gameRoot };
        }

        /// <summary>
        /// Change ui mode to game over
        /// </summary>
        public void StartGameOverMode()
        {
            Entity.Get<UIComponent>().Page = new UIPage { RootElement = gameOverRoot };
        }

        /// <summary>
        /// A function to update UI distance element.
        /// </summary>
        /// <param name="distance"></param>
        public void SetDistance(int distance)
        {
            distanceTextBlock.Text = "Distance : {0,6}".ToFormat(distance);
        }
    }
}
