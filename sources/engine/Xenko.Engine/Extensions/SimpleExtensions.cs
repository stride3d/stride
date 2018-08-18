// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Graphics;
using Xenko.Graphics.Data;
using Xenko.Rendering;

namespace Xenko.Extensions
{
    public static class SimpleExtensions
    {
        /// <summary>
        /// Determines whether the specified vertex buffer binding data is simple.
        /// A vertex buffer binding data is simple if:
        /// * Offset is 0.
        /// * Stride is 0 (automatic), or equals to Declaration.VertexStride.
        /// * Buffer.Content.Length is equal to Declaration.VertexStride * Count
        /// </summary>
        /// <param name="vertexBufferBindingData">The vertex buffer binding data.</param>
        /// <returns></returns>
        public static bool IsSimple(this VertexBufferBinding vertexBufferBindingData)
        {
            if (vertexBufferBindingData.Offset != 0)
                return false;

            var stride = vertexBufferBindingData.Declaration.VertexStride;
            if (vertexBufferBindingData.Stride != 0
                && vertexBufferBindingData.Stride != stride)
                return false;

            var buffer = vertexBufferBindingData.Buffer.GetSerializationData();
            if (buffer.Content.Length != stride * vertexBufferBindingData.Count)
                return false;

            return true;
        }

        /// <summary>
        /// Determines whether the specified index buffer binding data is simple.
        /// A index buffer binding data is simple if:
        /// * Offset is 0.
        /// * Is32Bit is true.
        /// * Buffer.Content.Length is equal to sizeof(int) * Count.
        /// </summary>
        /// <param name="indexBufferBindingData">The index buffer binding data.</param>
        /// <returns></returns>
        public static bool IsSimple(this IndexBufferBinding indexBufferBindingData)
        {
            if (indexBufferBindingData.Offset != 0)
                return false;

            if (!indexBufferBindingData.Is32Bit)
                return false;

            var buffer = indexBufferBindingData.Buffer.GetSerializationData();
            if (buffer.Content.Length != sizeof(int) * indexBufferBindingData.Count)
                return false;

            return true;
        }

        /// <summary>
        /// Determines whether the specified mesh draw data is simple.
        /// A <see cref="MeshDrawData"/> is simple if:
        /// * It contains only one <see cref="VertexBufferBindingData"/>, which must be simple.
        /// * It contains either no <see cref="IndexBufferBindingData"/>, or a simple one.
        /// * StartLocation is 0.
        /// * DrawCount is IndexBuffer.Count if there is an index buffer, otherwise VertexBuffers[0].Count.
        /// </summary>
        /// <param name="meshDrawData">The mesh draw data.</param>
        /// <returns></returns>
        public static bool IsSimple(this MeshDraw meshDrawData)
        {
            if (meshDrawData.VertexBuffers.Length != 1)
                return false;

            if (!meshDrawData.VertexBuffers[0].IsSimple())
                return false;

            if (meshDrawData.IndexBuffer != null)
            {
                if (!meshDrawData.IndexBuffer.IsSimple())
                    return false;

                if (meshDrawData.DrawCount != meshDrawData.IndexBuffer.Count)
                    return false;
            }
            else
            {
                if (meshDrawData.DrawCount != meshDrawData.VertexBuffers[0].Count)
                    return false;
            }

            if (meshDrawData.StartLocation != 0)
                return false;

            return true;
        }
    }
}
