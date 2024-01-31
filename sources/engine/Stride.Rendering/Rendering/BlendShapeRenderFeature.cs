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

        // private ConstantBufferOffsetReference tstVapwRef;
      //  private ObjectPropertyKey<Vector2[]> renderModelObjectInfoKey;
       // private ObjectPropertyKey<Vector3[]> renderModelObjectInfoKey3;
        private ObjectPropertyKey<Matrix[]> renderModelObjectInfoKey4;

        private ObjectPropertyKey<float> renderModelObjectInfoKey5;

        private ConstantBufferOffsetReference morphWeightsRef;

        private ConstantBufferOffsetReference morphTargetVerticesRef;

        private ConstantBufferOffsetReference BSHAPEDATARef;
        private ConstantBufferOffsetReference BasisKeyWeightRef;

        // private ConstantBufferOffsetReference morphTargetVertexIndicesRef;

        //   private ObjectPropertyKey<int> renderModelObjectIndexKey;

        bool bufferset = false;

        /// <inheritdoc/>
        protected override void InitializeCore()
        {
            //  renderModelObjectInfoKey = RootRenderFeature.RenderData.CreateObjectKey<float>();
            //   renderModelObjectInfoKey = RootRenderFeature.RenderData.CreateObjectKey<Vector2[]>();
            //  renderModelObjectInfoKey3 = RootRenderFeature.RenderData.CreateObjectKey<Vector3[]>();
            renderEffectKey = ((RootEffectRenderFeature)RootRenderFeature).RenderEffectKey;
            renderModelObjectInfoKey4 = RootRenderFeature.RenderData.CreateObjectKey<Matrix[]>();
            renderModelObjectInfoKey5= RootRenderFeature.RenderData.CreateObjectKey<float>();

            BSHAPEDATARef = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawCBufferOffsetSlot(TransformationBlendShape.BSHAPEDATA.Name);
            BasisKeyWeightRef= ((RootEffectRenderFeature)RootRenderFeature).CreateDrawCBufferOffsetSlot(TransformationBlendShape.BasisKeyWeight.Name);
            // morphWeightsRef = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawCBufferOffsetSlot(TransformationBlendShape.morphWeights.Name);

            //  morphTargetVerticesRef = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawCBufferOffsetSlot(TransformationBlendShape.morphTargetVertices.Name);


            //morphTargetVertexIndicesRef = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawCBufferOffsetSlot(TransformationBlendShape.morphTargetVertexIndices.Name);

            // tstVapwRef = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawCBufferOffsetSlot(TransformationBlendShape.Tstvapw.Name);
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
                // var modelData = ((RenderMesh)objectNode.RenderObject).RenderModel?.Model;
                var staticObjectNode = renderMesh.StaticObjectNode;


                for (int i = 0; i < effectSlotCount; ++i)
                {
                    var staticEffectObjectNode = staticObjectNode * effectSlotCount + i;
                    var renderEffect = renderEffects[staticEffectObjectNode];
                    if (renderEffect == null) { continue; }
                    //   renderMesh.Mesh.Parameters.Set(MaterialKeys.HasBlendShape, true);



                    if (renderMesh.HasBlendShapes)
                    {

                        renderEffect.EffectValidator.ValidateParameter(MaterialKeys.HasBlendShape, renderMesh.HasBlendShapes);
                        renderEffect.EffectValidator.ValidateParameter(MaterialKeys.MAX_VERTICES, renderMesh.VerticesCount);
                        renderEffect.EffectValidator.ValidateParameter(MaterialKeys.MAX_MORPH_TARGETS, renderMesh.BlendShapesCount);
                     
                    }
                }

            });
        }

        float[] arr = new float[12] { 1, -1, 1, -1, 1, -1, 1, -1, 1, -1, 1, -1 };

        ArrayBuffer buf = new ArrayBuffer();

        /// <inheritdoc/>
        public override void Extract()
        {
        //    var renderModelObjectInfo = RootRenderFeature.RenderData.GetData(renderModelObjectInfoKey);
            var renderModelObjectInfo4 = RootRenderFeature.RenderData.GetData(renderModelObjectInfoKey4);
            var renderModelObjectInfo5 = RootRenderFeature.RenderData.GetData(renderModelObjectInfoKey5);
            Dispatcher.ForEach(RootRenderFeature.ObjectNodeReferences, objectNodeReference =>
            {
                var objectNode = RootRenderFeature.GetObjectNode(objectNodeReference);
                var renderMesh = (RenderMesh)objectNode.RenderObject;


                //  var modelData = ((RenderMesh)objectNode.RenderObject).RenderModel?.Model;
                // TODO GRAPHICS REFACTOR: Extract copy of matrices
                //renderModelObjectInfo[objectNodeReference] =;

                //  var buffer = new ArrayBuffer();
                //buffer.FloatArray = new float[] { 3.0f, 9.0f, 6.0f, 11.0f };

                //  renderModelObjectInfo[objectNodeReference] = renderMesh.BlendShapeWeights;


                //  buf.FloatArray = new float[12] { 1, -1, 1, -1, 1, -1, 1, -1, 1, -1, 1, -1 };



                renderModelObjectInfo4[objectNodeReference] = renderMesh.MATBSHAPE;
                renderModelObjectInfo5[objectNodeReference] = renderMesh.BasisKeyWeight;

            });
        }

   //     Matrix[] mat = new Matrix[12];

        /// <inheritdoc/>
        public override unsafe void Prepare(RenderDrawContext context)
        {

           // var renderModelObjectInfoData = RootRenderFeature.RenderData.GetData(renderModelObjectInfoKey);

            var renderModelObjectInfoData4 = RootRenderFeature.RenderData.GetData(renderModelObjectInfoKey4);
            var renderModelObjectInfoData5 = RootRenderFeature.RenderData.GetData(renderModelObjectInfoKey5);

            Dispatcher.ForEach(((RootEffectRenderFeature)RootRenderFeature).RenderNodes, (ref RenderNode renderNode) =>
            {
                var renderMesh = (RenderMesh)renderNode.RenderObject;

                if (!renderMesh.HasBlendShapes) { return; }

                var perDrawLayout = renderNode.RenderEffect.Reflection?.PerDrawLayout;
                if (perDrawLayout == null)
                    return;

              //  var morphWeightsOffset = perDrawLayout.GetConstantBufferOffset(morphWeightsRef);
                //if (morphWeightsOffset == -1)
                  //  return;

                //var morphTargetVerticesOffset = perDrawLayout.GetConstantBufferOffset(morphTargetVerticesRef);
                //if (morphTargetVerticesOffset == -1)
                  //  return;

                var bdataVerticesOffset= perDrawLayout.GetConstantBufferOffset(BSHAPEDATARef);
                if(bdataVerticesOffset==-1)
                {
                    return;
                }
                var basisWeightDataOffset = perDrawLayout.GetConstantBufferOffset(BasisKeyWeightRef);
                if (basisWeightDataOffset == -1)
                {
                    return;
                }

                //   var renderModelObjectInfo = renderModelObjectInfoData[renderNode.RenderObject.ObjectNode];
                var renderModelObjectInfo4 = renderModelObjectInfoData4[renderNode.RenderObject.ObjectNode];
                var renderModelObjectInfo5 = renderModelObjectInfoData5[renderNode.RenderObject.ObjectNode];
                //   if (renderModelObjectInfo.FloatArray == null)
                //    return;

                // float renderModelObjectInfo = 5.0f;

                //var mappedCB = (byte*)renderNode.Resources.ConstantBuffer.Data + morphWeightsOffset;

                //fixed (Vector2* blendMatricesPtr = renderModelObjectInfo)
                {
                  //  Unsafe.CopyBlockUnaligned(mappedCB, blendMatricesPtr, (uint)renderModelObjectInfo.Length * (uint)Vector2.SizeInBytes);
                }

                /* var mappedCB = (byte*)renderNode.Resources.ConstantBuffer.Data + morphTargetVerticesOffset;
                  fixed (Vector3* vertVecPtr = renderModelObjectInfo3)
                  {
                      Unsafe.CopyBlockUnaligned(mappedCB, vertVecPtr, (uint)renderModelObjectInfo3.Length * (uint)Vector3.SizeInBytes);
                  } */


                const int SIZE = 12;
                var FloatArray = new float[SIZE];

                // Pin the float array in memory to get a pointer to its elements
           

                unsafe
                {

                    var mappedCB = (byte*)renderNode.Resources.ConstantBuffer.Data + bdataVerticesOffset;

                    //  uint floatSize = (uint)sizeof(float);

                    /*
                    for (int i = 0; i < SIZE; i++)
                    {
                        mat[i].Column1 = new Vector4(1, 2, 3, 4)+ new Vector4(i, i, i, i);
                        mat[i].Column2 = new Vector4(1, 2, 3, 4) + new Vector4(i, i, i, i);
                        mat[i].Column3 = new Vector4(1, 2, 3, 4) + new Vector4(i, i, i, i);
                        mat[i].Column4 = new Vector4(1, 2, 3, 4) + new Vector4(i, i, i, i);

                    }*/

                  //  mat = renderMesh.MATBSHAPE;

                    fixed (Matrix* matPtr = renderModelObjectInfo4)
                    {
                        Unsafe.CopyBlockUnaligned(mappedCB, matPtr, (uint)(renderMesh.VerticesCount*renderMesh.BlendShapesCount) * (uint)sizeof(Matrix));
                    }
                    float* floatPtr = &renderModelObjectInfo5;
                    
                        mappedCB = (byte*)renderNode.Resources.ConstantBuffer.Data + basisWeightDataOffset;
                        Unsafe.CopyBlockUnaligned(mappedCB, floatPtr,   (uint)sizeof(float));

                    

                    //    fixed (float* ptr =FloatArray)
                    //{
                    //    //byte* ptr2 = (byte*)ptr;
                    //    for (int i = 0; i < SIZE; i++)
                    //    {
                    //        float f = i;
                    //        float* ptr2 = &f;
                    //        Unsafe.CopyBlockUnaligned(mappedCB, ptr2, 4);
                    //        mappedCB += 4;
                    //    }
                    //}
                }

            });
        }
    }

    

    [StructLayout(LayoutKind.Sequential, Pack =4)]
    public struct ArrayBuffer
    {
        public float[] FloatArray;
    }


    
}
