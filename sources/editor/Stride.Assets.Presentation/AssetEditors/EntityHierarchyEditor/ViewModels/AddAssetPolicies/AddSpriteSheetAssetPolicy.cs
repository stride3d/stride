// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Annotations;
using Xenko.Assets.Presentation.ViewModel;
using Xenko.Assets.Sprite;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Rendering.Sprites;

namespace Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
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
