// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Assets.Textures;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Sprites;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    internal class AddTextureAssetPolicy : CreateComponentPolicyBase<TextureAsset, AssetViewModel<TextureAsset>>
    {
        /// <inheritdoc />
        [NotNull]
        protected override EntityComponent CreateComponentFromAsset(EntityHierarchyItemViewModel parent, AssetViewModel<TextureAsset> asset)
        {
            return new SpriteComponent
            {
                SpriteProvider = new SpriteFromTexture
                {
                    Texture = ContentReferenceHelper.CreateReference<Texture>(asset)
                }
            };
        }
    }
}
