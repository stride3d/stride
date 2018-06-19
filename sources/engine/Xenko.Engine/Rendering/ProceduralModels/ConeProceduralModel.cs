// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

using Xenko.Core;
using Xenko.Graphics;
using Xenko.Graphics.GeometricPrimitives;

namespace Xenko.Rendering.ProceduralModels
{
    /// <summary>
    /// A Cone descriptor
    /// </summary>
    [DataContract("ConeProceduralModel")]
    [Display("Cone")]
    public class ConeProceduralModel : PrimitiveProceduralModelBase
    {
        /// <summary>
        /// Initializes a new instance of the Cone descriptor class.
        /// </summary>
        public ConeProceduralModel()
        {
        }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The height.</value>
        /// <userdoc>The height of the cone.</userdoc>
        [DataMember(10)]
        [DefaultValue(1.0f)]
        public float Height { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets the radius of the base of the Cone.
        /// </summary>
        /// <value>The radius.</value>
        /// <userdoc>The radius of the cone.</userdoc>
        [DataMember(20)]
        [DefaultValue(0.5f)]
        public float Radius { get; set; } = 0.5f;

        /// <summary>
        /// Gets or sets the tessellation factor.
        /// </summary>
        /// <value>The tessellation.</value>
        /// <userdoc>The tessellation of the cone. That is the number of polygons composing it.</userdoc>
        [DataMember(30)]
        [DefaultValue(16)]
        public int Tessellation { get; set; } = 16;

        protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        {
            return GeometricPrimitive.Cone.New(Radius, Height, Tessellation, UvScale.X, UvScale.Y);
        }
    }
}
