// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using Xenko.Core.Mathematics;

namespace Xenko.Graphics.GeometricPrimitives
{
    /// <summary>
    /// Enumerates the different possible direction of a plane normal.
    /// </summary>
    public enum NormalDirection
    {
        UpZ,
        UpY,
        UpX,
    }

    public partial class GeometricPrimitive
    {
        /// <summary>
        /// A plane primitive.
        /// </summary>
        public static class Plane
        {
            /// <summary>
            /// Creates a Plane primitive on the X/Y plane with a normal equal to -<see cref="Vector3.UnitZ"/>.
            /// </summary>
            /// <param name="device">The device.</param>
            /// <param name="sizeX">The size X.</param>
            /// <param name="sizeY">The size Y.</param>
            /// <param name="tessellationX">The tessellation, as the number of quads per X axis.</param>
            /// <param name="tessellationY">The tessellation, as the number of quads per Y axis.</param>
            /// <param name="uFactor">Scale U coordinates between 0 and the values of this parameter.</param>
            /// <param name="vFactor">Scale V coordinates 0 and the values of this parameter.</param>
            /// <param name="generateBackFace">Add a back face to the plane</param>
            /// <param name="toLeftHanded">if set to <c>true</c> vertices and indices will be transformed to left handed. Default is false.</param>
            /// <param name="normalDirection">The direction of the plane normal</param>
            /// <returns>A Plane primitive.</returns>
            /// <exception cref="System.ArgumentOutOfRangeException">tessellation;tessellation must be > 0</exception>
            public static GeometricPrimitive New(GraphicsDevice device, float sizeX = 1.0f, float sizeY = 1.0f, int tessellationX = 1, int tessellationY = 1, float uFactor = 1f, float vFactor = 1f, bool generateBackFace = false, bool toLeftHanded = false, NormalDirection normalDirection = NormalDirection.UpZ)
            {
                return new GeometricPrimitive(device, New(sizeX, sizeY, tessellationX, tessellationY, uFactor, vFactor, generateBackFace, toLeftHanded, normalDirection));
            }

            /// <summary>
            /// Creates a Plane primitive on the X/Y plane with a normal equal to -<see cref="Vector3.UnitZ"/>.
            /// </summary>
            /// <param name="sizeX">The size X.</param>
            /// <param name="sizeY">The size Y.</param>
            /// <param name="tessellationX">The tessellation, as the number of quads per X axis.</param>
            /// <param name="tessellationY">The tessellation, as the number of quads per Y axis.</param>
            /// <param name="uFactor">Scale U coordinates between 0 and the values of this parameter.</param>
            /// <param name="vFactor">Scale V coordinates 0 and the values of this parameter.</param>
            /// <param name="generateBackFace">Add a back face to the plane</param>
            /// <param name="toLeftHanded">if set to <c>true</c> vertices and indices will be transformed to left handed. Default is false.</param>
            /// <param name="normalDirection">The direction of the plane normal</param>
            /// <returns>A Plane primitive.</returns>
            /// <exception cref="System.ArgumentOutOfRangeException">tessellation;tessellation must be > 0</exception>
            public static GeometricMeshData<VertexPositionNormalTexture> New(float sizeX = 1.0f, float sizeY = 1.0f, int tessellationX = 1, int tessellationY = 1, float uFactor = 1f, float vFactor = 1f, bool generateBackFace = false, bool toLeftHanded = false, NormalDirection normalDirection = 0)
            {
                if (tessellationX < 1)
                    tessellationX = 1;
                if (tessellationY < 1)
                    tessellationY = 1;

                var lineWidth = tessellationX + 1;
                var lineHeight = tessellationY + 1;
                var vertices = new VertexPositionNormalTexture[lineWidth * lineHeight * (generateBackFace ? 2 : 1)];
                var indices = new int[tessellationX * tessellationY * 6 * (generateBackFace ? 2 : 1)];

                var deltaX = sizeX / tessellationX;
                var deltaY = sizeY / tessellationY;

                sizeX /= 2.0f;
                sizeY /= 2.0f;

                int vertexCount = 0;
                int indexCount = 0;

                Vector3 normal;
                switch (normalDirection)
                {
                    default:
                    case NormalDirection.UpZ: normal = Vector3.UnitZ; break;
                    case NormalDirection.UpY: normal = Vector3.UnitY; break;
                    case NormalDirection.UpX: normal = Vector3.UnitX; break;
                }

                var uv = new Vector2(uFactor, vFactor);

                // Create vertices
                for (int y = 0; y < (tessellationY + 1); y++)
                {
                    for (int x = 0; x < (tessellationX + 1); x++)
                    {
                        Vector3 position;
                        switch (normalDirection)
                        {
                            default:
                            case NormalDirection.UpZ: position = new Vector3(-sizeX + deltaX * x, sizeY - deltaY * y, 0); break;
                            case NormalDirection.UpY: position = new Vector3(-sizeX + deltaX * x, 0, -sizeY + deltaY * y); break;
                            case NormalDirection.UpX: position = new Vector3(0, sizeY - deltaY * y, sizeX - deltaX * x); break;
                        }
                        var texCoord = new Vector2(uv.X * x / tessellationX, uv.Y * y / tessellationY);
                        vertices[vertexCount++] = new VertexPositionNormalTexture(position, normal, texCoord);
                    }
                }

                // Create indices
                for (int y = 0; y < tessellationY; y++)
                {
                    for (int x = 0; x < tessellationX; x++)
                    {
                        // Six indices (two triangles) per face.
                        int vbase = lineWidth * y + x;
                        indices[indexCount++] = (vbase + 1);
                        indices[indexCount++] = (vbase + 1 + lineWidth);
                        indices[indexCount++] = (vbase + lineWidth);

                        indices[indexCount++] = (vbase + 1);
                        indices[indexCount++] = (vbase + lineWidth);
                        indices[indexCount++] = (vbase);
                    }
                }
                if (generateBackFace)
                {
                    var numVertices = lineWidth * lineHeight;
                    normal = -normal;
                    for (int y = 0; y < (tessellationY + 1); y++)
                    {
                        for (int x = 0; x < (tessellationX + 1); x++)
                        {
                            var baseVertex = vertices[vertexCount - numVertices];
                            var position = new Vector3(baseVertex.Position.X, baseVertex.Position.Y, baseVertex.Position.Z);
                            var texCoord = new Vector2(uv.X * x / tessellationX, uv.Y * y / tessellationY);
                            vertices[vertexCount++] = new VertexPositionNormalTexture(position, normal, texCoord);
                        }
                    }
                    // Create indices
                    for (int y = 0; y < tessellationY; y++)
                    {
                        for (int x = 0; x < tessellationX; x++)
                        {
                            // Six indices (two triangles) per face.
                            int vbase = lineWidth * (y + tessellationY + 1) + x;
                            indices[indexCount++] = (vbase + 1);
                            indices[indexCount++] = (vbase + lineWidth);
                            indices[indexCount++] = (vbase + 1 + lineWidth);

                            indices[indexCount++] = (vbase + 1);
                            indices[indexCount++] = (vbase);
                            indices[indexCount++] = (vbase + lineWidth);
                        }
                    }
                }

                // Create the primitive object.
                return new GeometricMeshData<VertexPositionNormalTexture>(vertices, indices, toLeftHanded) { Name = "Plane" };
            }
        }
    }
}
