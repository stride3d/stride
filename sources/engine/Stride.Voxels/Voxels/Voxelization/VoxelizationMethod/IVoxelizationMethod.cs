// Copyright (c) Stride contributors (https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;

using Stride.Core;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Shaders;
using Stride.Graphics;
using Stride.Rendering.Lights;
using Stride.Rendering.Voxels;
using Stride.Core.Extensions;
using Stride.Rendering;

namespace Stride.Rendering.Voxels
{
    public interface IVoxelizationMethod
    {
        void Reset();
        void CollectVoxelizationPasses(VoxelizationPassList passList, IVoxelStorer storer, Matrix view, Vector3 resolution, VoxelAttribute attr, VoxelizationStage stage, bool output, bool shadows);
        void Render(VoxelStorageContext storageContext, RenderDrawContext drawContext, RenderView view);

        bool RequireGeometryShader();
        int GeometryShaderOutputCount();

        ShaderSource GetVoxelizationShader();
        bool CanShareRenderStage(IVoxelizationMethod method);
    }
}