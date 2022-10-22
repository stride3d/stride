// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Serializers;

namespace Stride.Graphics
{
    public class IndirectDrawingArgs
    {
        /// <summary>
        /// Creates an instance of <see cref="IndirectDrawingArgs"/> for DrawAuto with a stream-out vertex buffer.
        /// </summary>
        public IndirectDrawingArgs()
        {
            DrawAuto = true;
        }

        /// <summary>
        /// Creates an instance of <see cref="IndirectDrawingArgs"/> for DrawIndirect with a buffer containing the draw arguments.
        /// </summary>
        public IndirectDrawingArgs(Buffer argumentBuffer, int alignedByteOffset = 0)
        {
            if (argumentBuffer == null) throw new ArgumentNullException("argmentBuffer");
            ArgumentBuffer = argumentBuffer;
            AlignedByteOffset = alignedByteOffset;
        }

        public readonly bool DrawAuto;
        public readonly Buffer ArgumentBuffer;
        public readonly int AlignedByteOffset;
    }
}
