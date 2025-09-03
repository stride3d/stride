// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.Data;
using Stride.Graphics.Semantics;

namespace Stride.Extensions
{
    public static class TransformExtensions
    {
        /// <summary>
        /// Transform a vertex buffer positions, normals, tangents and bitangents using the given matrix.
        /// Using as source/destination data the provided bufferData byte array.
        /// </summary>
        /// <param name="vertexBufferBinding">The vertex container to transform</param>
        /// <param name="bufferData">The source/destination data array to transform</param>
        /// <param name="matrix">The matrix to use for the transform</param>
        public static void TransformBuffer(this VertexBufferBinding vertexBufferBinding, byte[] bufferData, ref Matrix matrix)
        {
            var helper = new VertexBufferHelper(vertexBufferBinding, bufferData, out _);
            
            // List of items that need to be transformed by the matrix
            helper.Write<PositionSemantic, Vector3, Transform>(new Transform { Matrix = matrix });

            // compute matrix inverse transpose
            Matrix inverseTransposeMatrix;
            Matrix.Invert(ref matrix, out inverseTransposeMatrix);
            Matrix.Transpose(ref inverseTransposeMatrix, out inverseTransposeMatrix);

            // List of items that need to be transformed by the inverse transpose matrix
            var inverseTransposeTransform = new InverseTranspose { InverseTransposeMatrix = inverseTransposeMatrix };
            helper.Write<Relaxed<NormalSemantic>, Vector4, InverseTranspose>(inverseTransposeTransform);
            helper.Write<TangentSemantic, Vector4, InverseTranspose>(inverseTransposeTransform);
            helper.Write<BiTangentSemantic, Vector4, InverseTranspose>(inverseTransposeTransform);

            if (Vector3.Dot(Vector3.Cross(matrix.Right, matrix.Forward), matrix.Up) < 0.0f)
                helper.Write<TangentSemantic, Vector4, FlipHandedness>(new FlipHandedness());
        }
        
        private struct Transform : VertexBufferHelper.IWriter<Vector3>
        {
            public required Matrix Matrix;
            
            public unsafe void Write<TConversion, TSource>(byte* sourcePointer, int elementCount, int stride)
                where TConversion : IConversion<TSource, Vector3>, IConversion<Vector3, TSource> where TSource : unmanaged
            {
                for (byte* end = sourcePointer + elementCount * stride; sourcePointer < end; sourcePointer += stride)
                {
                    if (typeof(TSource) == typeof(Vector4))
                    {
                        Vector4.Transform(ref *(Vector4*)sourcePointer, ref Matrix, out *(Vector4*)sourcePointer);
                    }
                    else
                    {
                        TConversion.Convert(*(TSource*)sourcePointer, out var val);
                        Vector3.TransformCoordinate(ref val, ref Matrix, out val);
                        TConversion.Convert(val, out *(TSource*)sourcePointer);
                    }
                }
            }
        }

        private struct InverseTranspose : VertexBufferHelper.IWriter<Vector4>
        {
            public required Matrix InverseTransposeMatrix;
            
            public unsafe void Write<TConversion, TSource>(byte* sourcePointer, int elementCount, int stride)
                where TConversion : IConversion<TSource, Vector4>, IConversion<Vector4, TSource> where TSource : unmanaged
            {
                for (byte* end = sourcePointer + elementCount * stride; sourcePointer < end; sourcePointer += stride)
                {
                    TConversion.Convert(*(TSource*)sourcePointer, out var val);

                    var v3Pointer = (Vector3*)&val;
                    Vector3.TransformNormal(ref *v3Pointer, ref InverseTransposeMatrix, out *v3Pointer);
                    v3Pointer->Normalize();
                    
                    TConversion.Convert(val, out *(TSource*)sourcePointer);
                }
            }
        }

        private struct FlipHandedness : VertexBufferHelper.IWriter<Vector4>
        {
            public unsafe void Write<TConversion, TSource>(byte* sourcePointer, int elementCount, int stride)
                where TConversion : IConversion<TSource, Vector4>, IConversion<Vector4, TSource> where TSource : unmanaged
            {
                for (byte* end = sourcePointer + elementCount * stride; sourcePointer < end; sourcePointer += stride)
                {
                    TConversion.Convert(*(TSource*)sourcePointer, out var val);
                    val.W = -val.W;
                    TConversion.Convert(val, out *(TSource*)sourcePointer);
                }
            }
        }

        /// <summary>
        /// Transform a vertex buffer positions, normals, tangents and bitangents using the given matrix.
        /// </summary>
        /// <param name="vertexBufferBinding">The vertex container to transform</param>
        /// <param name="matrix">The matrix to use for the transform</param>
        public static void TransformBuffer(this VertexBufferBinding vertexBufferBinding, ref Matrix matrix)
        {
            var bufferData = vertexBufferBinding.Buffer.GetSerializationData().Content;
            vertexBufferBinding.TransformBuffer(bufferData, ref matrix);
        }
    }
}
