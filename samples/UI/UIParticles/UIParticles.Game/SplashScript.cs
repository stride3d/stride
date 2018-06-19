// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Engine.Design;
using Xenko.Graphics;
using Xenko.Particles.Components;
using Xenko.Rendering.Sprites;
using Xenko.UI;
using Xenko.UI.Controls;
using Xenko.UI.Panels;

namespace UIParticles
{
    public enum GameState
    {
        None,
        NewGame,
        EndGame        
    }

    public class SplashScript : UISceneBase
    {
        // Life gauge
        private RectangleF gaugeBarRegion;
        private Grid lifeBarGrid;
        private Sprite lifeBarGaugeImage;

        public SpriteFont WesternFont;
        public SpriteSheet SplashScreenImages;

        public SpriteSheet ButtonsImages;

        [Display("Hit Effect Prefab")]
        public Prefab Prefab;

        private const int virtualWidth = 600;
        private const int virtualHeight = 600;

        private GameState currentState = GameState.NewGame;
        private GameState desiredState = GameState.NewGame;
        private float fusePercentage = 1f;

        protected override void LoadScene()
        {
            // Allow user to resize the window with the mouse.
            Game.Window.AllowUserResizing = true;

            // Create and initialize "Xenko Samples" Text
            var xenkoSampleTextBlock = new ContentDecorator
            {
                BackgroundImage = SpriteFromSheet.Create(SplashScreenImages, "xenko_sample_text_bg"),
                Content = new TextBlock
                {
                    Font = WesternFont,
                    TextSize = 60,
                    Text = "Xenko UI Particles",
                    TextColor = Color.White,
                },
                Padding = new Thickness(35, 15, 35, 25),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            xenkoSampleTextBlock.SetPanelZIndex(1);
            

            //*********************************
            // Confetti button
            var buttonImage = SpriteFromSheet.Create(SplashScreenImages, "button_long");

            var xenkoButtonConfetti = new Button
            {
                NotPressedImage = buttonImage,
                PressedImage = buttonImage,
                MouseOverImage = buttonImage,

                Content = new TextBlock
                {
                    Font = WesternFont,
                    TextColor = Color.White,
                    Text = "Click here to start the game over",
                    TextSize = 24
                },

                HorizontalAlignment = HorizontalAlignment.Right,
                Padding = new Thickness(90, 22, 25, 35),
//                BackgroundColor = Color.DarkOrchid
            };

            xenkoButtonConfetti.SetPanelZIndex(1);
            xenkoButtonConfetti.SetGridRow(1);

            xenkoButtonConfetti.Click += delegate
            {
                fusePercentage = 1f;
                desiredState = GameState.NewGame;
                var effectOffset = new Vector3(45 - xenkoButtonConfetti.RenderSize.X / 2, -5, 0);
                SpawnParticles(xenkoButtonConfetti.WorldMatrix.TranslationVector + effectOffset, Prefab, 2f);
            };
            //*********************************

            //*********************************
            // Stars button
            var buttonStars = SpriteFromSheet.Create(SplashScreenImages, "button_short");

            var xenkoButtonStars = new Button
            {
                NotPressedImage = buttonStars,
                PressedImage = buttonStars,
                MouseOverImage = buttonStars,

                Content = new TextBlock
                {
                    Font = WesternFont,
                    TextColor = Color.White,
                    Text = "Congratulations",
                    TextSize = 24
                },

                HorizontalAlignment = HorizontalAlignment.Right,
                Padding = new Thickness(90, 22, 25, 35),
//                BackgroundColor = Color.DarkOrchid

            };

            xenkoButtonStars.SetPanelZIndex(1);
            xenkoButtonStars.SetGridRow(4);

            xenkoButtonStars.Click += delegate
            {
                desiredState = GameState.EndGame;
                var effectOffset = new Vector3(45 - xenkoButtonStars.RenderSize.X / 2, -5, 0);
                SpawnParticles(xenkoButtonStars.WorldMatrix.TranslationVector + effectOffset, Prefab, 2f);
            };
            //*********************************

            var bottomBar = CreateBottomBar();
            bottomBar.SetPanelZIndex(1);
            bottomBar.SetGridRow(6);

            var grid = new Grid
            {
                MaximumWidth = virtualWidth,
                MaximumHeight = virtualHeight,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto)); // 0
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto)); // 1
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto)); // 2
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto)); // 3
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto)); // 4
            grid.RowDefinitions.Add(new StripDefinition(StripType.Fixed, 100)); // 5
            grid.RowDefinitions.Add(new StripDefinition(StripType.Fixed, 50)); // 5
            grid.ColumnDefinitions.Add(new StripDefinition());
            grid.LayerDefinitions.Add(new StripDefinition());

            grid.Children.Add(xenkoSampleTextBlock);
            grid.Children.Add(xenkoButtonConfetti);
            grid.Children.Add(xenkoButtonStars);
            grid.Children.Add(bottomBar);

            // Add the background
            var background = new ImageElement { Source = SpriteFromSheet.Create(SplashScreenImages, "background_uiimage"), StretchType = StretchType.Fill };
            background.SetPanelZIndex(-1);

            Entity.Get<UIComponent>().Page = new UIPage { RootElement = new UniformGrid { Children = { background, grid } } };
        }

        protected Vector3 ToOrthographicCamera(Vector3 worldPosition)
        {
            // Use a screen resolution of the same size as the one we set in the game settings
            // Y axis is reveres, because in UI Y is down, but in our scene Y is up
            var screenResolution = new Vector3(virtualWidth, -virtualHeight, 1);
            return worldPosition / screenResolution;
        }

        protected void SpawnParticles(Vector3 uiPosition, Prefab hitEffectPrefab, float time)
        {
            if (hitEffectPrefab == null)
                return;

            Func<Task> spawnTask = async () =>
            {
                var spawnedEntities = new List<Entity>();

                // Add
                foreach (var prefabEntity in hitEffectPrefab.Entities)
                {
                    var clonedEntity = EntityCloner.Clone(prefabEntity);

                    var component = clonedEntity.Get<ParticleSystemComponent>();
                    if (component != null)
                        component.ParticleSystem.ResetSimulation();

                    clonedEntity.Transform.Position = ToOrthographicCamera(uiPosition);

                    SceneSystem.SceneInstance.RootScene.Entities.Add(clonedEntity);

                    spawnedEntities.Add(clonedEntity);
                }

                // Countdown
                var secondsCountdown = time;
                while (secondsCountdown > 0f)
                {
                    await Script.NextFrame();
                    secondsCountdown -= (float)Game.UpdateTime.Elapsed.TotalSeconds;
                }

                // Remove
                foreach (var clonedEntity in spawnedEntities)
                {
                    SceneSystem.SceneInstance.RootScene.Entities.Remove(clonedEntity);

                    var component = clonedEntity.Get<ParticleSystemComponent>();
                    if (component != null)
                        component.ParticleSystem.Dispose();
                    clonedEntity.Dispose();
                }

                spawnedEntities.Clear();
            };

            Script.AddTask(spawnTask);
        }


        protected override void UpdateScene()
        {
            // Update camera
            //  The orthographic camera is centered at the center of the virtual grid and assumes positions in the 
            //  range (-0.5, 0.5) - (0.5, -0.5) to map to the virtual grid (0, 0) - (virtualWidth, virtualHeight)
            var cameraEntity = SceneSystem.SceneInstance.RootScene.Entities.FirstOrDefault(item => item.Name.Equals("UIParticlesCamera"));
            if (cameraEntity == null)
                return;

            var windowWidth = GraphicsDevice.Presenter.BackBuffer.Width;
            var windowHeight = GraphicsDevice.Presenter.BackBuffer.Height;

            cameraEntity.Get<CameraComponent>().AspectRatio = ((float) windowWidth / (float) windowHeight);
            cameraEntity.Get<CameraComponent>().OrthographicSize = ((float)windowHeight / virtualHeight);

            switch (currentState)
            {
                case GameState.NewGame: NewGameState();
                    break;

                case GameState.EndGame: EndGameState();
                    break;
            }

            desiredState = GameState.None;
        }

        private void EnterNewGame()
        {
            currentState = GameState.NewGame;
            fusePercentage = 1f;

            var particleFire = SceneSystem.SceneInstance.RootScene.Entities.FirstOrDefault(item => item.Name.Equals("Fire"));
            if (particleFire != null)
            {
                particleFire.Transform.Position = new Vector3(0, 0.65f, 0);
                particleFire.Get<ParticleSystemComponent>().ParticleSystem.Play();
            }

            var particleConfetti = SceneSystem.SceneInstance.RootScene.Entities.FirstOrDefault(item => item.Name.Equals("ConfettiBig"));
            if (particleConfetti != null)
            {
                particleConfetti.Transform.Position = new Vector3(10, 0, 0);
                particleConfetti.Get<ParticleSystemComponent>().ParticleSystem.Stop();
            }

            DrawFuse();
        }

        private void DrawFuse()
        {
            fusePercentage = Math.Min(1f, fusePercentage - (float)Game.UpdateTime.Elapsed.TotalSeconds * 0.04f);

            var gaugeCurrentRegion = lifeBarGaugeImage.Region;
            gaugeCurrentRegion.Width = Math.Max(1, fusePercentage * gaugeBarRegion.Width);
            lifeBarGaugeImage.Region = gaugeCurrentRegion;

            lifeBarGrid.ColumnDefinitions[1].SizeValue = gaugeCurrentRegion.Width / gaugeBarRegion.Width;
            lifeBarGrid.ColumnDefinitions[2].SizeValue = 1 - lifeBarGrid.ColumnDefinitions[1].SizeValue;

            var particleFire = SceneSystem.SceneInstance.RootScene.Entities.FirstOrDefault(item => item.Name.Equals("Fire"));
            if (particleFire == null)
                return;

            var firePos = lifeBarGrid.WorldMatrix.TranslationVector - new Vector3(lifeBarGrid.ActualWidth/2 - lifeBarGrid.ColumnDefinitions[0].ActualSize - lifeBarGrid.ColumnDefinitions[1].ActualSize + 5, 10, 0);
            particleFire.Transform.Position = ToOrthographicCamera(firePos);
        }

        private void EnterEndGame()
        {
            currentState = GameState.EndGame;
            fusePercentage = 0;
            DrawFuse();

            var particleFire = SceneSystem.SceneInstance.RootScene.Entities.FirstOrDefault(item => item.Name.Equals("Fire"));
            if (particleFire != null)
            {
                particleFire.Transform.Position = new Vector3(10, 0, 0);
                particleFire.Get<ParticleSystemComponent>().ParticleSystem.Stop();
            }

            var particleConfetti = SceneSystem.SceneInstance.RootScene.Entities.FirstOrDefault(item => item.Name.Equals("ConfettiBig"));
            if (particleConfetti != null)
            {
                particleConfetti.Transform.Position = new Vector3(0, 0.5f, 0);
                particleConfetti.Get<ParticleSystemComponent>().ParticleSystem.Play();
            }
        }

        private void NewGameState()
        {
            fusePercentage = Math.Min(1f, fusePercentage - (float)Game.UpdateTime.Elapsed.TotalSeconds * 0.03f);
            DrawFuse();

            if (desiredState == GameState.EndGame || fusePercentage <= 0f)
                EnterEndGame();
        }

        private void EndGameState()
        {
            if (desiredState == GameState.NewGame)
                EnterNewGame();
        }

        private UIElement CreateBottomBar()
        {
            // Create Life bar
            lifeBarGaugeImage = ButtonsImages["rope_small"];
            gaugeBarRegion = lifeBarGaugeImage.Region;

            var lifebarGauge = new ImageElement
            {
                Name = "LifeBarBackground",
                Source = SpriteFromSheet.Create(ButtonsImages, "rope_small"),
                StretchType = StretchType.Fill,
            };
            lifebarGauge.SetGridColumn(1);

            lifeBarGrid = new Grid();
            lifeBarGrid.Children.Add(lifebarGauge);
            lifeBarGrid.ColumnDefinitions.Add(new StripDefinition(StripType.Fixed, 8));
            lifeBarGrid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 0));
            lifeBarGrid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 100));
            lifeBarGrid.ColumnDefinitions.Add(new StripDefinition(StripType.Fixed, 8));
            lifeBarGrid.RowDefinitions.Add(new StripDefinition());
            lifeBarGrid.LayerDefinitions.Add(new StripDefinition());
            lifeBarGrid.SetCanvasRelativePosition(new Vector3(0f, 0.185f, 0f));
            lifeBarGrid.SetCanvasRelativeSize(new Vector3(1f, 1f, 1f));
            lifeBarGrid.SetPanelZIndex(-1);


            // the main grid of the top bar
            var mainLayer = new Canvas
            {
                VerticalAlignment = VerticalAlignment.Top,
                MaximumHeight = 10
            };

            mainLayer.Children.Add(lifeBarGrid);

            return mainLayer;
        }

    }
}
