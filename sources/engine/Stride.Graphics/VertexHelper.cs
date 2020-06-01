// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    /// <summary>
    /// An helper class for manipulating vertex buffers on CPU (Generate new vertex attributes...etc.)
    /// </summary>
    public static class VertexHelper
    {
        // TODO: The code is not optimal if we want to combine several transforms on a vertex buffer. We should have a better API for this.
        // TODO: Write some unit tests!

        /// <summary>
        /// Generates multi texture coordinates for an existing vertex buffer. See remarks.
        /// </summary>
        /// <param name="vertexDeclaration">The vertex declaration.</param>
        /// <param name="vertexBufferData">The vertex buffer data.</param>
        /// <param name="maxTexcoord">The maximum texcoord.</param>
        /// <returns>A new vertex buffer with additional texture coordinates.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// vertexDeclaration
        /// or
        /// vertexBufferData
        /// </exception>
        /// <remarks>The original vertex buffer must contain at least a TEXCOORD[0-9] attribute in order for this method to work.
        /// This method will copy the value of the first existing TEXCOORD found in the vertex buffer to the newly created TEXCOORDS.</remarks>
        public static unsafe VertexTransformResult GenerateMultiTextureCoordinates<T>(VertexDeclaration vertexDeclaration, T[] vertexBufferData, int maxTexcoord = 9) where T : struct
        {
            if (vertexDeclaration == null) throw new ArgumentNullException("vertexDeclaration");
            if (vertexBufferData == null) throw new ArgumentNullException("vertexBufferData");
            var vertexStride = Utilities.SizeOf<T>();
            var vertexBufferPtr = Interop.Fixed(vertexBufferData);
            return GenerateMultiTextureCoordinates(vertexDeclaration, (IntPtr)vertexBufferPtr, vertexBufferData.Length, 0, vertexStride, maxTexcoord);
        }

        /// <summary>
        /// Generates multi texture coordinates for an existing vertex buffer. See remarks.
        /// </summary>
        /// <param name="vertexDeclaration">The vertex declaration.</param>
        /// <param name="vertexBufferData">The vertex buffer data.</param>
        /// <param name="vertexStride">The vertex stride.</param>
        /// <param name="maxTexcoord">The maximum texcoord.</param>
        /// <returns>A new vertex buffer with additional texture coordinates.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// vertexDeclaration
        /// or
        /// vertexBufferData
        /// </exception>
        /// <remarks>The original vertex buffer must contain at least a TEXCOORD[0-9] attribute in order for this method to work.
        /// This method will copy the value of the first existing TEXCOORD found in the vertex buffer to the newly created TEXCOORDS.</remarks>
        public static unsafe VertexTransformResult GenerateMultiTextureCoordinates(VertexDeclaration vertexDeclaration, byte[] vertexBufferData, int vertexStride = 0, int maxTexcoord = 9)
        {
            if (vertexDeclaration == null) throw new ArgumentNullException("vertexDeclaration");
            if (vertexBufferData == null) throw new ArgumentNullException("vertexBufferData");
            var vertexBufferPtr = Interop.Fixed(vertexBufferData);
            if (vertexStride == 0)
            {
                vertexStride = vertexDeclaration.VertexStride;
            }
            var vertexCount = ((int)(vertexBufferData.Length / vertexStride));
            if (vertexBufferData.Length != (vertexCount * vertexStride))
            {
                throw new ArgumentOutOfRangeException("vertexBufferData", "The length of vertex buffer [{0}] doesn't match the expected length with the vertex stride [{1}]".ToFormat(vertexBufferData.Length, vertexCount * vertexStride));
            }

            return GenerateMultiTextureCoordinates(vertexDeclaration, (IntPtr)vertexBufferPtr, vertexCount, 0, vertexStride, maxTexcoord);
        }

        /// <summary>
        /// Generates multi texture coordinates for an existing vertex buffer. See remarks.
        /// </summary>
        /// <param name="transform">The result of a previous transform.</param>
        /// <param name="vertexStride">The vertex stride.</param>
        /// <param name="maxTexcoord">The maximum texcoord.</param>
        /// <returns>A new vertex buffer with additional texture coordinates.</returns>
        /// <remarks>The original vertex buffer must contain at least a TEXCOORD[0-9] attribute in order for this method to work.
        /// This method will copy the value of the first existing TEXCOORD found in the vertex buffer to the newly created TEXCOORDS.</remarks>
        public static VertexTransformResult GenerateMultiTextureCoordinates(VertexTransformResult transform, int vertexStride = 0, int maxTexcoord = 9)
        {
            return GenerateMultiTextureCoordinates(transform.Layout, transform.VertexBuffer, vertexStride, maxTexcoord);
        }

        /// <summary>
        /// Generates multi texture coordinates for an existing vertex buffer. See remarks.
        /// </summary>
        /// <param name="vertexDeclaration">The vertex declaration.</param>
        /// <param name="vertexBufferData">The vertex buffer data.</param>
        /// <param name="vertexCount">The vertex count.</param>
        /// <param name="vertexOffset">The vertex offset.</param>
        /// <param name="vertexStride">The vertex stride.</param>
        /// <param name="maxTexcoord">The maximum texcoord.</param>
        /// <returns>A new vertex buffer with additional texture coordinates.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// vertexDeclaration
        /// or
        /// vertexBufferData
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// vertexCount;vertexCount must be > 0
        /// or
        /// vertexStride;vertexStride must be >= 0
        /// or
        /// maxTexcoord;maxTexcoord must be >= 0
        /// </exception>
        /// <exception cref="System.InvalidOperationException">The vertex buffer must contain at least the TEXCOORD</exception>
        /// <remarks>
        /// The original vertex buffer must contain at least a TEXCOORD[0-9] attribute in order for this method to work. 
        /// This method will copy the value of the first existing TEXCOORD found in the vertex buffer to the newly created TEXCOORDS.
        /// </remarks>
        public static unsafe VertexTransformResult GenerateMultiTextureCoordinates(VertexDeclaration vertexDeclaration, IntPtr vertexBufferData, int vertexCount, int vertexOffset, int vertexStride, int maxTexcoord = 9)
        {
            if (vertexDeclaration == null) throw new ArgumentNullException("vertexDeclaration");
            if (vertexBufferData == IntPtr.Zero) throw new ArgumentNullException("vertexBufferData");
            if (vertexCount <= 0) throw new ArgumentOutOfRangeException("vertexCount", "vertexCount must be > 0");
            if (vertexStride < 0) throw new ArgumentOutOfRangeException("vertexStride", "vertexStride must be >= 0");
            if (maxTexcoord < 0) throw new ArgumentOutOfRangeException("maxTexcoord", "maxTexcoord must be >= 0");

            // Get the stride from the vertex declaration if necessary
            if (vertexStride == 0)
            {
                vertexStride = vertexDeclaration.VertexStride;
            }

            // TODO: Usage index in key
            var offsetMapping = vertexDeclaration
                .EnumerateWithOffsets()
                .ToDictionary(x => x.VertexElement.SemanticAsText, x => x.Offset);

            var newVertexElements = new List<VertexElement>();

            int vertexUVOffset = -1;

            // Use R16G16_SNorm as it is supported from HW Level 9.1
            // See https://msdn.microsoft.com/en-us/library/windows/desktop/ff471324%28v=vs.85%29.aspx
            const PixelFormat DefaultUVFormat = PixelFormat.R16G16_SNorm;
            var newUvSize = DefaultUVFormat.SizeInBytes();

            for (int i = 0; i <= maxTexcoord; i++)
            {
                var vertexElement = VertexElement.TextureCoordinate(i, DefaultUVFormat);
                var semantic = vertexElement.SemanticAsText;
                if (offsetMapping.TryGetValue(semantic, out int offset))
                {
                    if (vertexUVOffset < 0)
                    {
                        vertexUVOffset = offset;
                    }
                }
                else
                {
                    newVertexElements.Add(vertexElement);
                }
            }

            if (vertexUVOffset < 0)
            {
                throw new InvalidOperationException("The vertex buffer must contain at least the TEXCOORD");
            }

            var newVertexStride = vertexStride + newVertexElements.Count * newUvSize;
            var newVertexBuffer = new byte[newVertexStride * vertexCount];

            byte* oldBuffer = (byte*)vertexBufferData + vertexOffset;
            fixed (byte* newBuffer = newVertexBuffer)
            {
                var oldVertexOffset = 0;
                var newVertexOffset = 0;
                for (int i = 0; i < vertexCount; ++i)
                {
                    Utilities.CopyMemory(new IntPtr(&newBuffer[newVertexOffset]), new IntPtr(&oldBuffer[oldVertexOffset]), vertexStride);

                    var textureCoord = *(Vector2*)&oldBuffer[oldVertexOffset + vertexUVOffset];
                    for (int j = 0; j < newVertexElements.Count; j++)
                    {
                        var target = ((Half2*)(&newBuffer[newVertexOffset + vertexStride + j * newUvSize]));
                        *target = (Half2)textureCoord;
                    }

                    oldVertexOffset += vertexStride;
                    newVertexOffset += newVertexStride;
                }
            }

            var allVertexElements = new List<VertexElement>(vertexDeclaration.VertexElements);
            allVertexElements.AddRange(newVertexElements);

            return new VertexTransformResult(new VertexDeclaration(allVertexElements.ToArray()), newVertexBuffer);
        }

        /// <summary>
        /// Generates the tangent binormal for an existing vertex buffer.
        /// </summary>
        /// <param name="vertexDeclaration">The vertex declaration.</param>
        /// <param name="vertexBufferData">The vertex buffer data.</param>
        /// <param name="indexBuffer">The index buffer.</param>
        /// <returns>A new vertex buffer with its new layout.</returns>
        public static unsafe VertexTransformResult GenerateTangentBinormal<T>(VertexDeclaration vertexDeclaration, T[] vertexBufferData, int[] indexBuffer) where T : struct
        {
            if (vertexDeclaration == null) throw new ArgumentNullException("vertexDeclaration");
            if (vertexBufferData == null) throw new ArgumentNullException("vertexBufferData");
            if (typeof(T) == typeof(byte)) throw new ArgumentOutOfRangeException("T", "Type vertex can't be a byte");

            var vertexStride = Utilities.SizeOf<T>();
            var vertexBufferPtr = Interop.Fixed(vertexBufferData);
            fixed (void* indexBufferPtr = indexBuffer)
            {
                return GenerateTangentBinormal(vertexDeclaration, (IntPtr)vertexBufferPtr, vertexBufferData.Length, 0, vertexStride, (IntPtr)indexBufferPtr, true, indexBuffer != null ? indexBuffer.Length : 0);
            }
        }

        /// <summary>
        /// Generate Tangent BiNormal from an existing vertex/index buffer.
        /// </summary>
        /// <param name="vertexDeclaration">The vertex declaration of the vertex buffer passed by parameters.</param>
        /// <param name="vertexBufferData">The vertex buffer.</param>
        /// <param name="vertexCount">The vertex count.</param>
        /// <param name="vertexOffset">The vertex offset.</param>
        /// <param name="vertexStride">The vertex stride. If 0, It takes the stride from the vertex declaration</param>
        /// <param name="indexData">The index data.</param>
        /// <param name="is32BitIndex">if set to <c>true</c> [is32 bit index].</param>
        /// <param name="indexCountArg">The index count argument.</param>
        /// <returns>A new vertex buffer with its new layout.</returns>
        public static unsafe VertexTransformResult GenerateTangentBinormal(VertexDeclaration vertexDeclaration, IntPtr vertexBufferData, int vertexCount, int vertexOffset, int vertexStride, IntPtr indexData, bool is32BitIndex, int indexCountArg)
        {
            if (vertexDeclaration == null) throw new ArgumentNullException("vertexDeclaration");
            if (vertexBufferData == IntPtr.Zero) throw new ArgumentNullException("vertexBufferData");
            if (vertexCount <= 0) throw new ArgumentOutOfRangeException("vertexCount", "vertexCount must be > 0");
            if (vertexStride < 0) throw new ArgumentOutOfRangeException("vertexStride", "vertexStride must be >= 0");
            if (indexData != IntPtr.Zero && indexCountArg < 0) throw new ArgumentOutOfRangeException("indexCountArg", "indexCountArg must be >= 0");

            // Get the stride from the vertex declaration if necessary
            if (vertexStride == 0)
            {
                vertexStride = vertexDeclaration.VertexStride;
            }

            var indexBufferBinding = indexData;

            var oldVertexStride = vertexStride;
            var bufferData = vertexBufferData;

            // TODO: Usage index in key
            var offsetMapping = vertexDeclaration
                .EnumerateWithOffsets()
                .ToDictionary(x => x.VertexElement.SemanticAsText, x => x.Offset);

            var positionOffset = offsetMapping["POSITION"];
            var uvOffset = offsetMapping[VertexElementUsage.TextureCoordinate];
            var normalOffset = offsetMapping[VertexElementUsage.Normal];

            // Add tangent to vertex declaration
            var vertexElements = vertexDeclaration.VertexElements.ToList();
            if (!offsetMapping.ContainsKey(VertexElementUsage.Tangent))
                vertexElements.Add(VertexElement.Tangent<Vector4>());
            var newVertexDeclaration = new VertexDeclaration(vertexElements.ToArray());
            var newVertexStride = newVertexDeclaration.VertexStride;

            // Update mapping
            offsetMapping = newVertexDeclaration
                .EnumerateWithOffsets()
                .ToDictionary(x => x.VertexElement.SemanticAsText, x => x.Offset);

            var tangentOffset = offsetMapping[VertexElementUsage.Tangent];

            var newBufferData = new byte[vertexCount * newVertexStride];

            var tangents = new Vector3[vertexCount];
            var bitangents = new Vector3[vertexCount];

            byte* indexBufferStart = (byte*)indexData;
            byte* oldBuffer = (byte*)bufferData + vertexOffset;
            fixed (byte* newBuffer = newBufferData)
            {
                var indexBuffer32 = indexBufferBinding != IntPtr.Zero && is32BitIndex ? (int*)indexBufferStart : null;
                var indexBuffer16 = indexBufferBinding != IntPtr.Zero && !is32BitIndex ? (short*)indexBufferStart : null;

                var indexCount = indexBufferBinding != IntPtr.Zero ? indexCountArg : vertexCount;

                for (int i = 0; i < indexCount; i += 3)
                {
                    // Get indices
                    int index1 = i + 0;
                    int index2 = i + 1;
                    int index3 = i + 2;

                    if (indexBuffer32 != null)
                    {
                        index1 = indexBuffer32[index1];
                        index2 = indexBuffer32[index2];
                        index3 = indexBuffer32[index3];
                    }
                    else if (indexBuffer16 != null)
                    {
                        index1 = indexBuffer16[index1];
                        index2 = indexBuffer16[index2];
                        index3 = indexBuffer16[index3];
                    }

                    int vertexOffset1 = index1 * oldVertexStride;
                    int vertexOffset2 = index2 * oldVertexStride;
                    int vertexOffset3 = index3 * oldVertexStride;

                    // Get positions
                    var position1 = (Vector3*)&oldBuffer[vertexOffset1 + positionOffset];
                    var position2 = (Vector3*)&oldBuffer[vertexOffset2 + positionOffset];
                    var position3 = (Vector3*)&oldBuffer[vertexOffset3 + positionOffset];

                    // Get texture coordinates
                    var uv1 = (Vector2*)&oldBuffer[vertexOffset1 + uvOffset];
                    var uv2 = (Vector2*)&oldBuffer[vertexOffset2 + uvOffset];
                    var uv3 = (Vector2*)&oldBuffer[vertexOffset3 + uvOffset];

                    // Calculate position and UV vectors from vertex 1 to vertex 2 and 3
                    var edge1 = *position2 - *position1;
                    var edge2 = *position3 - *position1;
                    var uvEdge1 = *uv2 - *uv1;
                    var uvEdge2 = *uv3 - *uv1;

                    var dR = uvEdge1.X * uvEdge2.Y - uvEdge2.X * uvEdge1.Y;

                    // Workaround to handle degenerated case
                    // TODO: We need to understand more how we can handle this more accurately
                    if (MathUtil.IsZero(dR))
                    {
                        dR = 1;
                    }

                    var r = 1.0f / dR;
                    var t = (uvEdge2.Y * edge1 - uvEdge1.Y * edge2) * r;
                    var b = (uvEdge1.X * edge2 - uvEdge2.X * edge1) * r;

                    // Contribute to every vertex
                    tangents[index1] += t;
                    tangents[index2] += t;
                    tangents[index3] += t;

                    bitangents[index1] += b;
                    bitangents[index2] += b;
                    bitangents[index3] += b;
                }

                var oldVertexOffset = 0;
                var newVertexOffset = 0;
                for (int i = 0; i < vertexCount; ++i)
                {
                    Utilities.CopyMemory(new IntPtr(&newBuffer[newVertexOffset]), new IntPtr(&oldBuffer[oldVertexOffset]), oldVertexStride);

                    var normal = *(Vector3*)&oldBuffer[oldVertexOffset + normalOffset];
                    var newTangentPtr = ((float*)(&newBuffer[newVertexOffset + tangentOffset]));

                    var tangent = tangents[i];
                    var bitangent = bitangents[i];

                    // Gram-Schmidt orthogonalize
                    var newTangentUnormalized = tangent - normal * Vector3.Dot(normal, tangent);
                    var length = newTangentUnormalized.Length();
                    
                    // Workaround to handle degenerated case
                    // TODO: We need to understand more how we can handle this more accurately
                    if (MathUtil.IsZero(length))
                    {
                        tangent = Vector3.Cross(normal, Vector3.UnitX);
                        if (MathUtil.IsZero(tangent.Length()))
                        {
                            tangent = Vector3.Cross(normal, Vector3.UnitY);
                        }
                        tangent.Normalize();
                        *((Vector3*)newTangentPtr) = tangent;
                        bitangent = Vector3.Cross(normal, tangent);
                    }
                    else
                    {
                        *((Vector3*)newTangentPtr) = newTangentUnormalized / length;
                    }

                    // Calculate handedness
                    newTangentPtr[3] = Vector3.Dot(Vector3.Cross(normal, tangent), bitangent) < 0.0f ? -1.0f : 1.0f;

                    oldVertexOffset += oldVertexStride;
                    newVertexOffset += newVertexStride;
                }

                return new VertexTransformResult(newVertexDeclaration, newBufferData);
            }
        }

        /// <summary>
        /// Result of a vertex buffer transform.
        /// </summary>
        public struct VertexTransformResult
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="VertexTransformResult"/> struct.
            /// </summary>
            /// <param name="layout">The layout.</param>
            /// <param name="vertexBuffer">The vertex buffer.</param>
            public VertexTransformResult(VertexDeclaration layout, byte[] vertexBuffer)
            {
                Layout = layout;
                VertexBuffer = vertexBuffer;
            }

            /// <summary>
            /// The new layout of the vertex buffer.
            /// </summary>
            public readonly VertexDeclaration Layout;

            /// <summary>
            /// The vertex buffer.
            /// </summary>
            public readonly byte[] VertexBuffer;
        }
    }
}
