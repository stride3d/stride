// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Templates;

namespace Xenko.Assets.Presentation.Templates
{
    public class GraphicsCompositorTemplateGenerator : AssetFactoryTemplateGenerator
    {
        public new static readonly GraphicsCompositorTemplateGenerator Default = new GraphicsCompositorTemplateGenerator();

        public static readonly Guid Level9TemplateId = new Guid("20947F4A-7B50-4716-AC85-D10EFF58CD33");
        public static readonly Guid Level10TemplateId = new Guid("D4EE3BD3-9B06-460E-9175-D6AFB2459463");

        public override bool IsSupportingTemplate(TemplateDescription templateDescription)
        {
            if (templateDescription == null) throw new ArgumentNullException(nameof(templateDescription));
            return templateDescription.Id == Level9TemplateId || templateDescription.Id == Level10TemplateId;
        }

        protected override IEnumerable<AssetItem> CreateAssets(AssetTemplateGeneratorParameters parameters)
        {
            // Find default graphics compositor to create a derived asset from
            var graphicsCompositorUrl = parameters.Description.Id == Level9TemplateId ? XenkoPackageUpgrader.DefaultGraphicsCompositorLevel9Url : XenkoPackageUpgrader.DefaultGraphicsCompositorLevel10Url;
            var graphicsCompositor = parameters.Package.FindAsset(graphicsCompositorUrl);

            // Something went wrong, create an empty asset
            if (graphicsCompositor == null)
                return base.CreateAssets(parameters);

            // Create derived asset
            return new[] { new AssetItem(GenerateLocation(parameters), graphicsCompositor.CreateDerivedAsset()) };
        }
    }
}
