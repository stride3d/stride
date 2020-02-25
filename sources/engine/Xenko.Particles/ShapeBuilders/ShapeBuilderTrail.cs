// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Particles.Initializers;
using Xenko.Particles.Sorters;
using Xenko.Particles.VertexLayouts;
using Xenko.Particles.ShapeBuilders.Tools;

namespace Xenko.Particles.ShapeBuilders
{
    /// <summary>
    /// Shape builder which builds all particles as a trail, connecting adjacent particles in a ribbon defined by a fixed 3d axis
    /// </summary>
    [DataContract("ShapeBuilderTrail")]
    [Display("Trail")]
    public class ShapeBuilderTrail : ShapeBuilder
    {
        private SmoothingPolicy smoothingPolicy;

        private int segments;

        private int currentTotalParticles;

        private int currentQuadsPerParticle;

        /// <summary>
        /// Smoothing provides the option to additionally smooth the ribbon, enhancing visual quality for sharp angles
        /// </summary>
        /// <userdoc>
        /// Smoothing provides the option to additionally smooth the ribbon, enhancing visual quality for sharp angles
        /// </userdoc>
        [DataMember(5)]
        [Display("Smoothing")]
        public SmoothingPolicy SmoothingPolicy
        {
            get { return smoothingPolicy; }
            set
            {
                smoothingPolicy = value;

                QuadsPerParticle = (smoothingPolicy == SmoothingPolicy.None) ?
                    1 : segments;
            }
        }

        /// <summary>
        /// If the ribbon is smotthed, how many segments should be used between each two particles
        /// </summary>
        /// <userdoc>
        /// If the ribbon is smotthed, how many segments should be used between each two particles
        /// </userdoc>
        [DataMember(6)]
        [Display("Segments")]
        public int Segments
        {
            get { return segments; }
            set
            {
                segments = value;

                QuadsPerParticle = (smoothingPolicy == SmoothingPolicy.None) ?
                    1 : segments;
            }
        }

        /// <summary>
        /// Should the axis of control point be treated as the trail's edge or the trail's center
        /// </summary>
        /// <userdoc>
        /// Should the axis of control point be treated as the trail's edge or the trail's center
        /// </userdoc>
        [DataMember(8)]
        [Display("Axis")]
        public EdgePolicy EdgePolicy { get; set; }

        /// <summary>
        /// Specifies how texture coordinates for the ribbons should be built
        /// </summary>
        /// <userdoc>
        /// Specifies how texture coordinates for the ribbons should be built
        /// </userdoc>
        [DataMember(10)]
        [Display("UV Coords")]
        public TextureCoordinatePolicy TextureCoordinatePolicy { get; set; }

        /// <summary>
        /// The factor (coefficient) for length to use when building texture coordinates
        /// </summary>
        /// <userdoc>
        /// The factor (coefficient) for length to use when building texture coordinates
        /// </userdoc>
        [DataMember(20)]
        [Display("UV Factor")]
        public float TexCoordsFactor { get; set; }

        /// <summary>
        /// Texture coordinates flip and rotate policy
        /// </summary>
        /// <userdoc>
        /// Texture coordinates flip and rotate policy
        /// </userdoc>
        [DataMember(30)]
        [Display("UV Rotate")]
        public UVRotate UVRotate { get; set; }

        /// <inheritdoc />
        public override int QuadsPerParticle { get; protected set; } = 1;

        /// <inheritdoc />
        public override void SetRequiredQuads(int quadsPerParticle, int livingParticles, int totalParticles)
        {
            currentTotalParticles = totalParticles;
            currentQuadsPerParticle = quadsPerParticle;
        }

