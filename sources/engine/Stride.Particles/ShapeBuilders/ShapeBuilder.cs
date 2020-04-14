// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Particles.Sorters;
using Stride.Particles.VertexLayouts;

namespace Stride.Particles.ShapeBuilders
{
    /// <summary>
    /// The <see cref="ShapeBuilder"/> base class is responsible for generating shapes (procedural mesh) ready for rendering from the particle data
    /// </summary>
    [DataContract("ShapeBuilder")]
    public abstract class ShapeBuilder
    {
        /// <summary>
        /// Returns the number of quads required per particle to draw all particles. Assuming 1 Quad = 4 Vertices = 6 Indices
        /// </summary>
        [DataMemberIgnore]
        public abstract int QuadsPerParticle { get; protected set; }

        /// <summary>
        /// Indicates that the required vertex layout has changed and <see cref="UpdateVertexBuilder"/> should be called
        /// </summary>
        [DataMemberIgnore]
        public bool VertexLayoutHasChanged { get; protected set; } = true;

        public virtual void PreUpdate() { }

        /// <summary>
        /// Builds the actual vertex buffer for the current frame using the particle data
        /// </summary>
        /// <param name="bufferState">Target particle buffer state, used to populate the assigned vertex buffer</param>
        /// <param name="invViewX">Unit vector X (right) in camera space, extracted from the inverse view matrix</param>
        /// <param name="invViewY">Unit vector Y (up) in camera space, extracted from the inverse view matrix</param>
        /// <param name="spaceTranslation">Translation of the target draw space in regard to the particle data (world or local)</param>
        /// <param name="spaceRotation">Rotation of the target draw space in regard to the particle data (world or local)</param>
        /// <param name="spaceScale">Uniform scale of the target draw space in regard to the particle data (world or local)</param>
        /// <param name="sorter">Particle enumerator which can be iterated and returns sported particles</param>
        /// <param name="viewProj">The View-Projection matrix which is used for some shape builders</param>
        /// <returns></returns>
        public abstract int BuildVertexBuffer(ref ParticleBufferState bufferState, Vector3 invViewX, Vector3 invViewY,
            ref Vector3 spaceTranslation, ref Quaternion spaceRotation, float spaceScale, ref ParticleList sorter, ref Matrix viewProj);

        /// <summary>
        /// Check if ParticleVertexElements should be changed and set HasVertexLayoutChanged = true; if they do
        /// </summary>
        /// <param name="fieldsList">A container for the <see cref="ParticlePool"/> which can poll if a certain field exists as an attribute</param>
        public virtual void PrepareVertexLayout(ParticlePoolFieldsList fieldsList)
        {
            // Check if ParticleVertexElements should be changed and set HasVertexLayoutChanged = true; if they do
        }

        /// <summary>
        /// Should be invoked if the <see cref="VertexLayoutHasChanged"/> was <c>true</c> so that new layout fields can be added to the buffer builder
        /// </summary>
        /// <param name="vertexBuilder">Target vertex buffer stream builder which will be used for the current frame</param>
        public virtual void UpdateVertexBuilder(ParticleVertexBuilder vertexBuilder)
        {
            // You can add ParticleVertexElements here

            VertexLayoutHasChanged = false;
        }

        /// <summary>
        /// Sets the required quads per particle and number of particles so that a sufficiently big buffer can be allocated
        /// </summary>
        /// <param name="quadsPerParticle">Required quads per particle, assuming 1 quad = 4 vertices = 6 indices</param>
        /// <param name="livingParticles">Number of living particles this frame</param>
        /// <param name="totalParticles">Number of total number of possible particles for the parent emitter</param>
        public virtual void SetRequiredQuads(int quadsPerParticle, int livingParticles, int totalParticles) { }

        /// <summary>
        /// Finds the circumcenter coordinates for triangle ABC
        /// </summary>
        public static Vector3 Circumcenter(ref Vector3 A, ref Vector3 B, ref Vector3 C)
        {
            var a = A - C;
            var b = B - C;

            var crossAB = Vector3.Cross(a, b);
            var lenAB = crossAB.LengthSquared();
            if (lenAB < MathUtil.ZeroTolerance)
                return C;

            return C + Vector3.Cross(a.LengthSquared() * b - b.LengthSquared() * a, crossAB) / (2 * lenAB);
        }
    }
}
