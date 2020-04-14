// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;

namespace Stride.Assets.Presentation.Templates
{
    public class AssetFactoryTemplateGenerator : AssetTemplateGenerator
    {
        public static readonly AssetFactoryTemplateGenerator Default = new AssetFactoryTemplateGenerator();

        public override bool IsSupportingTemplate(TemplateDescription templateDescription)
        {
            if (templateDescription == null) throw new ArgumentNullException(nameof(templateDescription));
            return templateDescription is TemplateAssetFactoryDescription;
        }

        protected override IEnumerable<AssetItem> CreateAssets(AssetTemplateGeneratorParameters parameters)
        {
            var desc = parameters.Description as TemplateAssetFactoryDescription;
            if (desc == null)
                yield break;

            var factory = desc.GetFactory();
            if (factory == null)
                throw new InvalidOperationException("Unable to find the asset factory associated to this template.");

            var asset = factory.New();
            yield return new AssetItem(GenerateLocation(parameters), asset);
        }
    }
}
