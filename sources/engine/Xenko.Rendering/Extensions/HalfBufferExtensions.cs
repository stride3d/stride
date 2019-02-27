// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Graphics.Data;

namespace Xenko.Extensions
{
    public static class HalfBufferExtensions
    {
        public static unsafe void CompactHalf(ref VertexBufferBinding vertexBufferBinding)
        {
            var vertexElementsWithOffsets = vertexBufferBinding.Declaration
                .EnumerateWithOffsets()
                .OrderBy(x => x.Offset)
                .ToArray();

            var vertexElements = new VertexElementConvertInfo[vertexElementsWithOffsets.Length];

            int currentOffset = 0;
            for (int index = 0; index < vertexElementsWithOffsets.Length; index++)
            {
                var vertexElementConvertInfo = new VertexElementConvertInfo();
                vertexElementConvertInfo.VertexElementWithOffset = vertexElementsWithOffsets[index];
                var vertexElement = vertexElementsWithOffsets[index].VertexElement;
                var vertexElementFormat = vertexElementConvertInfo.VertexElementWithOffset.VertexElement.Format;

                // First iteration?
                if (index == 0)
                    currentOffset = vertexElementsWithOffsets[index].Offset;

                vertexElements[index] = vertexElementConvertInfo;
                vertexElementConvertInfo.OldFormat = vertexElementConvertInfo.VertexElementWithOffset.VertexElement.Format;

                int offsetShift = 0;

                switch (vertexElementFormat)
                {
                    case PixelFormat.R32G32_Float:
                        vertexElementFormat = PixelFormat.R16G16_Float;

                        // Adjust next offset if current object has been resized
                        offsetShift = Utilities.SizeOf<Half2>() - Utilities.SizeOf<Vector2>();
                        break;
                    case PixelFormat.R32G32B32_Float:
                        vertexElementFormat = PixelFormat.R16G16B16A16_Float;

                        // Adjust next offset if current object has been resized
                        offsetShift = Utilities.SizeOf<Half4>() - Utilities.SizeOf<Vector3>();
                        break;
                    case PixelFormat.R32G32B32A32_Float:
                        vertexElementFormat = PixelFormat.R16G16B16A16_Float;

                        // Adjust next offset if current object has been resized
                        offsetShift = Utilities.SizeOf<Half4>() - Utilities.SizeOf<Vector4>();
                        break;
                }

                // Has format changed?
                vertexElementConvertInfo.NeedConversion = vertexElementFormat != vertexElementConvertInfo.VertexElementWithOffset.VertexElement.Format;

                // Create new vertex element with adjusted offset, and maybe new vertex format (if modified)
                vertexElementConvertInfo.VertexElementWithOffset.VertexElement
                    = new VertexElement(vertexElement.SemanticName, vertexElement.SemanticIndex, vertexElementFormat, currentOffset);

                // Increment next offset by the same difference as in original declaration
                if (index + 1 < vertexElementsWithOffsets.Length)
                    currentOffset += vertexElementsWithOffsets[index + 1].Offset - vertexElementsWithOffsets[index].Offset;

                currentOffset += offsetShift;

                vertexElements[index] = vertexElementConvertInfo;
            }

            var oldVertexStride = vertexBufferBinding.Declaration.VertexStride;

            var vertexDeclaration = new VertexDeclaration(vertexElements.Select(x => x.VertexElementWithOffset.VertexElement).ToArray());

            var newVertexStride = vertexDeclaration.VertexStride;
            var newBufferData = new byte[vertexBufferBinding.Count * newVertexStride];
            fixed (byte* oldBuffer = &vertexBufferBinding.Buffer.GetSerializationData().Content[vertexBufferBinding.Offset])
            fixed (byte* newBuffer = &newBufferData[0])
            {
                var oldBufferVertexPtr = (IntPtr)oldBuffer;
                var newBufferVertexPtr = (IntPtr)newBuffer;
                for (int i = 0; i < vertexBufferBinding.Count; ++i)
                {
                    foreach (var element in vertexElements)
                    {
                        var oldBufferElementPtr = oldBufferVertexPtr + element.VertexElementWithOffset.Offset;
                        var newBufferElementPtr = newBufferVertexPtr + element.VertexElementWithOffset.VertexElement.AlignedByteOffset;

                        if (element.NeedConversion)
                        {
                            // Convert floatX => halfX
                            switch (element.OldFormat)
                            {
                                case PixelFormat.R32G32_Float:
                                    *((Half2*)newBufferElementPtr) = (Half2)(*((Vector2*)oldBufferElementPtr));
                                    break;
                                case PixelFormat.R32G32B32_Float:
                                    // Put 1.0f in 
                                    *((Half4*)newBufferElementPtr) = (Half4)(new Vector4(*((Vector3*)oldBufferElementPtr), 1.0f));
                                    break;
                                case PixelFormat.R32G32B32A32_Float:
                                    *((Half4*)newBufferElementPtr) = (Half4)(*((Vector4*)oldBufferElementPtr));
                                    break;
                            }
                        }
                        else
                        {
                            // Copy as is
                            Utilities.CopyMemory(newBufferElementPtr, oldBufferElementPtr, element.VertexElementWithOffset.Size);
                        }
                    }

                    oldBufferVertexPtr += oldVertexStride;
                    newBufferVertexPtr += newVertexStride;
                }
            }

            vertexBufferBinding = new VertexBufferBinding(new BufferData(BufferFlags.VertexBuffer, newBufferData).ToSerializableVersion(), vertexDeclaration, vertexBufferBinding.Count);
        }

        private struct VertexElementConvertInfo
        {
            public VertexElementWithOffset VertexElementWithOffset;
            public bool NeedConversion;
            public PixelFormat OldFormat;
        }
    }
}
