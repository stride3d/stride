// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Graphics.GeometricPrimitives;

namespace Xenko.Rendering.ProceduralModels
{
    /// <summary>
    /// The geometric descriptor for a plane.
    /// </summary>
    [DataContract("PlaneProceduralModel")]
    [Display("Plane")]
    public class PlaneProceduralModel : PrimitiveProceduralModelBase
    {
        /// <summary>
        /// Initializes a new instance of geometric descriptor for a plane.
        /// </summary>
        public PlaneProceduralModel()
        {
        }

        /// <summary>
        /// Gets or sets the size of the plane.
        /// </summary>
        /// <value>The size x.</value>
        /// <userdoc>The size of plane along the X/Y axis</userdoc>
        [DataMember(10)]
        [Display("Size")]
        public Vector2 Size { get; set; } = new Vector2(1.0f);

        /// <summary>
        /// Gets or sets the tessellation of the plane.
        /// </summary>
        /// <value>The tessellation x.</value>
        /// <userdoc>The tessellation of the plane along the X/Y axis. That is the number polygons the plane is made of.</userdoc>
        [DataMember(20)]
        [Display("Tessellation")]
        public Int2 Tessellation { get; set; } = new Int2(1);

        /// <summary>
        /// Gets or sets the normal direction of the plane.
        /// </summary>
        /// <userdoc>The direction of the normal of the plane. This changes the default orientation of the plane.</userdoc>
        [DataMember(40)]
        [DefaultValue(NormalDirection.UpY)]
        [Display("Normal")]
        public NormalDirection Normal { get; set; } = NormalDirection.UpY;

        /// <summary>
        /// Gets or sets value indicating if a back face should be added.
        /// </summary>
        /// <userdoc>Check this combo box to generate a back face to the plane</userdoc>
        [DataMember(50)]
        [DefaultValue(false)]
        [Display("Back Face")]
        public bool GenerateBackFace { get; set; }

        protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        {
            return GeometricPrimitive.Plane.New(Size.X, Size.Y, Tessellation.X, Tessellation.Y, UvScale.X, UvScale.Y, GenerateBackFace, false, Normal);
        }
    }
}
