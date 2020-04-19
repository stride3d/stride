// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Particles.Initializers;
using Stride.Particles.Sorters;
using Stride.Particles.VertexLayouts;
using Stride.Particles.ShapeBuilders.Tools;

namespace Stride.Particles.ShapeBuilders
{
    /// <summary>
    /// Shape builder which builds all particles as a ribbon, connecting adjacent particles with camera-facing quads
    /// </summary>
    [DataContract("ShapeBuilderRibbon")]
    [Display("Ribbon")]
    public class ShapeBuilderRibbon : ShapeBuilder
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

            var orderField = sorter.GetField(ParticleFields.Order);

            // Check if the draw space is identity - in this case we don't need to transform the position, scale and rotation vectors
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            var trsIdentity = (spaceScale == 1f);
            trsIdentity = trsIdentity && (spaceTranslation.Equals(Vector3.Zero));
            trsIdentity = trsIdentity && (spaceRotation.Equals(Quaternion.Identity));

            var ribbonizer = new Ribbonizer(this, currentTotalParticles, currentQuadsPerParticle);

            var renderedParticles = 0;
            bufferState.StartOver();

            uint oldOrderValue = 0;

            foreach (var particle in sorter)
            {
                if (orderField.IsValid())
                {
                    var orderValue = (*((uint*)particle[orderField]));

                    if ((orderValue >> SpawnOrderConst.GroupBitOffset) != (oldOrderValue >> SpawnOrderConst.GroupBitOffset)) 
                    {
                        ribbonizer.Ribbonize(ref bufferState, invViewX, invViewY, QuadsPerParticle, ref viewProj);
                        ribbonizer.RibbonSplit();
                    }

                    oldOrderValue = orderValue;
                }

                var centralPos = particle.Get(positionField);

                var particleSize = sizeField.IsValid() ? particle.Get(sizeField) : 1f;

                if (!trsIdentity)
                {
                    spaceRotation.Rotate(ref centralPos);
                    centralPos = centralPos * spaceScale + spaceTranslation;
                    particleSize *= spaceScale;
                }
                
                ribbonizer.AddParticle(ref centralPos, particleSize);
                renderedParticles++;
            }

            ribbonizer.Ribbonize(ref bufferState, invViewX, invViewY, QuadsPerParticle, ref viewProj);

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
            private readonly IntPtr sizeData;

            private readonly int particleCapacity;
            private readonly ShapeBuilderRibbon parentRibbon;

            public Ribbonizer(ShapeBuilderRibbon ribbon, int newCapacity, int sectionsPerParticle)
            {
                parentRibbon = ribbon;

                lastParticle = 0;
                sections = sectionsPerParticle;

                int requiredCapacity = sectionsPerParticle * newCapacity;

                particleCapacity = requiredCapacity;

                int positionDataSize = Utilities.SizeOf<Vector3>() * particleCapacity;
                positionDataSize = (positionDataSize % 4 == 0) ? positionDataSize : (positionDataSize + 4 - (positionDataSize % 4));
                positionData = Utilities.AllocateMemory(positionDataSize);

                int sizeDataSize = Utilities.SizeOf<float>() * particleCapacity;
                sizeDataSize = (sizeDataSize % 4 == 0) ? sizeDataSize : (sizeDataSize + 4 - (sizeDataSize % 4));
                sizeData = Utilities.AllocateMemory(sizeDataSize);
            }

            public void Free()
            {
                Utilities.FreeMemory(positionData);
                Utilities.FreeMemory(sizeData);
            }

            public TextureCoordinatePolicy TextureCoordinatePolicy => parentRibbon.TextureCoordinatePolicy;

            SmoothingPolicy SmoothingPolicy => parentRibbon.SmoothingPolicy;

            float TexCoordsFactor => parentRibbon.TexCoordsFactor;

            UVRotate UVRotate => parentRibbon.UVRotate;

            /// <summary>
            /// Splits (cuts) the ribbon without restarting or rebuilding the vertex buffer
            /// </summary>
            public void RibbonSplit()
            {
                lastParticle = 0;
            }

            /// <summary>
            /// Adds a new particle position and size to the point string
            /// </summary>
            /// <param name="position"></param>
            /// <param name="size"></param>
            public unsafe void AddParticle(ref Vector3 position, float size)
            {
                if (lastParticle >= particleCapacity)
                    return;

                var positions = (Vector3*) positionData;
                var sizes = (float*)sizeData;

                positions[lastParticle] = position;
                sizes[lastParticle] = size;

                lastParticle += sections;
            }

