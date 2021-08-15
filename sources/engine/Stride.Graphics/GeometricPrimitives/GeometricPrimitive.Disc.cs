// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core.Mathematics;

namespace Stride.Graphics.GeometricPrimitives
{
    public partial class GeometricPrimitive
    {
        /// <summary>
        /// A disc - a circular base, or a circular sector.
        /// </summary>
        public static class Disc
        {
            /// <summary>
            /// Creates a disc.
            /// </summary>
            /// <param name="device">The device.</param>
            /// <param name="radius">The radius of the base</param>
            /// <param name="angle">The angle of the circular sector</param>
            /// <param name="tessellation">The number of segments composing the base</param>
            /// <param name="uScale">Scale U coordinates between 0 and the values of this parameter.</param>
            /// <param name="vScale">Scale V coordinates 0 and the values of this parameter.</param>
            /// <param name="toLeftHanded">if set to <c>true</c> vertices and indices will be transformed to left handed. Default is false.</param>
            /// <returns>A disc.</returns>
            public static GeometricPrimitive New(GraphicsDevice device, float radius = 0.5f, float angle = 2 * MathF.PI, int tessellation = 16, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
            {
                // Create the primitive object.
                return new GeometricPrimitive(device, New(radius, angle, tessellation, uScale, vScale, toLeftHanded));
            }

            /// <summary>
            /// Creates a disc.
            /// </summary>
            /// <param name="radius">The radius of the base</param>
            /// <param name="sectorAngle">The angle of the circular sector</param>
            /// <param name="tessellation">The number of segments composing the base</param>
            /// <param name="uScale">Scale U coordinates between 0 and the values of this parameter.</param>
            /// <param name="vScale">Scale V coordinates 0 and the values of this parameter.</param>
            /// <param name="toLeftHanded">if set to <c>true</c> vertices and indices will be transformed to left handed. Default is false.</param>
            /// <returns>A cone.</returns>
            public static GeometricMeshData<VertexPositionNormalTexture> New(float radius = 0.5f, float sectorAngle = 2 * MathF.PI, int tessellation = 16, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
            {
                if (tessellation < 4)
                    tessellation = 4;
                
                var numberOfSections = tessellation + 2;
                var indices = new int[2 * tessellation * 3];
                var vertices = new VertexPositionNormalTexture[2 * numberOfSections];

                var index = 0;
                var vertice = 0;

                // f in {-1, 1} - two faces
                for (var f = -1; f < 2; f += 2)
                {
                    var normal = new Vector3(0, f, 0);

                    // center point
                    vertices[vertice++] = new VertexPositionNormalTexture { Position = new Vector3(), Normal = normal, TextureCoordinate = new Vector2() };

                    // edge points
                    for (var i = 0; i <= tessellation; ++i)
                    {
                        var angle = i / (double)tessellation * sectorAngle;
                        // FIXME: I don't really know how to set up texture coordinates in a sane way
                        var textureCoordinate = new Vector2((float)i / tessellation, 1);
                        textureCoordinate.X *= uScale;
                        textureCoordinate.Y *= vScale;
                        var position = new Vector3((float)Math.Cos(angle) * radius, 0, (float)Math.Sin(angle) * radius);

                        vertices[vertice++] = new VertexPositionNormalTexture { Position = position, Normal = normal, TextureCoordinate = textureCoordinate };
                    }
                }

                var secondFaceOffset = numberOfSections;

                // the indices
                for (int i = 1; i <= tessellation; ++i)
                {
                    indices[index++] = 0;
                    indices[index++] = i;
                    indices[index++] = i+1;

                    // note the opposite order of vertices - this is required to make the second face look the other way
                    indices[index++] = secondFaceOffset + i + 1;
                    indices[index++] = secondFaceOffset + i;
                    indices[index++] = secondFaceOffset + 0;
                }

                return new GeometricMeshData<VertexPositionNormalTexture>(vertices, indices, toLeftHanded) { Name = "Disc" };
            }
        }
    }
}
