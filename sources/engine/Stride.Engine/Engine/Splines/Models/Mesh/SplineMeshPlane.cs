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
            // First generate the arrays for vertices and indices with the correct size
            var vertexCount = 4;
            var indexCount = 6;
            var vertices = new VertexPositionNormalTexture[vertexCount];
            var indices = new int[indexCount];
            var halfWidth = Width / 2;
            var forward = (TargetOffset - LocalOffset);
            forward.Normalize();
            var right = Vector3.Cross(forward, Vector3.UnitY);
            var left = -right;

            // Create custom vertices, in this case just a quad facing in Y direction
            var normal = Vector3.UnitY;

            vertices[0] = new VertexPositionNormalTexture(halfWidth * right, normal, new Vector2(0, 0));
            vertices[1] = new VertexPositionNormalTexture(halfWidth * left, normal, new Vector2(1, 0));
            vertices[2] = new VertexPositionNormalTexture(forward + (halfWidth * right), normal, new Vector2(0, 1));
            vertices[3] = new VertexPositionNormalTexture(forward + (halfWidth * left), normal, new Vector2(1, 1));

            // Create custom indices
            indices[0] = 0;
            indices[1] = 1;
            indices[2] = 2;
            indices[3] = 1;
            indices[4] = 3;
            indices[5] = 2;

            // Create the primitive object for further processing by the base class
            return new GeometricMeshData<VertexPositionNormalTexture>(vertices, indices, isLeftHanded: false) { Name = "Spline mesh plane" };
        }
    }
}
