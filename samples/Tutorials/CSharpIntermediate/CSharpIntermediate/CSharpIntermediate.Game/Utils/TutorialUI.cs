using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Events;
using Stride.UI.Panels;

namespace CSharpIntermediate.Code
{
    public class TutorialUI : StartupScript
    {
        public bool ShowMenuOnSceneLoad = false;
        private UIPage activePage;
        private Button btnTutorialMenu;
        private Canvas tutorialMenu;
        private StackPanel buttonsStartUI;
        private StackPanel buttonsCompletedUI;

        public override void Start()
        {
            activePage = Entity.Get<UIComponent>().Page;
            tutorialMenu = activePage.RootElement.FindVisualChildOfType<Canvas>("TutorialMenu");
            buttonsStartUI = activePage.RootElement.FindVisualChildOfType<StackPanel>("ButtonsStart");
            buttonsCompletedUI = activePage.RootElement.FindVisualChildOfType<StackPanel>("ButtonsCompleted");

            // Create buttons for the first scene
            if (buttonsStartUI.Children.Count == 1)
            {
                Game.Window.IsMouseVisible = true;

                btnTutorialMenu = activePage.RootElement.FindVisualChildOfType<Button>("BtnTutorialMenu");
                btnTutorialMenu.Click += BtnTutorialMenuClicked;

                CreateTutorialButtons();
            }
            else
            {
                tutorialMenu.Visibility = Visibility.Hidden;
            }

            if (!ShowMenuOnSceneLoad)
            {
                buttonsStartUI.Visibility = Visibility.Hidden;
                buttonsCompletedUI.Visibility = Visibility.Hidden;
            }
        }

        private void CreateTutorialButtons()
        {
            var startButton = buttonsStartUI.Children[0] as Button;
            var startText = startButton.VisualChildren[0] as TextBlock;
            var completedButton = buttonsCompletedUI.Children[0] as Button;
            var complextedText = completedButton.VisualChildren[0] as TextBlock;
            startButton.Visibility = Visibility.Hidden;
            completedButton.Visibility = Visibility.Hidden;

            // Start tutorials
            var tutorialScenes = new Dictionary<string, string>();
            tutorialScenes.Add("UI interaction", "01-UI-basics/Start-UI-basics");
            tutorialScenes.Add("Collision trigger", "02_CollisionTriggers/Start-CollisionTriggers");
            tutorialScenes.Add("Raycasting", "03_Raycasting/Start-Raycasting");
            tutorialScenes.Add("Project & Unproject", "04_ProjectUnproject/Start-ProjectUnproject");
            tutorialScenes.Add("Async Scripts", "05_Async/Start-AsyncScript");
            tutorialScenes.Add("Scene loading", "06_SceneLoading/Start-SceneA");
            tutorialScenes.Add("Animation basics", "07_Animation-basics/Start-Animations");
            tutorialScenes.Add("Audio", "08_Audio/Start-Audio");
            tutorialScenes.Add("First person camera", "09_FirstPersonCamera/Start-FirstPersonCamera");
            tutorialScenes.Add("Third person camera", "10_ThirdPersonCamera/Start-ThirdPersonCamera");
            tutorialScenes.Add("Navigation", "11_Navigation/Start-Navigation");
            CreateButton(startButton, startText, tutorialScenes, buttonsStartUI);

            //Completed tutorials
            tutorialScenes = new Dictionary<string, string>();
            tutorialScenes.Add("UI interaction", "01-UI-basics/Completed-UI-basics");
            tutorialScenes.Add("Collision trigger", "02_CollisionTriggers/Completed-CollisionTriggers");
            tutorialScenes.Add("Raycasting", "03_Raycasting/Completed-Raycasting");
            tutorialScenes.Add("Project & Unproject", "04_ProjectUnproject/Completed-ProjectUnproject");
            tutorialScenes.Add("Async Scripts", "05_Async/Completed-AsyncScript");
            tutorialScenes.Add("Scene loading", "06_SceneLoading/Completed-SceneA");
            tutorialScenes.Add("Animation basics", "07_Animation-basics/Completed-Animations");
            tutorialScenes.Add("Audio", "08_Audio/Completed-Audio");
            tutorialScenes.Add("First person camera", "09_FirstPersonCamera/Completed-FirstPersonCamera");
            tutorialScenes.Add("Third person camera", "10_ThirdPersonCamera/Completed-ThirdPersonCamera");
            tutorialScenes.Add("Navigation", "11_Navigation/Completed-Navigation");

            CreateButton(completedButton, complextedText, tutorialScenes, buttonsCompletedUI);

            buttonsStartUI.Children.Remove(startButton);
            buttonsCompletedUI.Children.Remove(completedButton);
        }

        private void CreateButton(Button baseButtonButton, TextBlock textBlock, Dictionary<string, string> tutorialScenes, StackPanel stackPanel)
        {
            foreach (var keyPair in tutorialScenes)
            {
                var button = new Button
                {

                    Content = new TextBlock
                    {
                        Font = textBlock.Font,
                        TextSize = textBlock.TextSize,
                        Height = baseButtonButton.Content.Height,
                        Text = keyPair.Key,
                        TextColor = Color.White,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    Height = baseButtonButton.Height,
                    NotPressedImage = baseButtonButton.NotPressedImage,
                    PressedImage = baseButtonButton.PressedImage,
                    MouseOverImage = baseButtonButton.MouseOverImage,

                };
                button.Click += (sender, e) => BtnLoadTutorial(sender, e, keyPair);

                stackPanel.Children.Add(button);
            }
        }

        private void BtnTutorialMenuClicked(object sender, RoutedEventArgs e)
        {
            tutorialMenu.Visibility = tutorialMenu.IsVisible ? Visibility.Hidden : Visibility.Visible;
            buttonsStartUI.Visibility = buttonsStartUI.IsVisible ? Visibility.Hidden : Visibility.Visible;
            buttonsCompletedUI.Visibility = buttonsCompletedUI.IsVisible ? Visibility.Hidden : Visibility.Visible;

            if (tutorialMenu.Visibility == Visibility.Visible)
            {
                Game.Window.IsMouseVisible = true;
            }
        }

        private void BtnLoadTutorial(object sender, RoutedEventArgs e, KeyValuePair<string, string> newTutorialScene)
        {
            if (SceneSystem.SceneInstance.RootScene.Children.Count > 0)
            {
                SceneSystem.SceneInstance.RootScene.Children.RemoveAt(0);
            }

            var tutorialScene = Content.Load<Scene>("Scenes/" + newTutorialScene.Value);
            SceneSystem.SceneInstance.RootScene = tutorialScene;
        }
    }
}
