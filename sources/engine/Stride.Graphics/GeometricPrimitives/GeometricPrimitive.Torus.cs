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
using System.Collections.Generic;

using Stride.Core.Mathematics;

namespace Stride.Graphics.GeometricPrimitives
{
    public partial class GeometricPrimitive
    {
        /// <summary>
        /// A Torus primitive.
        /// </summary>
        public static class Torus
        {
            /// <summary>
            /// Creates a torus primitive.
            /// </summary>
            /// <param name="device">The device.</param>
            /// <param name="majorRadius">The majorRadius.</param>
            /// <param name="minorRadius">The minorRadius.</param>
            /// <param name="tessellation">The tessellation.</param>
            /// <param name="uScale">Scale U coordinates between 0 and the values of this parameter.</param>
            /// <param name="vScale">Scale V coordinates 0 and the values of this parameter.</param>
            /// <param name="toLeftHanded">if set to <c>true</c> vertices and indices will be transformed to left handed. Default is false.</param>
            /// <returns>A Torus primitive.</returns>
            /// <exception cref="System.ArgumentOutOfRangeException">tessellation;tessellation parameter out of range</exception>
            public static GeometricPrimitive New(GraphicsDevice device, float majorRadius = 0.5f, float minorRadius = 0.16666f, int tessellation = 32, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
            {
                return new GeometricPrimitive(device, New(majorRadius, minorRadius, tessellation, uScale, vScale, toLeftHanded));
            }

            /// <summary>
            /// Creates a torus primitive.
            /// </summary>
            /// <param name="majorRadius">The major radius of the torus.</param>
            /// <param name="minorRadius">The minor radius of the torus.</param>
            /// <param name="tessellation">The tessellation.</param>
            /// <param name="uScale">Scale U coordinates between 0 and the values of this parameter.</param>
            /// <param name="vScale">Scale V coordinates 0 and the values of this parameter.</param>
            /// <param name="toLeftHanded">if set to <c>true</c> vertices and indices will be transformed to left handed. Default is false.</param>
            /// <returns>A Torus primitive.</returns>
            /// <exception cref="System.ArgumentOutOfRangeException">tessellation;tessellation parameter out of range</exception>
            public static GeometricMeshData<VertexPositionNormalTexture> New(float majorRadius = 0.5f, float minorRadius = 0.16666f, int tessellation = 32, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
            {
                var vertices = new List<VertexPositionNormalTexture>();
                var indices = new List<int>();

                if (tessellation < 3)
                    tessellation = 3;

                int stride = tessellation + 1;

                var texFactor = new Vector2(uScale, vScale);

                // First we loop around the main ring of the torus.
                for (int i = 0; i <= tessellation; i++)
                {
                    float u = (float)i / tessellation;

                    float outerAngle = i * MathUtil.TwoPi / tessellation - MathUtil.PiOverTwo;

                    // Create a transform matrix that will align geometry to
                    // slice perpendicularly though the current ring position.
                    var transform = Matrix.Translation(majorRadius, 0, 0) * Matrix.RotationY(outerAngle);

                    // Now we loop along the other axis, around the side of the tube.
                    for (int j = 0; j <= tessellation; j++)
                    {
                        float v = 1 - (float)j / tessellation;

                        float innerAngle = j * MathUtil.TwoPi / tessellation + MathUtil.Pi;
                        float dx = (float)Math.Cos(innerAngle), dy = (float)Math.Sin(innerAngle);

                        // Create a vertex.
                        var normal = new Vector3(dx, dy, 0);
                        var position = normal * minorRadius;
                        var textureCoordinate = new Vector2(u, v);

                        Vector3.TransformCoordinate(ref position, ref transform, out position);
                        Vector3.TransformNormal(ref normal, ref transform, out normal);

                        vertices.Add(new VertexPositionNormalTexture(position, normal, textureCoordinate * texFactor));

                        // And create indices for two triangles.
                        int nextI = (i + 1) % stride;
                        int nextJ = (j + 1) % stride;

                        indices.Add(i * stride + j);
                        indices.Add(i * stride + nextJ);
                        indices.Add(nextI * stride + j);

                        indices.Add(i * stride + nextJ);
                        indices.Add(nextI * stride + nextJ);
                        indices.Add(nextI * stride + j);
                    }
                }

                // Create the primitive object.
                return new GeometricMeshData<VertexPositionNormalTexture>(vertices.ToArray(), indices.ToArray(), toLeftHanded) { Name = "Torus" };
            }
        }
    }
}
