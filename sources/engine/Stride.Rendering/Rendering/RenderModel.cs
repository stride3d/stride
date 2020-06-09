// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

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

        /// <summary>
        /// The number of <see cref="Mesh"/>es when <see cref="Meshes"/> was generated.
        /// </summary>
        /// <remarks>
        /// A single mesh may be split into multiple RenderMeshes due to multiple material passes.
        /// </remarks>
        public int UniqueMeshCount;
        public struct MaterialInfo
        {
            public Material Material;
            public int MeshStartIndex;
            public int MeshCount;
        }
    }
}
