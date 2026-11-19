// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
    [AssetFormatVersion(StrideConfig.LogicalPackageName, CurrentVersion, "2.1.0.1")]
    [AssetUpgrader(StrideConfig.LogicalPackageName, "2.1.0.1", "3.1.0.1", typeof(CharacterComponentGravityVector3Upgrader))]
    public partial class SceneAsset : EntityHierarchyAssetBase
    {
        private const string CurrentVersion = "3.1.0.1";

        public const string FileSceneExtension = ".sdscene";

        /// <summary>
        /// A collection of identifier of all the children of this scene.
        /// </summary>
        /// <remarks>
        /// Each child scene names this scene in its <see cref="Parent"/>. When it does not, or when no scene has the identifier, the editor
        /// removes the identifier from this list when it loads the scene, and logs a warning.
        /// </remarks>
        [DataMember(10)]
        [Display(Browsable = false)]
        [NonIdentifiableCollectionItems]
        [NotNull]
        public List<AssetId> ChildrenIds { get; } = new List<AssetId>();

        /// <summary>
        /// The parent scene.
        /// </summary>
        /// <remarks>
        /// The parent scene lists this scene in its <see cref="ChildrenIds"/>. When it does not, the editor adds this scene to that list
        /// when it loads the scenes.
        /// </remarks>
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
