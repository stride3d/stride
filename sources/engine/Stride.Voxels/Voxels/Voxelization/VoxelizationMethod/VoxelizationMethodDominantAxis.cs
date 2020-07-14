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
    //Uses a geometry shader to project each triangle to the axis
    //of maximum coverage, and lets the pipeline generate fragments from there
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("Dominant Axis (Geometry Shader)")]
    public class VoxelizationMethodDominantAxis : IVoxelizationMethod
    {
        List<RenderView> VoxelizationViews { get; } = new List<RenderView>();
        Dictionary<RenderView, Int2> VoxelizationViewSizes { get; } = new Dictionary<RenderView, Int2>();
        int currentViewIndex = 0;

        public MultisampleCount MultisampleCount = MultisampleCount.X8;

        public override bool Equals(object obj)
        {
            VoxelizationMethodDominantAxis method = obj as VoxelizationMethodDominantAxis;
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

        public bool CanShareRenderStage(IVoxelizationMethod obj)
        {
            VoxelizationMethodDominantAxis method = obj as VoxelizationMethodDominantAxis;
            if (method == null)
            {
                return false;
            }
            return true;
        }
        public override int GetHashCode()
        {
            return MultisampleCount.GetHashCode();
        }

        public void CollectVoxelizationPasses(VoxelizationPassList passList, IVoxelStorer storer, Matrix view, Vector3 resolution, VoxelAttribute attr, VoxelizationStage stage, bool output, bool shadows)
        {
            while (VoxelizationViews.Count <= currentViewIndex)
            {
                VoxelizationViews.Add(new RenderView());
                VoxelizationViewSizes[VoxelizationViews[VoxelizationViews.Count-1]] = new Int2();
            }
            RenderView voxelizationView = VoxelizationViews[currentViewIndex];

            voxelizationView.View = Matrix.Identity;
            voxelizationView.Projection = view;
            voxelizationView.ViewProjection = view;

            float maxRes = Math.Max(resolution.X, Math.Max(resolution.Y, resolution.Z));
            Matrix aspectScale = Matrix.Scaling(resolution / maxRes);
            voxelizationView.Projection *= aspectScale;
            voxelizationView.ViewProjection = voxelizationView.View * voxelizationView.Projection;

            voxelizationView.ViewSize = new Vector2(maxRes * 8, maxRes * 8);
            VoxelizationViewSizes[voxelizationView] = new Int2((int)maxRes, (int)maxRes);


            //The BoundingFrustum constructor doesn't end up calculating the correct Near Plane for the symmetric matrix, squish it so the Z is from 0 to 1
            Matrix SquishedMatrix = voxelizationView.ViewProjection * Matrix.Scaling(1f, 1f, 0.5f) * Matrix.Translation(new Vector3(0, 0, 0.5f));
            voxelizationView.Frustum = new BoundingFrustum(ref SquishedMatrix);

            voxelizationView.CullingMode = CameraCullingMode.None;
            voxelizationView.NearClipPlane = 0.1f;
            voxelizationView.FarClipPlane = 1000.0f;

            currentViewIndex++;

            passList.AddDirect(storer, this, voxelizationView, attr, stage, output, shadows);
        }

        Stride.Graphics.Texture MSAARenderTarget = null;

        public void Render(VoxelStorageContext storageContext, RenderDrawContext drawContext, RenderView view)
        {
            RenderView voxelizationView = view;
            Int2 ViewSize = VoxelizationViewSizes[view];

            if (VoxelUtils.DisposeTextureBySpecs(MSAARenderTarget, new Vector3(ViewSize.X, ViewSize.Y, 1), PixelFormat.R8G8B8A8_UNorm, MultisampleCount))
            {
                MSAARenderTarget = Texture.New(storageContext.device, TextureDescription.New2D(ViewSize.X, ViewSize.Y, new MipMapCount(false), PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget, 1, GraphicsResourceUsage.Default, MultisampleCount), null);
            }

            drawContext.CommandList.ResetTargets();
            if (MSAARenderTarget != null)
                drawContext.CommandList.SetRenderTarget(null, MSAARenderTarget);

            var renderSystem = drawContext.RenderContext.RenderSystem;

            drawContext.CommandList.SetViewport(new Viewport(0, 0, ViewSize.X, ViewSize.Y));

            renderSystem.Draw(drawContext, voxelizationView, renderSystem.RenderStages[voxelizationView.RenderStages[0].Index]);
        }
        public void Reset()
        {
            currentViewIndex = 0;
        }

        ShaderClassSource method = new ShaderClassSource("VoxelizationMethodDominantAxis");
        ShaderMixinSource methodmixin = null;
        public ShaderSource GetVoxelizationShader()
        {
            if (methodmixin == null)
            {
                methodmixin = new ShaderMixinSource();
                methodmixin.Mixins.Add(method);
            }
            return methodmixin;
        }
        public bool RequireGeometryShader()
        {
            return true;
        }
        public int GeometryShaderOutputCount()
        {
            return 3;
        }
    }
}