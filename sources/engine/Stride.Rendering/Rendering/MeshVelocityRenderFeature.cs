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

        public override void PrepareEffectPermutations(RenderDrawContext context)
        {
            using var _ = Profiler.Begin(PrepareEffectPermutationsKey);
            base.PrepareEffectPermutations(context);

            var rootEffectRenderFeature = ((RootEffectRenderFeature)RootRenderFeature);
            var renderEffects = RootRenderFeature.RenderData.GetData(((RootEffectRenderFeature)RootRenderFeature).RenderEffectKey);
            int effectSlotCount = ((RootEffectRenderFeature)RootRenderFeature).EffectPermutationSlotCount;

            Dispatcher.ForBatched(RootRenderFeature.ObjectNodeReferences.Count, new PrepareEffectPermutationJob
            {
                RootRenderFeature = RootRenderFeature,
                RenderSystem = RenderSystem,
                RootEffectRenderFeature = rootEffectRenderFeature,
                RenderEffects = renderEffects,
                EffectSlotCount = effectSlotCount,
            });
        }

        private readonly struct PrepareEffectPermutationJob : Dispatcher.IBatchJob
        {
            public required RootRenderFeature RootRenderFeature { get; init; }
            public required RenderSystem RenderSystem { get; init; }
            public required RootEffectRenderFeature RootEffectRenderFeature { get; init; }
            public required StaticObjectPropertyData<RenderEffect> RenderEffects { get; init; }
            public required int EffectSlotCount { get; init; }

            public void Process(int start, int endExclusive)
            {
                foreach (var objectNodeReference in RootRenderFeature.ObjectNodeReferences.Items.AsSpan(start..endExclusive))
                {
                    var objectNode = RootRenderFeature.GetObjectNode(objectNodeReference);
                    var renderMesh = (RenderMesh)objectNode.RenderObject;
                    var staticObjectNode = renderMesh.StaticObjectNode;

                    renderMesh.ActiveMeshDraw = renderMesh.Mesh.Draw;

                    foreach (var stage in RenderSystem.RenderStages)
                    {
                        if (stage == null)
                            continue;
                        var effectSlot = RootEffectRenderFeature.GetEffectPermutationSlot(stage);
                        var staticEffectObjectNode = staticObjectNode * EffectSlotCount + effectSlot.Index;
                        var renderEffect = RenderEffects[staticEffectObjectNode];

                        if (renderEffect != null)
                        {
                            renderEffect.EffectValidator.ValidateParameter(StrideEffectBaseKeys.ComputeVelocityShader, new ShaderClassSource("MeshVelocity"));
                        }
                    }
                }
            }
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

                Dispatcher.ForBatched(viewFeature.ViewObjectNodes.Count, new PreviousViewJob
                {
                    RootRenderFeature = RootRenderFeature,
                    ViewObjectNodes = viewFeature.ViewObjectNodes,
                    RenderModelObjectInfoData = renderModelObjectInfoData,
                    PreviousTransformationViewInfoData = previousTransformationViewInfoData,
                    ViewData = viewData,
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
            var renderNodes = ((RootEffectRenderFeature)RootRenderFeature).RenderNodes;
            Dispatcher.ForBatched(renderNodes.Count, new PreviousPerDrawJob
            {
                RenderNodes = renderNodes,
                PreviousWorldViewProjectionOffset = previousWorldViewProjection,
                RenderModelObjectInfoData = renderModelObjectInfoData,
                PreviousTransformationViewInfoData = previousTransformationViewInfoData,
                PreviousTransformationInfoData = previousTransformationInfoData,
            });
        }

        private readonly struct PreviousViewJob : Dispatcher.IBatchJob
        {
            public required RootRenderFeature RootRenderFeature { get; init; }
            public required ConcurrentCollector<ViewObjectNodeReference> ViewObjectNodes { get; init; }
            public required ObjectPropertyData<TransformRenderFeature.RenderModelFrameInfo> RenderModelObjectInfoData { get; init; }
            public required ViewObjectPropertyData<PreviousObjectViewInfo> PreviousTransformationViewInfoData { get; init; }
            public required RenderViewData ViewData { get; init; }

            public void Process(int start, int endExclusive)
            {
                foreach (var renderPerViewNodeReference in ViewObjectNodes.Items.AsSpan(start..endExclusive))
                {
                    var renderPerViewNode = RootRenderFeature.GetViewObjectNode(renderPerViewNodeReference);
                    var renderModelFrameInfo = RenderModelObjectInfoData[renderPerViewNode.ObjectNode];

                    Matrix previousViewProjection = ViewData.PreviousViewProjection;

                    Matrix previousWorldViewProjection;
                    Matrix.Multiply(ref renderModelFrameInfo.World, ref previousViewProjection, out previousWorldViewProjection);

                    PreviousTransformationViewInfoData[renderPerViewNodeReference] = new PreviousObjectViewInfo
                    {
                        WorldViewProjection = previousWorldViewProjection,
                    };
                }
            }
        }

        private readonly struct PreviousPerDrawJob : Dispatcher.IBatchJob
        {
            public required ConcurrentCollector<RenderNode> RenderNodes { get; init; }
            public required ConstantBufferOffsetReference PreviousWorldViewProjectionOffset { get; init; }
            public required ObjectPropertyData<TransformRenderFeature.RenderModelFrameInfo> RenderModelObjectInfoData { get; init; }
            public required ViewObjectPropertyData<PreviousObjectViewInfo> PreviousTransformationViewInfoData { get; init; }
            public required StaticObjectPropertyData<StaticObjectInfo> PreviousTransformationInfoData { get; init; }

            public unsafe void Process(int start, int endExclusive)
            {
                foreach (ref var renderNode in RenderNodes.Items.AsSpan(start..endExclusive))
                {
                    var perDrawLayout = renderNode.RenderEffect.Reflection?.PerDrawLayout;
                    if (perDrawLayout == null)
                        continue;

                    var previousWvpOffset = perDrawLayout.GetConstantBufferOffset(PreviousWorldViewProjectionOffset);
                    if (previousWvpOffset == -1)
                        continue;

                    var mappedCB = renderNode.Resources.ConstantBuffer.Data;
                    var previousPerDraw = (PreviousPerDraw*)((byte*)mappedCB + previousWvpOffset);

                    var renderModelFrameInfo = RenderModelObjectInfoData[renderNode.RenderObject.ObjectNode];
                    var renderModelPreviousFrameInfo = PreviousTransformationViewInfoData[renderNode.ViewObjectNode];

                    // Shift current world transform into previous transform
                    PreviousTransformationInfoData[renderNode.RenderObject.StaticObjectNode] = new StaticObjectInfo
                    {
                        World = renderModelFrameInfo.World,
                    };

                    previousPerDraw->PreviousWorldViewProjection = renderModelPreviousFrameInfo.WorldViewProjection;
                }
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
