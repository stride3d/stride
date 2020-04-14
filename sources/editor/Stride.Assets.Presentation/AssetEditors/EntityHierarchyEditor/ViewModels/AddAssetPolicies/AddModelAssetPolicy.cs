// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Assets.Models;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
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
