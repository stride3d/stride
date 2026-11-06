// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Annotations;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Models;
using Stride.Engine;
using Stride.Rendering;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.Services;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.ViewModels
{
    internal abstract class AddModelAssetPolicy<TModelAsset> : CreateComponentPolicyBase<TModelAsset, AssetViewModel<TModelAsset>>
        where TModelAsset : Asset, IModelAsset
    {
        [NotNull]
        protected override EntityComponent CreateComponentFromAsset(IEntityHierarchyItemViewModel parent, AssetViewModel<TModelAsset> asset)
        {
            return new ModelComponent
            {
                Model = ContentReferenceHelper.CreateReference<Model>(asset)
            };
        }
    }

    internal sealed class AddModelAssetPolicy : AddModelAssetPolicy<ModelAsset>
    {
    }

    internal sealed class AddPrefabModelAssetPolicy : AddModelAssetPolicy<PrefabModelAsset>
    {
    }

    internal sealed class AddProceduralModelAssetPolicy : AddModelAssetPolicy<ProceduralModelAsset>
    {
    }
}
