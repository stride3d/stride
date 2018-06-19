// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Templates;
using Xenko.Core;
using Xenko.Core.IO;
using Xenko.Assets.Models;
using Xenko.Rendering;
using Xenko.Rendering.ProceduralModels;

namespace Xenko.Assets.Presentation.Templates
{
    public class ProceduralModelFactoryTemplateGenerator : AssetFactoryTemplateGenerator
    {
        private static readonly PropertyKey<Material> MaterialKey = new PropertyKey<Material>("Material", typeof(ProceduralModelFactoryTemplateGenerator));
        public new static readonly ProceduralModelFactoryTemplateGenerator Default = new ProceduralModelFactoryTemplateGenerator();

        public static readonly Guid TemplateId = new Guid("8267F08C-3DC8-48F4-81EA-4888A1CEF9CE");

        public override bool IsSupportingTemplate(TemplateDescription templateDescription)
        {
            if (templateDescription == null) throw new ArgumentNullException(nameof(templateDescription));
            return templateDescription.Id == TemplateId;
        }

        protected override async Task<bool> PrepareAssetCreation(AssetTemplateGeneratorParameters parameters)
        {
            if (!await base.PrepareAssetCreation(parameters))
                return false;

            var acceptedTypes = AssetRegistry.GetAssetTypes(typeof(Material));
            var materialViewModel = await BrowseForAsset(parameters.Package, acceptedTypes, new UFile(parameters.Name).GetFullDirectory(), "Select a material to use for this model - _cancel to leave the material empty_");
            var material = ContentReferenceHelper.CreateReference<Material>(materialViewModel);
            parameters.SetTag(MaterialKey, material);
            return true;
        }

        protected override void PostAssetCreation(AssetTemplateGeneratorParameters parameters, AssetItem assetItem)
        {
            base.PostAssetCreation(parameters, assetItem);
            var material = parameters.GetTag(MaterialKey);
            ((PrimitiveProceduralModelBase)((ProceduralModelAsset)assetItem.Asset).Type).MaterialInstance.Material = material;
        }
    }
}
