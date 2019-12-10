using System.Collections.Generic;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.UI;
using Xenko.UI.Controls;
using Xenko.UI.Events;
using Xenko.UI.Panels;

namespace CSharpBeginner.Code
{
    public class TutorialUI : SyncScript
    {
        public Entity Camera;

        private Scene _tutorialScene;
        private UIPage _activePage;
        private Button _btnTutorialMenu;
        private StackPanel _tutorialButtonsStackPanel;
        private TextBlock _tutorialTitleTxt;

        private float _cameraLerpTimer = 0;
        private float _cameraLerpTime = 2.0f;

        private TransformComponent _startTransform;
        private TransformComponent _targetTransform;

        public override void Start()
        {
            Game.Window.IsMouseVisible = true;

            _activePage = Entity.Get<UIComponent>().Page;

            _btnTutorialMenu = _activePage.RootElement.FindVisualChildOfType<Button>("BtnTutorialMenu");
            _btnTutorialMenu.Click += BtnTutorialMenuClicked;

            _tutorialTitleTxt = _activePage.RootElement.FindVisualChildOfType<TextBlock>("tutorialTitleTxt");
            _tutorialTitleTxt.Text = "";

            _tutorialButtonsStackPanel = _activePage.RootElement.FindVisualChildOfType<StackPanel>("TutorialButtonsStackPanel");
            var placeHolderButton = _tutorialButtonsStackPanel.Children[0] as Button;
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
            tutorialScenes.Add("Cloning entities", "Cloning entities");
            tutorialScenes.Add("Removing entities", "Removing entities");
            tutorialScenes.Add("Delta time", "DeltaTime");
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
                
                _tutorialButtonsStackPanel.Children.Add(button);
            }
            _tutorialButtonsStackPanel.Children.Remove(placeHolderButton);
        }

        private void BtnTutorialMenuClicked(object sender, RoutedEventArgs e)
        {
            _tutorialButtonsStackPanel.Visibility = _tutorialButtonsStackPanel.IsVisible ? Visibility.Hidden : Visibility.Visible;
        }

        private void BtnLoadTutorial(object sender, RoutedEventArgs e, KeyValuePair<string, string> newTutorialScene)
        {
            _startTransform = new TransformComponent();
            _targetTransform = new TransformComponent();
            _cameraLerpTimer = 0;

            if (_tutorialScene != null)
            {
                SceneSystem.SceneInstance.RootScene.Children.Remove(_tutorialScene);
            }
            _startTransform.Position = Camera.Transform.Position;
            _startTransform.Rotation = Camera.Transform.Rotation;

            _tutorialTitleTxt.Text = newTutorialScene.Key;
            _tutorialButtonsStackPanel.Visibility = Visibility.Hidden;
            _tutorialScene = Content.Load<Scene>("Scenes/Basics/" + newTutorialScene.Value);
            _tutorialScene.Parent = Entity.Scene;  
            foreach (var entity in this._tutorialScene.Entities)
            {
                if (entity.Name == "Camera")
                {
                    _targetTransform.Position = entity.Transform.Position;
                    _targetTransform.Rotation = entity.Transform.Rotation;
                    break;
                }
            }
        }

        public override void Update()
        {
            if (_startTransform != null && _cameraLerpTimer < _cameraLerpTime)
            {
                var deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
                _cameraLerpTimer += deltaTime;
                var lerpTime = _cameraLerpTimer / _cameraLerpTime;
                Camera.Transform.Position = Vector3.Lerp(_startTransform.Position, _targetTransform.Position, lerpTime);
                Camera.Transform.Rotation = Quaternion.Lerp(_startTransform.Rotation, _targetTransform.Rotation, lerpTime);
            }
        }
    }
}
