// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Rendering.Compositing;
using Stride.Shaders;

namespace Stride.Rendering
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
        
        private static readonly ProfilingKey PrepareEffectPermutationsKey = new ProfilingKey("MeshVelocityRenderFeature.PrepareEffectPermutations");

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

        private readonly record struct PrepareEffectPermutationParameters
        {
            public required RootRenderFeature RootRenderFeature { get; init; }
            public required RenderSystem RenderSystem { get; init; }
            public required RootEffectRenderFeature RootEffectRenderFeature { get; init; }
            public required StaticObjectPropertyData<RenderEffect> RenderEffects { get; init; }
            public required int EffectSlotCount { get; init; }
        }

        public override void PrepareEffectPermutations(RenderDrawContext context)
        {
            using var _ = Profiler.Begin(PrepareEffectPermutationsKey);
            base.PrepareEffectPermutations(context);

            var rootEffectRenderFeature = ((RootEffectRenderFeature)RootRenderFeature);
            var renderEffects = RootRenderFeature.RenderData.GetData(((RootEffectRenderFeature)RootRenderFeature).RenderEffectKey);
            int effectSlotCount = ((RootEffectRenderFeature)RootRenderFeature).EffectPermutationSlotCount;

            var parameters = new PrepareEffectPermutationParameters
            {
                RootRenderFeature = RootRenderFeature,
                RenderSystem = RenderSystem,
                RootEffectRenderFeature = rootEffectRenderFeature,
                RenderEffects = renderEffects,
                EffectSlotCount = effectSlotCount,
            };
            unsafe
            {
                Dispatcher.ForEach<ObjectNodeReference, ConcurrentCollector<ObjectNodeReference>, PrepareEffectPermutationParameters>(RootRenderFeature.ObjectNodeReferences, in parameters, &ProcessObjectNode);
            }

            static void ProcessObjectNode(ref readonly PrepareEffectPermutationParameters parameters, ObjectNodeReference objectNodeReference)
            {
                var objectNode = parameters.RootRenderFeature.GetObjectNode(objectNodeReference);
                var renderMesh = (RenderMesh)objectNode.RenderObject;
                var staticObjectNode = renderMesh.StaticObjectNode;

                renderMesh.ActiveMeshDraw = renderMesh.Mesh.Draw;

                foreach (var stage in parameters.RenderSystem.RenderStages)
                {
                    if (stage == null)
                        continue;
                    var effectSlot = parameters.RootEffectRenderFeature.GetEffectPermutationSlot(stage);
                    var staticEffectObjectNode = staticObjectNode * parameters.EffectSlotCount + effectSlot.Index;
                    var renderEffect = parameters.RenderEffects[staticEffectObjectNode];

                    if (renderEffect != null)
                    {
                        renderEffect.EffectValidator.ValidateParameter(StrideEffectBaseKeys.ComputeVelocityShader, new ShaderClassSource("MeshVelocity"));
                    }
                }
            }
        }

        private readonly record struct PreviousViewParameters
        {
            public required RootRenderFeature RootRenderFeature { get; init; }
            public required ObjectPropertyData<TransformRenderFeature.RenderModelFrameInfo> RenderModelObjectInfoData { get; init; }
            public required ViewObjectPropertyData<PreviousObjectViewInfo> PreviousTransformationViewInfoData { get; init; }
            public required RenderViewData ViewData { get; init; }
        }

        private readonly record struct PreviousPerDrawParameters
        {
            public required ConcurrentCollector<RenderNode> RenderNodes { get; init; }
            public required ConstantBufferOffsetReference PreviousWorldViewProjectionOffset { get; init; }
            public required ObjectPropertyData<TransformRenderFeature.RenderModelFrameInfo> RenderModelObjectInfoData { get; init; }
            public required ViewObjectPropertyData<PreviousObjectViewInfo> PreviousTransformationViewInfoData { get; init; }
            public required StaticObjectPropertyData<StaticObjectInfo> PreviousTransformationInfoData { get; init; }
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

                var parameters = new PreviousViewParameters
                {
                    RootRenderFeature = RootRenderFeature,
                    RenderModelObjectInfoData = renderModelObjectInfoData,
                    PreviousTransformationViewInfoData = previousTransformationViewInfoData,
                    ViewData = viewData,
                };
                Dispatcher.ForEach<ViewObjectNodeReference, ConcurrentCollector<ViewObjectNodeReference>, PreviousViewParameters>(viewFeature.ViewObjectNodes, in parameters, &ProcessViewObjectNode);

                static void ProcessViewObjectNode(ref readonly PreviousViewParameters parameters, ViewObjectNodeReference renderPerViewNodeReference)
                {
                    var renderPerViewNode = parameters.RootRenderFeature.GetViewObjectNode(renderPerViewNodeReference);
                    var renderModelFrameInfo = parameters.RenderModelObjectInfoData[renderPerViewNode.ObjectNode];

                    Matrix previousViewProjection = parameters.ViewData.PreviousViewProjection;

                    Matrix previousWorldViewProjection;
                    Matrix.Multiply(ref renderModelFrameInfo.World, ref previousViewProjection, out previousWorldViewProjection);

                    parameters.PreviousTransformationViewInfoData[renderPerViewNodeReference] = new PreviousObjectViewInfo
                    {
                        WorldViewProjection = previousWorldViewProjection,
                    };
                }

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
            var perDrawParameters = new PreviousPerDrawParameters
            {
                RenderNodes = ((RootEffectRenderFeature)RootRenderFeature).RenderNodes,
                PreviousWorldViewProjectionOffset = previousWorldViewProjection,
                RenderModelObjectInfoData = renderModelObjectInfoData,
                PreviousTransformationViewInfoData = previousTransformationViewInfoData,
                PreviousTransformationInfoData = previousTransformationInfoData,
            };
            Dispatcher.For(0, perDrawParameters.RenderNodes.Count, in perDrawParameters, &UpdatePerDraw);

            static void UpdatePerDraw(ref readonly PreviousPerDrawParameters parameters, int index)
            {
                ref var renderNode = ref parameters.RenderNodes.Items[index];
                var perDrawLayout = renderNode.RenderEffect.Reflection?.PerDrawLayout;
                if (perDrawLayout == null)
                    return;

                var previousWvpOffset = perDrawLayout.GetConstantBufferOffset(parameters.PreviousWorldViewProjectionOffset);
                if (previousWvpOffset == -1)
                    return;

                var mappedCB = renderNode.Resources.ConstantBuffer.Data;
                var previousPerDraw = (PreviousPerDraw*)((byte*)mappedCB + previousWvpOffset);

                var renderModelFrameInfo = parameters.RenderModelObjectInfoData[renderNode.RenderObject.ObjectNode];
                var renderModelPreviousFrameInfo = parameters.PreviousTransformationViewInfoData[renderNode.ViewObjectNode];

                // Shift current world transform into previous transform
                parameters.PreviousTransformationInfoData[renderNode.RenderObject.StaticObjectNode] = new StaticObjectInfo
                {
                    World = renderModelFrameInfo.World,
                };

                previousPerDraw->PreviousWorldViewProjection = renderModelPreviousFrameInfo.WorldViewProjection;
            }
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
