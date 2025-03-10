// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.UIEditor.Game;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Stride.Assets.Presentation.AssetEditors.UIEditor.Adorners
{
    internal enum MarginEdge
    {
        Left,
        Top,
        Right,
        Bottom
    }

    internal sealed class MarginAdorner : AdornerBase<Canvas>
    {
        private readonly Border border;
        private readonly TextBlock textBlock;
        private float thickness;

        public MarginAdorner(UIEditorGameAdornerService service, UIElement gameSideElement, MarginEdge marginEdge, Graphics.SpriteFont font)
            : base(service, gameSideElement)
        {
            Visual = new Canvas
            {
                CanBeHitByUser = false,
                Name = $"[Margin] {marginEdge}",
            };
            border = new Border();
            textBlock = new TextBlock
            {
                BackgroundColor = Color.WhiteSmoke*0.5f,
                Font = font,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Visual.Children.Add(border);
            Visual.Children.Add(textBlock);

            MarginEdge = marginEdge;

            InitializeAttachedProperties();
        }

        public MarginEdge MarginEdge { get; }

        public override Canvas Visual { get; }

        public override void Disable()
        {
            // do nothing (margin adorners are not hitable)
        }

        public override void Enable()
        {
            // do nothing (margin adorners are not hitable)
        }

        public override void Update(Vector2 position)
        {
            UpdateFromSettings();

            var margin = GameSideElement.Margin;
            var offset = (Vector2)GameSideElement.RenderSize * 0.5f;

            Vector2 pinOrigin;
            Size2F size;
            Vector2 textRelativePosition;
            float value;
            switch (MarginEdge)
            {
                case MarginEdge.Left:
                    size = new Size2F(Math.Abs(margin.Left), thickness);
                    value = margin.Left;
                    pinOrigin = new Vector2(margin.Left >= 0 ? 1.0f : 0.0f, 0.5f);
                    position += new Vector2(-offset.X, 0.0f);
                    textRelativePosition = new Vector2(margin.Left < 0 ? 1.0f : 0.0f, 0.5f);
                    break;

                case MarginEdge.Right:
                    size = new Size2F(Math.Abs(margin.Right), thickness);
                    value = margin.Right;
                    pinOrigin = new Vector2(margin.Right <= 0 ? 1.0f : 0.0f, 0.0f);
                    position += new Vector2(offset.X, 0.0f);
                    textRelativePosition = new Vector2(margin.Right > 0 ? 1.0f : 0.0f, 0.5f);
                    break;

                case MarginEdge.Top:
                    size = new Size2F(thickness, Math.Abs(margin.Top));
                    value = margin.Top;
                    pinOrigin = new Vector2(0.5f, margin.Top >= 0 ? 1.0f : 0.0f);
                    position += new Vector2(0.0f, -offset.Y);
                    textRelativePosition = new Vector2(0.5f, margin.Top < 0 ? 1.0f : 0.0f);
                    break;

                case MarginEdge.Bottom:
                    size = new Size2F(thickness, Math.Abs(margin.Bottom));
                    value = margin.Bottom;
                    pinOrigin = new Vector2(0.5f, margin.Bottom <= 0 ? 1.0f : 0.0f);
                    position += new Vector2(0.0f, offset.Y);
                    textRelativePosition = new Vector2(0.5f, margin.Bottom > 0 ? 1.0f : 0.0f);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }

            border.Size = size;

            textBlock.Text = value.ToString("0.###", CultureInfo.CurrentUICulture);
            textBlock.Visibility = Math.Abs(value) > 1.0f ? Visibility.Visible : Visibility.Hidden;
            textBlock.SetCanvasPinOrigin(pinOrigin);
            textBlock.SetCanvasRelativePosition(textRelativePosition);

            Visual.Size = size;
            Visual.SetCanvasAbsolutePosition(position);
            Visual.SetCanvasPinOrigin(pinOrigin);
        }

        private void UpdateFromSettings()
        {
            var editor = Service.Controller.Editor;

            thickness = editor.GuidelineThickness;
            border.BackgroundColor = editor.GuidelineColor;
            textBlock.TextColor = editor.GuidelineColor;
        }
    }
}
