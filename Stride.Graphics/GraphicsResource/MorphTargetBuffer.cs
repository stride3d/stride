using System;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Graphics
{
    public class MorphTargetBuffer : IDisposable
    {
        public Buffer OffsetBuffer { get; private set; }
        public int TargetCount { get; private set; }

        public MorphTargetBuffer(GraphicsDevice device, int vertexCount, int targetCount)
        {
            TargetCount = targetCount;
            // Buffer contendo os offsets de posição para cada morph target
            OffsetBuffer = device.Factory.CreateBuffer(new BufferDescription(
                vertexCount * targetCount * Vector3.SizeInBytes,
                BufferFlags.ShaderResource,
                ResourceUsage.Default));
        }

        public void Dispose()
        {
            OffsetBuffer?.Dispose();
        }
    }
}
