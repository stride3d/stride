// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

using Xenko.Core;
using Xenko.Graphics;
using Xenko.Graphics.GeometricPrimitives;

namespace Xenko.Rendering.ProceduralModels
{
    /// <summary>
    /// A teapot procedural model.
    /// </summary>
    [DataContract("TeapotProceduralModel")]
    [Display("Teapot")]
    public class TeapotProceduralModel : PrimitiveProceduralModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TeapotProceduralModel"/> class.
        /// </summary>
        public TeapotProceduralModel()
        {
        }

        /// <summary>
        /// Gets or sets the size of this teapot.
        /// </summary>
        /// <value>The size.</value>
        /// <userdoc>The size of the teapot.</userdoc>
        [DataMember(10)]
        [DefaultValue(1.0f)]
        public float Size { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets the tessellation factor (default: 3.0)
        /// </summary>
        /// <value>The tessellation of the teapot. That is the number of polygons composing it.</value>
        [DataMember(20)]
        [DefaultValue(8)]
        public int Tessellation { get; set; } = 8;

        protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        {
            return GeometricPrimitive.Teapot.New(Size, Tessellation, UvScale.X, UvScale.Y);
        }
    }
}