        /// <inheritdoc />
        public override unsafe int BuildVertexBuffer(ref ParticleBufferState bufferState, Vector3 invViewX, Vector3 invViewY,
            ref Vector3 spaceTranslation, ref Quaternion spaceRotation, float spaceScale, ref ParticleList sorter, ref Matrix viewProj)
        {
            // Get all the required particle fields
            var positionField = sorter.GetField(ParticleFields.Position);
            if (!positionField.IsValid())
                return 0;
            var sizeField = sorter.GetField(ParticleFields.Size);
            var directionField = sorter.GetField(ParticleFields.Direction);

            // Check if the draw space is identity - in this case we don't need to transform the position, scale and rotation vectors
            var trsIdentity = (spaceScale == 1f);
            trsIdentity = trsIdentity && (spaceTranslation.Equals(Vector3.Zero));
            trsIdentity = trsIdentity && (spaceRotation.Equals(Quaternion.Identity));

            var ribbonizer = new Ribbonizer(this, currentTotalParticles, currentQuadsPerParticle);

            var renderedParticles = 0;
            bufferState.StartOver();

            uint oldOrderValue = 0;
            var orderField = sorter.GetField(ParticleFields.Order);

            foreach (var particle in sorter)
            {
                if (orderField.IsValid())
                {
                    var orderValue = (*((uint*)particle[orderField]));

                    if ((orderValue >> SpawnOrderConst.GroupBitOffset) != (oldOrderValue >> SpawnOrderConst.GroupBitOffset))
                    {
                        ribbonizer.Ribbonize(ref bufferState, QuadsPerParticle);
                        ribbonizer.RibbonSplit();
                    }

                    oldOrderValue = orderValue;
                }

                var centralPos = particle.Get(positionField);

                var particleSize = sizeField.IsValid() ? particle.Get(sizeField) : 1f;
                var particleDirection = directionField.IsValid() ? particle.Get(directionField) * particleSize : new Vector3(0f, particleSize, 0f);

                if (!trsIdentity)
                {
                    spaceRotation.Rotate(ref centralPos);
                    centralPos = centralPos * spaceScale + spaceTranslation;

                    // Direction
                    spaceRotation.Rotate(ref particleDirection);
                    particleDirection *= spaceScale;
                }

                ribbonizer.AddParticle(ref centralPos, ref particleDirection);

                renderedParticles++;
            }

            ribbonizer.Ribbonize(ref bufferState, QuadsPerParticle);

            ribbonizer.Free();

            var vtxPerShape = 4 * QuadsPerParticle;
            return renderedParticles * vtxPerShape;
        }

        /// <summary>
        /// The <see cref="Ribbonizer"/> takes a list of points and creates a ribbon (connected quads), adjusting its texture coordinates accordingly
        /// </summary>
        struct Ribbonizer
        {
            private int lastParticle;
            private int sections;

            private readonly IntPtr positionData;
            private readonly IntPtr directionData;

            private readonly int particleCapacity;
            private readonly ShapeBuilderTrail parentTrail;

            public Ribbonizer(ShapeBuilderTrail ribbon, int newCapacity, int sectionsPerParticle)
            {
                parentTrail = ribbon;

                lastParticle = 0;
                sections = sectionsPerParticle;

                int requiredCapacity = sectionsPerParticle * newCapacity;

                particleCapacity = requiredCapacity;

                int positionDataSize = Utilities.SizeOf<Vector3>() * particleCapacity;
                positionDataSize = (positionDataSize % 4 == 0) ? positionDataSize : (positionDataSize + 4 - (positionDataSize % 4));
                positionData = Utilities.AllocateMemory(positionDataSize);

                int directionDataSize = Utilities.SizeOf<Vector3>() * particleCapacity;
                directionDataSize = (directionDataSize % 4 == 0) ? directionDataSize : (directionDataSize + 4 - (directionDataSize % 4));
                directionData = Utilities.AllocateMemory(directionDataSize);
            }

            public void Free()
            {
                Utilities.FreeMemory(positionData);
                Utilities.FreeMemory(directionData);
            }

            EdgePolicy EdgePolicy => parentTrail.EdgePolicy;

            TextureCoordinatePolicy TextureCoordinatePolicy => parentTrail.TextureCoordinatePolicy;

            SmoothingPolicy SmoothingPolicy => parentTrail.SmoothingPolicy;

            float TexCoordsFactor => parentTrail.TexCoordsFactor;

            UVRotate UVRotate => parentTrail.UVRotate;

            /// <summary>
            /// Splits (cuts) the trail without restarting or rebuilding the vertex buffer
            /// </summary>
            public void RibbonSplit()
            {
                lastParticle = 0;
            }

            /// <summary>
            /// Adds a new particle position and size to the point string
            /// </summary>
            /// <param name="position">Position of the control point</param>
            /// <param name="direction">Direction or offset from the control point</param>
            public unsafe void AddParticle(ref Vector3 position, ref Vector3 direction)
            {
                if (lastParticle >= particleCapacity)
                    return;

                var positions = (Vector3*)positionData;
                var directions = (Vector3*)directionData;

                positions[lastParticle] = position;
                directions[lastParticle] = direction;

                lastParticle += sections;
            }

