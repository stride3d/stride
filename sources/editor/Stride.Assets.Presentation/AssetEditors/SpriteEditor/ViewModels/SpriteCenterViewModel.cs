// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Quantum;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.Sprite;

namespace Stride.Assets.Presentation.AssetEditors.SpriteEditor.ViewModels
{
    public class SpriteCenterViewModel : ResizableSpriteInfoPartViewModel
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

            DependentProperties.Add(nameof(Center), new[] { nameof(ActualCenter) });
            DependentProperties.Add(nameof(CenterFromMiddle), new[] { nameof(ActualCenter) });
            DependentProperties.Add(nameof(ScaleFactor), new[] { nameof(ActualCenter) });
        }

        public Vector2 Center { get { return spriteCenterNodeBinding.Value; } set { spriteCenterNodeBinding.Value = value; } }

        public bool CenterFromMiddle { get { return centerFromMiddleBinding.Value; } set { centerFromMiddleBinding.Value = value; } }

        public Thickness ActualCenter =>
            CenterFromMiddle
                ? new Thickness((Center.X + textureRegionBinding.Value.Width * 0.5) * ScaleFactor, (Center.Y + textureRegionBinding.Value.Height * 0.5) * ScaleFactor, 0.0, 0.0)
                : new Thickness((Center.X + 0.5) * ScaleFactor, (Center.Y + 0.5) * ScaleFactor, 0.0, 0.0);
        
        /// <inheritdoc/>
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(SpriteCenterViewModel));
            spriteCenterNodeBinding.Dispose();
            centerFromMiddleBinding.Dispose();
            textureRegionBinding.Dispose();
            base.Destroy();
        }

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

        protected override string ComputeTransactionName(ResizingDirection direction, double horizontalChange, double verticalChange)
        {
            return direction == ResizingDirection.Center ? "Move sprite center" : null;
        }
    }
}
