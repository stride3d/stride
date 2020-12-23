// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Particles.Sorters;
using Stride.Particles.VertexLayouts;

namespace Stride.Particles.Materials
{
    /// <summary>
    /// Animates the texture coordinates starting with one rectangle and scrolling/zooming it to an ending rectangle over the particle's life
    /// </summary>
    [DataContract("UVBuilderScroll")]
    [Display("Scrolling")]
    public class UVBuilderScroll : UVBuilder, IAttributeTransformer<Vector2, Vector4>
    {
        /// <summary>
        /// Starting sub-region (rectangle) for the scroll
        /// </summary>
        /// <userdoc>
        /// The rectangular sub-region of the texture where the scrolling should start, given as (Xmin, Ymin, Xmax, Ymax) ( (0, 0, 1, 1) being the entire texture). Numbers also can be negative or bigger than 1.
        /// </userdoc>
        [DataMember(200)]
        [Display("Start frame")]
        public Vector4 StartFrame { get; set; } = new Vector4(0, 0, 1, 1);

        /// <summary>
        /// Ending sub-region (rectangle) for the scroll
        /// </summary>
        /// <userdoc>
        /// The rectangular sub-region of the texture where the scrolling should end at the particle life's end, given as (Xmin, Ymin, Xmax, Ymax) ( (0, 0, 1, 1) being the entire texture). Numbers also can be negative or bigger than 1.
        /// </userdoc>
        [DataMember(240)]
        [Display("End frame")]
        public Vector4 EndFrame { get; set; } = new Vector4(0, 1, 1, 2);

        /// <inheritdoc />
        public unsafe override void BuildUVCoordinates(ref ParticleBufferState bufferState, ref ParticleList sorter, AttributeDescription texCoordsDescription)
        {
            var lifeField = sorter.GetField(ParticleFields.RemainingLife);

            if (!lifeField.IsValid())
                return;

            var texAttribute = bufferState.GetAccessor(texCoordsDescription);
            if (texAttribute.Size == 0 && texAttribute.Offset == 0)
            {
                return;
            }

            var texDefault = bufferState.GetAccessor(bufferState.DefaultTexCoords);
            if (texDefault.Size == 0 && texDefault.Offset == 0)
            {
                return;
            }

            foreach (var particle in sorter)
            {
                var normalizedTimeline = 1f - *(float*)(particle[lifeField]);

                Vector4 uvTransform = Vector4.Lerp(StartFrame, EndFrame, normalizedTimeline);
                uvTransform.Z -= uvTransform.X;
                uvTransform.W -= uvTransform.Y;

                bufferState.TransformAttributePerSegment(texDefault, texAttribute, this, ref uvTransform);

                bufferState.NextSegment();
            }


            bufferState.StartOver();
        }

        public void Transform(ref Vector2 attribute, ref Vector4 uvTransform)
        {
            attribute.X = uvTransform.X + uvTransform.Z * attribute.X;
            attribute.Y = uvTransform.Y + uvTransform.W * attribute.Y;
        }

    }
}
