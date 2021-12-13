using System;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Engine.Splines.Models.Mesh
{
    public class BulbMesh
    {
        private const int Tesselation = 360 / 6;

        public MeshDraw MeshDraw;

        private Buffer vertexBuffer;
        private float _range;

        private readonly GraphicsDevice graphicsDevice;

        public BulbMesh(GraphicsDevice graphicsDevice, float range = 0.4f)
        {
            this.graphicsDevice = graphicsDevice;
            _range = range;
        }

        public void Build()
        {
            var indices = new int[2 * Tesselation * 3];
            var vertices = new VertexPositionNormalTexture[(Tesselation + 1) * 3];

            var indexCount = 0;
            var vertexCount = 0;
            // the two rings
            for (var j = 0; j < 3; j++)
            {
                var rotation = Matrix.Identity;
                if (j == 1)
                {
                    rotation = Matrix.RotationX((float)Math.PI / 2);
                }
                else if (j == 2)
                {
                    rotation = Matrix.RotationY((float)Math.PI / 2);
                }

                for (var i = 0; i <= Tesselation; i++)
                {
                    var longitude = (float)(i * 2.0 * Math.PI / Tesselation);
                    var dx = (float)Math.Cos(longitude);
                    var dy = (float)Math.Sin(longitude);
                    var normal = new Vector3(dx * _range, dy * _range, 0);
                    Vector3.TransformNormal(ref normal, ref rotation, out normal);

                    if (i < Tesselation)
                    {
                        indices[indexCount++] = vertexCount;
                        indices[indexCount++] = vertexCount + 1;
                    }

                    vertices[vertexCount++] = new VertexPositionNormalTexture(normal, normal, new Vector2(0));
                }
            }

            vertexBuffer = Buffer.Vertex.New(graphicsDevice, vertices);
            MeshDraw = new MeshDraw
            {
                PrimitiveType = PrimitiveType.LineList,
                DrawCount = indices.Length,
                IndexBuffer = new IndexBufferBinding(Buffer.Index.New(graphicsDevice, indices), true, indices.Length),
                VertexBuffers = new[] { new VertexBufferBinding(vertexBuffer, VertexPositionNormalTexture.Layout, vertexBuffer.ElementCount) },
            };
        }
    }
}
