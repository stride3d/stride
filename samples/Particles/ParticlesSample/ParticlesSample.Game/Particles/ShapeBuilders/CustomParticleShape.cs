// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Particles;
using Stride.Particles.Sorters;
using Stride.Particles.VertexLayouts;
using Stride.Particles.ShapeBuilders;

namespace ParticlesSample.Particles.ShapeBuilders
{
    [DataContract("CustomParticleShape")]
    [Display("CustomShape")]
    public class CustomParticleShape : ShapeBuilder
    {
        [DataMember(100)]
        public bool FixYAxis = false;

        private int quadsPerParticle = 1;

        //  One particle requires 1 quad = 4 vertices (6 indices)
        public override int QuadsPerParticle
        {
            get { return quadsPerParticle; }
            protected set { quadsPerParticle = value; }
        }

        /// <inheritdoc />
        public override unsafe int BuildVertexBuffer(ref ParticleBufferState bufferState, Vector3 invViewX, Vector3 invViewY,
            ref Vector3 spaceTranslation, ref Quaternion spaceRotation, float spaceScale, ref ParticleList sorter, ref Matrix viewProj)
        {
            // Step 1 - get all required fields to build the particle shapes. Some fields may not exist if no initializer or updater operates on them
            //  In that case we just decide on a default value for that field and skip the update

            var positionField = sorter.GetField(ParticleFields.Position);
            if (!positionField.IsValid())
                return 0; // We can't display the particles without position. All other fields are optional

            var sizeField = sorter.GetField(ParticleFields.Size);
            var angleField = sorter.GetField(ParticleFields.Angle);
            var hasAngle = angleField.IsValid();

            var rectField = sorter.GetField(CustomParticleFields.RectangleXY);
            var isRectangle = rectField.IsValid();

            // In case of Local space particles they are simulated in local emitter space, but drawn in world space
            //  If the draw space is identity (i.e. simulation space = draw space) skip transforming the particle's location later
            var trsIdentity = (spaceScale == 1f);
            trsIdentity = trsIdentity && (spaceTranslation.Equals(new Vector3(0, 0, 0)));
            trsIdentity = trsIdentity && (spaceRotation.Equals(new Quaternion(0, 0, 0, 1)));

            // Custom feature - fix the Y axis to always point up in world space rather than screen space
            if (FixYAxis)
            {
                invViewY = new Vector3(0, 1, 0);
                invViewX.Y = 0;
                invViewX.Normalize();
            }

            var renderedParticles = 0;

            var posAttribute = bufferState.GetAccessor(VertexAttributes.Position);
            var texAttribute = bufferState.GetAccessor(bufferState.DefaultTexCoords);

            foreach (var particle in sorter)
            {
                var centralPos = particle.Get(positionField);

                var particleSize = sizeField.IsValid() ? particle.Get(sizeField) : 1f;

                if (!trsIdentity)
                {
                    spaceRotation.Rotate(ref centralPos);
                    centralPos = centralPos * spaceScale + spaceTranslation;
                    particleSize *= spaceScale;
                }

                var unitX = invViewX * particleSize;
                var unitY = invViewY * particleSize;
                if (isRectangle)
                {
                    var rectSize = particle.Get(rectField);

                    unitX *= rectSize.X;
                    unitY *= rectSize.Y;
                }

                // Particle rotation. Positive value means clockwise rotation.
                if (hasAngle)
                {
                    var rotationAngle = particle.Get(angleField);
                    var cosA = (float)Math.Cos(rotationAngle);
                    var sinA = (float)Math.Sin(rotationAngle);
                    var tempX = unitX * cosA - unitY * sinA;
                    unitY = unitY * cosA + unitX * sinA;
                    unitX = tempX;
                }


                var particlePos = centralPos - unitX + unitY;
                var uvCoord = new Vector2(0, 0);
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

            // Return the number of updated vertices
            var vtxPerShape = 4 * QuadsPerParticle;
            return renderedParticles * vtxPerShape;
        }
    }
}
