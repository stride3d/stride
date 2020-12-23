// Copyright (c) Stride contributors (https://stride3d.net) and Tebjan Halm
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Graphics;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Rendering
{
    public struct InstancingData 
    {
        public int InstanceCount;
        public int ModelTransformUsage;

        // Data
        public Matrix[] WorldMatrices;
        public Matrix[] WorldInverseMatrices;

        // GPU buffers
        public bool BuffersManagedByUser;
        public Buffer InstanceWorldBuffer;
        public Buffer InstanceWorldInverseBuffer;
    }

    public class InstancingRenderFeature : SubRenderFeature
    {
        [DataMemberIgnore]
        public static readonly PropertyKey<Dictionary<RenderModel, RenderInstancing>> ModelToInstancingMap = new PropertyKey<Dictionary<RenderModel, RenderInstancing>>("InstancingRenderFeature.ModelToInstancingMap", typeof(InstancingRenderFeature));

        private StaticObjectPropertyKey<InstancingData> renderObjectInstancingDataInfoKey;

        private StaticObjectPropertyKey<RenderEffect> renderEffectKey;
        private LogicalGroupReference instancingResourceGroupKey;

        private Dictionary<Buffer, bool> bufferUploaded = new Dictionary<Buffer, bool>();

        /// <inheritdoc/>
        protected override void InitializeCore()
        {
            renderObjectInstancingDataInfoKey = RootRenderFeature.RenderData.CreateStaticObjectKey<InstancingData>();
            renderEffectKey = ((RootEffectRenderFeature)RootRenderFeature).RenderEffectKey;
            instancingResourceGroupKey = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawLogicalGroup("Instancing");
        }

        /// <inheritdoc/>
        public override void Extract()
        {
            if (!Context.VisibilityGroup.Tags.TryGetValue(ModelToInstancingMap, out var modelToInstancingMap))
                return;

            var renderObjectInstancingData = RootRenderFeature.RenderData.GetData(renderObjectInstancingDataInfoKey);
            bufferUploaded.Clear();

            foreach (var objectNodeReference in RootRenderFeature.ObjectNodeReferences)
            {
                var objectNode = RootRenderFeature.GetObjectNode(objectNodeReference);
                var renderMesh = objectNode.RenderObject as RenderMesh;
                if (renderMesh == null)
                    continue;

                var renderModel = renderMesh.RenderModel;
                if (renderModel == null)
                    continue;

                if (!modelToInstancingMap.TryGetValue(renderModel, out var renderInstancing))
                {
                    renderMesh.InstanceCount = 0;
                    continue;
                }

                ref var instancingData = ref renderObjectInstancingData[renderMesh.StaticObjectNode];

                // Instancing data
                if (renderInstancing.InstanceCount > 0)
                {
                    instancingData.InstanceCount = renderInstancing.InstanceCount;
                    instancingData.ModelTransformUsage = renderInstancing.ModelTransformUsage;
                    instancingData.BuffersManagedByUser = renderInstancing.BuffersManagedByUser;
                    instancingData.WorldMatrices = renderInstancing.WorldMatrices;
                    instancingData.WorldInverseMatrices = renderInstancing.WorldInverseMatrices;
                    instancingData.InstanceWorldBuffer = renderInstancing.InstanceWorldBuffer;
                    instancingData.InstanceWorldInverseBuffer = renderInstancing.InstanceWorldInverseBuffer;

                    bufferUploaded[renderInstancing.InstanceWorldBuffer] = renderInstancing.BuffersManagedByUser;
                }
                else
                {
                    instancingData.InstanceCount = 0;
                } 
                
                // Update instance count on mesh
                renderMesh.InstanceCount = instancingData.InstanceCount;
            }
        }

        private static unsafe void SetBufferData<TData>(CommandList commandList, Buffer buffer, TData[] fromData, int elementCount) where TData : struct
        {
            var dataPointer = new DataPointer(Interop.Fixed(fromData), Math.Min(elementCount, fromData.Length) * Utilities.SizeOf<TData>());
            buffer.SetData(commandList, dataPointer);
        }

        public override void PrepareEffectPermutations(RenderDrawContext context)
        {
            var renderObjectInstancingData = RootRenderFeature.RenderData.GetData(renderObjectInstancingDataInfoKey);

            var renderEffects = RootRenderFeature.RenderData.GetData(renderEffectKey);
            int effectSlotCount = ((RootEffectRenderFeature)RootRenderFeature).EffectPermutationSlotCount;

            Dispatcher.ForEach(RootRenderFeature.RenderObjects, renderObject =>
            {
                var renderMesh = (RenderMesh)renderObject;

                var staticObjectNode = renderMesh.StaticObjectNode;
                var instancingData = renderObjectInstancingData[staticObjectNode];

                for (int i = 0; i < effectSlotCount; i++)
                {
                    var staticEffectObjectNode = staticObjectNode * effectSlotCount + i;
                    var renderEffect = renderEffects[staticEffectObjectNode];

                    // Skip effects not used during this frame
                    if (renderEffect == null || !renderEffect.IsUsedDuringThisFrame(RenderSystem))
                        continue;

                    if (instancingData.InstanceCount > 0)
                    {
                        renderEffect.EffectValidator.ValidateParameter(StrideEffectBaseKeys.ModelTransformUsage, instancingData.ModelTransformUsage);
                        renderEffect.EffectValidator.ValidateParameter(StrideEffectBaseKeys.HasInstancing, instancingData.InstanceCount > 0); 
                    }
                }
            });
        }

        /// <inheritdoc/>
        public unsafe override void Prepare(RenderDrawContext context)
        {
            var renderObjectInstancingData = RootRenderFeature.RenderData.GetData(renderObjectInstancingDataInfoKey);

            // Upload buffers data per render object
            foreach (var renderObject in RootRenderFeature.RenderObjects)
            {
                if (instancingResourceGroupKey.Index < 0)
                    continue;

                var renderMesh = renderObject as RenderMesh;
                if (renderMesh == null)
                    continue;

                ref var instancingData = ref renderObjectInstancingData[renderMesh.StaticObjectNode];

                if (instancingData.InstanceCount <= 0 || instancingData.BuffersManagedByUser || !bufferUploaded.TryGetValue(instancingData.InstanceWorldBuffer, out var uploaded) || uploaded)
                    continue;

                SetBufferData(context.CommandList, instancingData.InstanceWorldBuffer, instancingData.WorldMatrices, instancingData.InstanceCount);
                SetBufferData(context.CommandList, instancingData.InstanceWorldInverseBuffer, instancingData.WorldInverseMatrices, instancingData.InstanceCount);

                bufferUploaded[instancingData.InstanceWorldBuffer] = true;
            }

            // Assign buffers to render node
            foreach (var renderNode in ((RootEffectRenderFeature)RootRenderFeature).RenderNodes)
            {
                var perDrawLayout = renderNode.RenderEffect.Reflection?.PerDrawLayout;
                if (perDrawLayout == null)
                    continue;

                if (instancingResourceGroupKey.Index < 0)
                    continue;

                var group = perDrawLayout.GetLogicalGroup(instancingResourceGroupKey);
                if (group.DescriptorEntryStart == -1)
                    continue;
                
                var renderMesh = renderNode.RenderObject as RenderMesh;
                if (renderMesh == null)
                    continue;

                ref var instancingData = ref renderObjectInstancingData[renderMesh.StaticObjectNode];

                if (instancingData.InstanceCount > 0)
                { 
                    renderNode.Resources.DescriptorSet.SetShaderResourceView(group.DescriptorEntryStart, instancingData.InstanceWorldBuffer);
                    renderNode.Resources.DescriptorSet.SetShaderResourceView(group.DescriptorEntryStart + 1, instancingData.InstanceWorldInverseBuffer);
                }
            }
        }
    }
}
