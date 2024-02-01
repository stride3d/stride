// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.CompilerServices;
using Silk.NET.SDL;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Rendering.Materials;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections;

namespace Stride.Rendering
{
    /// <summary>
    /// Computes and uploads skinning info.
    /// </summary>
    public class BlendShapeRenderFeature : SubRenderFeature
    {
        private StaticObjectPropertyKey<RenderEffect> renderEffectKey;

        private ObjectPropertyKey<Matrix[]> renderModelObjectInfoKey;

        private ConstantBufferOffsetReference BshpDataOffssetRef;
 
        /// <inheritdoc/>
        protected override void InitializeCore()
        {
            renderEffectKey = ((RootEffectRenderFeature)RootRenderFeature).RenderEffectKey;
            renderModelObjectInfoKey = RootRenderFeature.RenderData.CreateObjectKey<Matrix[]>();
            BshpDataOffssetRef = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawCBufferOffsetSlot(TransformationBlendShape.BSHAPEDATA.Name);
        }

        /// <inheritdoc/>
        public override void PrepareEffectPermutations(RenderDrawContext context)
        {
            var renderEffects = RootRenderFeature.RenderData.GetData(renderEffectKey);
            int effectSlotCount = ((RootEffectRenderFeature)RootRenderFeature).EffectPermutationSlotCount;


            Dispatcher.ForEach(((RootEffectRenderFeature)RootRenderFeature).ObjectNodeReferences, objectNodeReference =>
            {
                var objectNode = RootRenderFeature.GetObjectNode(objectNodeReference);
                var renderMesh = (RenderMesh)objectNode.RenderObject;
                var staticObjectNode = renderMesh.StaticObjectNode;

                for (int i = 0; i < effectSlotCount; ++i)
                {
                    var staticEffectObjectNode = staticObjectNode * effectSlotCount + i;
                    var renderEffect = renderEffects[staticEffectObjectNode];
                    if (renderEffect == null) { continue; }
                    if (renderMesh.HasBlendShapes)
                    {

                        renderEffect.EffectValidator.ValidateParameter(MaterialKeys.HasBlendShape, renderMesh.HasBlendShapes);
                        renderEffect.EffectValidator.ValidateParameter(MaterialKeys.MAX_VERTICES, renderMesh.VerticesCount);
                        renderEffect.EffectValidator.ValidateParameter(MaterialKeys.MAX_MORPH_TARGETS, renderMesh.BlendShapesCount);
                     
                    }
                }

            });
        }


        /// <inheritdoc/>
        public override void Extract()
        {
            var renderModelObjectInfo4 = RootRenderFeature.RenderData.GetData(renderModelObjectInfoKey);
            Dispatcher.ForEach(RootRenderFeature.ObjectNodeReferences, objectNodeReference =>
            {
                var objectNode = RootRenderFeature.GetObjectNode(objectNodeReference);
                var renderMesh = (RenderMesh)objectNode.RenderObject;
                renderModelObjectInfo4[objectNodeReference] = renderMesh.MATBSHAPE;
            });
        }

   
        /// <inheritdoc/>
        public override unsafe void Prepare(RenderDrawContext context)
        {
            var renderModelObjectInfoData = RootRenderFeature.RenderData.GetData(renderModelObjectInfoKey);
      
            Dispatcher.ForEach(((RootEffectRenderFeature)RootRenderFeature).RenderNodes, (ref RenderNode renderNode) =>
            {
                var renderMesh = (RenderMesh)renderNode.RenderObject;

                if (!renderMesh.HasBlendShapes) { return; }

                var perDrawLayout = renderNode.RenderEffect.Reflection?.PerDrawLayout;
                if (perDrawLayout == null)
                    return;

                var bdataVerticesOffset= perDrawLayout.GetConstantBufferOffset(BshpDataOffssetRef);
                if(bdataVerticesOffset==-1)
                {
                    return;
                }
           
                var renderModelObjectInfo = renderModelObjectInfoData[renderNode.RenderObject.ObjectNode];
                if(renderModelObjectInfo == null)
                {
                    return;
                }
               

                unsafe
                {

                    var mappedCB = (byte*)renderNode.Resources.ConstantBuffer.Data + bdataVerticesOffset;
                    fixed (Matrix* matPtr = renderModelObjectInfo)
                    {
                        Unsafe.CopyBlockUnaligned(mappedCB, matPtr, (uint)(renderMesh.VerticesCount*renderMesh.BlendShapesCount) * (uint)sizeof(Matrix));
                    }
                }
            });
        }
    }

    
    
}
