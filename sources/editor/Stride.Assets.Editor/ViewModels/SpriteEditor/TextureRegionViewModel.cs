// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Quantum;

namespace Stride.Assets.Editor.ViewModels.SpriteEditor;

/// <summary>
/// The region of the image that a sprite uses, which the handles of the view move and resize, clamped to the image.
/// </summary>
public sealed class TextureRegionViewModel : ResizableSpriteInfoPartViewModel
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

        DependentProperties.Add(nameof(Region), [nameof(ActualLeft), nameof(ActualTop), nameof(ActualWidth), nameof(ActualHeight), nameof(ActualRightOffset), nameof(ActualBottomOffset)]);
        DependentProperties.Add(nameof(ScaleFactor), [nameof(ActualLeft), nameof(ActualTop), nameof(ActualWidth), nameof(ActualHeight), nameof(ActualRightOffset), nameof(ActualBottomOffset)]);
        DependentProperties.Add(nameof(ImageWidth), [nameof(ActualRightOffset), nameof(ActualBottomOffset)]);
        DependentProperties.Add(nameof(ImageHeight), [nameof(ActualRightOffset), nameof(ActualBottomOffset)]);
    }

    /// <summary>
    /// Gets or sets the width of the source image, in pixels. A smaller image clamps the region.
    /// </summary>
    public double ImageWidth
    {
        get => imageWidth;
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

    /// <summary>
    /// Gets or sets the height of the source image, in pixels. A smaller image clamps the region.
    /// </summary>
    public double ImageHeight
    {
        get => imageHeight;
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

    /// <summary>
    /// Gets or sets the region of the image, in pixels. A new value is clamped to the image.
    /// </summary>
    public RectangleF Region
    {
        get => textureRegionBinding.GetNodeValue();
        set
        {
            ClampHorizontally(ref value, (float)imageWidth, (float)imageWidth);
            ClampVertically(ref value, (float)imageHeight, (float)imageHeight);
            textureRegionBinding.SetNodeValue(value);
        }
    }

    /// <summary>
    /// Gets the left of the region in the view (scaled by the zoom).
    /// </summary>
    public double ActualLeft => Region.Left * ScaleFactor;

    /// <summary>
    /// Gets the top of the region in the view (scaled by the zoom).
    /// </summary>
    public double ActualTop => Region.Top * ScaleFactor;

    /// <summary>
    /// Gets the distance from the right of the region to the right of the image in the view (scaled by the zoom).
    /// </summary>
    public double ActualRightOffset => (ImageWidth - Region.Right) * ScaleFactor;

    /// <summary>
    /// Gets the distance from the bottom of the region to the bottom of the image in the view (scaled by the zoom).
    /// </summary>
    public double ActualBottomOffset => (ImageHeight - Region.Bottom) * ScaleFactor;

    /// <summary>
    /// Gets the width of the region in the view (scaled by the zoom).
    /// </summary>
    public double ActualWidth => Region.Width * ScaleFactor;

    /// <summary>
    /// Gets the height of the region in the view (scaled by the zoom).
    /// </summary>
    public double ActualHeight => Region.Height * ScaleFactor;

    /// <inheritdoc/>
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(TextureRegionViewModel));
        Sprite.Editor.Initialized -= EditorInitialized;
        textureRegionBinding.Dispose();
        base.Destroy();
    }

    /// <summary>
    /// Sets the region to the whole image.
    /// </summary>
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
        var size = Sprite.Editor.Cache?.GetPixelSize(Sprite.Source);
        ImageWidth = size?.Width ?? 0;
        ImageHeight = size?.Height ?? 0;
    }

    private void EditorInitialized(object? sender, EventArgs eventArgs)
    {
        RefreshImageSize();
        Sprite.Editor.Initialized -= EditorInitialized;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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
