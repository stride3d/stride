// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Stride.Core;
using Stride.Graphics.Data;
using Stride.Rendering;

namespace Stride.Extensions
{
    public static class VertexExtensions
    {
        /// <summary>
        /// Extracts a selection of vertices from a vertex buffer stored in this mesh data.
        /// </summary>
        /// <param name="meshData">The mesh data.</param>
        /// <param name="vertexElementToExtract">The declaration to extract (e.g. "POSITION0"...etc.) </param>
        public static unsafe T[] GetVertexBufferData<T>(this MeshDraw meshData, params string[] vertexElementToExtract) where T : unmanaged
        {
            var declaration = meshData.VertexBuffers[0].Declaration;

            var offsets = declaration.EnumerateWithOffsets().Where(vertexElementOffset => vertexElementToExtract.Contains(vertexElementOffset.VertexElement.SemanticAsText)).ToList();

            int expectedSize = offsets.Sum(vertexElementWithOffset => vertexElementWithOffset.Size);

            var count = meshData.VertexBuffers[0].Count;

            int outputSize = expectedSize * count;

            int checkSize = (int)(outputSize / Unsafe.SizeOf<T>()) * Unsafe.SizeOf<T>();
            if (checkSize != outputSize)
                throw new ArgumentException(string.Format("Size of T is not a multiple of totalSize {0}", outputSize));

            var output = new T[outputSize / Unsafe.SizeOf<T>()];

            fixed (T* ptrOutputT = output)
            fixed (byte* ptrInputStart = meshData.VertexBuffers[0].Buffer.GetSerializationData().Content) {
                var ptrOutput = (byte*)ptrOutputT;
                var ptrInput = ptrInputStart;
                for (int i = 0; i < count; i++)
                {
                    foreach (var vertexElementWithOffset in offsets)
                    {
                        Unsafe.CopyBlockUnaligned(ptrOutput, ptrInput + vertexElementWithOffset.Offset, (uint)vertexElementWithOffset.Size);
                        ptrOutput += vertexElementWithOffset.Size;
                    }
                    ptrInput += declaration.VertexStride;
                }
            }

            return output;
        }
    }
}
