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
using Stride.Assets.Physics;
using Stride.Physics;
using Stride.Rendering;

namespace Stride.Assets.Presentation.Templates
{
    public class ColliderShapeHullFactoryTemplateGenerator : AssetFactoryTemplateGenerator
    {
        private static readonly PropertyKey<Model> ModelKey = new PropertyKey<Model>("Model", typeof(ColliderShapeHullFactoryTemplateGenerator));
        public new static readonly ColliderShapeHullFactoryTemplateGenerator Default = new ColliderShapeHullFactoryTemplateGenerator();

        public static readonly Guid TemplateId = new Guid("5A3CD5E3-4318-46AF-A41C-8146D2D21F80");

        public override bool IsSupportingTemplate(TemplateDescription templateDescription)
        {
            if (templateDescription == null) throw new ArgumentNullException(nameof(templateDescription));
            return templateDescription.Id == TemplateId;
        }

        protected override async Task<bool> PrepareAssetCreation(AssetTemplateGeneratorParameters parameters)
        {
            if (!await base.PrepareAssetCreation(parameters))
                return false;

            var modelViewModel = await BrowseForAsset(parameters.Package, new[] { typeof(IModelAsset) }, new UFile(parameters.Name).GetFullDirectory(), "Select a model to use to generate the hull - _cancel to leave the model empty_");
            var model = ContentReferenceHelper.CreateReference<Model>(modelViewModel);
            parameters.SetTag(ModelKey, model);
            return true;
        }

        protected override void PostAssetCreation(AssetTemplateGeneratorParameters parameters, AssetItem assetItem)
        {
            base.PostAssetCreation(parameters, assetItem);
            var model = parameters.GetTag(ModelKey);
            ((ConvexHullColliderShapeDesc)((ColliderShapeAsset)assetItem.Asset).ColliderShapes[0]).Model = model;
        }
    }
}
