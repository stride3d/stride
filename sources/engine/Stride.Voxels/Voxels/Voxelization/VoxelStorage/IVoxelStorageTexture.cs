﻿// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using Stride.Graphics;
using Stride.Shaders;

namespace Stride.Rendering.Voxels
{
    public interface IVoxelStorageTexture
    {
        void UpdateVoxelizationLayout(string compositionName);
        void UpdateSamplingLayout(string compositionName);
        void ApplyVoxelizationParameters(ObjectParameterKey<Texture> MainKey, ParameterCollection parameters);
        void PostProcess(RenderDrawContext drawContext, ShaderSource[] mipmapShaders);
        ShaderClassSource GetSamplingShader();
        void ApplySamplingParameters(VoxelViewContext viewContext, ParameterCollection parameters);
    }
}
