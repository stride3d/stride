// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.UI.Controls
{
    /// <summary>
    /// A border element adds an uniform color border around its content.
    /// </summary>
    [DataContract(nameof(Border))]
    public class Border : ContentControl
    {
        internal Color BorderColorInternal = Color.Black;
        private Thickness borderThickness = Thickness.Uniform(0);

        /// <summary>
        /// Gets or sets the color of the border.
        /// </summary>
        /// <userdoc>The color of the border.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        public Color BorderColor
        {
            get { return BorderColorInternal; }
            set { BorderColorInternal = value; }
        }

        /// <summary>
        /// Gets or sets the thickness of the border.
        /// </summary>
        /// <userdoc>The thickness of the border.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        public Thickness BorderThickness
        {
            get { return borderThickness; }
            set
            {
                borderThickness = value;
                InvalidateMeasure();
            }
        }

        protected override Size2F MeasureOverride(Size2F availableSizeWithoutMargins)
        {
            var availableLessBorders = availableSizeWithoutMargins - borderThickness;

            var neededSize = base.MeasureOverride(availableLessBorders);

            return neededSize + borderThickness;
        }

        protected override Size2F ArrangeOverride(Size2F finalSizeWithoutMargins)
        {
            // arrange the content
            if (VisualContent != null)
            {
                // calculate the remaining space for the child after having removed the padding and border space.
                var availableLessBorders = finalSizeWithoutMargins - borderThickness;
                var childSizeWithoutPadding = availableLessBorders - padding;

                // arrange the child
                VisualContent.Arrange(childSizeWithoutPadding, IsCollapsed);

                // compute the rendering offsets of the child element wrt the parent origin (0,0,0)
                var childOffsets = new Vector2(padding.Left + borderThickness.Left, padding.Top + borderThickness.Top) - (Vector2)finalSizeWithoutMargins / 2;

                // set the arrange matrix of the child.
                VisualContent.DependencyProperties.Set(ContentArrangeMatrixPropertyKey, Matrix.Translation(new Vector3(childOffsets.X, childOffsets.Y, 0)));
            }

            return finalSizeWithoutMargins;
        }
    }
}
