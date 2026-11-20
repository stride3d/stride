// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Particles.Sorters;
using Stride.Particles.VertexLayouts;
using System.Numerics;

namespace Stride.Particles.ShapeBuilders
{
    /// <summary>
    /// Shape builder which builds each particle as a non-uniform quad oriented along an axis
    /// </summary>
    [DataContract("ShapeBuilderOrientedQuad")]
    [Display("Direction Aligned Sprite")]
    public class ShapeBuilderOrientedQuad : ShapeBuilderCommon
    {
        /// <summary>
        /// If <c>true</c>, length will scale with particle size
        /// </summary>
        /// <userdoc>
        /// If true, length will scale with particle size
        /// </userdoc>
        [DataMember(300)]
        [Display("Size to Length")]
        public bool ScaleLength { get; set; } = true;

        /// <summary>
        /// Length will be modified with this factor
        /// </summary>
        /// <userdoc>
        /// Length will be modified with this factor
        /// </userdoc>
        [DataMember(310)]
        [Display("Length factor")]
        public float LengthFactor { get; set; } = 1f;

        /// <inheritdoc />
        public override int QuadsPerParticle { get; protected set; } = 1;

        /// <inheritdoc />
        public override unsafe int BuildVertexBuffer(ref ParticleBufferState bufferState, Vector3 invViewX, Vector3 invViewY,
            ref Vector3 spaceTranslation, ref Quaternion spaceRotation, float spaceScale, ref ParticleList sorter, ref Matrix4x4 viewProj)
        {
            // Update the curve samplers if required
            base.BuildVertexBuffer(ref bufferState, invViewX, invViewY, ref spaceTranslation, ref spaceRotation, spaceScale, ref sorter, ref viewProj);

            // Get all the required particle fields
            var positionField = sorter.GetField(ParticleFields.Position);
            if (!positionField.IsValid())
                return 0;
            var lifeField = sorter.GetField(ParticleFields.Life);
            var sizeField = sorter.GetField(ParticleFields.Size);
            var directionField = sorter.GetField(ParticleFields.Direction);

            // Check if the draw space is identity - in this case we don't need to transform the position, scale and rotation vectors
            var trsIdentity = (spaceScale == 1f);
            trsIdentity = trsIdentity && (spaceTranslation.NearEquals(Vector3.Zero));
            trsIdentity = trsIdentity && (spaceRotation.NearEquals(Quaternion.Identity));


            var renderedParticles = 0;

            var posAttribute = bufferState.GetAccessor(VertexAttributes.Position);
            var texAttribute = bufferState.GetAccessor(bufferState.DefaultTexCoords);

            // TODO Use viewProj
            Vector3 invViewZ;
            invViewZ = Vector3.Cross(invViewX, invViewY);
            invViewZ = Vector3.NormalizeSafe(invViewZ);

            foreach (var particle in sorter)
            {
                var centralPos = GetParticlePosition(particle, positionField, lifeField);

                var centralOffset = (directionField.IsValid()) ? particle.Get(directionField) : new Vector3(0, 1, 0);

                var particleSize = GetParticleSize(particle, sizeField, lifeField);

                if (!trsIdentity)
                {
                    centralPos = Vector3.Transform(centralPos, spaceRotation);
                    centralPos = centralPos * spaceScale + spaceTranslation;

                    centralOffset = Vector3.Transform(centralOffset, spaceRotation);
                    centralOffset = centralOffset * spaceScale;

                    particleSize *= spaceScale;
                }

                var unitX = invViewX;
                var unitY = invViewY;

                {
                    var centralAxis = centralOffset;

                    float dotZ;
                    dotZ = Vector3.Dot(centralAxis, invViewZ);
                    centralAxis -= invViewZ*dotZ;
                    centralAxis = Vector3.NormalizeSafe(centralAxis);

                    float dotX;
                    dotX = Vector3.Dot(centralAxis, unitX);

                    float dotY;
                    dotY = Vector3.Dot(centralAxis, unitY);

                    unitX = unitX*dotY - unitY*dotX;
                    unitX = Vector3.NormalizeSafe(unitX);

                    unitY = centralOffset;
                }

                // Use half size to make a Size = 1 result in a Billboard of 1m x 1m
                unitX *= (particleSize * 0.5f);
                if (ScaleLength)
                {
                    unitY *= (LengthFactor * particleSize * 0.5f);
                }
                else
                {
                    unitY *= (LengthFactor * 0.5f);
                }

                var particlePos = centralPos - unitX + unitY;
                var uvCoord = Vector2.Zero;
                // 0f 0f
                bufferState.SetAttribute(posAttribute, (IntPtr)(&particlePos));
                bufferState.SetAttribute(texAttribute, (IntPtr)(&uvCoord));
                bufferState.NextVertex();


                // 1f 0f
                particlePos += unitX * 2;
                uvCoord.X = 1;
                bufferState.SetAttribute(posAttribute, (IntPtr)(&particlePos));
                bufferState.SetAttribute(texAttribute, (IntPtr)(&uvCoord));
                bufferState.NextVertex();


                // 1f 1f
                particlePos -= unitY * 2;
                uvCoord.Y = 1;
                bufferState.SetAttribute(posAttribute, (IntPtr)(&particlePos));
                bufferState.SetAttribute(texAttribute, (IntPtr)(&uvCoord));
                bufferState.NextVertex();


                // 0f 1f
                particlePos -= unitX * 2;
                uvCoord.X = 0;
                bufferState.SetAttribute(posAttribute, (IntPtr)(&particlePos));
                bufferState.SetAttribute(texAttribute, (IntPtr)(&uvCoord));
                bufferState.NextVertex();

                renderedParticles++;
            }

            var vtxPerShape = 4 * QuadsPerParticle;
            return renderedParticles * vtxPerShape;
        }

    }
}
