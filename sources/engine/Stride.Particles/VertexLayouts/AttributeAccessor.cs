// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Particles.VertexLayouts
{
    /// <summary>
    /// <see cref="AttributeAccessor"/> is use to access and modify a particle vertex attribute.
    /// </summary>
    public struct AttributeAccessor
    {
        /// <summary>
        /// Offset of the attribute from the beginning of the vertex position
        /// </summary>
        public int Offset;

        /// <summary>
        /// Size of the attribute field
        /// </summary>
        public int Size;
    }
}
