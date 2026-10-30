// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Rendering.Materials;

namespace Stride.Rendering
{
    /// <summary>
    /// Skips the back faces of a see-through volume unless the camera may be inside it.
    /// </summary>
    /// <remarks>Back faces only blend for a camera inside the volume; skipping them saves shading what would be discarded.</remarks>
    [DataContract("VolumeBackFaceRenderStageFilter")]
    public class VolumeBackFaceRenderStageFilter : RenderStageFilter
    {
        // Margin, in near plane distances, for a camera whose near plane already cuts the volume's front faces
        private const float NearPlaneMargin = 4f;

        /// <inheritdoc/>
        public override bool IsVisible(RenderObject renderObject, RenderView renderView, RenderViewStage renderViewStage)
        {
            if (renderObject is not RenderMesh { MaterialPass: { PassIndex: MaterialTransparencyBlendFeature.VolumeBackFacePass } materialPass }
                || materialPass.Parameters.Get(MaterialVolumeKeys.Absorption) <= 0)
                return true;

            Matrix.Invert(ref renderView.View, out var viewInverse);
            var bounds = renderObject.BoundingBox;
            var margin = renderView.NearClipPlane * NearPlaneMargin;
            var offset = viewInverse.TranslationVector - bounds.Center;
            return MathF.Abs(offset.X) <= bounds.Extent.X + margin && MathF.Abs(offset.Y) <= bounds.Extent.Y + margin && MathF.Abs(offset.Z) <= bounds.Extent.Z + margin;
        }
    }
}
