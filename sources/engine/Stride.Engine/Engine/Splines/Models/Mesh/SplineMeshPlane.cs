//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Engine.Splines.Models
{
    [DataContract("SplineMeshPlane")]
    [Display("Spline mesh plane")]
    public class SplineMeshPlane : SplineMesh
    {


        [Display("Width")]
        public float Width { get; set; } = 1;


        protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        {
            var vertexCount = bezierPoints.Length * 2;
            var indexCount = (bezierPoints.Length - 1) * 6;
            var vertices = new VertexPositionNormalTexture[vertexCount];
            var indices = new int[indexCount];
            var halfWidth = Width / 2;

            var index = 0;
            for (int i = 0; i < bezierPoints.Length - 1; i++)
            {
                var startPoint = bezierPoints[i];
                var targetPoint = bezierPoints[i + 1];
                var forward = (targetPoint.Position - startPoint.Position);
                forward.Normalize();
                var right = Vector3.Cross(forward, Vector3.UnitY);
                var left = -right;

                // Create custom vertices, in this case just a quad facing in Y direction
                var normal = Vector3.UnitY;

                vertices[index] = new VertexPositionNormalTexture(halfWidth * right, normal, new Vector2(0, 0));
                vertices[index + 1] = new VertexPositionNormalTexture(halfWidth * left, normal, new Vector2(1, 0));
                vertices[index + 2] = new VertexPositionNormalTexture(forward + (halfWidth * right), normal, new Vector2(0, 1));
                vertices[index + 3] = new VertexPositionNormalTexture(forward + (halfWidth * left), normal, new Vector2(1, 1));

                // Create custom indices
                var indiceIndex = i * 6;
                indices[indiceIndex + 0] = index + 0;
                indices[indiceIndex + 1] = index + 1;
                indices[indiceIndex + 2] = index + 2;
                indices[indiceIndex + 3] = index + 1;
                indices[indiceIndex + 4] = index + 3;
                indices[indiceIndex + 5] = index + 2;

                index += 2;
            }

            // Create the primitive object for further processing by the base class
            return new GeometricMeshData<VertexPositionNormalTexture>(vertices, indices, isLeftHanded: false) { Name = "Spline mesh plane" };
        }
    }
}
