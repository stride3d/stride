// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Particles.Sorters;
using Xenko.Particles.VertexLayouts;

namespace Xenko.Particles.ShapeBuilders
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
            ref Vector3 spaceTranslation, ref Quaternion spaceRotation, float spaceScale, ref ParticleList sorter, ref Matrix viewProj)
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
            trsIdentity = trsIdentity && (spaceTranslation.Equals(Vector3.Zero));
            trsIdentity = trsIdentity && (spaceRotation.Equals(Quaternion.Identity));


            var renderedParticles = 0;

            var posAttribute = bufferState.GetAccessor(VertexAttributes.Position);
            var texAttribute = bufferState.GetAccessor(bufferState.DefaultTexCoords);

            // TODO Use viewProj
            Vector3 invViewZ;
            Vector3.Cross(ref invViewX, ref invViewY, out invViewZ);
            invViewZ.Normalize();

            foreach (var particle in sorter)
            {
                var centralPos = GetParticlePosition(particle, positionField, lifeField);

                var centralOffset = (directionField.IsValid()) ? particle.Get(directionField) : new Vector3(0, 1, 0);

                var particleSize = GetParticleSize(particle, sizeField, lifeField);

                if (!trsIdentity)
                {
                    spaceRotation.Rotate(ref centralPos);
                    centralPos = centralPos * spaceScale + spaceTranslation;

                    spaceRotation.Rotate(ref centralOffset);
                    centralOffset = centralOffset * spaceScale;

                    particleSize *= spaceScale;
                }

                var unitX = invViewX;
                var unitY = invViewY;

                {
                    var centralAxis = centralOffset;

                    float dotZ;
                    Vector3.Dot(ref centralAxis, ref invViewZ, out dotZ);
                    centralAxis -= invViewZ*dotZ;
                    centralAxis.Normalize();

                    float dotX;
                    Vector3.Dot(ref centralAxis, ref unitX, out dotX);

                    float dotY;
                    Vector3.Dot(ref centralAxis, ref unitY, out dotY);

                    unitX = unitX*dotY - unitY*dotX;
                    unitX.Normalize();

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
