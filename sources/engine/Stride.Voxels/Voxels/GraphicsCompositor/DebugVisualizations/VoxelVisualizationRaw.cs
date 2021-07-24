﻿// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Rendering.Images;

namespace Stride.Rendering.Voxels.Debug
{
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("Debug")]
    public class VoxelVisualizationRaw : IVoxelVisualization
    {
        public int Mipmap = 0;
        public Vector2 Range = new Vector2(0.0f,1.0f);
        public int RangeOffset = 0;
        private ImageEffectShader voxelDebugEffectShader = new ImageEffectShader("VoxelVisualizationRawEffect");
        public ImageEffectShader GetShader(RenderDrawContext context, VoxelAttribute attr)
        {
            VoxelViewContext viewContext = new VoxelViewContext(false);
            attr.UpdateSamplingLayout("Attribute");
            attr.ApplySamplingParameters(viewContext, voxelDebugEffectShader.Parameters);
            voxelDebugEffectShader.Parameters.Set(VoxelVisualizationRawShaderKeys.Attribute, attr.GetSamplingShader());
            voxelDebugEffectShader.Parameters.Set(VoxelVisualizationRawShaderKeys.mip, Mipmap);
            voxelDebugEffectShader.Parameters.Set(VoxelVisualizationRawShaderKeys.rangeOffset, RangeOffset);
            voxelDebugEffectShader.Parameters.Set(VoxelVisualizationRawShaderKeys.range, Range);

            return voxelDebugEffectShader;
        }
    }
}
