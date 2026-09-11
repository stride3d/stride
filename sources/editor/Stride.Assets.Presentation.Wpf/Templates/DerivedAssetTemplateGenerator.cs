// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;

namespace Stride.Assets.Presentation.Templates
{
    /// <summary>
    /// Creates an asset derived from the one a template names in <see cref="TemplateAssetFactoryDescription.DerivedFrom"/>
    /// (a package's default graphics compositor, for instance); falls back to a fresh asset when that one is not in the
    /// session.
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
            if (archetype == null)
                return base.CreateAssets(parameters);

            return new[] { new AssetItem(GenerateLocation(parameters), archetype.CreateDerivedAsset()) };
        }
    }
}
