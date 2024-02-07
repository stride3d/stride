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

        private ObjectPropertyKey<Matrix[]> renderModelObjectInfoKey2;

        private ConstantBufferOffsetReference BshpDataOffssetRef;

        private ConstantBufferOffsetReference LOOKUPREF;

        /// <inheritdoc/>
        protected override void InitializeCore()
        {
            renderEffectKey = ((RootEffectRenderFeature)RootRenderFeature).RenderEffectKey;
            renderModelObjectInfoKey = RootRenderFeature.RenderData.CreateObjectKey<Matrix[]>();
            renderModelObjectInfoKey2 = RootRenderFeature.RenderData.CreateObjectKey<Matrix[]>();
            BshpDataOffssetRef = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawCBufferOffsetSlot(TransformationBlendShape.BSHAPEDATA.Name);
            LOOKUPREF= ((RootEffectRenderFeature)RootRenderFeature).CreateDrawCBufferOffsetSlot(TransformationBlendShape.LOOKUP.Name);
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
                        renderEffect.EffectValidator.ValidateParameter(MaterialKeys.MAT_COUNT, renderMesh.MATBSHAPE.Length);
                    }
                }

            });
        }


        /// <inheritdoc/>
        public override void Extract()
        {
            var renderModelObjectInfo4 = RootRenderFeature.RenderData.GetData(renderModelObjectInfoKey);
            var renderModelObjectInfo5 = RootRenderFeature.RenderData.GetData(renderModelObjectInfoKey2);
            Dispatcher.ForEach(RootRenderFeature.ObjectNodeReferences, objectNodeReference =>
            {
                var objectNode = RootRenderFeature.GetObjectNode(objectNodeReference);
                var renderMesh = (RenderMesh)objectNode.RenderObject;
                renderModelObjectInfo4[objectNodeReference] = renderMesh.MATBSHAPE;
                renderModelObjectInfo5[objectNodeReference] = arr;
            });
        }


        public static  Matrix[] arr = new Matrix[] { new Matrix(1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1), 
            new Matrix(2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2) }; 
   
        /// <inheritdoc/>
        public override unsafe void Prepare(RenderDrawContext context)
        {
            var renderModelObjectInfoData = RootRenderFeature.RenderData.GetData(renderModelObjectInfoKey);
            var renderModelObjectInfoData2 = RootRenderFeature.RenderData.GetData(renderModelObjectInfoKey2);

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

                var renderModelObjectInfo2 = renderModelObjectInfoData2[renderNode.RenderObject.ObjectNode];
                if (renderModelObjectInfo2 == null)
                {
                    return;
                }

                unsafe
                {
                   
                    var mappedCB = (byte*)renderNode.Resources.ConstantBuffer.Data + bdataVerticesOffset;
                    fixed (Matrix* matPtr = renderModelObjectInfo)
                    {
                        Unsafe.CopyBlockUnaligned(mappedCB, matPtr, (uint)(renderMesh.MATBSHAPE.Length) * (uint)sizeof(Matrix));
                        
                    }
                   
                    var lookupoffset = perDrawLayout.GetConstantBufferOffset(LOOKUPREF);
                    mappedCB = (byte*)renderNode.Resources.ConstantBuffer.Data + lookupoffset;
                   // Vector2[] arr = new Vector2[2] { new Vector2(3, 4), new Vector2(1, 2) };
                    fixed (Matrix* v= renderModelObjectInfo2)
                    {


                        Unsafe.CopyBlockUnaligned(mappedCB, v, 2 * (uint)sizeof(Matrix)); 
                  //      Vector2* v1 = v + 1;
                    //    mappedCB = mappedCB + Vector2.SizeInBytes;
                      //   Unsafe.CopyBlockUnaligned(mappedCB, v1,  (uint)sizeof(Vector2));
                        
                    }
                    
                }
            });
        }
    }

    
    
}
