using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Events;
using Stride.UI.Panels;

namespace CSharpIntermediate.Code
{
    public class TutorialUI : SyncScript
    {

        private Scene tutorialScene;
        private UIPage activePage;
        private Button btnTutorialMenu;
        private StackPanel buttonsStartUI;
        private StackPanel buttonsCompletedUI;
        private TextBlock tutorialTitleTxt;

        public override void Start()
        {
            Game.Window.IsMouseVisible = true;

            activePage = Entity.Get<UIComponent>().Page;

            btnTutorialMenu = activePage.RootElement.FindVisualChildOfType<Button>("BtnTutorialMenu");
            btnTutorialMenu.Click += BtnTutorialMenuClicked;

            tutorialTitleTxt = activePage.RootElement.FindVisualChildOfType<TextBlock>("tutorialTitleTxt");
            tutorialTitleTxt.Text = "";

            buttonsStartUI = activePage.RootElement.FindVisualChildOfType<StackPanel>("TutorialButtonsStartStackPanel");
            buttonsCompletedUI = activePage.RootElement.FindVisualChildOfType<StackPanel>("TutorialButtonsCompletedStackPanel");
            var placeHolderButton = buttonsStartUI.Children[0] as Button;
            var placeHolderTextBlock = placeHolderButton.VisualChildren[0] as TextBlock;
            placeHolderButton.Visibility = Visibility.Hidden;


            //Start tutorials
            var tutorialScenes = new Dictionary<string, string>();
            //tutorialScenes.Add("UI interaction", "01.UI basics/Start-UI basics");
            //tutorialScenes.Add("Scene loading", "02.SceneLoading/Start-SceneA");
            //tutorialScenes.Add("Collision trigger", "Scenes/03.CollisionTriggers/Start-CollisionTriggers");
            //tutorialScenes.Add("Project & Unproject", "Scenes/04.ProjectUnproject/Start-ProjectUnproject");
            //tutorialScenes.Add("Raycasting", "Scenes/05.Raycasting/Start-Raycasting");
            //tutorialScenes.Add("Async Scripts", "Scenes/06.Async/Start-AsyncScriptsTriggers");
            //tutorialScenes.Add("Audio", "Scenes/07.Audio/Start-Audio");
            //tutorialScenes.Add("First person camera", "Scenes/08.FirstPersonCamera/Start-FirstPersonCamera");
            //tutorialScenes.Add("Third person camera", "Scenes/09.ThirdPersonCamera/Start-ThirdPersonCamera");
            //tutorialScenes.Add("Animation basics", "Scenes/10.Animation basics/Start-Animations");
            //tutorialScenes.Add("Navigation", "Scenes/11.Navigation/Start-Navigation");
            //CreateButton(placeHolderButton, placeHolderTextBlock, tutorialScenes, buttonsStartUI);

            //Completed tutorials
            tutorialScenes = new Dictionary<string, string>();
            tutorialScenes.Add("UI interaction", "01.UI basics/Completed-UI basics");
            tutorialScenes.Add("Scene loading", "02.SceneLoading/Completed-SceneA");
            tutorialScenes.Add("Collision trigger", "Scenes/03.CollisionTriggers/Completed-CollisionTriggers");
            tutorialScenes.Add("Project & Unproject", "Scenes/04.ProjectUnproject/Completed-ProjectUnproject");
            tutorialScenes.Add("Raycasting", "Scenes/05.Raycasting/Completed-Raycasting");
            tutorialScenes.Add("Async Scripts", "Scenes/06.Async/Completed-AsyncScriptsTriggers");
            tutorialScenes.Add("Audio", "Scenes/07.Audio/Completed-Audio");
            tutorialScenes.Add("First person camera", "Scenes/08.FirstPersonCamera/Completed-FirstPersonCamera");
            tutorialScenes.Add("Third person camera", "Scenes/09.ThirdPersonCamera/Completed-ThirdPersonCamera");
            tutorialScenes.Add("Animation basics", "Scenes/10.Animation basics/Completed-Animations");
            tutorialScenes.Add("Navigation", "Scenes/11.Navigation/Completed-Navigation");

            CreateButton(placeHolderButton, placeHolderTextBlock, tutorialScenes, buttonsCompletedUI);

            buttonsCompletedUI.Children.Remove(placeHolderButton);
        }

        private void CreateButton(Button baseButtonButton, TextBlock textBlock, Dictionary<string, string> tutorialScenes, 
            StackPanel stackPanel)
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
            buttonsStartUI.Visibility = buttonsStartUI.IsVisible ? Visibility.Hidden : Visibility.Visible;
            buttonsCompletedUI.Visibility = buttonsCompletedUI.IsVisible ? Visibility.Hidden : Visibility.Visible;
        }

        private void BtnLoadTutorial(object sender, RoutedEventArgs e, KeyValuePair<string, string> newTutorialScene)
        {
            if (tutorialScene != null)
            {
                SceneSystem.SceneInstance.RootScene.Children.Remove(tutorialScene);
            }
           
            tutorialTitleTxt.Text = newTutorialScene.Key;
            buttonsStartUI.Visibility = Visibility.Hidden;
            buttonsCompletedUI.Visibility = Visibility.Hidden;
            tutorialScene = Content.Load<Scene>("Scenes/" + newTutorialScene.Value);
            tutorialScene.Parent = Entity.Scene;
 
        }

        public override void Update()
        {
        }
    }
}
