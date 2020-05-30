using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.Engine.Rendering
{
    public struct InstancingData 
    {
        public Matrix[] WorldMatrices;
        public Matrix[] WorldInverseMatrices;

        // GPU buffers, managed by the render feature
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
                    continue;

                var data = renderObjectInstancingData[renderMesh.StaticObjectNode];
                data.WorldMatrices = instancingComponent.WorldMatrices;
                data.WorldInverseMatrices = instancingComponent.WorldInverseMatrices;
                renderMesh.ActiveMeshDraw.InstanceCount = instancingComponent.InstanceCount;

                renderMesh.BoundingBox.Center += instancingComponent.BoundingBox.Center;
                renderMesh.BoundingBox.Extent += instancingComponent.BoundingBox.Extent;
            }
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

                instancingData.InstanceWorldBuffer.SetData(context.CommandList, instancingData.WorldMatrices);
                instancingData.InstanceWorldInverseBuffer.SetData(context.CommandList, instancingData.WorldInverseMatrices);

                renderNode.Resources.DescriptorSet.SetShaderResourceView(group.DescriptorEntryStart, instancingData.InstanceWorldBuffer);
                renderNode.Resources.DescriptorSet.SetShaderResourceView(group.DescriptorEntryStart + 1, instancingData.InstanceWorldInverseBuffer);
            }
        }
    }
}
