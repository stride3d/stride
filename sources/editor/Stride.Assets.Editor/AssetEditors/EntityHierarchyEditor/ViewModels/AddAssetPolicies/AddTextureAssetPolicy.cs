// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Annotations;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Textures;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Sprites;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.Services;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.ViewModels
{
    internal sealed class AddTextureAssetPolicy : CreateComponentPolicyBase<TextureAsset, AssetViewModel<TextureAsset>>
    {
        /// <inheritdoc />
        [NotNull]
        protected override EntityComponent CreateComponentFromAsset(IEntityHierarchyItemViewModel parent, AssetViewModel<TextureAsset> asset)
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
