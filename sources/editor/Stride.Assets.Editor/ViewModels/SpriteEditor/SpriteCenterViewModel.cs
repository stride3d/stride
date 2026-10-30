// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Sprite;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Quantum;

namespace Stride.Assets.Editor.ViewModels.SpriteEditor;

/// <summary>
/// The center of a sprite of a 2D sprite sheet, which a handle of the view moves.
/// </summary>
public sealed class SpriteCenterViewModel : ResizableSpriteInfoPartViewModel
{
    private readonly MemberGraphNodeBinding<bool> centerFromMiddleBinding;
    private readonly MemberGraphNodeBinding<Vector2> spriteCenterNodeBinding;
    private readonly MemberGraphNodeBinding<Rectangle> textureRegionBinding;

    public SpriteCenterViewModel(SpriteInfoViewModel sprite, IObjectNode spriteNode)
        : base(sprite)
    {
        var spriteCenterNode = spriteNode[nameof(SpriteInfo.Center)];
        spriteCenterNodeBinding = new MemberGraphNodeBinding<Vector2>(spriteCenterNode, nameof(Center), OnPropertyChanging, OnPropertyChanged, UndoRedoService);
        var centerFromMiddleNode = spriteNode[nameof(SpriteInfo.CenterFromMiddle)];
        centerFromMiddleBinding = new MemberGraphNodeBinding<bool>(centerFromMiddleNode, nameof(CenterFromMiddle), OnPropertyChanging, OnPropertyChanged, UndoRedoService);

        var textureRegionNode = spriteNode[nameof(SpriteInfo.TextureRegion)];
        textureRegionBinding = new MemberGraphNodeBinding<Rectangle>(textureRegionNode, nameof(ActualCenter), OnPropertyChanging, OnPropertyChanged, UndoRedoService);

        DependentProperties.Add(nameof(Center), [nameof(ActualCenter)]);
        DependentProperties.Add(nameof(CenterFromMiddle), [nameof(ActualCenter)]);
        DependentProperties.Add(nameof(ScaleFactor), [nameof(ActualCenter)]);
    }

    /// <summary>
    /// Gets or sets the center of the sprite, in pixels, from the middle or the top-left of the region (<see cref="CenterFromMiddle"/>).
    /// </summary>
    public Vector2 Center { get => spriteCenterNodeBinding.Value; set => spriteCenterNodeBinding.Value = value; }

    /// <summary>
    /// Gets or sets whether <see cref="Center"/> is relative to the middle of the region rather than its top-left.
    /// </summary>
    public bool CenterFromMiddle { get => centerFromMiddleBinding.Value; set => centerFromMiddleBinding.Value = value; }

    /// <summary>
    /// Gets the center in the view (scaled by the zoom), from the top-left of the region.
    /// </summary>
    public Vector2 ActualCenter =>
        CenterFromMiddle
            ? new Vector2((float)((Center.X + textureRegionBinding.Value.Width * 0.5) * ScaleFactor), (float)((Center.Y + textureRegionBinding.Value.Height * 0.5) * ScaleFactor))
            : new Vector2((float)((Center.X + 0.5) * ScaleFactor), (float)((Center.Y + 0.5) * ScaleFactor));

    /// <inheritdoc/>
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(SpriteCenterViewModel));
        spriteCenterNodeBinding.Dispose();
        centerFromMiddleBinding.Dispose();
        textureRegionBinding.Dispose();
        base.Destroy();
    }

    /// <inheritdoc/>
    protected override void OnResizeDelta(ResizingDirection direction, double horizontalChange, double verticalChange)
    {
        var scaledHorizontalChange = (float)Math.Round(horizontalChange / ScaleFactor, MidpointRounding.AwayFromZero);
        var scaledVerticalChange = (float)Math.Round(verticalChange / ScaleFactor, MidpointRounding.AwayFromZero);
        var newCenter = Center;
        if (direction.HasFlag(ResizingDirection.Center))
        {
            newCenter.X += scaledHorizontalChange;
            newCenter.Y += scaledVerticalChange;
        }
        Center = newCenter;
    }

    /// <inheritdoc/>
    protected override string? ComputeTransactionName(ResizingDirection direction, double horizontalChange, double verticalChange)
    {
        return direction == ResizingDirection.Center ? "Move sprite center" : null;
    }
}
