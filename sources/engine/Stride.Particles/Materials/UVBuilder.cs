// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Particles.Sorters;
using Stride.Particles.VertexLayouts;

namespace Stride.Particles.Materials
{
    /// <summary>
    /// Base class for building and animating the texture coordinates in a particle vertex buffer stream
    /// </summary>
    [DataContract("UVBuilder")]
    public abstract class UVBuilder
    {
        /// <summary>
        /// Enhances or animates the texture coordinates using already existing base coordinates of (0, 0, 1, 1) or similar
        /// (base texture coordinates may differ depending on the actual shape)
        /// </summary>
        /// <param name="bufferState">The particle buffer state which is used to build the assigned vertex buffer</param>
        /// <param name="sorter"><see cref="ParticleSorter"/> to use to iterate over all particles drawn this frame</param>
        /// <param name="texCoordsDescription">Attribute description of the texture coordinates in the current vertex layout</param>
        public abstract void BuildUVCoordinates(ref ParticleBufferState bufferState, ref ParticleList sorter, AttributeDescription texCoordsDescription);
    }
}
