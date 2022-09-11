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
        public BezierPoint[] bezierPoints;
        public bool Loop;

        protected abstract override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData();
    }
}
