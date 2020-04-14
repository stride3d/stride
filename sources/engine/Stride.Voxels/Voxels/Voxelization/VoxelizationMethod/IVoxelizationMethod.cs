using System;
using System.Collections.Generic;
using System.Text;

using Xenko.Core;
using Xenko.Core.Collections;
using Xenko.Core.Diagnostics;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Shaders;
using Xenko.Graphics;
using Xenko.Rendering.Lights;
using Xenko.Rendering.Voxels;
using Xenko.Core.Extensions;
using Xenko.Rendering;

namespace Xenko.Rendering.Voxels
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