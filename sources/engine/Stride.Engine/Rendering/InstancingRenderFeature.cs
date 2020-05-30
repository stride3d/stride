using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core.Mathematics;
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

        private LogicalGroupReference instancingResourceGroupKey;

        /// <inheritdoc/>
        protected override void InitializeCore()
        {
            renderObjectInstancingDataInfoKey = RootRenderFeature.RenderData.CreateStaticObjectKey<InstancingData>();
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

                var instancingData = renderObjectInstancingData[renderMesh.StaticObjectNode];

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

                    // Bounding box
                    renderMesh.BoundingBox.Center += instancingComponent.BoundingBox.Center;
                    renderMesh.BoundingBox.Extent += instancingComponent.BoundingBox.Extent;
                }
                else
                {
                    instancingData.InstanceCount = 0;
                }

                // Update instance count on mesh
                renderMesh.InstanceCount = instancingData.InstanceCount;

                renderObjectInstancingData[renderMesh.StaticObjectNode] = instancingData;
            }
        }

        private static Buffer<Matrix> CreateMatrixBuffer(GraphicsDevice graphicsDevice, int elementCount)
        {
            return Buffer.New<Matrix>(graphicsDevice, elementCount, BufferFlags.ShaderResource | BufferFlags.StructuredBuffer, GraphicsResourceUsage.Dynamic);
        }

        /// <inheritdoc/>
        public unsafe override void Prepare(RenderDrawContext context)
        {
            var renderObjectInstancingData = RootRenderFeature.RenderData.GetData(renderObjectInstancingDataInfoKey);

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
                    if (!instancingData.BuffersManagedByUser)
                    {
                        instancingData.InstanceWorldBuffer.SetData(context.CommandList, instancingData.WorldMatrices);
                        instancingData.InstanceWorldInverseBuffer.SetData(context.CommandList, instancingData.WorldInverseMatrices);
                    }

                    renderNode.Resources.DescriptorSet.SetShaderResourceView(group.DescriptorEntryStart, instancingData.InstanceWorldBuffer);
                    renderNode.Resources.DescriptorSet.SetShaderResourceView(group.DescriptorEntryStart + 1, instancingData.InstanceWorldInverseBuffer);
                }
            }
        }
    }
}
