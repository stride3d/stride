// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;

namespace Stride.Rendering.ProceduralModels
{
    /// <summary>
    /// A sphere procedural.
    /// </summary>
    [DataContract("SphereProceduralModel")]
    [Display("Sphere")]
    public class SphereProceduralModel : PrimitiveProceduralModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SphereProceduralModel"/> class.
        /// </summary>
        public SphereProceduralModel()
        {
        }

        /// <summary>
        /// Gets or sets the radius of this sphere.
        /// </summary>
        /// <value>The radius.</value>
        /// <userdoc>The radius of the sphere.</userdoc>
        [DataMember(10)]
        [DefaultValue(0.5f)]
        public float Radius { get; set; } = 0.5f;

        /// <summary>
        /// Gets or sets the tessellation factor (default: 3.0)
        /// </summary>
        /// <value>The tessellation.</value>
        /// <userdoc>The tessellation of the sphere. That is the number of polygons composing it.</userdoc>
        [DataMember(20)]
        [DefaultValue(16)]
        public int Tessellation { get; set; } = 16;

        protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        {
            return GeometricPrimitive.Sphere.New(Radius, Tessellation, UvScale.X, UvScale.Y);
        }
    }
}
