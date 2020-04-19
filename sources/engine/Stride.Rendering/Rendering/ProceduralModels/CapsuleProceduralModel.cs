// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

using Stride.Core;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;

namespace Stride.Rendering.ProceduralModels
{
    /// <summary>
    /// A Capsule descriptor
    /// </summary>
    [DataContract("CapsuleProceduralModel")]
    [Display("Capsule")]
    public class CapsuleProceduralModel : PrimitiveProceduralModelBase
    {
        /// <summary>
        /// Initializes a new instance of the Capsule descriptor class.
        /// </summary>
        public CapsuleProceduralModel()
        {
        }

        /// <summary>
        /// Gets or sets the length.
        /// </summary>
        /// <value>The length.</value>
        /// <userdoc>The length of the capsule. That is the distance between the center of two extremity spheres.</userdoc>
        [DataMember(10)]
        [DefaultValue(0.5f)]
        public float Length { get; set; } = 0.5f;

        /// <summary>
        /// Gets or sets the radius of the base of the Capsule.
        /// </summary>
        /// <value>The radius.</value>
        /// <userdoc>The radius of the capsule.</userdoc>
        [DataMember(20)]
        [DefaultValue(0.35f)]
        public float Radius { get; set; } = 0.35f;

        /// <summary>
        /// Gets or sets the tessellation factor.
        /// </summary>
        /// <userdoc>The tessellation of the capsule. That is the number of polygons composing it.</userdoc>
        [DataMember(30)]
        [DefaultValue(8)]
        public int Tessellation { get; set; } = 8;

        protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        {
            return GeometricPrimitive.Capsule.New(Length, Radius, Tessellation, UvScale.X, UvScale.Y);
        }
    }
}
