// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Templates;
using Stride.Core;
using Stride.Core.IO;
using Stride.Assets.Models;
using Stride.Rendering;
using Stride.Rendering.ProceduralModels;

namespace Stride.Assets.Presentation.Templates
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
