// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.Assets.Entities
{
    /// <summary>
    /// A scene asset.
    /// </summary>
    [DataContract("SceneAsset")]
    [AssetDescription(FileSceneExtension, AllowArchetype = false)]
    [AssetContentType(typeof(Scene))]
    [AssetFormatVersion(StrideConfig.PackageName, CurrentVersion, "2.1.0.1")]
    [AssetUpgrader(StrideConfig.PackageName, "2.1.0.1", "3.1.0.1", typeof(CharacterComponentGravityVector3Upgrader))]
    public partial class SceneAsset : EntityHierarchyAssetBase
    {
        private const string CurrentVersion = "3.1.0.1";

        public const string FileSceneExtension = ".sdscene";

        /// <summary>
        /// A collection of identifier of all the children of this scene..
        /// </summary>
        [DataMember(10)]
        [Display(Browsable = false)]
        [NonIdentifiableCollectionItems]
        [NotNull]
        public List<AssetId> ChildrenIds { get; } = new List<AssetId>();

        /// <summary>
        /// The parent scene.
        /// </summary>
        /// <userdoc>The parent scene.</userdoc>
        [DataMember(20)]
        [Display(Browsable = false)] // TODO: make it visible in the property grid, but readonly.
        [DefaultValue(null)]
        public Scene Parent { get; set; }

        /// <summary>
        /// The translation offset relative to the <see cref="Parent"/> scene.
        /// </summary>
        /// <userdoc>The translation offset of the scene with regard to its parent scene, if any.</userdoc>
        [DataMember(30)]
        public Vector3 Offset { get; set; }
    }
}
