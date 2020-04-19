// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Templates;
using Stride.Core;
using Stride.Core.IO;
using Stride.Assets.Skyboxes;
using Stride.Graphics;

namespace Stride.Assets.Presentation.Templates
{
    public class SkyboxFactoryTemplateGenerator : AssetFactoryTemplateGenerator
    {
        private static readonly PropertyKey<Texture> TextureKey = new PropertyKey<Texture>("Texture", typeof(SkyboxFactoryTemplateGenerator));
        public new static readonly SkyboxFactoryTemplateGenerator Default = new SkyboxFactoryTemplateGenerator();

        public static readonly Guid TemplateId = new Guid("F16AB154-86A6-4020-B410-88FBD87D3CA5");

        public override bool IsSupportingTemplate(TemplateDescription templateDescription)
        {
            if (templateDescription == null) throw new ArgumentNullException(nameof(templateDescription));
            return templateDescription.Id == TemplateId;
        }

        protected override async Task<bool> PrepareAssetCreation(AssetTemplateGeneratorParameters parameters)
        {
            if (!await base.PrepareAssetCreation(parameters))
                return false;

            var acceptedTypes = AssetRegistry.GetAssetTypes(typeof(Texture));
            var textureViewModel = await BrowseForAsset(parameters.Package, acceptedTypes, new UFile(parameters.Name).GetFullDirectory(), "Select a cubemap texture to use for this skybox - _cancel to leave the texture empty_");
            var texture = ContentReferenceHelper.CreateReference<Texture>(textureViewModel);
            parameters.SetTag(TextureKey, texture);
            return true;
        }

        protected override void PostAssetCreation(AssetTemplateGeneratorParameters parameters, AssetItem assetItem)
        {
            base.PostAssetCreation(parameters, assetItem);
            var texture = parameters.GetTag(TextureKey);
            ((SkyboxAsset)assetItem.Asset).CubeMap = texture;
        }
    }
}
