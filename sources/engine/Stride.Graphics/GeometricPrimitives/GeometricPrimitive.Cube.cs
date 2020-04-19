// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
// -----------------------------------------------------------------------------
// The following code is a port of DirectXTk http://directxtk.codeplex.com
// -----------------------------------------------------------------------------
// Microsoft Public License (Ms-PL)
//
// This license governs use of the accompanying software. If you use the 
// software, you accept this license. If you do not accept the license, do not
// use the software.
//
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and 
// "distribution" have the same meaning here as under U.S. copyright law.
// A "contribution" is the original software, or any additions or changes to 
// the software.
// A "contributor" is any person that distributes its contribution under this 
// license.
// "Licensed patents" are a contributor's patent claims that read directly on 
// its contribution.
//
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the 
// license conditions and limitations in section 3, each contributor grants 
// you a non-exclusive, worldwide, royalty-free copyright license to reproduce
// its contribution, prepare derivative works of its contribution, and 
// distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license
// conditions and limitations in section 3, each contributor grants you a 
// non-exclusive, worldwide, royalty-free license under its licensed patents to
// make, have made, use, sell, offer for sale, import, and/or otherwise dispose
// of its contribution in the software or derivative works of the contribution 
// in the software.
//
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any 
// contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that 
// you claim are infringed by the software, your patent license from such 
// contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all 
// copyright, patent, trademark, and attribution notices that are present in the
// software.
// (D) If you distribute any portion of the software in source code form, you 
// may do so only under this license by including a complete copy of this 
// license with your distribution. If you distribute any portion of the software
// in compiled or object code form, you may only do so under a license that 
// complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The
// contributors give no express warranties, guarantees or conditions. You may
// have additional consumer rights under your local laws which this license 
// cannot change. To the extent permitted under your local laws, the 
// contributors exclude the implied warranties of merchantability, fitness for a
// particular purpose and non-infringement.

using System;
using Stride.Core.Mathematics;

namespace Stride.Graphics.GeometricPrimitives
{
    public partial class GeometricPrimitive
    {
        /// <summary>
        /// A cube has six faces, each one pointing in a different direction.
        /// </summary>
        public static class Cube
        {
            // TODO: Add support to tesselate the faces of the cube

            private const int CubeFaceCount = 6;

            private static readonly Vector3[] FaceNormals = new Vector3[CubeFaceCount]
                {
                    new Vector3(0, 0, 1),
                    new Vector3(0, 0, -1),
                    new Vector3(1, 0, 0),
                    new Vector3(-1, 0, 0),
                    new Vector3(0, 1, 0),
                    new Vector3(0, -1, 0),
                };

            private static readonly Vector2[] TextureCoordinates = new Vector2[4]
                {
                    new Vector2(1, 0),
                    new Vector2(1, 1),
                    new Vector2(0, 1),
                    new Vector2(0, 0),
                };

            /// <summary>
            /// Creates a cube with six faces each one pointing in a different direction.
            /// </summary>
            /// <param name="device">The device.</param>
            /// <param name="size">The size.</param>
            /// <param name="uScale">Scale U coordinates between 0 and the values of this parameter.</param>
            /// <param name="vScale">Scale V coordinates 0 and the values of this parameter.</param>
            /// <param name="toLeftHanded">if set to <c>true</c> vertices and indices will be transformed to left handed. Default is false.</param>
            /// <returns>A cube.</returns>
            public static GeometricPrimitive New(GraphicsDevice device, float size = 1.0f, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
            {
                // Create the primitive object.
                return new GeometricPrimitive(device, New(size, uScale, vScale, toLeftHanded));
            }

            /// <summary>
            /// Creates a cube with six faces each one pointing in a different direction.
            /// </summary>
            /// <param name="device">The device.</param>
            /// <param name="size">The size.</param>
            /// <param name="uScale">Scale U coordinates between 0 and the values of this parameter.</param>
            /// <param name="vScale">Scale V coordinates 0 and the values of this parameter.</param>
            /// <param name="toLeftHanded">if set to <c>true</c> vertices and indices will be transformed to left handed. Default is false.</param>
            /// <returns>A cube.</returns>
            public static GeometricPrimitive New(GraphicsDevice device, Vector3 size, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
            {
                // Create the primitive object.
                return new GeometricPrimitive(device, New(size, uScale, vScale, toLeftHanded));
            }

            /// <summary>
            /// Creates a cube with six faces each one pointing in a different direction.
            /// </summary>
            /// <param name="size">The size.</param>
            /// <param name="uScale">Scale U coordinates between 0 and the values of this parameter.</param>
            /// <param name="vScale">Scale V coordinates 0 and the values of this parameter.</param>
            /// <param name="toLeftHanded">if set to <c>true</c> vertices and indices will be transformed to left handed. Default is false.</param>
            /// <returns>A cube.</returns>
            public static GeometricMeshData<VertexPositionNormalTexture> New(float size = 1.0f, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
            {
                return New(new Vector3(size), uScale, vScale, toLeftHanded);
            }

            /// <summary>
            /// Creates a cube with six faces each one pointing in a different direction.
            /// </summary>
            /// <param name="size">The size.</param>
            /// <param name="uScale">Scale U coordinates between 0 and the values of this parameter.</param>
            /// <param name="vScale">Scale V coordinates 0 and the values of this parameter.</param>
            /// <param name="toLeftHanded">if set to <c>true</c> vertices and indices will be transformed to left handed. Default is false.</param>
            /// <returns>A cube.</returns>
            public static GeometricMeshData<VertexPositionNormalTexture> New(Vector3 size, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
            {
                var vertices = new VertexPositionNormalTexture[CubeFaceCount * 4];
                var indices = new int[CubeFaceCount * 6];

                var texCoords = new Vector2[4];
                for (var i = 0; i < 4; i++)
                {
                    texCoords[i] = TextureCoordinates[i] * new Vector2(uScale, vScale);
                }

                size /= 2.0f;

                int vertexCount = 0;
                int indexCount = 0;
                // Create each face in turn.
                for (int i = 0; i < CubeFaceCount; i++)
                {
                    Vector3 normal = FaceNormals[i];

                    // Get two vectors perpendicular both to the face normal and to each other.
                    Vector3 basis = (i >= 4) ? Vector3.UnitZ : Vector3.UnitY;

                    Vector3 side1;
                    Vector3.Cross(ref normal, ref basis, out side1);

                    Vector3 side2;
                    Vector3.Cross(ref normal, ref side1, out side2);

                    // Six indices (two triangles) per face.
                    int vbase = i * 4;
                    indices[indexCount++] = (vbase + 0);
                    indices[indexCount++] = (vbase + 1);
                    indices[indexCount++] = (vbase + 2);

                    indices[indexCount++] = (vbase + 0);
                    indices[indexCount++] = (vbase + 2);
                    indices[indexCount++] = (vbase + 3);

                    // Four vertices per face.
                    vertices[vertexCount++] = new VertexPositionNormalTexture((normal - side1 - side2) * size, normal, texCoords[0]);
                    vertices[vertexCount++] = new VertexPositionNormalTexture((normal - side1 + side2) * size, normal, texCoords[1]);
                    vertices[vertexCount++] = new VertexPositionNormalTexture((normal + side1 + side2) * size, normal, texCoords[2]);
                    vertices[vertexCount++] = new VertexPositionNormalTexture((normal + side1 - side2) * size, normal, texCoords[3]);
                }

                // Create the primitive object.
                return new GeometricMeshData<VertexPositionNormalTexture>(vertices, indices, toLeftHanded) { Name = "Cube" };
            }
        }
    }
}
