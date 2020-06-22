using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Events;
using Stride.UI.Panels;

namespace CSharpBeginner.Code
{
    public class TutorialUI : SyncScript
    {
        public Entity Camera;

        private Scene tutorialScene;
        private UIPage activePage;
        private Button btnTutorialMenu;
        private StackPanel tutorialButtonsStackPanel;
        private TextBlock tutorialTitleTxt;

        private float cameraLerpTimer = 0;
        private float cameraLerpTime = 2.0f;

        private TransformComponent startTransform;
        private TransformComponent targetTransform;

        public override void Start()
        {
            Game.Window.IsMouseVisible = true;

            activePage = Entity.Get<UIComponent>().Page;

            btnTutorialMenu = activePage.RootElement.FindVisualChildOfType<Button>("BtnTutorialMenu");
            btnTutorialMenu.Click += BtnTutorialMenuClicked;

            tutorialTitleTxt = activePage.RootElement.FindVisualChildOfType<TextBlock>("tutorialTitleTxt");
            tutorialTitleTxt.Text = "";

            tutorialButtonsStackPanel = activePage.RootElement.FindVisualChildOfType<StackPanel>("TutorialButtonsStackPanel");
            var placeHolderButton = tutorialButtonsStackPanel.Children[0] as Button;
            var placeHolderTextBlock = placeHolderButton.VisualChildren[0] as TextBlock;
            placeHolderButton.Visibility = Visibility.Hidden;

            //Tutorial button text - Tutorial scene name
            var tutorialScenes = new Dictionary<string, string>();
            tutorialScenes.Add("Getting the entity", "Getting the entity");
            tutorialScenes.Add("Child entities", "Child entities");
            tutorialScenes.Add("Transform position", "TransformPosition");
            tutorialScenes.Add("Properties", "Properties");
            tutorialScenes.Add("Getting a component", "Getting a component");
            tutorialScenes.Add("Adding a component", "Adding a component");
            tutorialScenes.Add("Delta time", "DeltaTime");
            tutorialScenes.Add("Cloning entities", "Cloning entities");
            tutorialScenes.Add("Removing entities", "Removing entities");
            tutorialScenes.Add("Keyboard input", "Keyboard input");
            tutorialScenes.Add("Mouse input", "Mouse input");
            tutorialScenes.Add("Virtual buttons", "Virtual buttons");
            tutorialScenes.Add("Linear Interpolation", "Linear Interpolation");
            tutorialScenes.Add("Loading content from code", "Loading content");
            tutorialScenes.Add("Instantiating prefabs", "Instantiating prefabs");

            foreach (var keyPair in tutorialScenes)
            {
                var button = new Button
                {

                    Content = new TextBlock
                    {
                        Font = placeHolderTextBlock.Font,
                        TextSize = placeHolderTextBlock.TextSize,
                        Height = placeHolderButton.Content.Height,
                        Text = keyPair.Key,
                        TextColor = Color.White,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    Height = placeHolderButton.Height,
                    NotPressedImage = placeHolderButton.NotPressedImage,
                    PressedImage = placeHolderButton.PressedImage,
                    MouseOverImage = placeHolderButton.MouseOverImage,

                };
                button.Click += (sender, e) => BtnLoadTutorial(sender, e, keyPair);

                tutorialButtonsStackPanel.Children.Add(button);
            }
            tutorialButtonsStackPanel.Children.Remove(placeHolderButton);
        }

        private void BtnTutorialMenuClicked(object sender, RoutedEventArgs e)
        {
            tutorialButtonsStackPanel.Visibility = tutorialButtonsStackPanel.IsVisible ? Visibility.Hidden : Visibility.Visible;
        }

        private void BtnLoadTutorial(object sender, RoutedEventArgs e, KeyValuePair<string, string> newTutorialScene)
        {
            startTransform = new TransformComponent();
            targetTransform = new TransformComponent();
            cameraLerpTimer = 0;

            if (tutorialScene != null)
            {
                SceneSystem.SceneInstance.RootScene.Children.Remove(tutorialScene);
            }
            startTransform.Position = Camera.Transform.Position;
            startTransform.Rotation = Camera.Transform.Rotation;

            tutorialTitleTxt.Text = newTutorialScene.Key;
            tutorialButtonsStackPanel.Visibility = Visibility.Hidden;
            tutorialScene = Content.Load<Scene>("Scenes/Basics/" + newTutorialScene.Value);
            tutorialScene.Parent = Entity.Scene;
            foreach (var entity in this.tutorialScene.Entities)
            {
                if (entity.Name == "Camera")
                {
                    targetTransform.Position = entity.Transform.Position;
                    targetTransform.Rotation = entity.Transform.Rotation;
                    break;
                }
            }
        }

        public override void Update()
        {
            if (startTransform != null && cameraLerpTimer < cameraLerpTime)
            {
                var deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
                cameraLerpTimer += deltaTime;
                var lerpTime = cameraLerpTimer / cameraLerpTime;
                Camera.Transform.Position = Vector3.Lerp(startTransform.Position, targetTransform.Position, lerpTime);
                Camera.Transform.Rotation = Quaternion.Lerp(startTransform.Rotation, targetTransform.Rotation, lerpTime);
            }
        }
    }
}