            /// <summary>
            /// Advanced interpolation, drawing the vertices in a circular arc between two adjacent control points
            /// </summary>
            private unsafe void ExpandVertices_Circular()
            {
                if (sections <= 1)
                    return;

                var positions = (Vector3*)positionData;
                var directions = (Vector3*)directionData;

                var lerpStep = 1f / sections;

                var Pt0 = positions[0] * 2 - positions[sections];
                var Pt1 = positions[0];
                var Pt2 = positions[sections];

                var O1 = Circumcenter(ref Pt0, ref Pt1, ref Pt2);
                var R1 = (O1 - Pt1).Length();

                var d1 = directions[0];
                var d2 = directions[sections];

                int index = 0;
                while (index < lastParticle)
                {
                    var Pt3 = (index + sections * 2 < lastParticle) ? positions[index + sections * 2] : Pt2;
                    var d3 = (index + sections * 2 < lastParticle) ? directions[index + sections * 2] : new Vector3(0f, 0f, 0f);
                    var O2 = Circumcenter(ref Pt1, ref Pt2, ref Pt3);
                    var R2 = (O2 - Pt2).Length();

                    if (index + sections * 2 >= lastParticle)
                    {
                        O2 = O1;
                        R2 = R1;
                    }

                    for (int j = 1; j < sections; j++)
                    {
                        positions[index + j] = Vector3.Lerp(Pt1, Pt2, j * lerpStep);

                        // Circular motion
                        var dist1 = positions[index + j] - O1;
                        dist1.Normalize();
                        var dist2 = positions[index + j] - O2;
                        dist2.Normalize();

                        positions[index + j] = Vector3.Lerp(O1 + dist1 * R1, O2 + dist2 * R2, j * lerpStep);

                        directions[index + j] = Vector3.Lerp(d1, d2, j * lerpStep);
                    }

                    index += sections;
                    Pt1 = Pt2; Pt2 = Pt3;
                    d1 = d2; d2 = d3;
                    O1 = O2;
                    R1 = R2;

                }
            }

            /// <summary>
            /// Simple interpolation using Catmull-Rom
            /// </summary>
            private unsafe void ExpandVertices_CatmullRom()
            {
                var positions = (Vector3*)positionData;
                var directions = (Vector3*)directionData;

                var lerpStep = 1f / sections;

                var Pt0 = positions[0] * 2 - positions[sections];
                var Pt1 = positions[0];
                var Pt2 = positions[sections];

                var d1 = directions[0];
                var d2 = directions[sections];

                int index = 0;
                while (index < lastParticle)
                {
                    var Pt3 = (index + sections * 2 < lastParticle) ? positions[index + sections * 2] : Pt2;

                    for (int j = 1; j < sections; j++)
                    {
                        positions[index + j] = Vector3.CatmullRom(Pt0, Pt1, Pt2, Pt3, j * lerpStep);
                        directions[index + j] = Vector3.Lerp(d1, d2, j * lerpStep);
                    }

                    Pt0 = Pt1; Pt1 = Pt2; Pt2 = Pt3;
                    d1 = d2; d2 = (index + sections * 2 < lastParticle) ? directions[index + sections * 2] : new Vector3(0f, 0f, 0f);

                    index += sections;
                }
            }

