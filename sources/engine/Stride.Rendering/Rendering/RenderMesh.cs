// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Rendering.Materials;

namespace Stride.Rendering
{
    /// <summary>
    /// Used by <see cref="MeshRenderFeature"/> to render a <see cref="Rendering.Mesh"/>.
    /// </summary>
    public class RenderMesh : RenderObject
    {
        public MeshDraw ActiveMeshDraw;

        public RenderModel RenderModel;

        /// <summary>
        /// Underlying mesh, can be accessed only during <see cref="RenderFeature.Extract"/> phase.
        /// </summary>
        public Mesh Mesh;

        // Material
        // TODO: Extract with MaterialRenderFeature
        public MaterialPass MaterialPass;

        // TODO GRAPHICS REFACTOR store that in RenderData (StaticObjectNode?)
        internal MaterialRenderFeature.MaterialInfo MaterialInfo;

        public bool IsShadowCaster;

        public bool IsScalingNegative;

        public bool IsPreviousScalingNegative;

        public Matrix World = Matrix.Identity;

        public Matrix[] BlendMatrices;

        public int InstanceCount;
    }
}
