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
using System.Collections.Generic;

using Xenko.Core;
using Xenko.Core.Mathematics;

namespace Xenko.Graphics.GeometricPrimitives
{
    public partial class GeometricPrimitive
    {
        /// <summary>
        /// A Cylinder primitive.
        /// </summary>
        public static class Cylinder
        {
            // Helper computes a point on a unit circle, aligned to the x/z plane and centered on the origin.
            private static Vector3 GetCircleVector(int i, int tessellation)
            {
                var angle = (float)(i * 2.0 * Math.PI / tessellation);
                var dx = (float)Math.Sin(angle);
                var dz = (float)Math.Cos(angle);

                return new Vector3(dx, 0, dz);
            }

            // Helper creates a triangle fan to close the end of a cylinder.
            private static void CreateCylinderCap(List<VertexPositionNormalTexture> vertices, List<int> indices, int tessellation, float height, float radius, float uScale, float vScale, bool isTop)
            {
                // Create cap indices.
                for (int i = 0; i < tessellation - 2; i++)
                {
                    int i1 = (i + 1) % tessellation;
                    int i2 = (i + 2) % tessellation;

                    if (isTop)
                    {
                        Utilities.Swap(ref i1, ref i2);
                    }

                    int vbase = vertices.Count;
                    indices.Add(vbase);
                    indices.Add(vbase + i1);
                    indices.Add(vbase + i2);
                }

                // Which end of the cylinder is this?
                var normal = Vector3.UnitY;
                var textureScale = new Vector2(-0.5f);

                if (!isTop)
                {
                    normal = -normal;
                    textureScale.X = -textureScale.X;
                }

                // Create cap vertices.
                for (int i = 0; i < tessellation; i++)
                {
                    var circleVector = GetCircleVector(i, tessellation);
                    var position = (circleVector * radius) + (normal * height);
                    var textureCoordinate = new Vector2(uScale * (circleVector.X * textureScale.X + 0.5f), vScale * (circleVector.Z * textureScale.Y + 0.5f));

                    vertices.Add(new VertexPositionNormalTexture(position, normal, textureCoordinate));
                }
            }

            /// <summary>
            /// Creates a cylinder primitive.
            /// </summary>
            /// <param name="device">The device.</param>
            /// <param name="height">The height.</param>
            /// <param name="radius">The radius.</param>
            /// <param name="tessellation">The tessellation.</param>
            /// <param name="uScale">Scale U coordinates between 0 and the values of this parameter.</param>
            /// <param name="vScale">Scale V coordinates 0 and the values of this parameter.</param>
            /// <param name="toLeftHanded">if set to <c>true</c> vertices and indices will be transformed to left handed. Default is false.</param>
            /// <returns>A cylinder primitive.</returns>
            /// <exception cref="System.ArgumentOutOfRangeException">tessellation;tessellation must be &gt;= 3</exception>
            public static GeometricPrimitive New(GraphicsDevice device, float height = 1.0f, float radius = 0.5f, int tessellation = 32, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
            {
                // Create the primitive object.
                return new GeometricPrimitive(device, New(height, radius, tessellation, uScale, vScale, toLeftHanded));
            }

            /// <summary>
            /// Creates a cylinder primitive.
            /// </summary>
            /// <param name="height">The height.</param>
            /// <param name="radius">The radius.</param>
            /// <param name="tessellation">The tessellation.</param>
            /// <param name="uScale">Scale U coordinates between 0 and the values of this parameter.</param>
            /// <param name="vScale">Scale V coordinates 0 and the values of this parameter.</param>
            /// <param name="toLeftHanded">if set to <c>true</c> vertices and indices will be transformed to left handed. Default is false.</param>
            /// <returns>A cylinder primitive.</returns>
            /// <exception cref="System.ArgumentOutOfRangeException">tessellation;tessellation must be &gt;= 3</exception>
            public static GeometricMeshData<VertexPositionNormalTexture> New(float height = 1.0f, float radius = 0.5f, int tessellation = 32, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
            {
                if (tessellation < 3)
                    tessellation = 3;

                var vertices = new List<VertexPositionNormalTexture>();
                var indices = new List<int>();

                height /= 2;

                var topOffset = Vector3.UnitY * height;

                int stride = tessellation + 1;

                // Create a ring of triangles around the outside of the cylinder.
                for (int i = 0; i <= tessellation; i++)
                {
                    var normal = GetCircleVector(i, tessellation);

                    var sideOffset = normal * radius;

                    var textureCoordinate = new Vector2((float)i / tessellation, 0);
                    
                    vertices.Add(new VertexPositionNormalTexture(sideOffset + topOffset, normal, textureCoordinate * new Vector2(uScale, vScale)));
                    vertices.Add(new VertexPositionNormalTexture(sideOffset - topOffset, normal, (textureCoordinate + Vector2.UnitY) * new Vector2(uScale, vScale)));

                    indices.Add(i * 2);
                    indices.Add((i * 2 + 2) % (stride * 2));
                    indices.Add(i * 2 + 1);

                    indices.Add(i * 2 + 1);
                    indices.Add((i * 2 + 2) % (stride * 2));
                    indices.Add((i * 2 + 3) % (stride * 2));
                }

                // Create flat triangle fan caps to seal the top and bottom.
                CreateCylinderCap(vertices, indices, tessellation, height, radius, uScale, vScale, true);
                CreateCylinderCap(vertices, indices, tessellation, height, radius, uScale, vScale, false);

                // Create the primitive object.
                return new GeometricMeshData<VertexPositionNormalTexture>(vertices.ToArray(), indices.ToArray(), toLeftHanded) { Name = "Cylinder" };
            }
        }
    }
}
