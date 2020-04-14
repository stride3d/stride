// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Quantum;
using Stride.Assets.Presentation.ViewModel;

namespace Stride.Assets.Presentation.AssetEditors.SpriteEditor.ViewModels
{
    public class TextureRegionViewModel : ResizableSpriteInfoPartViewModel
    {
        private readonly GraphNodeBinding<RectangleF, Rectangle> textureRegionBinding;
        private double imageWidth;
        private double imageHeight;

        public TextureRegionViewModel(SpriteInfoViewModel sprite, IMemberNode textureRegionNode)
            : base(sprite)
        {
            if (sprite.Editor.Cache != null)
            {
                RefreshImageSize();
            }
            else
            {
                sprite.Editor.Initialized += EditorInitialized;
            }
            textureRegionBinding = new MemberGraphNodeBinding<RectangleF, Rectangle>(textureRegionNode, nameof(Region), OnPropertyChanging, OnPropertyChanged, x => (Rectangle)x, UndoRedoService);

            DependentProperties.Add(nameof(Region), new[] { nameof(ActualLeft), nameof(ActualTop), nameof(ActualWidth), nameof(ActualHeight), nameof(ActualRightOffset), nameof(ActualBottomOffset) });
            DependentProperties.Add(nameof(ScaleFactor), new[] { nameof(ActualLeft), nameof(ActualTop), nameof(ActualWidth), nameof(ActualHeight), nameof(ActualRightOffset), nameof(ActualBottomOffset) });
            DependentProperties.Add(nameof(ImageWidth), new[] { nameof(ActualRightOffset), nameof(ActualBottomOffset) });
            DependentProperties.Add(nameof(ImageHeight), new[] { nameof(ActualRightOffset), nameof(ActualBottomOffset) });
        }

        public double ImageWidth
        {
            get { return imageWidth; }
            set
            {
                var needClamp = value >= 0 && imageWidth > value;
                SetValue(ref imageWidth, value);
                if (needClamp)
                {
                    var copy = Region;
                    ClampHorizontally(ref copy, (float)imageWidth - copy.Width, (float)imageWidth);
                    Region = copy;
                }
            }
        }

        public double ImageHeight
        {
            get { return imageHeight; }
            set
            {
                var needClamp = value >= 0 && imageHeight > value;
                SetValue(ref imageHeight, value);
                if (needClamp)
                {
                    var copy = Region;
                    ClampVertically(ref copy, (float)imageHeight - copy.Height, (float)imageHeight);
                    Region = copy;
                }
            }
        }

        public RectangleF Region
        {
            get { return textureRegionBinding.GetNodeValue(); }
            set
            {
                ClampHorizontally(ref value, (float)imageWidth, (float)imageWidth);
                ClampVertically(ref value, (float)imageHeight, (float)imageHeight);
                textureRegionBinding.SetNodeValue(value);
            }
        }

        public double ActualLeft => Region.Left*ScaleFactor;

        public double ActualTop => Region.Top*ScaleFactor;

        public double ActualRightOffset => (ImageWidth - Region.Right) * ScaleFactor;

        public double ActualBottomOffset => (ImageHeight - Region.Bottom) * ScaleFactor;

        public double ActualWidth => Region.Width*ScaleFactor;

        public double ActualHeight => Region.Height*ScaleFactor;

        /// <inheritdoc/>
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(TextureRegionViewModel));
            textureRegionBinding.Dispose();
            base.Destroy();
        }

        public void UseWholeImage()
        {
            Region = new RectangleF
            {
                Left = 0,
                Top = 0,
                Width = (float)ImageWidth,
                Height = (float)ImageHeight
            };
        }

        internal void RefreshImageSize()
        {
            var size = Sprite.Editor.Cache.GetPixelSize(Sprite.Source);
            ImageWidth = size?.Width ?? 0;
            ImageHeight = size?.Height ?? 0;
        }

        private void EditorInitialized(object sender, EventArgs eventArgs)
        {
            RefreshImageSize();
            Sprite.Editor.Initialized -= EditorInitialized;
        }

        protected override void OnResizeDelta(ResizingDirection direction, double horizontalChange, double verticalChange)
        {
            var scaledHorizontalChange = (float)Math.Round(horizontalChange / ScaleFactor, MidpointRounding.AwayFromZero);
            var scaledVerticalChange = (float)Math.Round(verticalChange / ScaleFactor, MidpointRounding.AwayFromZero);
            var newRegion = Region;
            if (direction.HasFlag(ResizingDirection.Center))
            {
                newRegion.Left += scaledHorizontalChange;
                newRegion.Top += scaledVerticalChange;
                // Clamp to keep region size
                ClampHorizontally(ref newRegion, (float)imageWidth - Region.Width, (float)imageWidth);
                ClampVertically(ref newRegion, (float)imageHeight - Region.Height, (float)imageHeight);
            }
            else
            {
                if (direction.HasFlag(ResizingDirection.Left))
                {
                    var xOffset = MathUtil.Clamp(scaledHorizontalChange, -newRegion.Left, newRegion.Width);
                    newRegion.Left += xOffset;
                    newRegion.Width -= xOffset;
                }
                if (direction.HasFlag(ResizingDirection.Right))
                {
                    var xOffset = Math.Max(-newRegion.Width, scaledHorizontalChange);
                    newRegion.Width += xOffset;
                }
                if (direction.HasFlag(ResizingDirection.Top))
                {
                    var yOffset = MathUtil.Clamp(scaledVerticalChange, -newRegion.Top, newRegion.Height);
                    newRegion.Top += yOffset;
                    newRegion.Height -= yOffset;
                }
                if (direction.HasFlag(ResizingDirection.Bottom))
                {
                    var yOffset = Math.Max(-newRegion.Height, scaledVerticalChange);
                    newRegion.Height += yOffset;
                }
            }
            Region = newRegion;
        }

        protected override string ComputeTransactionName(ResizingDirection direction, double horizontalChange, double verticalChange)
        {
            return direction == ResizingDirection.Center ? "Move region" : "Update region size";
        }

        private static void ClampHorizontally(ref RectangleF value, float maxLeft, float maxWidth)
        {
            value.Left = MathUtil.Clamp(value.Left, 0.0f, Math.Max(0, maxLeft));
            value.Width = MathUtil.Clamp(value.Width, 0.0f, Math.Max(0, maxWidth - value.Left));
        }

        private static void ClampVertically(ref RectangleF value, float maxTop, float maxHeight)
        {
            value.Top = MathUtil.Clamp(value.Top, 0.0f, Math.Max(0, maxTop));
            value.Height = MathUtil.Clamp(value.Height, 0.0f, Math.Max(0, maxHeight - value.Top));
        }
    }
}