            /// <summary>
            /// Constructs the ribbon by outputting vertex stream based on the positions and sizes specified previously
            /// </summary>
            /// <param name="vtxBuilder">Target <see cref="ParticleVertexBuilder"/></param> to use
            /// <param name="quadsPerParticle">The required number of quads per each particle</param>
            public unsafe void Ribbonize(ref ParticleBufferState bufferState, int quadsPerParticle)
            {
                if (lastParticle <= 0)
                    return;

                var posAttribute = bufferState.GetAccessor(VertexAttributes.Position);
                var texAttribute = bufferState.GetAccessor(bufferState.DefaultTexCoords);

                if (lastParticle <= sections)
                {
                    // Optional - connect first particle to the origin/emitter

                    // Draw a dummy quad for the first particle
                    var particlePos = Vector3.Zero;
                    var uvCoord = Vector2.Zero;

                    for (var particleIdx = 0; particleIdx < lastParticle; particleIdx++)
                    {
                        for (var vtxIdx = 0; vtxIdx < 4; vtxIdx++)
                        {
                            bufferState.SetAttribute(posAttribute, (IntPtr)(&particlePos));
                            bufferState.SetAttribute(texAttribute, (IntPtr)(&uvCoord));
                            bufferState.NextVertex();
                        }
                    }

                    return;
                }

                if (sections > 1)
                {
                    if (SmoothingPolicy == SmoothingPolicy.Best)
                        ExpandVertices_Circular();
                    else // if (SmoothingPolicy == SmoothingPolicy.Fast)
                        ExpandVertices_CatmullRom();
                }

                var positions = (Vector3*)positionData;
                var directions = (Vector3*)directionData;

                bufferState.SetVerticesPerSegment(quadsPerParticle * 6, quadsPerParticle * 4, quadsPerParticle * 2);

                var axis0 = positions[0] - positions[1];
                axis0.Normalize();

                var oldPoint = positions[0];
                var oldUnitX = directions[0];

                // Step 2 - Draw each particle, connecting it to the previous (front) position

                var vCoordOld = 0f;

                for (int i = 0; i < lastParticle; i++)
                {
                    var centralPos = positions[i];

                    // Directions for smoothing
                    var axis1 = (i + 1 < lastParticle) ? positions[i] - positions[i + 1] : positions[lastParticle - 2] - positions[lastParticle - 1];
                    axis1.Normalize();

                    var unitX = directions[i];

                    // Particle rotation - intentionally IGNORED for ribbon

                    var particlePos = (EdgePolicy == EdgePolicy.Center) ? oldPoint - oldUnitX : oldPoint;
                    var uvCoord = Vector2.Zero;
                    var rotatedCoord = uvCoord;


                    // Top Left - 0f 0f
                    uvCoord.Y = (TextureCoordinatePolicy == TextureCoordinatePolicy.AsIs) ? 0 : vCoordOld;
                    bufferState.SetAttribute(posAttribute, (IntPtr)(&particlePos));

                    rotatedCoord = UVRotate.GetCoords(uvCoord);
                    bufferState.SetAttribute(texAttribute, (IntPtr)(&rotatedCoord));

                    bufferState.NextVertex();


                    // Top Right - 1f 0f
                    particlePos += (EdgePolicy == EdgePolicy.Center) ? oldUnitX * 2 : oldUnitX;
                    bufferState.SetAttribute(posAttribute, (IntPtr)(&particlePos));

                    uvCoord.X = 1;
                    rotatedCoord = UVRotate.GetCoords(uvCoord);
                    bufferState.SetAttribute(texAttribute, (IntPtr)(&rotatedCoord));

                    bufferState.NextVertex();


                    // Move the position to the next particle in the ribbon
                    particlePos += centralPos - oldPoint;
                    particlePos += unitX - oldUnitX;
                    vCoordOld = (TextureCoordinatePolicy == TextureCoordinatePolicy.Stretched) ?
                        ((i + 1) / (float)(lastParticle) * TexCoordsFactor) : ((centralPos - oldPoint).Length() * TexCoordsFactor) + vCoordOld;


                    // Bottom Left - 1f 1f
                    uvCoord.Y = (TextureCoordinatePolicy == TextureCoordinatePolicy.AsIs) ? 1 : vCoordOld;
                    bufferState.SetAttribute(posAttribute, (IntPtr)(&particlePos));

                    rotatedCoord = UVRotate.GetCoords(uvCoord);
                    bufferState.SetAttribute(texAttribute, (IntPtr)(&rotatedCoord));

                    bufferState.NextVertex();


                    // Bottom Right - 0f 1f
                    particlePos -= (EdgePolicy == EdgePolicy.Center) ? unitX * 2 : unitX;
                    bufferState.SetAttribute(posAttribute, (IntPtr)(&particlePos));

                    uvCoord.X = 0;
                    rotatedCoord = UVRotate.GetCoords(uvCoord);
                    bufferState.SetAttribute(texAttribute, (IntPtr)(&rotatedCoord));

                    bufferState.NextVertex();


                    // Preserve the old attributes for the next cycle
                    oldUnitX = unitX;
                    oldPoint = centralPos;
                }
            }
        }
    }
}
