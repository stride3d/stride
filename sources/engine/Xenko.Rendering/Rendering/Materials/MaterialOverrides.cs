// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Mathematics;

namespace Xenko.Rendering.Materials
{
    /// <summary>
    /// Material overrides used in a <see cref="MaterialBlendLayer"/>
    /// </summary>
    [DataContract("MaterialOverrides")]
    [Display("Layer Overrides")]
    public class MaterialOverrides
    {
        // TODO: This class should use instead pluggable ovrride so that the system can support new overrides

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialOverrides"/> class.
        /// </summary>
        public MaterialOverrides()
        {
            //SurfaceContribution = 1.0f;
            //MicroSurfaceContribution = 1.0f;
            //DiffuseContribution = 1.0f;
            //SpecularContribution = 1.0f;
            //OcclusionContribution = 1.0f;
            UVScale = new Vector2(1.0f, 1.0f);
        }

        /*
        /// <summary>
        /// Gets or sets the surface contribution.
        /// </summary>
        /// <value>The surface contribution.</value>
        [Display("Surface Contribution")]
        [DefaultValue(1.0f)]
        [DataMember(10)]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1)]
        public float SurfaceContribution { get; set; }

        /// <summary>
        /// Gets or sets the micro surface contribution.
        /// </summary>
        /// <value>The micro surface contribution.</value>
        [Display("MicroSurface Contribution")]
        [DefaultValue(1.0f)]
        [DataMember(20)]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1)]
        public float MicroSurfaceContribution { get; set; }

        /// <summary>
        /// Gets or sets the diffuse contribution.
        /// </summary>
        /// <value>The diffuse contribution.</value>
        [Display("Diffuse Contribution")]
        [DefaultValue(1.0f)]
        [DataMember(30)]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1)]
        public float DiffuseContribution { get; set; }

        /// <summary>
        /// Gets or sets the specular contribution.
        /// </summary>
        /// <value>The specular contribution.</value>
        [Display("Specular Contribution")]
        [DefaultValue(1.0f)]
        [DataMember(40)]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1)]
        public float SpecularContribution { get; set; }

        /// <summary>
        /// Gets or sets the occlusion contribution.
        /// </summary>
        /// <value>The occlusion contribution.</value>
        [Display("Occlusion Contribution")]
        [DefaultValue(1.0f)]
        [DataMember(50)]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1)]
        public float OcclusionContribution { get; set; }
        */

        /// <summary>
        /// Gets or sets the scale u and v.
        /// </summary>
        /// <value>The scale u.</value>
        /// <userdoc>The scales to apply on the layered material texture coordinates.</userdoc>
        [DataMember(80)]
        [Display("UV Scale")]
        public Vector2 UVScale { get; set; }

        ///// <summary>
        ///// Gets or sets the scale v.
        ///// </summary>
        ///// <value>The scale v.</value>
        //[DefaultValue(1.0f)]
        //[DataMember(90)]
        //public float ScaleV { get; set; }

        /// <summary>
        /// Implements the multiply operator in order to support correct calculation of overrides for a particular layer.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static MaterialOverrides operator *(MaterialOverrides left, MaterialOverrides right)
        {
            var overrides = new MaterialOverrides { UVScale = left.UVScale * right.UVScale };
            return overrides;
        }
    }
}
