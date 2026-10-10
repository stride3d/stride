// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;

namespace Stride.Rendering.ProceduralModels
{
    /// <summary>
    /// A cube procedural model
    /// </summary>
    [DataContract("CubeProceduralModel")]
    [Display("Cube")]
    public class CubeProceduralModel : PrimitiveProceduralModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CubeProceduralModel"/> class.
        /// </summary>
        public CubeProceduralModel()
        {
        }

        /// <summary>
        /// Gets or sets the size of the cube.
        /// </summary>
        /// <value>The size.</value>
        /// <userdoc>The size of the cube along the Ox, Oy and Oz axis.</userdoc>
        [DataMember(10)]
        public Vector3 Size { get; set; } = Vector3.One;

        /// <summary>
        /// Gets or sets the number of cuts added along each edge of a face.
        /// </summary>
        /// <remarks>Negative values are treated as 0.</remarks>
        /// <userdoc>The number of cuts added along each edge, splitting every face into a grid, so that the cube can bend when its vertices are deformed.</userdoc>
        [DataMember(20)]
        [DefaultValue(0)]
        public int Subdivisions { get; set; } = 0;

        protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        {
            return GeometricPrimitive.Cube.New(Size, UvScale.X, UvScale.Y, subdivisions: Subdivisions);
        }
    }
}
