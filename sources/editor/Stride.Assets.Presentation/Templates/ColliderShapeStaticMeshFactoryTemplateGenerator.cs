// Copyright (c) Stride contributors (https://stride3d.net)
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
    public class ColliderShapeStaticMeshFactoryTemplateGenerator : AssetFactoryTemplateGenerator
    {
        private static readonly PropertyKey<Model> ModelKey = new PropertyKey<Model>("Model", typeof(ColliderShapeStaticMeshFactoryTemplateGenerator));
        public new static readonly ColliderShapeStaticMeshFactoryTemplateGenerator Default = new ColliderShapeStaticMeshFactoryTemplateGenerator();

        public static readonly Guid TemplateId = new Guid("5A3CD5E3-4328-36AF-A41C-8146D2D21F83");

        public override bool IsSupportingTemplate(TemplateDescription templateDescription)
        {
            if (templateDescription == null) throw new ArgumentNullException(nameof(templateDescription));
            return templateDescription.Id == TemplateId;
        }

        protected override async Task<bool> PrepareAssetCreation(AssetTemplateGeneratorParameters parameters)
        {
            if (!await base.PrepareAssetCreation(parameters))
                return false;

            var modelViewModel = await BrowseForAsset(parameters.Package, new[] { typeof(IModelAsset) }, new UFile(parameters.Name).GetFullDirectory(), "Select a model to use for this static mesh - _cancel to leave the model empty_");
            var model = ContentReferenceHelper.CreateReference<Model>(modelViewModel);
            parameters.SetTag(ModelKey, model);
            return true;
        }

        protected override void PostAssetCreation(AssetTemplateGeneratorParameters parameters, AssetItem assetItem)
        {
            base.PostAssetCreation(parameters, assetItem);
            var model = parameters.GetTag(ModelKey);
            ((StaticMeshColliderShapeDesc)((ColliderShapeAsset)assetItem.Asset).ColliderShapes[0]).Model = model;
        }
    }
}
