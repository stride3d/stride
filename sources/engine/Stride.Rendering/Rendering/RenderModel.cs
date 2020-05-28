// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;

namespace Stride.Rendering
{
    /// <summary>
    /// Contains information related to the <see cref="Rendering.Model"/> so that the <see cref="RenderMesh"/> can access it.
    /// </summary>
    public class RenderModel
    {
        public Model Model;
        public RenderMesh[] Meshes;
        public MaterialInfo[] Materials;

        // Transformation instancing, this could move up to RenderMesh, if needed per mesh
        public bool IsInstanced;
        public Matrix[] InstanceWorldMatrices = new Matrix[0];
        public Matrix[] InstanceWorldInverseMatrices = new Matrix[0];

        // TODO: Manage buffers, where to transfer the data to the buffers?
        public Buffer<Matrix> InstanceWorld;
        public Buffer<Matrix> InstanceWorldInverse;

        public struct MaterialInfo
        {
            public Material Material;
            public int MeshStartIndex;
            public int MeshCount;
        }
    }
}
