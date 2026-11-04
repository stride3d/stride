// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Rendering.Materials;

namespace Stride.Rendering
{
    /// <summary>
    /// Computes and uploads skinning info, and binds the morph targets applied in the vertex shader.
    /// </summary>
    public class SkinningRenderFeature : SubRenderFeature
    {
        private StaticObjectPropertyKey<RenderEffect> renderEffectKey;
        private StaticObjectPropertyKey<SkinningInfo> skinningInfoKey;
        private ObjectPropertyKey<Matrix[]> renderModelObjectInfoKey;

        private ConstantBufferOffsetReference blendMatrices;
        private LogicalGroupReference morphGroup;
        private ConstantBufferOffsetReference morphTargetCount;

        private static readonly ProfilingKey PrepareEffectPermutationsKey = new ProfilingKey("SkinningRenderFeature.PrepareEffectPermutations");

        // Good number for low profiles?
        public int MaxBones { get; set; } = 56;

        private struct SkinningInfo
        {
            public ParameterCollection Parameters;
            public int PermutationCounter;

            public bool HasSkinningPosition;
            public bool HasSkinningNormal;
            public bool HasSkinningTangent;
        }

        /// <inheritdoc/>
        protected override void InitializeCore()
        {
            renderModelObjectInfoKey = RootRenderFeature.RenderData.CreateObjectKey<Matrix[]>();
            skinningInfoKey = RootRenderFeature.RenderData.CreateStaticObjectKey<SkinningInfo>();
            renderEffectKey = ((RootEffectRenderFeature)RootRenderFeature).RenderEffectKey;

            blendMatrices = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawCBufferOffsetSlot(TransformationSkinningKeys.BlendMatrixArray.Name);
            morphGroup = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawLogicalGroup("Morph");
            morphTargetCount = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawCBufferOffsetSlot("TransformationMorph.MorphTargetCount");
        }

