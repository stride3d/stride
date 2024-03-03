// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core;
using Stride.BepuPhysics.Definitions;
using System.Collections.Generic;
using Stride.Core.Annotations;
using Stride.Physics;
using Stride.Rendering;
using Stride.Core.Mathematics;

namespace Stride.Assets.BepuPhysics
{
    [DataContract("HullAsset")]
    [AssetDescription(FileExtension)]
    [AssetContentType(typeof(DecomposedHulls))]
    [AssetFormatVersion(StrideConfig.PackageName, CurrentVersion, "2.0.0.0")]
    public partial class HullAsset : Asset
    {
        private const string CurrentVersion = "2.0.0.0";

        public const string FileExtension = ".sdhull";

        /// <summary>
        /// Multiple meshes -> Multiple Hulls per mesh
        /// </summary>
        [Display(Browsable = false)]
        [DataMember(10)]
        public List<List<DecomposedHulls.Hull>> ConvexHulls = null;

        /// <userdoc>
        /// Model asset from where the engine will derive the convex hull.
        /// </userdoc>
        [DataMember(30)]
        public Model Model;

        /// <userdoc>
        /// The offset with the real graphic mesh.
        /// </userdoc>
        [DataMember(31)]
        public Vector3 LocalOffset;

        /// <userdoc>
        /// The local rotation of the collider shape.
        /// </userdoc>
        [DataMember(32)]
        public Quaternion LocalRotation = Quaternion.Identity;

        /// <userdoc>
        /// The scaling of the generated convex hull.
        /// </userdoc>
        [DataMember(45)]
        public Vector3 Scaling = Vector3.One;

        /// <summary>
        /// Parameters used when decomposing the given <see cref="Model"/> into a hull
        /// </summary>
        [DataMember(50)]
        [NotNull]
        public ConvexHullDecompositionParameters Decomposition { get; set; } = new ConvexHullDecompositionParameters();
    }
}
