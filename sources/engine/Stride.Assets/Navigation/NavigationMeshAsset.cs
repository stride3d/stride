// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Engine;
using Stride.Navigation;
using Stride.Physics;

namespace Stride.Assets.Navigation
{
    [DataContract("NavigationMeshAsset")]
    [AssetDescription(FileExtension)]
    [AssetContentType(typeof(NavigationMesh))]
    [AssetFormatVersion(StrideConfig.PackageName, CurrentVersion, "2.0.0.0")]
    public partial class NavigationMeshAsset : Asset
    {
        private const string CurrentVersion = "2.0.0.0";

        public const string FileExtension = ".sdnavmesh";

        /// <summary>
        /// Scene that is used for building the navigation mesh
        /// </summary>
        /// <userdoc>
        /// The scene this navigation mesh applies to
        /// </userdoc>
        [DataMember(10)]
        public Scene Scene { get; set; }

        /// <summary>
        /// Collision filter that indicates which colliders are used in navmesh generation
        /// </summary>
        /// <userdoc>
        /// Set which collision groups the navigation mesh uses.
        /// </userdoc>
        [DataMember(20)]
        public CollisionFilterGroupFlags IncludedCollisionGroups { get; set; }

        /// <summary>
        /// Build settings used by Recast
        /// </summary>
        /// <userdoc>
        /// Advanced settings for the navigation mesh
        /// </userdoc>
        [DataMember(30)]
        public NavigationMeshBuildSettings BuildSettings { get; set; }

        /// <summary>
        /// Groups that this navigation mesh should be built for
        /// </summary>
        /// <userdoc>
        /// The groups that use this navigation mesh
        /// </userdoc>
        [DataMember(40)]
        public List<Guid> SelectedGroups { get; } = new List<Guid>();
    }
}
