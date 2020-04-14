// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Annotations;
using Stride.Assets.Presentation.ViewModel;
using Stride.Engine;
using Stride.SpriteStudio.Offline;
using Stride.SpriteStudio.Runtime;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    internal class AddSpriteStudioModelAssetPolicy : CreateComponentPolicyBase<SpriteStudioModelAsset, SpriteStudioModelViewModel>
    {
        /// <inheritdoc />
        [NotNull]
        protected override EntityComponent CreateComponentFromAsset(EntityHierarchyItemViewModel parent, SpriteStudioModelViewModel asset)
        {
            return new SpriteStudioComponent
            {
                Sheet = ContentReferenceHelper.CreateReference<SpriteStudioSheet>(asset),
            };
        }
    }
}
