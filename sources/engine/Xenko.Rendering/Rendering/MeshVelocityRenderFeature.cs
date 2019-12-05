// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Core.Threading;
using Xenko.Rendering.Compositing;
using Xenko.Shaders;

namespace Xenko.Rendering
{
    /// <summary>
    /// Output per-pixel motion vectors to a separate render target
    /// </summary>
    [DataContract(DefaultMemberMode = DataMemberMode.Never)]
    public class MeshVelocityRenderFeature : SubRenderFeature
    {
        public TransformRenderFeature TransformRenderFeature;

        private StaticObjectPropertyKey<StaticObjectInfo> previousTransformationInfoKey;
        private ViewObjectPropertyKey<PreviousObjectViewInfo> previousTransformationViewInfoKey;

        private ObjectPropertyKey<TransformRenderFeature.RenderModelFrameInfo> renderModelObjectInfoKey;

        private ConstantBufferOffsetReference previousWorldViewProjection;

        private Dictionary<RenderView, RenderViewData> renderViewDatas = new Dictionary<RenderView, RenderViewData>();
        private int usageCounter = 0;

        private HashSet<RenderView> updatedViews = new HashSet<RenderView>();

        protected override void InitializeCore()
        {
            if (TransformRenderFeature == null)
            {
                TransformRenderFeature = ((MeshRenderFeature)RootRenderFeature).RenderFeatures.OfType<TransformRenderFeature>().FirstOrDefault();
                if (TransformRenderFeature == null)
                    throw new ArgumentNullException(nameof(TransformRenderFeature));
            }

            previousTransformationInfoKey = RootRenderFeature.RenderData.CreateStaticObjectKey<StaticObjectInfo>();
            previousTransformationViewInfoKey = RootRenderFeature.RenderData.CreateViewObjectKey<PreviousObjectViewInfo>();
            renderModelObjectInfoKey = TransformRenderFeature.RenderModelObjectInfoKey;

            previousWorldViewProjection = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawCBufferOffsetSlot(MeshVelocityKeys.PreviousWorldViewProjection.Name);
        }

        public override void PrepareEffectPermutations(RenderDrawContext context)
        {
            base.PrepareEffectPermutations(context);

            var rootEffectRenderFeature = ((RootEffectRenderFeature)RootRenderFeature);
            var renderEffects = RootRenderFeature.RenderData.GetData(((RootEffectRenderFeature)RootRenderFeature).RenderEffectKey);
            int effectSlotCount = ((RootEffectRenderFeature)RootRenderFeature).EffectPermutationSlotCount;

            Dispatcher.ForEach(RootRenderFeature.ObjectNodeReferences, (RootRenderFeature, RenderSystem, rootEffectRenderFeature, renderEffects, effectSlotCount),
                delegate (ref (RootRenderFeature rootRenderFeature, RenderSystem RenderSystem, RootEffectRenderFeature rootEffectRenderFeature, StaticObjectPropertyData<RenderEffect> renderEffects, int effectSlotCount) p, ObjectNodeReference objectNodeReference)
                {
                    var (rootRenderFeature, renderSystem1, rootEffectRenderFeature1, staticObjectPropertyData, effectSlotCount1) = p;
                    var objectNode = rootRenderFeature.GetObjectNode(objectNodeReference);
                    var renderMesh = (RenderMesh)objectNode.RenderObject;
                    var staticObjectNode = renderMesh.StaticObjectNode;

                    renderMesh.ActiveMeshDraw = renderMesh.Mesh.Draw;

                    foreach (var stage in renderSystem1.RenderStages)
                    {
                        if (stage == null)
                            continue;
                        var effectSlot = rootEffectRenderFeature1.GetEffectPermutationSlot(stage);
                        var staticEffectObjectNode = staticObjectNode * effectSlotCount1 + effectSlot.Index;
                        var renderEffect = staticObjectPropertyData[staticEffectObjectNode];

                        if (renderEffect != null)
                        {
                            renderEffect.EffectValidator.ValidateParameter(XenkoEffectBaseKeys.ComputeVelocityShader, new ShaderClassSource("MeshVelocity"));
                        }
                    }
                });
        }

