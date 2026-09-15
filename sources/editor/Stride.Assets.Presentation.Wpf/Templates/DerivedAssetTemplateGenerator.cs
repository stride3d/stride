// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Assets.Rendering;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;

namespace Stride.Assets.Presentation.Templates
{
    /// <summary>
    /// Creates an asset derived from <see cref="TemplateAssetFactoryDescription.DerivedFrom"/>, or a fresh asset when that one is not found.
    /// </summary>
    public class DerivedAssetTemplateGenerator : AssetFactoryTemplateGenerator
    {
        public new static readonly DerivedAssetTemplateGenerator Default = new DerivedAssetTemplateGenerator();

        public override bool IsSupportingTemplate(TemplateDescription templateDescription)
        {
            if (templateDescription == null) throw new ArgumentNullException(nameof(templateDescription));
            return templateDescription is TemplateAssetFactoryDescription { DerivedFrom: not null };
        }

        protected override IEnumerable<AssetItem> CreateAssets(AssetTemplateGeneratorParameters parameters)
        {
            var archetype = parameters.Package.FindAsset(((TemplateAssetFactoryDescription)parameters.Description).DerivedFrom);
            var assetItems = archetype == null
                ? base.CreateAssets(parameters).ToList()
                : [new AssetItem(GenerateLocation(parameters), archetype.CreateDerivedAsset())];

            // A new compositor gets the render features of the loaded packages
            foreach (var assetItem in assetItems)
            {
                if (assetItem.Asset is GraphicsCompositorAsset compositor)
                    RenderFeatureProviders.AddPackageRenderFeatures(compositor, assetItem.YamlMetadata);
            }
            return assetItems;
        }
    }
}
