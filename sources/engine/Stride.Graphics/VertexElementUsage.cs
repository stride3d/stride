// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Graphics
{
    public static class VertexElementUsage
    {
        /// <summary>
        /// Vertex data contains diffuse or specular color.
        /// </summary>
        public static readonly string Color = "COLOR";

        /// <summary>
        /// Vertex normal data.
        /// </summary>
        public static readonly string Normal = "NORMAL";

        /// <summary>
        /// Position data.
        /// </summary>
        public static readonly string Position = "POSITION";

        /// <summary>
        /// Position transformed data.
        /// </summary>
        public static readonly string PositionTransformed = "SV_POSITION";

        /// <summary>
        /// Vertex tangent data.
        /// </summary>
        public static readonly string Tangent = "TANGENT";

        /// <summary>
        /// Vertex Bitangent data.
        /// </summary>
        public static readonly string BiTangent = "BITANGENT";

        /// <summary>
        /// Texture coordinate data.
        /// </summary>
        public static readonly string TextureCoordinate = "TEXCOORD";

        /// <summary>
        /// Bone blend indices data.
        /// </summary>
        public static readonly string BlendIndices = "BLENDINDICES";

        /// <summary>
        /// Bone blend weight data.
        /// </summary>
        public static readonly string BlendWeight = "BLENDWEIGHT";
    }
}