        public override unsafe void Prepare(RenderDrawContext context)
        {
            var previousTransformationInfoData = RootRenderFeature.RenderData.GetData(previousTransformationInfoKey);
            var previousTransformationViewInfoData = RootRenderFeature.RenderData.GetData(previousTransformationViewInfoKey);
            var renderModelObjectInfoData = RootRenderFeature.RenderData.GetData(renderModelObjectInfoKey);

            // Calculate previous WVP matrix per view and object
            usageCounter++;
            for (int index = 0; index < RenderSystem.Views.Count; index++)
            {
                var view = RenderSystem.Views[index];
                var viewFeature = view.Features[RootRenderFeature.Index];

                bool useView = false;
                foreach (var stage in RenderSystem.RenderStages)
                {
                    foreach (var renderViewStage in view.RenderStages)
                    {
                        if (renderViewStage.Index == stage.Index && stage.OutputValidator?.Find<VelocityTargetSemantic>() >= 0)
                        {
                            useView = true;
                            break;
                        }
                    }
                }
                if (!useView)
                    continue;

                // Cache per-view data locally
                RenderViewData viewData;
                if (!renderViewDatas.TryGetValue(view, out viewData))
                {
                    viewData = new RenderViewData();
                    renderViewDatas.Add(view, viewData);
                }

                Dispatcher.ForEach(viewFeature.ViewObjectNodes, (viewFeature.ViewObjectNodes, RootRenderFeature, previousTransformationViewInfoData, renderModelObjectInfoData, viewData),
                    delegate (ref (ConcurrentCollector<ViewObjectNodeReference> ViewObjectNodes, RootRenderFeature RootRenderFeature, ViewObjectPropertyData<PreviousObjectViewInfo> previousTransformationViewInfoData, ObjectPropertyData<TransformRenderFeature.RenderModelFrameInfo> renderModelObjectInfoData, RenderViewData viewData) p, ViewObjectNodeReference renderPerViewNodeReference)
                    {
                        var renderPerViewNode = p.RootRenderFeature.GetViewObjectNode(renderPerViewNodeReference);
                        var renderModelFrameInfo = p.renderModelObjectInfoData[renderPerViewNode.ObjectNode];

                        Matrix previousViewProjection = p.viewData.PreviousViewProjection;

                        Matrix previousWorldViewProjection;
                        Matrix.Multiply(ref renderModelFrameInfo.World, ref previousViewProjection, out previousWorldViewProjection);

                        p.previousTransformationViewInfoData[renderPerViewNodeReference] = new PreviousObjectViewInfo
                        {
                            WorldViewProjection = previousWorldViewProjection,
                        };
                    });

                // Shift current view projection transform into previous
                viewData.PreviousViewProjection = view.ViewProjection;
                viewData.UsageCounter = usageCounter;
                updatedViews.Add(view);
            }

            foreach (var view in renderViewDatas.Keys.ToArray())
            {
                if (!updatedViews.Contains(view))
                    renderViewDatas.Remove(view);
            }
            updatedViews.Clear();

            // Update cbuffer for previous WVP matrix
            var nodes = ((RootEffectRenderFeature)RootRenderFeature).RenderNodes;
            Dispatcher.ForEach(nodes, (@this: this, previousTransformationInfoData, previousTransformationViewInfoData, renderModelObjectInfoData),
                delegate (ref (MeshVelocityRenderFeature @this, StaticObjectPropertyData<StaticObjectInfo> previousTransformationInfoData, ViewObjectPropertyData<PreviousObjectViewInfo> previousTransformationViewInfoData, ObjectPropertyData<TransformRenderFeature.RenderModelFrameInfo> renderModelObjectInfoData) p, RenderNode renderNode)
            {
                var (meshVelocityRenderFeature, staticObjectPropertyData, viewObjectPropertyData, objectPropertyData) = p;
                var perDrawLayout = renderNode.RenderEffect.Reflection?.PerDrawLayout;
                if (perDrawLayout == null)
                    return;

                var previousWvpOffset = perDrawLayout.GetConstantBufferOffset(meshVelocityRenderFeature.previousWorldViewProjection);
                if (previousWvpOffset == -1)
                    return;

                var mappedCB = renderNode.Resources.ConstantBuffer.Data;
                var previousPerDraw = (PreviousPerDraw*)((byte*)mappedCB + previousWvpOffset);

                var renderModelFrameInfo = objectPropertyData[renderNode.RenderObject.ObjectNode];
                var renderModelPreviousFrameInfo = viewObjectPropertyData[renderNode.ViewObjectNode];

                // Shift current world transform into previous transform
                staticObjectPropertyData[renderNode.RenderObject.StaticObjectNode] = new StaticObjectInfo
                {
                    World = renderModelFrameInfo.World,
                };

                previousPerDraw->PreviousWorldViewProjection = renderModelPreviousFrameInfo.WorldViewProjection;
            });
        }

        internal class RenderViewData
        {
            public Matrix PreviousViewProjection;
            public int UsageCounter;
        }

        internal struct StaticObjectInfo
        {
            public Matrix World;
        }

        internal struct PreviousObjectViewInfo
        {
            public Matrix WorldViewProjection;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PreviousPerDraw
        {
            public Matrix PreviousWorldViewProjection;
        }
    }
}
