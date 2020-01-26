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
    //Renders the scene 3 times from different axis to generate all the fragments, no geometry shader needed. Shadows don't work currently.
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("Tri Axis")]
    public class VoxelizationMethodTriAxis : IVoxelizationMethod
    {
        VoxelizationMethodSingleAxis axisX = new VoxelizationMethodSingleAxis
        {
            VoxelizationAxis = VoxelizationMethodSingleAxis.Axis.X
        };
        VoxelizationMethodSingleAxis axisY = new VoxelizationMethodSingleAxis
        {
            VoxelizationAxis = VoxelizationMethodSingleAxis.Axis.Y
        };
        VoxelizationMethodSingleAxis axisZ = new VoxelizationMethodSingleAxis
        {
            VoxelizationAxis = VoxelizationMethodSingleAxis.Axis.Z
        };

        public MultisampleCount MultisampleCount = MultisampleCount.X8;

        public void CollectVoxelizationPasses(VoxelizationPassList passList, IVoxelStorer storer, Matrix view, Vector3 resolution, VoxelAttribute attr, VoxelizationStage stage, bool output, bool shadows)
        {
            axisX.MultisampleCount = MultisampleCount;
            axisY.MultisampleCount = MultisampleCount;
            axisZ.MultisampleCount = MultisampleCount;

            axisX.CollectVoxelizationPasses(passList, storer, view, resolution, attr, stage, output, shadows);
            axisY.CollectVoxelizationPasses(passList, storer, view, resolution, attr, stage, output, shadows);
            axisZ.CollectVoxelizationPasses(passList, storer, view, resolution, attr, stage, output, shadows);
        }
        public void Render(VoxelStorageContext storageContext, RenderDrawContext drawContext, RenderView view)
        {
        }
        public void Reset()
        {
        }
        public ShaderSource GetVoxelizationShader()
        {
            return null;
        }
        public bool RequireGeometryShader()
        {
            return false;
        }
        public int GeometryShaderOutputCount()
        {
            return 3;
        }
        public bool CanShareRenderStage(IVoxelizationMethod obj)
        {
            VoxelizationMethodSingleAxis method = obj as VoxelizationMethodSingleAxis;
            if (method == null)
            {
                return false;
            }
            if (method.MultisampleCount != MultisampleCount)
            {
                return false;
            }
            return true;
        }
    }
}