// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core;
using Stride.Rendering.Materials;

namespace Stride.Rendering
{
    /// <summary>
    /// Sends the meshes whose material's opacity grows with thickness to the stage that sums their thickness.
    /// </summary>
    public class MeshVolumeThicknessRenderStageSelector : RenderStageSelector
    {
        /// <summary>The stage the thickness of see-through volumes is drawn in.</summary>
        [DefaultValue(null)]
        public RenderStage VolumeThicknessRenderStage { get; set; }

        /// <summary>The render groups this selector looks at.</summary>
        public RenderGroupMask RenderGroup { get; set; } = RenderGroupMask.All;

        /// <summary>The effect the stage draws with, usually the mesh effect's <c>VolumeThickness</c> child.</summary>
        public string EffectName { get; set; }

        /// <inheritdoc/>
        public override void Process(RenderObject renderObject)
        {
            if (VolumeThicknessRenderStage == null || ((RenderGroupMask)(1U << (int)renderObject.RenderGroup) & RenderGroup) == 0)
                return;

            var materialPass = ((RenderMesh)renderObject).MaterialPass;
            if (materialPass != null && materialPass.Parameters.Get(MaterialVolumeKeys.Absorption) > 0)
                renderObject.ActiveRenderStages[VolumeThicknessRenderStage.Index] = new ActiveRenderStage(EffectName);
        }
    }
}
