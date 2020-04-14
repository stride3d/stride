// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
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
        public static T[] GetVertexBufferData<T>(this MeshDraw meshData, params string[] vertexElementToExtract) where T : struct
        {
            var declaration = meshData.VertexBuffers[0].Declaration;

            var offsets = declaration.EnumerateWithOffsets().Where(vertexElementOffset => vertexElementToExtract.Contains(vertexElementOffset.VertexElement.SemanticAsText)).ToList();

            int expectedSize = offsets.Sum(vertexElementWithOffset => vertexElementWithOffset.Size);

            var count = meshData.VertexBuffers[0].Count;

            int outputSize = expectedSize * count;

            int checkSize = (int)(outputSize / Utilities.SizeOf<T>()) * Utilities.SizeOf<T>();
            if (checkSize != outputSize)
                throw new ArgumentException(string.Format("Size of T is not a multiple of totalSize {0}", outputSize));

            var output = new T[outputSize / Utilities.SizeOf<T>()];

            var handleOutput = GCHandle.Alloc(output, GCHandleType.Pinned);
            var ptrOutput = handleOutput.AddrOfPinnedObject();

            var handleInput = GCHandle.Alloc(meshData.VertexBuffers[0].Buffer.GetSerializationData().Content, GCHandleType.Pinned);
            var ptrInput = handleInput.AddrOfPinnedObject();

            for (int i = 0; i < count; i++)
            {
                foreach (var vertexElementWithOffset in offsets)
                {
                    Utilities.CopyMemory(ptrOutput, ptrInput + vertexElementWithOffset.Offset, vertexElementWithOffset.Size);
                    ptrOutput = ptrOutput + vertexElementWithOffset.Size;
                }
                ptrInput += declaration.VertexStride;
            }

            handleInput.Free();
            handleOutput.Free();
            return output;
        }
    }
}
