// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.Sprite;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Sprites;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    internal class AddSpriteSheetAssetPolicy : CreateComponentPolicyBase<SpriteSheetAsset, SpriteSheetViewModel>
    {
        /// <inheritdoc />
        protected override bool CanAddOrInsert(EntityHierarchyItemViewModel parent, SpriteSheetViewModel asset, AddChildModifiers modifiers, int index, out string message, params object[] messageArgs)
        {
            if (asset.Asset.Type == SpriteSheetType.UI)
            {
                message = "Sprite sheet type is UI";
                return false;
            }
            message = string.Format("Add to {0}", messageArgs);
            return true;
        }

        /// <inheritdoc />
        [NotNull]
        protected override EntityComponent CreateComponentFromAsset(EntityHierarchyItemViewModel parent, SpriteSheetViewModel asset)
        {
            return new SpriteComponent
            {
                SpriteProvider = new SpriteFromSheet
                {
                    Sheet = ContentReferenceHelper.CreateReference<SpriteSheet>(asset)
                }
            };
        }
    }
}
