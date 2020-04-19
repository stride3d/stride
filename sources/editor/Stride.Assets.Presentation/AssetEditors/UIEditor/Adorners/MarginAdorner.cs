// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
        Back,
        Right,
        Bottom,
        Front
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
                DepthAlignment = DepthAlignment.Center,
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

        public override void Update(Vector3 position)
        {
            UpdateFromSettings();

            var margin = GameSideElement.Margin;
            var offset = GameSideElement.RenderSize*0.5f;

            Vector3 pinOrigin;
            Vector3 size;
            Vector3 textRelativePosition;
            float value;
            switch (MarginEdge)
            {
                case MarginEdge.Left:
                    size = new Vector3(Math.Abs(margin.Left), thickness, thickness);
                    value = margin.Left;
                    pinOrigin = new Vector3(margin.Left >= 0 ? 1.0f : 0.0f, 0.5f, 0.5f);
                    position += new Vector3(-offset.X, 0.0f, 0.0f);
                    textRelativePosition = new Vector3(margin.Left < 0 ? 1.0f : 0.0f, 0.5f, 0.5f);
                    break;

                case MarginEdge.Right:
                    size = new Vector3(Math.Abs(margin.Right), thickness, thickness);
                    value = margin.Right;
                    pinOrigin = new Vector3(margin.Right <= 0 ? 1.0f : 0.0f, 0.0f, 0.5f);
                    position += new Vector3(offset.X, 0.0f, 0.0f);
                    textRelativePosition = new Vector3(margin.Right > 0 ? 1.0f : 0.0f, 0.5f, 0.5f);
                    break;

                case MarginEdge.Top:
                    size = new Vector3(thickness, Math.Abs(margin.Top), thickness);
                    value = margin.Top;
                    pinOrigin = new Vector3(0.5f, margin.Top >= 0 ? 1.0f : 0.0f, 0.5f);
                    position += new Vector3(0.0f, -offset.Y, 0.0f);
                    textRelativePosition = new Vector3(0.5f, margin.Top < 0 ? 1.0f : 0.0f, 0.5f);
                    break;

                case MarginEdge.Bottom:
                    size = new Vector3(thickness, Math.Abs(margin.Bottom), thickness);
                    value = margin.Bottom;
                    pinOrigin = new Vector3(0.5f, margin.Bottom <= 0 ? 1.0f : 0.0f, 0.5f);
                    position += new Vector3(0.0f, offset.Y, 0.0f);
                    textRelativePosition = new Vector3(0.5f, margin.Bottom > 0 ? 1.0f : 0.0f, 0.5f);
                    break;

                case MarginEdge.Back:
                case MarginEdge.Front:
                    // FIXME: to be reviewed: not supported yet
                    throw new NotSupportedException();

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
