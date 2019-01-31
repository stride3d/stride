// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Compiler;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Engine;
using Xenko.UI;

namespace Xenko.Assets.UI
{
    [DataContract("UILibraryAsset")]
    [AssetDescription(FileExtension, AllowArchetype = false)]
    [AssetContentType(typeof(UILibrary))]
    [AssetFormatVersion(XenkoConfig.PackageName, CurrentVersion, "2.1.0.1")]
    public partial class UILibraryAsset : UIAssetBase
    {
        private const string CurrentVersion = "2.1.0.1";

        /// <summary>
        /// The default file extension used by the <see cref="UILibraryAsset"/>.
        /// </summary>
        public const string FileExtension = ".xkuilib";

        /// <summary>
        /// Gets the dictionary of publicly exposed controls.
        /// </summary>
        [DataMember(20)]
        [NonIdentifiableCollectionItems]
        public Dictionary<Guid, string> PublicUIElements { get; } = new Dictionary<Guid, string>();

        /// <summary>
        /// Creates a instance of the given control that can be added to another <see cref="UIAssetBase"/>.
        /// </summary>
        /// <param name="targetContainer">The container in which the instance will be added.</param>
        /// <param name="targetLocation">The location of the <see paramref="targetContainer"/> asset.</param>
        /// <param name="elementId">The id of the element to instantiate.</param>
        /// <returns>An <see cref="AssetCompositeHierarchyData{UIElementDesign, UIElement}"/> containing the cloned elements of </returns>
        /// <remarks>This method will update the <see cref="Asset.BaseParts"/> property of the <see paramref="targetContainer"/>.</remarks>
        [NotNull]
        public AssetCompositeHierarchyData<UIElementDesign, UIElement> CreateElementInstance(UIAssetBase targetContainer, [NotNull] string targetLocation, Guid elementId)
        {
            Guid unused;
            return CreateElementInstance(targetContainer, targetLocation, elementId, out unused);
        }

        /// <summary>
        /// Creates a instance of the given control that can be added to another <see cref="UIAssetBase"/>.
        /// </summary>
        /// <param name="targetContainer">The container in which the instance will be added.</param>
        /// <param name="targetLocation">The location of this asset.</param>
        /// <param name="elementId">The id of the element to instantiate.</param>
        /// <param name="instanceId">The identifier of the created instance.</param>
        /// <returns>An <see cref="AssetCompositeHierarchyData{UIElementDesign, UIElement}"/> containing the cloned elements of </returns>
        /// <remarks>This method will update the <see cref="Asset.BaseParts"/> property of the <see paramref="targetContainer"/>.</remarks>
        [NotNull]
        public AssetCompositeHierarchyData<UIElementDesign, UIElement> CreateElementInstance(UIAssetBase targetContainer, [NotNull] string targetLocation, Guid elementId, out Guid instanceId)
        {
            // TODO: make a common base method in AssetCompositeHierarchy - the beginning of the method is similar to CreatePrefabInstance
            var idRemapping = new Dictionary<Guid, Guid>();
            var instance = (UILibraryAsset)CreateDerivedAsset(targetLocation, out idRemapping);

            var rootElementId = idRemapping[elementId];
            if (instance.Hierarchy.RootParts.All(x => x.Id != rootElementId))
                throw new ArgumentException(@"The given id cannot be found in the root parts of this library.", nameof(elementId));

            instanceId = instance.Hierarchy.Parts.Values.FirstOrDefault()?.Base?.InstanceId ?? Guid.NewGuid();

            var result = new AssetCompositeHierarchyData<UIElementDesign, UIElement>();
            result.RootParts.Add(instance.Hierarchy.Parts[rootElementId].UIElement);
            result.Parts.Add(instance.Hierarchy.Parts[rootElementId]);
            foreach (var element in this.EnumerateChildPartDesigns(instance.Hierarchy.Parts[rootElementId], instance.Hierarchy, true))
            {
                result.Parts.Add(element);
            }
            return result;
        }
    }
}
