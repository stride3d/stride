// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Annotations;
using Xenko.Assets.Models;
using Xenko.Engine;
using Xenko.Rendering;

namespace Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    internal class AddModelAssetPolicy<TModelAsset> : CreateComponentPolicyBase<TModelAsset, AssetViewModel<TModelAsset>>
        where TModelAsset : Asset, IModelAsset
    {
        /// <inheritdoc />
        [NotNull]
        protected override EntityComponent CreateComponentFromAsset(EntityHierarchyItemViewModel parent, AssetViewModel<TModelAsset> asset)
        {
            return new ModelComponent
            {
                Model = ContentReferenceHelper.CreateReference<Model>(asset)
            };
        }
    }
}
