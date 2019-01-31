// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Assets;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Engine;

namespace Xenko.Assets.Entities
{
    [DataContract("PrefabAsset")]
    [AssetDescription(FileExtension, AllowArchetype = false)]
    [AssetContentType(typeof(Prefab))]
    [AssetFormatVersion(XenkoConfig.PackageName, CurrentVersion, "2.1.0.1")]
    [AssetUpgrader(XenkoConfig.PackageName, "2.1.0.1", "3.1.0.1", typeof(CharacterComponentGravityVector3Upgrader))]
    public partial class PrefabAsset : EntityHierarchyAssetBase
    {
        private const string CurrentVersion = "3.1.0.1";

        /// <summary>
        /// The default file extension used by the <see cref="PrefabAsset"/>.
        /// </summary>
        public const string FileExtension = ".xkprefab";

        /// <summary>
        /// Creates a instance of this prefab that can be added to another <see cref="EntityHierarchyAssetBase"/>.
        /// </summary>
        /// <param name="targetLocation">The location of the target container asset.</param>
        /// <returns>An <see cref="AssetCompositeHierarchyData{EntityDesign, Entity}"/> containing the cloned entities of </returns>
        [NotNull]
        public AssetCompositeHierarchyData<EntityDesign, Entity> CreatePrefabInstance([NotNull] string targetLocation)
        {
            Guid unused;
            return CreatePrefabInstance(targetLocation, out unused);
        }

        /// <summary>
        /// Creates a instance of this prefab that can be added to another <see cref="EntityHierarchyAssetBase"/>.
        /// </summary>
        /// <param name="targetLocation">The location of the target container asset.</param>
        /// <param name="instanceId">The identifier of the created instance.</param>
        /// <returns>An <see cref="AssetCompositeHierarchyData{EntityDesign, Entity}"/> containing the cloned entities of </returns>
        [NotNull]
        public AssetCompositeHierarchyData<EntityDesign, Entity> CreatePrefabInstance([NotNull] string targetLocation, out Guid instanceId)
        {
            Dictionary<Guid, Guid> idRemapping;
            var instance = (PrefabAsset)CreateDerivedAsset(targetLocation, out idRemapping);
            instanceId = instance.Hierarchy.Parts.Values.FirstOrDefault()?.Base?.InstanceId ?? Guid.NewGuid();
            return instance.Hierarchy;
        }
    }
}