        private readonly struct PrepareEffectPermutationJob : Dispatcher.IBatchJob
        {
            public required RootRenderFeature RootRenderFeature { get; init; }
            public required RenderSystem RenderSystem { get; init; }
            public required StaticObjectPropertyData<SkinningInfo> SkinningInfos { get; init; }
            public required StaticObjectPropertyData<RenderEffect> RenderEffects { get; init; }
            public required int EffectSlotCount { get; init; }
            public required int MaxBones { get; init; }

            public void Process(int start, int endExclusive)
            {
                foreach (var objectNodeReference in RootRenderFeature.ObjectNodeReferences.Items.AsSpan(start..endExclusive))
                {
                    var objectNode = RootRenderFeature.GetObjectNode(objectNodeReference);
                    var renderMesh = (RenderMesh)objectNode.RenderObject;
                    var staticObjectNode = renderMesh.StaticObjectNode;

                    ref var skinningInfo = ref SkinningInfos[staticObjectNode];
                    var parameters = renderMesh.Mesh.Parameters;
                    if (parameters != skinningInfo.Parameters || parameters.PermutationCounter != skinningInfo.PermutationCounter)
                    {
                        skinningInfo.Parameters = parameters;
                        skinningInfo.PermutationCounter = parameters.PermutationCounter;

                        skinningInfo.HasSkinningPosition = parameters.Get(MaterialKeys.HasSkinningPosition);
                        skinningInfo.HasSkinningNormal = parameters.Get(MaterialKeys.HasSkinningNormal);
                        skinningInfo.HasSkinningTangent = parameters.Get(MaterialKeys.HasSkinningTangent);
                    }

                    for (int i = 0; i < EffectSlotCount; ++i)
                    {
                        var staticEffectObjectNode = staticObjectNode * EffectSlotCount + i;
                        var renderEffect = RenderEffects[staticEffectObjectNode];

                        // Skip effects not used during this frame
                        if (renderEffect == null || !renderEffect.IsUsedDuringThisFrame(RenderSystem))
                            continue;

                        if (renderMesh.Mesh.Skinning != null)
                        {
                            renderEffect.EffectValidator.ValidateParameter(MaterialKeys.HasSkinningPosition, skinningInfo.HasSkinningPosition);
                            renderEffect.EffectValidator.ValidateParameter(MaterialKeys.HasSkinningNormal, skinningInfo.HasSkinningNormal);
                            renderEffect.EffectValidator.ValidateParameter(MaterialKeys.HasSkinningTangent, skinningInfo.HasSkinningTangent);

                            var skinningBones = Math.Max(MaxBones, renderMesh.Mesh.Skinning.Bones.Length);
                            renderEffect.EffectValidator.ValidateParameter(MaterialKeys.SkinningMaxBones, skinningBones);
                        }

                        if (renderMesh.MorphBuffers != null)
                            renderEffect.EffectValidator.ValidateParameter(StrideEffectBaseKeys.MorphTargets, renderMesh.MorphBuffers.Flags);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override void PrepareEffectPermutations(RenderDrawContext context)
        {
            using var _ = Profiler.Begin(PrepareEffectPermutationsKey);
            var skinningInfos = RootRenderFeature.RenderData.GetData(skinningInfoKey);

            var renderEffects = RootRenderFeature.RenderData.GetData(renderEffectKey);
            int effectSlotCount = ((RootEffectRenderFeature)RootRenderFeature).EffectPermutationSlotCount;

            Dispatcher.ForBatched(RootRenderFeature.ObjectNodeReferences.Count, new PrepareEffectPermutationJob
            {
                RootRenderFeature = RootRenderFeature,
                RenderSystem = RenderSystem,
                SkinningInfos = skinningInfos,
                RenderEffects = renderEffects,
                EffectSlotCount = effectSlotCount,
                MaxBones = MaxBones,
            });
        }

        private readonly struct ExtractJob : Dispatcher.IBatchJob
        {
            public required RootRenderFeature RootRenderFeature { get; init; }
            public required ObjectPropertyData<Matrix[]> RenderModelObjectInfo { get; init; }

            public void Process(int start, int endExclusive)
            {
                foreach (var objectNodeReference in RootRenderFeature.ObjectNodeReferences.Items.AsSpan(start..endExclusive))
                {
                    var objectNode = RootRenderFeature.GetObjectNode(objectNodeReference);
                    var renderMesh = (RenderMesh)objectNode.RenderObject;

                    // TODO GRAPHICS REFACTOR: Extract copy of matrices
                    RenderModelObjectInfo[objectNodeReference] = renderMesh.BlendMatrices;
                }
            }
        }

        /// <inheritdoc/>
        public override void Extract()
        {
            var renderModelObjectInfo = RootRenderFeature.RenderData.GetData(renderModelObjectInfoKey);

            Dispatcher.ForBatched(RootRenderFeature.ObjectNodeReferences.Count, new ExtractJob
            {
                RootRenderFeature = RootRenderFeature,
                RenderModelObjectInfo = renderModelObjectInfo,
            });
        }

        /// <inheritdoc/>
        public override unsafe void Prepare(RenderDrawContext context)
        {
            var renderModelObjectInfoData = RootRenderFeature.RenderData.GetData(renderModelObjectInfoKey);
            Dispatcher.ForBatched(RootRenderFeature.RenderNodes.Count, (this, renderModelObjectInfoData), &ForBatchedAction);

            static void ForBatchedAction((SkinningRenderFeature, ObjectPropertyData<Matrix[]>) state, int from, int toExclusive)
            {
                var (@this, renderModelObjectInfoData) = state;
                for (int i = from; i < toExclusive; i++)
                {
                    var renderNode = @this.RootRenderFeature.RenderNodes[i];
                    var perDrawLayout = renderNode.RenderEffect.Reflection?.PerDrawLayout;
                    if (perDrawLayout == null)
                        continue;

                    if (@this.morphGroup.Index >= 0 && ((RenderMesh)renderNode.RenderObject).MorphBuffers is { } morphBuffers)
                    {
                        var group = perDrawLayout.GetLogicalGroup(@this.morphGroup);
                        if (group.DescriptorEntryStart != -1)
                        {
                            renderNode.Resources.DescriptorSet.SetShaderResourceView(group.DescriptorEntryStart, morphBuffers.Entries);
                            renderNode.Resources.DescriptorSet.SetShaderResourceView(group.DescriptorEntryStart + 1, morphBuffers.VertexOffsets);
                            renderNode.Resources.DescriptorSet.SetShaderResourceView(group.DescriptorEntryStart + 2, morphBuffers.Weights);
                        }
                        var targetCountOffset = perDrawLayout.GetConstantBufferOffset(@this.morphTargetCount);
                        if (targetCountOffset != -1)
                            *(uint*)((byte*)renderNode.Resources.ConstantBuffer.Data + targetCountOffset) = (uint)morphBuffers.TargetCount;
                    }

                    var blendMatricesOffset = perDrawLayout.GetConstantBufferOffset(@this.blendMatrices);
                    if (blendMatricesOffset == -1)
                        continue;

                    var renderModelObjectInfo = renderModelObjectInfoData[renderNode.RenderObject.ObjectNode];
                    if (renderModelObjectInfo == null)
                        continue;

                    var mappedCB = (byte*)renderNode.Resources.ConstantBuffer.Data + blendMatricesOffset;

                    fixed (Matrix* blendMatricesPtr = renderModelObjectInfo)
                    {
                        MemoryUtilities.CopyWithAlignmentFallback(mappedCB, blendMatricesPtr, (uint)renderModelObjectInfo.Length * (uint)sizeof(Matrix));
                    }
                }
            }
        }
    }
}
