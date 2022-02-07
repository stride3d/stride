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
        public SpriteFont Font;

        private Button button;
        private TextBlock textBlock;

        public override void Start()
        {
            button = CreateButton("Show me the time!");
            textBlock = CreateTextBlock("...");

            //We get or create a UI component and create a page with various elements
            var uiComponent = Entity.GetOrCreate<UIComponent>();
            uiComponent.Page = new UIPage
            {
                RootElement = new StackPanel
                {
                    DefaultHeight = 400,
                    DefaultWidth = 600,
                    Margin = new Thickness(600.0f, 0, 0, 0),
                    BackgroundColor = new Color(1, 0.6f, 0.6f, 0.5f),
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
            var button = new Button
            {
                Name = "ButtonByCode",
                HorizontalAlignment = HorizontalAlignment.Center,
                BackgroundColor = Color.DarkKhaki,
                Content = new TextBlock {
                    Text = buttonText, 
                    Font = Font,
                    TextSize = 16, 
                    TextColor = Color.BlanchedAlmond,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

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
                Font = Font,
                TextColor = Color.DarkViolet,
                BackgroundColor = Color.LightSkyBlue,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            return textBlock;
        }
    }
}
