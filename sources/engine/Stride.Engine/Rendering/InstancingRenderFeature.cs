using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core;
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

                if (instancingComponent.Type is InstancingManyBase instancingManyBase)
                {
                    // Instancing data
                    if (instancingComponent.Enabled && instancingManyBase.InstanceCount > 0)
                    {
                        instancingData.InstanceCount = instancingManyBase.InstanceCount;

                        if (instancingManyBase is InstancingUserBuffer instancingUserBuffer)
                        {
                            instancingData.InstanceWorldBuffer = instancingUserBuffer.InstanceWorldBuffer;
                            instancingData.InstanceWorldInverseBuffer = instancingUserBuffer.InstanceWorldInverseBuffer;
                            instancingData.BuffersManagedByUser = true;
                        }
                        else if (instancingManyBase is InstancingUserArray instancingMany)
                        {
                            instancingData.WorldMatrices = instancingMany.WorldMatrices;
                            instancingData.WorldInverseMatrices = instancingMany.WorldInverseMatrices;

                            if (instancingData.InstanceWorldBuffer == null || instancingData.InstanceWorldBuffer.ElementCount < instancingManyBase.InstanceCount)
                            {
                                instancingData.InstanceWorldBuffer?.Dispose();
                                instancingData.InstanceWorldInverseBuffer?.Dispose();

                                instancingData.InstanceWorldBuffer = CreateMatrixBuffer(Context.GraphicsDevice, instancingManyBase.InstanceCount);
                                instancingData.InstanceWorldInverseBuffer = CreateMatrixBuffer(Context.GraphicsDevice, instancingManyBase.InstanceCount);
                            }

                            instancingData.BuffersManagedByUser = false;
                        }
                    }
                    else
                    {
                        instancingData.InstanceCount = 0;
                    } 
                }

                // Update instance count on mesh
                renderMesh.InstanceCount = instancingData.InstanceCount;
            }
        }

        private static Buffer<Matrix> CreateMatrixBuffer(GraphicsDevice graphicsDevice, int elementCount)
        {
            return Buffer.New<Matrix>(graphicsDevice, elementCount, BufferFlags.ShaderResource | BufferFlags.StructuredBuffer, GraphicsResourceUsage.Dynamic);
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
                        SetBufferData(context.CommandList, instancingData.InstanceWorldBuffer, instancingData.WorldMatrices, instancingData.InstanceCount);
                        SetBufferData(context.CommandList, instancingData.InstanceWorldInverseBuffer, instancingData.WorldInverseMatrices, instancingData.InstanceCount);
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
