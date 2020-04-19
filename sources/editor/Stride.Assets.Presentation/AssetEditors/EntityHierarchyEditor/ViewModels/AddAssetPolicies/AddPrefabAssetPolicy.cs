// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.ViewModel;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    public class AddPrefabAssetPolicy : CreateEntitiesPolicyBase<PrefabAsset, PrefabViewModel>
    {
        /// <inheritdoc />
        protected override bool CanAddOrInsert(EntityHierarchyItemViewModel parent, PrefabViewModel asset, AddChildModifiers modifiers, int index, out string message, params object[] messageArgs)
        {
            var prefab = asset;
            if (prefab == parent.Asset || prefab.GatherAllBasePartAssets().Contains(parent.Asset))
            {
                message = "This prefab depends on this asset and can't be added.";
                return false;
            }
            if (prefab.Asset.Hierarchy.Parts.Count == 0)
            {
                message = "This prefab is empty and can't be added.";
                return false;
            }
            message = (modifiers & AddChildModifiers.Alt) != AddChildModifiers.Alt
                ? string.Format("Add the prefab to {0}\r\n(Hold Alt to add without a container entity)", messageArgs)
                : string.Format("Add the prefab to {0}\r\n(Release Alt to add with a container entity)", messageArgs);
            return true;
        }

        /// <inheritdoc />
        [NotNull]
        protected override AssetCompositeHierarchyData<EntityDesign, Entity> CreateEntitiesFromAsset(EntityHierarchyItemViewModel parent, PrefabViewModel asset)
        {
            return asset.Asset.CreatePrefabInstance(asset.Url);
        }
    }
}
