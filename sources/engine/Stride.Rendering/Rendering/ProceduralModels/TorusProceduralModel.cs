// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

using Stride.Core;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;

namespace Stride.Rendering.ProceduralModels
{
    /// <summary>
    /// The Torus Model.
    /// </summary>
    [DataContract("TorusProceduralModel")]
    [Display("Torus")]
    public class TorusProceduralModel : PrimitiveProceduralModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TorusProceduralModel"/> class.
        /// </summary>
        public TorusProceduralModel()
        {
        }

        /// <summary>
        /// Gets or sets the size of this Torus.
        /// </summary>
        /// <value>The radius.</value>
        /// <userdoc>The major radius of the torus.</userdoc>
        [DataMember(10)]
        [DefaultValue(0.375f)]
        public float Radius { get; set; } = 0.375f;

        /// <summary>
        /// Gets or sets the thickness.
        /// </summary>
        /// <value>The minor radius of the torus. That is the radius of the ring.</value>
        [DataMember(20)]
        [DefaultValue(0.125f)]
        public float Thickness { get; set; } = 0.125f;

        /// <summary>
        /// Gets or sets the tessellation factor (default: 3.0)
        /// </summary>
        /// <value>The tessellation.</value>
        /// <userdoc>The tessellation of the torus. That is the number of polygons composing it.</userdoc>
        [DataMember(30)]
        [DefaultValue(32)]
        public int Tessellation { get; set; } = 32;

        protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        {
            return GeometricPrimitive.Torus.New(Radius, Thickness, Tessellation, UvScale.X, UvScale.Y);
        }
    }
}
