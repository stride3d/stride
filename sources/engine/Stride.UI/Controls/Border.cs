// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
        private Thickness borderThickness = Thickness.UniformCuboid(0);

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

        protected override Vector3 MeasureOverride(Vector3 availableSizeWithoutMargins)
        {
            var availableLessBorders = CalculateSizeWithoutThickness(ref availableSizeWithoutMargins, ref borderThickness);

            var neededSize = base.MeasureOverride(availableLessBorders);

            return CalculateSizeWithThickness(ref neededSize, ref borderThickness);
        }

        protected override Vector3 ArrangeOverride(Vector3 finalSizeWithoutMargins)
        {
            // arrange the content
            if (VisualContent != null)
            {
                // calculate the remaining space for the child after having removed the padding and border space.
                var availableLessBorders = CalculateSizeWithoutThickness(ref finalSizeWithoutMargins, ref borderThickness);
                var childSizeWithoutPadding = CalculateSizeWithoutThickness(ref availableLessBorders, ref padding);

                // arrange the child
                VisualContent.Arrange(childSizeWithoutPadding, IsCollapsed);

                // compute the rendering offsets of the child element wrt the parent origin (0,0,0)
                var childOffsets = new Vector3(padding.Left + borderThickness.Left, padding.Top + borderThickness.Top, padding.Front + borderThickness.Front) - finalSizeWithoutMargins / 2;

                // set the arrange matrix of the child.
                VisualContent.DependencyProperties.Set(ContentArrangeMatrixPropertyKey, Matrix.Translation(childOffsets));
            }

            return finalSizeWithoutMargins;
        }
    }
}
