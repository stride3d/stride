//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.ProceduralModels;

namespace Stride.Engine.Splines.Models
{
    [DataContract("Spline mesh")]
    public abstract class SplineMesh : PrimitiveProceduralModelBase
    {
        /// <summary>
        /// Gets or sets the target offset that will be applied to the procedural model's vertexes.
        /// </summary>
        [DataMember(530)]
        public Vector3 TargetOffset { get; set; }


        public BezierPoint[] bezierPoints;

        protected abstract override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData();
    }
}
