// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xenko.Core.Mathematics;
using Xenko.Core.Threading;
using Xenko.Graphics;
using Xenko.Rendering;

namespace Xenko.Rendering
{
    /// <summary>
    /// A render feature that computes and upload World, View and Projection matrices for each views and for each objects.
    /// </summary>
    public class TransformRenderFeature : SubRenderFeature
    {
        internal ObjectPropertyKey<RenderModelFrameInfo> RenderModelObjectInfoKey;
        internal ViewObjectPropertyKey<RenderModelViewInfo> RenderModelViewInfoKey;

        private ConstantBufferOffsetReference time; // TODO: Move this at a more global level so that it applies on everything? (i.e. RootEffectRenderFeature)
        private ConstantBufferOffsetReference view;
        private ConstantBufferOffsetReference world;
        private ConstantBufferOffsetReference worldInverse;
        private ConstantBufferOffsetReference camera;

        internal struct RenderModelFrameInfo
        {
            // Copied during Extract
            public Matrix World;
        }

        internal struct RenderModelViewInfo
        {
            // Copied during Extract
            public Matrix WorldViewProjection;
            public Matrix WorldView;
        }

        /// <inheritdoc/>
        protected override void InitializeCore()
        {
            RenderModelObjectInfoKey = RootRenderFeature.RenderData.CreateObjectKey<RenderModelFrameInfo>();
            RenderModelViewInfoKey = RootRenderFeature.RenderData.CreateViewObjectKey<RenderModelViewInfo>();

            time = ((RootEffectRenderFeature)RootRenderFeature).CreateFrameCBufferOffsetSlot(GlobalKeys.Time.Name);
            view = ((RootEffectRenderFeature)RootRenderFeature).CreateViewCBufferOffsetSlot(TransformationKeys.View.Name);
            camera = ((RootEffectRenderFeature)RootRenderFeature).CreateViewCBufferOffsetSlot(CameraKeys.NearClipPlane.Name);
            world = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawCBufferOffsetSlot(TransformationKeys.World.Name);
            worldInverse = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawCBufferOffsetSlot(TransformationKeys.WorldInverse.Name);
        }

        /// <inheritdoc/>
        public override void Extract()
        {
            var renderModelObjectInfo = RootRenderFeature.RenderData.GetData(RenderModelObjectInfoKey);

            //for (int index = 0; index < RootRenderFeature.ObjectNodeReferences.Count; index++)
            Dispatcher.For(0, RootRenderFeature.ObjectNodeReferences.Count, index =>
            {
                var objectNodeReference = RootRenderFeature.ObjectNodeReferences[index];
                var objectNode = RootRenderFeature.GetObjectNode(objectNodeReference);
                var renderMesh = objectNode.RenderObject as RenderMesh;

                // TODO: Extract world
                renderModelObjectInfo[objectNodeReference].World = renderMesh != null ? renderMesh.World : Matrix.Identity;
            });
        }

