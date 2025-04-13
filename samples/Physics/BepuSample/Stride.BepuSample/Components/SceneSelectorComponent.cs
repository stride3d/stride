// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Input;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Stride.BepuSample.Components
{
    [ComponentCategory("BepuDemo")]
    public class SceneSelectorComponent : SyncScript
    {
        public Scene? MainScene { get; set; }

        public List<UrlReference<Scene>> SceneList { get; set; } = [];
        public UILibrary Library { get; set; }
        public UIComponent SelectorUI { get; set; }

        private Scene? _last { get; set; } = null;

        private StackPanel _sceneStackPanel;

        public override void Start()
        {
            _sceneStackPanel = SelectorUI.Page.RootElement.FindVisualChildOfType<StackPanel>("SceneStackPanel");

            Game.Window.AllowUserResizing = true;
            Game.Window.Title = "Stride and Bepu Physics V2";

            MainScene ??= Entity.Scene;

            InitializeUI();
        }
        public override void Update()
        {
            if(Input.IsKeyPressed(Keys.Tab))
            {
                SelectorUI.Enabled = !SelectorUI.Enabled;
            }
        }

        private void SetScene(UrlReference<Scene> sceneRef)
        {
            if (MainScene == null)
                return;

            if (_last != null)
            {
                MainScene.Children.Clear();
                Content.Unload(_last);
                _last.Dispose();
            }

            if (sceneRef != null)
            {
                _last = Content.Load(sceneRef);
                MainScene.Children.Add(_last);
            }
        }

        private void InitializeUI()
        {
            if (SelectorUI == null)
                return;

            foreach (var scene in SceneList)
            {
                CreateButton(scene);
            }
        }

        private void CreateButton(UrlReference<Scene> scene)
        {
            var button = Library.InstantiateElement<Button>("TextButton");
            //button.Content = item.Name;
            button.FindVisualChildOfType<TextBlock>().Text = $"{scene.Url}";

            button.Click += (s, e) =>
            {
                SetScene(scene);
            };

            _sceneStackPanel.Children.Add(button);
        }
    }
}
