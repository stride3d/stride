// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace CSharpIntermediate.Code
{
    public class UIByCode : StartupScript
    {
        private SpriteFont font;
        private Button button;
        private TextBlock textBlock;

        public override void Start()
        {
            font = Content.Load<SpriteFont>("UI/Ebrima");
            button = CreateButton("Show me the time!");
            textBlock = CreateTextBlock("...");

            //We get or create a UI component and create a page with various elements
            var uiComponent = Entity.GetOrCreate<UIComponent>();

            uiComponent.Page = new UIPage
            {
                RootElement = new StackPanel
                {
                    DefaultHeight = 200,
                    DefaultWidth = 600,
                    Margin = new Thickness(600.0f, 600, 0, 0),

                    BackgroundColor = new Color(0, 1, 0, 0.1f),
                    Children =
                    {
                        button,
                        textBlock
                    }
                }
            };
        }

        private Button CreateButton(string buttonText)
        {
            // We create a new button. The content of the button is a TextBlock
            var button = new Button
            {
                Name = "ButtonByCode",
                HorizontalAlignment = HorizontalAlignment.Center,
                BackgroundColor = Color.DarkKhaki,
                Content = new TextBlock {
                    Text = buttonText, 
                    Font = font,
                    TextSize = 16, 
                    TextColor = Color.Black,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            // We send up the click event of the button
            button.Click += (sender, args) =>
            {
                textBlock.Text = $"Date: {DateTime.Now.ToShortTimeString()}";
            };

            return button;
        }

        private TextBlock CreateTextBlock(string defaultText)
        {
            var textBlock = new TextBlock
            {
                Name = "TextBlockByCode",
                Text = defaultText,
                Font = font,
                TextColor = Color.Yellow,
                BackgroundColor = Color.OrangeRed,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            return textBlock;
        }
    }
}
