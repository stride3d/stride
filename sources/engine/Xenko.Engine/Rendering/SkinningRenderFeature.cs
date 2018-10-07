// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Core.Threading;
using Xenko.Rendering.Materials;

namespace Xenko.Rendering
{
    /// <summary>
    /// Computes and uploads skinning info.
    /// </summary>
    public class SkinningRenderFeature : SubRenderFeature
    {
        private StaticObjectPropertyKey<RenderEffect> renderEffectKey;
        private StaticObjectPropertyKey<SkinningInfo> skinningInfoKey;
        private ObjectPropertyKey<Matrix[]> renderModelObjectInfoKey;

        private ConstantBufferOffsetReference blendMatrices;

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
        }

        /// <param name="context"></param>
        /// <inheritdoc/>
        public override void PrepareEffectPermutations(RenderDrawContext context)
        {
            var skinningInfos = RootRenderFeature.RenderData.GetData(skinningInfoKey);

            var renderEffects = RootRenderFeature.RenderData.GetData(renderEffectKey);
            int effectSlotCount = ((RootEffectRenderFeature)RootRenderFeature).EffectPermutationSlotCount;

            //foreach (var objectNodeReference in RootRenderFeature.ObjectNodeReferences)
            Dispatcher.ForEach(((RootEffectRenderFeature)RootRenderFeature).ObjectNodeReferences, objectNodeReference =>
            {
                var objectNode = RootRenderFeature.GetObjectNode(objectNodeReference);
                var renderMesh = (RenderMesh)objectNode.RenderObject;
                var staticObjectNode = renderMesh.StaticObjectNode;

                ref var skinningInfo = ref skinningInfos[staticObjectNode];
                var parameters = renderMesh.Mesh.Parameters;
                if (parameters != skinningInfo.Parameters || parameters.PermutationCounter != skinningInfo.PermutationCounter)
                {
                    skinningInfo.Parameters = parameters;
                    skinningInfo.PermutationCounter = parameters.PermutationCounter;

                    skinningInfo.HasSkinningPosition = parameters.Get(MaterialKeys.HasSkinningPosition);
                    skinningInfo.HasSkinningNormal = parameters.Get(MaterialKeys.HasSkinningNormal);
                    skinningInfo.HasSkinningTangent = parameters.Get(MaterialKeys.HasSkinningTangent);
                }

                for (int i = 0; i < effectSlotCount; ++i)
                {
                    var staticEffectObjectNode = staticObjectNode * effectSlotCount + i;
                    var renderEffect = renderEffects[staticEffectObjectNode];

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
                }
            });
        }

        /// <inheritdoc/>
        public override void Extract()
        {
            var renderModelObjectInfo = RootRenderFeature.RenderData.GetData(renderModelObjectInfoKey);

            Dispatcher.ForEach(RootRenderFeature.ObjectNodeReferences, objectNodeReference =>
            {
                var objectNode = RootRenderFeature.GetObjectNode(objectNodeReference);
                var renderMesh = (RenderMesh)objectNode.RenderObject;

                // TODO GRAPHICS REFACTOR: Extract copy of matrices
                renderModelObjectInfo[objectNodeReference] = renderMesh.BlendMatrices;
            });
        }

        /// <inheritdoc/>
        public override unsafe void Prepare(RenderDrawContext context)
        {
            var renderModelObjectInfoData = RootRenderFeature.RenderData.GetData(renderModelObjectInfoKey);

            Dispatcher.ForEach(((RootEffectRenderFeature)RootRenderFeature).RenderNodes, (ref RenderNode renderNode) =>
            {
                var perDrawLayout = renderNode.RenderEffect.Reflection.PerDrawLayout;
                if (perDrawLayout == null)
                    return;

                var blendMatricesOffset = perDrawLayout.GetConstantBufferOffset(blendMatrices);
                if (blendMatricesOffset == -1)
                    return;

                var renderModelObjectInfo = renderModelObjectInfoData[renderNode.RenderObject.ObjectNode];
                if (renderModelObjectInfo == null)
                    return;

                var mappedCB = renderNode.Resources.ConstantBuffer.Data + blendMatricesOffset;

                fixed (Matrix* blendMatricesPtr = &renderModelObjectInfo[0])
                {
                    Utilities.CopyMemory(mappedCB, new IntPtr(blendMatricesPtr), renderModelObjectInfo.Length * sizeof(Matrix));
                }
            });
        }
    }
}
