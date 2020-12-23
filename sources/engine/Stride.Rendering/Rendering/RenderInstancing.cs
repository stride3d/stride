// Copyright (c) Stride contributors (https://stride3d.net) and Tebjan Halm
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Rendering
{
    /// <summary>
    /// Contains information for model instancing. Used by the <see cref="InstancingRenderFeature"/>
    /// </summary>
    public class RenderInstancing
    {
        public int InstanceCount;
        public int ModelTransformUsage;

        // Data
        public Matrix[] WorldMatrices;
        public Matrix[] WorldInverseMatrices;

        // GPU buffers
        public bool BuffersManagedByUser;
        public Buffer InstanceWorldBuffer;
        public Buffer InstanceWorldInverseBuffer;
    }
}