            /// <summary>
            /// Returns the half width vector at the sampled position along the ribbon
            /// </summary>
            /// <param name="particleSize">Particle's size, sampled from the size field</param>
            /// <param name="invViewZ">Unit vector Z in clip space, pointing towards the camera</param>
            /// <param name="axis0">Central axis between the particle and the previous point along the ribbon</param>
            /// <param name="axis1">Central axis between the particle and the next point along the ribbon</param>
            /// <returns></returns>
            private static Vector3 GetWidthVector(float particleSize, ref Vector3 invViewZ, ref Vector3 axis0, ref Vector3 axis1)
            {
                // Simplest
                // return invViewX * (particleSize * 0.5f);

                // Camera-oriented
                var unitX = axis0 + axis1;
                unitX -= Vector3.Dot(invViewZ, unitX) * invViewZ;
                var rotationQuaternion = Quaternion.RotationAxis(invViewZ, -MathUtil.PiOverTwo);
                rotationQuaternion.Rotate(ref unitX);
                unitX.Normalize();

                return unitX * (particleSize * 0.5f);
            }

            /// <summary>
            /// Advanced interpolation, drawing the vertices in a circular arc between two adjacent control points
            /// </summary>
            private unsafe void ExpandVertices_Circular()
            {
                if (sections <= 1)
                    return;

                var positions = (Vector3*)positionData;
                var sizes = (float*)sizeData;

                var lerpStep = 1f/sections;

                var Pt0 = positions[0] * 2 - positions[sections];
                var Pt1 = positions[0];
                var Pt2 = positions[sections];

                var O1 = Circumcenter(ref Pt0, ref Pt1, ref Pt2);
                var R1 = (O1 - Pt1).Length();

                var s1 = sizes[0];
                var s2 = sizes[sections];

                int index = 0;
                while (index < lastParticle)
                {
                    var Pt3 = (index + sections * 2 < lastParticle) ? positions[index + sections * 2] : Pt2;
                    var s3  = (index + sections * 2 < lastParticle) ? sizes[index + sections * 2] : 0f;
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

                            positions[index + j] = Vector3.Lerp(O1 + dist1*R1, O2 + dist2*R2, j*lerpStep);

                        sizes[index + j] = s1 * (1 - j * lerpStep) + s2 * (j * lerpStep);
                    }

                    index += sections;
                    Pt0 = Pt1;  Pt1 = Pt2;  Pt2 = Pt3;
                    s1 = s2;    s2 = s3;
                    O1 = O2;
                    R1 = R2;

                }
            }

            /// <summary>
            /// Simple interpolation using Catmull-Rom
            /// </summary>
            private unsafe void ExpandVertices_CatmullRom()
            {
                var lerpStep = 1f / sections;

                var positions = (Vector3*)positionData;
                var sizes = (float*)sizeData;

                var Pt0 = positions[0] * 2 - positions[sections];
                var Pt1 = positions[0];
                var Pt2 = positions[sections];

                var s1 = sizes[0];
                var s2 = sizes[sections];

                int index = 0;
                while (index < lastParticle)
                {
                    var Pt3 = (index + sections * 2 < lastParticle) ? positions[index + sections * 2] : Pt2;

                    for (int j = 1; j < sections; j++)
                    {
                        positions[index + j] = Vector3.CatmullRom(Pt0, Pt1, Pt2, Pt3, j * lerpStep);
                        sizes[index + j] = s1 * (1 - j * lerpStep) + s2 * (j * lerpStep);
                    }

                    Pt0 = Pt1; Pt1 = Pt2; Pt2 = Pt3;
                    s1 = s2; s2 = (index + sections * 2 < lastParticle) ? sizes[index + sections * 2] : 0f;

                    index += sections;
                }
            }

            /// <summary>
            /// Constructs the ribbon by outputting vertex stream based on the positions and sizes specified previously
            /// </summary>
            /// <param name="bufferState">Target <see cref="ParticleBufferState"/></param> to use
            /// <param name="invViewX">Unit vector X in clip space as calculated from the inverse view matrix</param>
            /// <param name="invViewY">Unit vector Y in clip space as calculated from the inverse view matrix</param>
            /// <param name="quadsPerParticle">The required number of quads per each particle</param>
            public unsafe void Ribbonize(ref ParticleBufferState bufferState, Vector3 invViewX, Vector3 invViewY, int quadsPerParticle, ref Matrix viewProj)
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

                bufferState.SetVerticesPerSegment(quadsPerParticle * 6, quadsPerParticle * 4, quadsPerParticle * 2);

                var positions = (Vector3*)positionData;
                var sizes = (float*)sizeData;

                // Step 1 - Determine the origin of the ribbon
                var invViewZ = Vector3.Cross(invViewX, invViewY);
                invViewZ.Normalize();

                Vector4 projectedPosition;
                Vector3.Transform(ref positions[0], ref viewProj, out projectedPosition);
                var projPt0 = new Vector3(projectedPosition.X / projectedPosition.W, projectedPosition.Y / projectedPosition.W, 0);

                Vector3.Transform(ref positions[1], ref viewProj, out projectedPosition);
                var projPt1 = new Vector3(projectedPosition.X / projectedPosition.W, projectedPosition.Y / projectedPosition.W, 0);

                var axis0 = projPt0 - projPt1;
                axis0.Normalize();

                var oldPoint = positions[0];
                var oldUnitX = axis0 * (sizes[0] * 0.5f);
                oldUnitX = oldUnitX.Y * invViewX - oldUnitX.X * invViewY;

                // Step 2 - Draw each particle, connecting it to the previous (front) position

                var vCoordOld = 0f;

                for (int i = 0; i < lastParticle; i++)
                {
                    var centralPos = positions[i];

                    var particleSize = sizes[i];

                    // Directions for smoothing
                    var axis1 = Vector3.Zero;
                    if (i + 1 < lastParticle)
                    {
                        Vector3.Transform(ref positions[i], ref viewProj, out projectedPosition);
                        projPt0 = new Vector3(projectedPosition.X / projectedPosition.W, projectedPosition.Y / projectedPosition.W, 0);

                        Vector3.Transform(ref positions[i+1], ref viewProj, out projectedPosition);
                        projPt1 = new Vector3(projectedPosition.X / projectedPosition.W, projectedPosition.Y / projectedPosition.W, 0);

                        axis1 = projPt0 - projPt1;
                    }
                    else
                    {
                        Vector3.Transform(ref positions[lastParticle - 2], ref viewProj, out projectedPosition);
                        projPt0 = new Vector3(projectedPosition.X / projectedPosition.W, projectedPosition.Y / projectedPosition.W, 0);

                        Vector3.Transform(ref positions[lastParticle - 1], ref viewProj, out projectedPosition);
                        projPt1 = new Vector3(projectedPosition.X / projectedPosition.W, projectedPosition.Y / projectedPosition.W, 0);

                        axis1 = projPt0 - projPt1;
                    }
                    axis1.Normalize();

                    var axisAvg = axis0 + axis1;
                    axisAvg.Normalize();
                    var unitX = axisAvg * (particleSize * 0.5f);
                    unitX = unitX.Y * invViewX - unitX.X * invViewY;

                    axis0 = axis1;

                    // Particle rotation - intentionally IGNORED for ribbon

                    var particlePos = oldPoint - oldUnitX;
                    var uvCoord = Vector2.Zero;
                    var rotatedCoord = uvCoord;


                    // Top Left - 0f 0f
                    uvCoord.Y = (TextureCoordinatePolicy == TextureCoordinatePolicy.AsIs) ? 0 : vCoordOld;
                    bufferState.SetAttribute(posAttribute, (IntPtr)(&particlePos));

                    rotatedCoord = UVRotate.GetCoords(uvCoord);
                    bufferState.SetAttribute(texAttribute, (IntPtr)(&rotatedCoord));

                    bufferState.NextVertex();


                    // Top Right - 1f 0f
                    particlePos += oldUnitX * 2;
                    bufferState.SetAttribute(posAttribute, (IntPtr)(&particlePos));

                    uvCoord.X = 1;
                    rotatedCoord = UVRotate.GetCoords(uvCoord);
                    bufferState.SetAttribute(texAttribute, (IntPtr)(&rotatedCoord));

                    bufferState.NextVertex();


                    // Move the position to the next particle in the ribbon
                    particlePos += centralPos - oldPoint;
                    particlePos += unitX - oldUnitX;
                    vCoordOld = (TextureCoordinatePolicy == TextureCoordinatePolicy.Stretched) ? 
                        ((i + 1)/(float)(lastParticle) * TexCoordsFactor) : ((centralPos - oldPoint).Length() * TexCoordsFactor) + vCoordOld;


                    // Bottom Left - 1f 1f
                    uvCoord.Y = (TextureCoordinatePolicy == TextureCoordinatePolicy.AsIs) ? 1 : vCoordOld;
                    bufferState.SetAttribute(posAttribute, (IntPtr)(&particlePos));

                    rotatedCoord = UVRotate.GetCoords(uvCoord);
                    bufferState.SetAttribute(texAttribute, (IntPtr)(&rotatedCoord));

                    bufferState.NextVertex();


                    // Bottom Right - 0f 1f
                    particlePos -= unitX * 2;
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
