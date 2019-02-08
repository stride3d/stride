// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Engine;

namespace Xenko.Rendering
{
    /// <summary>
    /// Contains information related to the <see cref="Rendering.Model"/> so that the <see cref="RenderMesh"/> can access it.
    /// </summary>
    public class RenderModel
    {
        public Model Model;
        public RenderMesh[] Meshes;
        public MaterialInfo[] Materials;


        public struct MaterialInfo
        {
            public Material Material;
            public int MeshStartIndex;
            public int MeshCount;
        }
    }
}
