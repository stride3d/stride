// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.UI;
using Xenko.UI.Controls;
using Xenko.UI.Panels;

namespace PhysicsSample
{
    public class NextSceneScript : SyncScript
    {
        private Scene targetScene;

        public Scene NextScene;

        public Scene PreviousScene;

        public SpriteFont Font;

        public override void Start()
        {
            SetupUI();
        }

        public override void Update()
        {
            if (targetScene != null)
            {
                SceneSystem.SceneInstance.RootScene = targetScene;
                targetScene = null;
            }
        }

        private void SetupUI()
        {
            var uiComponent = Entity.Get<UIComponent>();
            if (uiComponent == null)
                return;

            // Create the UI
            Entity.Get<UIComponent>().Page = new UIPage
            {
                RootElement = new Grid
                {
                    Children =
                    {
                        CreateButton("<<", 72, 0, PreviousScene),
                        CreateButton(">>", 72, 2, NextScene)
                    },

                    ColumnDefinitions =
                    {
                        new StripDefinition(StripType.Auto, 10),
                        new StripDefinition(StripType.Star, 80),
                        new StripDefinition(StripType.Auto, 10),

                    }
                }
            };
        }

        private Button CreateButton(string text, int textSize, int columnId, Scene newTargetScene)
        {
            var button = new Button
            {
                Name = text,
                HorizontalAlignment = HorizontalAlignment.Center,
                Content = new TextBlock { Text = text, Font = Font, TextSize = textSize, TextColor = new Color(200, 200, 200, 255), VerticalAlignment = VerticalAlignment.Center },
                BackgroundColor = new Color(new Vector4(0.2f, 0.2f, 0.2f, 0.2f)),
            };

            button.SetGridColumn(columnId);

            button.Click += (sender, args) =>
            {
                targetScene = newTargetScene;
            };

            return button;
        }
    }
}
