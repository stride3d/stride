//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.ProceduralModels;

namespace Stride.Engine.Splines.Models.Mesh
{
    [DataContract("Spline mesh")]
    public abstract class SplineMesh : PrimitiveProceduralModelBase
    {
        [DataMemberIgnore] public BezierPoint[] BezierPoints;

        [DataMemberIgnore] public bool Loop;

        public float Rotation;

        /// <summary>
        /// Generate geometry for endings
        /// </summary>
        public bool CloseEnds;

        protected VertexPositionNormalTexture[] vertices;
        protected int[] indices;

        protected abstract override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData();

        protected void CreateVertex(int verticesIndex, Vector3 position, Vector3 normal, Vector2 texture)
        {
            vertices[verticesIndex] = new VertexPositionNormalTexture(position, normal, texture);
        }
    }
}
