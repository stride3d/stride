using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Engine.Rendering
{
    public struct InstancingData 
    {
        public int InstanceCount;

        public Matrix[] WorldMatrices;
        public Matrix[] WorldInverseMatrices;

        // GPU buffers, managed by the render feature
        public bool BuffersManagedByUser;
        public Buffer<Matrix> InstanceWorldBuffer;
        public Buffer<Matrix> InstanceWorldInverseBuffer;
    }

    public class InstancingRenderFeature : SubRenderFeature
    {
        private StaticObjectPropertyKey<InstancingData> renderObjectInstancingDataInfoKey;

        private StaticObjectPropertyKey<RenderEffect> renderEffectKey;
        private LogicalGroupReference instancingResourceGroupKey;

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
            var renderObjectInstancingData = RootRenderFeature.RenderData.GetData(renderObjectInstancingDataInfoKey);

            foreach (var objectNodeReference in RootRenderFeature.ObjectNodeReferences)
            {
                var objectNode = RootRenderFeature.GetObjectNode(objectNodeReference);
                var renderMesh = objectNode.RenderObject as RenderMesh;
                if (renderMesh == null)
                    continue;

                var modelComponent = renderMesh.Source as ModelComponent;
                if (modelComponent == null)
                    continue;

                var instancingComponent = modelComponent.Entity.Get<InstancingComponent>();
                if (instancingComponent == null)
                {
                    renderMesh.InstanceCount = 0;
                    continue;
                }

                ref var instancingData = ref renderObjectInstancingData[renderMesh.StaticObjectNode];

                // Instancing data
                if (instancingComponent.Enabled && instancingComponent.InstanceCount > 0)
                {
                    instancingData.InstanceCount = instancingComponent.InstanceCount;
                    instancingData.WorldMatrices = instancingComponent.WorldMatrices;
                    instancingData.WorldInverseMatrices = instancingComponent.WorldInverseMatrices;

                    if (instancingComponent.InstanceWorldBuffer != null)
                    {
                        instancingData.InstanceWorldBuffer = instancingComponent.InstanceWorldBuffer;
                        instancingData.InstanceWorldInverseBuffer = instancingComponent.InstanceWorldInverseBuffer;
                        instancingData.BuffersManagedByUser = true;
                    }
                    else
                    {
                        if (instancingData.InstanceWorldBuffer == null || instancingData.InstanceWorldBuffer.ElementCount < instancingComponent.InstanceCount)
                        {
                            instancingData.InstanceWorldBuffer?.Dispose();
                            instancingData.InstanceWorldInverseBuffer?.Dispose();

                            instancingData.InstanceWorldBuffer = CreateMatrixBuffer(Context.GraphicsDevice, instancingComponent.InstanceCount);
                            instancingData.InstanceWorldInverseBuffer = CreateMatrixBuffer(Context.GraphicsDevice, instancingComponent.InstanceCount);
                        }

                        instancingData.BuffersManagedByUser = false;
                    }
                }
                else
                {
                    instancingData.InstanceCount = 0;
                }

                // Update instance count on mesh
                renderMesh.InstanceCount = instancingData.InstanceCount;
            }
        }

        private static Buffer<Matrix> CreateMatrixBuffer(GraphicsDevice graphicsDevice, int elementCount)
        {
            return Buffer.New<Matrix>(graphicsDevice, elementCount, BufferFlags.ShaderResource | BufferFlags.StructuredBuffer, GraphicsResourceUsage.Dynamic);
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

                    if (renderEffect != null)
                    {
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

                var instancingData = renderObjectInstancingData[renderMesh.StaticObjectNode];

                if (instancingData.InstanceCount > 0)
                {
                    if (!instancingData.BuffersManagedByUser)
                    {
                        instancingData.InstanceWorldBuffer.SetData(context.CommandList, instancingData.WorldMatrices);
                        instancingData.InstanceWorldInverseBuffer.SetData(context.CommandList, instancingData.WorldInverseMatrices);
                    }
                }
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

                var instancingData = renderObjectInstancingData[renderMesh.StaticObjectNode];

                if (instancingData.InstanceCount > 0)
                { 
                    renderNode.Resources.DescriptorSet.SetShaderResourceView(group.DescriptorEntryStart, instancingData.InstanceWorldBuffer);
                    renderNode.Resources.DescriptorSet.SetShaderResourceView(group.DescriptorEntryStart + 1, instancingData.InstanceWorldInverseBuffer);
                }
            }
        }
    }
}
