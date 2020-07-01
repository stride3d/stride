// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Graphics;
using Stride.Rendering.Sprites;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace JumpyJet
{
    /// <summary>
    /// UIScript manages UIElements using in the game.
    /// At one time UI.RootElement is one of each root which corresponding to each state of the game.
    /// 
    /// It provides a ButtonClickedEvent Action that could be subscribed by its user.
    /// This action provides the name of Button element that is clicked,
    ///  which is one of {startButton, MenuBotton and RestartButton}
    /// </summary>
    public class UIScript : SyncScript
    {
        private EventReceiver gameOverListener = new EventReceiver(GameGlobals.GameOverEventKey);
        private EventReceiver pipePassedListener = new EventReceiver(GameGlobals.PipePassedEventKey);

        public SpriteFont Font;
        public SpriteSheet UIImages;

        private ModalElement mainMenuRoot;
        private Canvas gameRoot;
        private ModalElement gameOverRoot;

        private TextBlock scoreTextBlock;
        private ISpriteProvider buttonImage;

        private int currentScore = 0;

        /// <summary>
        /// Load resource and construct ui components
        /// </summary>
        public override void Start()
        {
            // Load resources shared by different UI screens
            buttonImage = SpriteFromSheet.Create(UIImages, "button");

            // Load and create specific UI screens.
            CreateMainMenuUI();
            CreateGameUI();
            CreateGameOverUI();

            // set the default screen to main screen
            StartMainMenuMode();
        }

        public override void Update()
        {
            // Increase the score if a new pipe has been passed
            if (pipePassedListener.TryReceive())
                ++currentScore;

            // move to game over UI
            if (gameOverListener.TryReceive())
            {
                currentScore = 0;
                Entity.Get<UIComponent>().Page = new UIPage { RootElement = gameOverRoot };
            }

            // Update the current score
            scoreTextBlock.Text = "Score : {0,2}".ToFormat(currentScore);
        }

        /// <summary>
        /// Change UI mode to main menu
        /// </summary>
        public void StartMainMenuMode()
        {
            Entity.Get<UIComponent>().Page = new UIPage { RootElement = mainMenuRoot };
        }
        /// <summary>
        /// Change UI mode to game mode
        /// </summary>
        public void StartGameMode()
        {
            Entity.Get<UIComponent>().Page = new UIPage { RootElement = gameRoot };
        }

        private void CreateMainMenuUI()
        {
            var strideLogo = new ImageElement { Source = SpriteFromSheet.Create(UIImages, "sd_logo") };

            strideLogo.SetCanvasPinOrigin(new Vector3(0.5f, 0.5f, 1f));
            strideLogo.SetCanvasRelativeSize(new Vector3(0.75f, 0.5f, 1f));
            strideLogo.SetCanvasRelativePosition(new Vector3(0.5f, 0.3f, 1f));

            var startButton = new Button
            {
                Content = new TextBlock
                {
                    Font = Font,
                    Text = "Touch to Start",
                    TextColor = Color.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                NotPressedImage = buttonImage,
                PressedImage = buttonImage,
                MouseOverImage = buttonImage,
                Padding = new Thickness(77, 30, 25, 30),
                MinimumWidth = 250f,
            };

            startButton.SetCanvasPinOrigin(new Vector3(0.5f, 0.5f, 1f));
            startButton.SetCanvasRelativePosition(new Vector3(0.5f, 0.7f, 0f));
            startButton.Click += (sender, args) =>
            {
                GameGlobals.GameStartedEventKey.Broadcast();
                StartGameMode();
            };

            var mainMenuCanvas = new Canvas();
            mainMenuCanvas.Children.Add(strideLogo);
            mainMenuCanvas.Children.Add(startButton);

            mainMenuRoot = new ModalElement
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Content = mainMenuCanvas
            };
        }

        private void CreateGameUI()
        {
            scoreTextBlock = new TextBlock
            {
                Font = Font,
                TextColor = Color.Black,
                VerticalAlignment = VerticalAlignment.Center
            };
            scoreTextBlock.SetCanvasPinOrigin(new Vector3(0.5f, 0.5f, 1f));
            scoreTextBlock.SetCanvasRelativePosition(new Vector3(0.2f, 0.05f, 0f));

            var scoreBoard = new ContentDecorator
            {
                BackgroundImage = SpriteFromSheet.Create(UIImages, "score_bg"),
                Content = scoreTextBlock,
                Padding = new Thickness(60, 31, 25, 35),
                MinimumWidth = 190f // Set the minimum width of score button so that it wont modify when the content (text) changes, and less than minimum.
            };

            gameRoot = new Canvas();
            gameRoot.Children.Add(scoreBoard);
        }

        private void CreateGameOverUI()
        {
            var menuButton = new Button
            {
                Content = new TextBlock
                {
                    Font = Font,
                    Text = "Menu",
                    TextColor = Color.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                PressedImage = buttonImage,
                NotPressedImage = buttonImage,
                MouseOverImage = buttonImage,
                Padding = new Thickness(77, 30, 25, 30),
                MinimumWidth = 190f,
            };

            menuButton.SetCanvasPinOrigin(new Vector3(0.5f, 0.5f, 1f));
            menuButton.SetCanvasRelativePosition(new Vector3(0.70f, 0.7f, 0f));
            menuButton.Click += (sender, args) =>
            {
                GameGlobals.GameResetEventKey.Broadcast();
                StartMainMenuMode();
            };

            var retryButton = new Button
            {
                Content = new TextBlock
                {
                    Font = Font,
                    Text = "Retry",
                    TextColor = Color.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                Padding = new Thickness(74, 30, 25, 30),
                MinimumWidth = 190f,
                PressedImage = buttonImage,
                MouseOverImage = buttonImage,
                NotPressedImage = buttonImage
            };

            retryButton.SetCanvasPinOrigin(new Vector3(0.5f, 0.5f, 1f));
            retryButton.SetCanvasRelativePosition(new Vector3(0.3f, 0.7f, 0f));
            retryButton.Click += (sender, args) =>
            {
                GameGlobals.GameResetEventKey.Broadcast();
                GameGlobals.GameStartedEventKey.Broadcast();
                StartGameMode();
            };

            var gameOverCanvas = new Canvas();
            gameOverCanvas.Children.Add(menuButton);
            gameOverCanvas.Children.Add(retryButton);

            gameOverRoot = new ModalElement
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Content = gameOverCanvas
            };
        }
    }
}
