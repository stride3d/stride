// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Stride.Assets.Models;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Rendering;

namespace Stride.Assets.Presentation.Templates
{
    public class HullAssetFactoryTemplateGenerator : AssetFactoryTemplateGenerator
    {
        private static readonly PropertyKey<Model> ModelKey = new PropertyKey<Model>("Model", typeof(HullAssetFactoryTemplateGenerator));
        public new static readonly HullAssetFactoryTemplateGenerator Default = new HullAssetFactoryTemplateGenerator();

        public static readonly Guid TemplateId = new Guid("FDBCB65A-9866-4EF2-9A03-AF21F943BDFD");

        public override bool IsSupportingTemplate(TemplateDescription templateDescription)
        {
            if (templateDescription == null) throw new ArgumentNullException(nameof(templateDescription));
            return templateDescription.Id == TemplateId;
        }

        protected override async Task<bool> PrepareAssetCreation(AssetTemplateGeneratorParameters parameters)
        {
            if (!await base.PrepareAssetCreation(parameters))
                return false;

            var modelViewModel = await BrowseForAsset(parameters.Package, new[] { typeof(IModelAsset) }, new UFile(parameters.Name).GetFullDirectory(), "Select the model to use to generate a convex hull - _cancel to leave the model empty_");
            var model = ContentReferenceHelper.CreateReference<Model>(modelViewModel);
            parameters.SetTag(ModelKey, model);
            return true;
        }

        protected override void PostAssetCreation(AssetTemplateGeneratorParameters parameters, AssetItem assetItem)
        {
            base.PostAssetCreation(parameters, assetItem);
            var model = parameters.GetTag(ModelKey);
            // Template generators aren't currently written to be implemented plugin-side, so we'll have to do some reflexion to assign the hull back
            assetItem.Asset.GetType().GetField("Model").SetValue(assetItem.Asset, model);
        }
    }
}
