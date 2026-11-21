// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Tebjan Halm
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;
using Stride.Graphics;
using Buffer = Stride.Graphics.Buffer;
using System.Numerics;

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
        public Matrix4x4[] WorldMatrices;
        public Matrix4x4[] WorldInverseMatrices;

        // GPU buffers
        public bool BuffersManagedByUser;
        public Buffer InstanceWorldBuffer;
        public Buffer InstanceWorldInverseBuffer;
    }
}