        /// <param name="context"></param>
        /// <inheritdoc/>
        public override unsafe void Prepare(RenderDrawContext context)
        {
            // Compute WorldView, WorldViewProj
            var renderModelObjectInfoData = RootRenderFeature.RenderData.GetData(RenderModelObjectInfoKey);
            var renderModelViewInfoData = RootRenderFeature.RenderData.GetData(RenderModelViewInfoKey);

            // Update PerFrame (time)
            // TODO Move that to RootEffectRenderFeature?
            foreach (var frameLayout in ((RootEffectRenderFeature)RootRenderFeature).FrameLayouts)
            {
                var timeOffset = frameLayout.GetConstantBufferOffset(time);
                if (timeOffset == -1)
                    continue;

                var resourceGroup = frameLayout.Entry.Resources;
                var mappedCB = resourceGroup.ConstantBuffer.Data;

                var perFrameTime = (PerFrameTime*)((byte*)mappedCB + timeOffset);
                perFrameTime->Time = (float)Context.Time.Total.TotalSeconds;
                perFrameTime->TimeStep = (float)Context.Time.Elapsed.TotalSeconds;
            }

            // Update PerView (View, Proj, etc...)
            for (int index = 0; index < RenderSystem.Views.Count; index++)
            {
                var view = RenderSystem.Views[index];
                var viewFeature = view.Features[RootRenderFeature.Index];

                // Compute WorldView and WorldViewProjection
                Dispatcher.ForEach(viewFeature.ViewObjectNodes, renderPerViewNodeReference =>
                {
                    var renderPerViewNode = RootRenderFeature.GetViewObjectNode(renderPerViewNodeReference);
                    ref var renderModelFrameInfo = ref renderModelObjectInfoData[renderPerViewNode.ObjectNode];
                    ref var renderModelViewInfo = ref renderModelViewInfoData[renderPerViewNodeReference];

                    Matrix.Multiply(ref renderModelFrameInfo.World, ref view.View, out renderModelViewInfo.WorldView);
                    Matrix.Multiply(ref renderModelFrameInfo.World, ref view.ViewProjection, out renderModelViewInfo.WorldViewProjection);
                });

                // Copy ViewProjection to PerView cbuffer
                foreach (var viewLayout in viewFeature.Layouts)
                {
                    var viewProjectionOffset = viewLayout.GetConstantBufferOffset(this.view);
                    if (viewProjectionOffset == -1)
                        continue;

                    var resourceGroup = viewLayout.Entries[view.Index].Resources;
                    var mappedCB = resourceGroup.ConstantBuffer.Data;

                    var perView = (PerView*)((byte*)mappedCB + viewProjectionOffset);

                    // Fill PerView
                    perView->View = view.View;
                    Matrix.Invert(ref view.View, out perView->ViewInverse);
                    perView->Projection = view.Projection;
                    Matrix.Invert(ref view.Projection, out perView->ProjectionInverse);
                    perView->ViewProjection = view.ViewProjection;
                    perView->ProjScreenRay = new Vector2(-1.0f / view.Projection.M11, 1.0f / view.Projection.M22);
                    // TODO GRAPHICS REFACTOR avoid cbuffer read
                    perView->Eye = new Vector4(perView->ViewInverse.M41, perView->ViewInverse.M42, perView->ViewInverse.M43, 1.0f);
                }

                // Copy Camera to PerView cbuffer
                foreach (var viewLayout in viewFeature.Layouts)
                {
                    var cameraOffset = viewLayout.GetConstantBufferOffset(camera);
                    if (cameraOffset == -1)
                        continue;

                    var resourceGroup = viewLayout.Entries[view.Index].Resources;
                    var mappedCB = resourceGroup.ConstantBuffer.Data;

                    var perViewCamera = (PerViewCamera*)((byte*)mappedCB + cameraOffset);

                    perViewCamera->NearClipPlane = view.NearClipPlane;
                    perViewCamera->FarClipPlane = view.FarClipPlane;
                    perViewCamera->ZProjection = CameraKeys.ZProjectionACalculate(view.NearClipPlane, view.FarClipPlane);
                    // TODO: Report a warning if ViewSize is 0
                    perViewCamera->ViewSize = view.ViewSize;
                    perViewCamera->AspectRatio = view.ViewSize.X / Math.Max(view.ViewSize.Y, 1.0f);
                }
            }

            // Update PerDraw (World, WorldViewProj, etc...)
            // Copy Entity.World to PerDraw cbuffer
            // TODO: Have a PerObject cbuffer?
            Dispatcher.ForEach(((RootEffectRenderFeature)RootRenderFeature).RenderNodes, (ref RenderNode renderNode) =>
            {
                var perDrawLayout = renderNode.RenderEffect.Reflection?.PerDrawLayout;
                if (perDrawLayout == null)
                    return;

                var worldOffset = perDrawLayout.GetConstantBufferOffset(this.world);
                var worldInverseOffset = perDrawLayout.GetConstantBufferOffset(this.worldInverse);

                if (worldOffset == -1 && worldInverseOffset == -1)
                    return;

                ref var renderModelObjectInfo = ref renderModelObjectInfoData[renderNode.RenderObject.ObjectNode];
                ref var renderModelViewInfo = ref renderModelViewInfoData[renderNode.ViewObjectNode];

                var mappedCB = renderNode.Resources.ConstantBuffer.Data;
                if (worldOffset != -1)
                {
                    var world = (Matrix*)((byte*)mappedCB + worldOffset);
                    *world = renderModelObjectInfo.World;
                }

                if (worldInverseOffset != -1)
                {
                    var perDraw = (PerDrawExtra*)((byte*)mappedCB + worldInverseOffset);

                    // Fill PerDraw
                    var perDrawData = new PerDrawExtra
                    {
                        WorldView = renderModelViewInfo.WorldView,
                        WorldViewProjection = renderModelViewInfo.WorldViewProjection,
                    };

                    Matrix.Invert(ref renderModelObjectInfo.World, out perDrawData.WorldInverse);
                    Matrix.Transpose(ref perDrawData.WorldInverse, out perDrawData.WorldInverseTranspose);
                    Matrix.Invert(ref renderModelViewInfo.WorldView, out perDrawData.WorldViewInverse);

                    perDrawData.WorldScale = new Vector3(
                        ((Vector3)renderModelObjectInfo.World.Row1).Length(),
                        ((Vector3)renderModelObjectInfo.World.Row2).Length(),
                        ((Vector3)renderModelObjectInfo.World.Row3).Length());

                    perDrawData.EyeMS = new Vector4(perDrawData.WorldInverse.M41, perDrawData.WorldInverse.M42, perDrawData.WorldInverse.M43, 1.0f);

                    *perDraw = perDrawData;
                }
            });
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PerFrameTime
        {
            public float Time;
            public float TimeStep;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PerView
        {
            public Matrix View;
            public Matrix ViewInverse;
            public Matrix Projection;
            public Matrix ProjectionInverse;
            public Matrix ViewProjection;
            public Vector2 ProjScreenRay;
            private Vector2 padding1;
            public Vector4 Eye;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PerDrawExtra
        {
            public Matrix WorldInverse;
            public Matrix WorldInverseTranspose;
            public Matrix WorldView;
            public Matrix WorldViewInverse;
            public Matrix WorldViewProjection;
            public Vector3 WorldScale;
            private float padding1;
            public Vector4 EyeMS;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PerViewCamera
        {
            public float NearClipPlane;
            public float FarClipPlane;
            public Vector2 ZProjection;

            public Vector2 ViewSize;
            public float AspectRatio;
        }
    }
}
