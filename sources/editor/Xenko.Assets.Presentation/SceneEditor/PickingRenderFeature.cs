// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;
using Xenko.Rendering;

namespace Xenko.Assets.Presentation.SceneEditor
{
    /// <summary>
    /// Performs picking.
    /// </summary>
    public class PickingRenderFeature : SubRenderFeature
    {
        private ObjectPropertyKey<PickingObjectInfo> renderObjectInfoKey;

        private ConstantBufferOffsetReference pickingData;

        /// <inheritdoc/>
        protected override void InitializeCore()
        {
            renderObjectInfoKey = rootRenderFeature.RenderData.CreateObjectKey<PickingObjectInfo>();

            pickingData = ((RootEffectRenderFeature)rootRenderFeature).CreateDrawCBufferOffsetSlot(PickingShaderKeys.PickingData.Name);
        }

        /// <inheritdoc/>
        public override void Extract()
        {
            var modelObjectInfo = rootRenderFeature.RenderData.GetData(renderObjectInfoKey);

            foreach (var objectNodeReference in rootRenderFeature.ObjectNodeReferences)
            {
                var objectNode = rootRenderFeature.GetObjectNode(objectNodeReference);
                var renderMesh = objectNode.RenderObject as RenderMesh;
                if (renderMesh == null)
                    continue;

                int meshIndex = 0;
                for (int i = 0; i < renderMesh.RenderModel.ModelComponent.Model.Meshes.Count; i++)
                {
                    if (renderMesh.RenderModel.ModelComponent.Model.Meshes[i] == renderMesh.Mesh)
                    {
                        meshIndex = i;
                        break;
                    }
                }

                modelObjectInfo[objectNodeReference] = new PickingObjectInfo(RuntimeIdHelper.ToRuntimeId(renderMesh.RenderModel.ModelComponent), meshIndex, renderMesh.Mesh.MaterialIndex);
            }
        }

        /// <inheritdoc/>
        public unsafe override void Prepare(RenderDrawContext context)
        {
            var renderObjectInfo = rootRenderFeature.RenderData.GetData(renderObjectInfoKey);

            foreach (var renderNode in ((RootEffectRenderFeature)rootRenderFeature).RenderNodes)
            {
                var perDrawLayout = renderNode.RenderEffect.Reflection.PerDrawLayout;
                if (perDrawLayout == null)
                    continue;

                var pickingDataOffset = perDrawLayout.GetConstantBufferOffset(this.pickingData);
                if (pickingDataOffset == -1)
                    continue;

                var renderModelObjectInfo = renderObjectInfo[renderNode.RenderObject.ObjectNode];

                var mappedCB = renderNode.Resources.ConstantBuffer.Data;
                var pickingData = (PickingObjectInfo*)((byte*)mappedCB + pickingDataOffset);

                *pickingData = renderModelObjectInfo;
            }
        }
    }
}
