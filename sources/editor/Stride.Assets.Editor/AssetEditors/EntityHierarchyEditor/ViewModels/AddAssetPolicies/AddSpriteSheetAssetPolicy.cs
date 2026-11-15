// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Annotations;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Sprite;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Sprites;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.Services;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.ViewModels
{
    internal sealed class AddSpriteSheetAssetPolicy : CreateComponentPolicyBase<SpriteSheetAsset, SpriteSheetViewModel>
    {
        /// <inheritdoc />
        protected override bool CanAddOrInsert(IEntityHierarchyItemViewModel parent, SpriteSheetViewModel asset, AddChildModifiers modifiers, int index, out string message, params object[] messageArgs)
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
        protected override EntityComponent CreateComponentFromAsset(IEntityHierarchyItemViewModel parent, SpriteSheetViewModel asset)
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
