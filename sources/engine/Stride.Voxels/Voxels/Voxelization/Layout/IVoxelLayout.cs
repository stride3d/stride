// Copyright (c) Stride contributors (https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Shaders;
using static Stride.Rendering.Voxels.VoxelAttributeEmissionOpacity;

namespace Stride.Rendering.Voxels
{
    public interface IVoxelLayout
    {
        int PrepareLocalStorage(VoxelStorageContext context, IVoxelStorage storage);
        void PrepareOutputStorage(VoxelStorageContext context, IVoxelStorage storage);
        void ClearOutputStorage();

        void PostProcess(RenderDrawContext drawContext, LightFalloffs LightFalloff);

        //Writing
        ShaderSource GetVoxelizationShader(List<VoxelModifierEmissionOpacity> modifiers);
        void UpdateVoxelizationLayout(string compositionName, List<VoxelModifierEmissionOpacity> modifiers);
        void ApplyVoxelizationParameters(ParameterCollection parameters, List<VoxelModifierEmissionOpacity> modifiers);

        //Sampling
        ShaderSource GetSamplingShader();
        void UpdateSamplingLayout(string compositionName);
        void ApplySamplingParameters(VoxelViewContext viewContext, ParameterCollection parameters);
    }
}
