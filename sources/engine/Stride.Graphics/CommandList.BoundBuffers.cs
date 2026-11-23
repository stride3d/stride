// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_VULKAN || STRIDE_GRAPHICS_API_DIRECT3D12

namespace Stride.Graphics
{
    public partial class CommandList
    {
        /// <summary>
        ///   The number of Vertex Buffer slots tracked at first. The tracking grows when a higher slot is bound.
        /// </summary>
        private const int InitialTrackedVertexBufferCount = 16;

        private struct BoundBuffer
        {
            public Buffer Buffer;
            public int Offset;
            public int Stride;
            public bool Is32Bits;
        }

        private BoundBuffer[] boundVertexBuffers = new BoundBuffer[InitialTrackedVertexBufferCount];
        private BoundBuffer boundIndexBuffer;

        private void TrackVertexBuffer(int index, Buffer buffer, int offset, int stride)
        {
            if (index < 0)
                return;

            if (index >= boundVertexBuffers.Length)
            {
                var newLength = System.Math.Max(index + 1, boundVertexBuffers.Length * 2);
                System.Array.Resize(ref boundVertexBuffers, newLength);
            }

            boundVertexBuffers[index] = new BoundBuffer { Buffer = buffer, Offset = offset, Stride = stride };
        }

        private void TrackIndexBuffer(Buffer buffer, int offset, bool is32Bits)
        {
            boundIndexBuffer = new BoundBuffer { Buffer = buffer, Offset = offset, Is32Bits = is32Bits };
        }

        private void ClearBoundBuffers()
        {
            System.Array.Clear(boundVertexBuffers);
            boundIndexBuffer = default;
        }

        /// <summary>
        ///   Binds again the slots that hold a Buffer whose native resource was replaced, so the next draws read the new one.
        /// </summary>
        private void RebindBuffer(Buffer buffer)
        {
            for (var index = 0; index < boundVertexBuffers.Length; index++)
            {
                var vertexBuffer = boundVertexBuffers[index];
                if (vertexBuffer.Buffer == buffer)
                    SetVertexBuffer(index, buffer, vertexBuffer.Offset, vertexBuffer.Stride);
            }

            if (boundIndexBuffer.Buffer == buffer)
                SetIndexBuffer(buffer, boundIndexBuffer.Offset, boundIndexBuffer.Is32Bits);
        }
    }
}

#endif
