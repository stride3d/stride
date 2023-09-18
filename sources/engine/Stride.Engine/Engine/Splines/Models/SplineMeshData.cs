//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.Engine.Splines.Models
{
    public class SplineMeshData : IDisposable
    {
        private Buffer<VertexPositionNormalTexture> buffer;
        private List<VertexPositionNormalTexture> vertices = new List<VertexPositionNormalTexture>();
        private List<int> indices;
        private Buffer<int> indexBuffer;

        public SplineMeshData()
        {
            indices = new List<int>();
        }

        public MeshDraw Build(Vector3[] points, GraphicsDevice graphicsDevice)
        {
            var indexCount = 0;
            var vertexCount = 0;
            var totalDistance = 0.0f;
            const float halfWidth = 0.01f;

            for (var i = 0; i < points.Length; i++)
            {
                // Calculate forward direction
                var forward = Vector3.Zero;
                if (i < points.Length - 1)
                {
                    forward += points[i + 1] - points[i];
                }

                if (i > 0)
                {
                    forward += points[i] - points[i - 1];
                }

                forward.Normalize();

                var right = Vector3.Cross(forward, Vector3.UnitY);
                var left = -right;

                var p0 = points[i] + left * halfWidth;
                var p1 = points[i];
                var p2 = points[i] + right * halfWidth;

                // Calculate UV coordinates
                var uvY = 0.0f;
                if (i > 0)
                {
                    var distance = (points[i] - points[i - 1]).Length();
                    totalDistance += distance;

                    uvY = totalDistance / 1;
                }

                // Calculate color
                var color = Color.White;

                // Alpha fades out at edges
                if (i == 0 || i == points.Length - 1)
                    color.A = 0;

                // Store forward direction in RGB
                var forwardBiasedAndScaled = (forward + 1.0f) / 2.0f * 255.0f;
                color.R = (byte)forwardBiasedAndScaled.X;
                color.G = (byte)forwardBiasedAndScaled.Y;
                color.B = (byte)forwardBiasedAndScaled.Z;

                vertices.Add(new VertexPositionNormalTexture(p0, Vector3.UnitY, Vector2.Zero));
                vertices.Add(new VertexPositionNormalTexture(p1, Vector3.UnitY, Vector2.Zero));
                vertices.Add(new VertexPositionNormalTexture(p2, Vector3.UnitY, Vector2.Zero));

                if (i < points.Length - 1)
                {
                    indices.Add(vertexCount + 0);
                    indices.Add(vertexCount + 3);
                    indices.Add(vertexCount + 1);

                    indices.Add(vertexCount + 1);
                    indices.Add(vertexCount + 3);
                    indices.Add(vertexCount + 4);

                    indices.Add(vertexCount + 1);
                    indices.Add(vertexCount + 4);
                    indices.Add(vertexCount + 2);

                    indices.Add(vertexCount + 2);
                    indices.Add(vertexCount + 4);
                    indices.Add(vertexCount + 5);
                }

                vertexCount += 3;
                indexCount += 12;
            }

            buffer = Graphics.Buffer.Vertex.New(graphicsDevice, vertices.ToArray(), GraphicsResourceUsage.Dynamic);
            indexBuffer?.Dispose();
            indexBuffer = Graphics.Buffer.Index.New(graphicsDevice, indices.ToArray(), GraphicsResourceUsage.Dynamic);

            var meshDraw = new MeshDraw
            {
                PrimitiveType = PrimitiveType.TriangleList,
                DrawCount = indices.Count,
                IndexBuffer = new IndexBufferBinding(indexBuffer, true, indices.Count),
                VertexBuffers = new[] { new VertexBufferBinding(buffer, VertexPositionNormalTexture.Layout, buffer.ElementCount) }
            };


            return meshDraw;
        }
        

        public void Dispose()
        {
            indexBuffer?.Dispose();
            buffer?.Dispose();

            indexBuffer = null;
            buffer = null;
        }
    }
}
