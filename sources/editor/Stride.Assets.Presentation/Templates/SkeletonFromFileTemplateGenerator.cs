// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;
using Stride.Core;
using Stride.Core.IO;
using Stride.Assets.Models;

namespace Stride.Assets.Presentation.Templates
{
    public class SkeletonFromFileTemplateGenerator : AssetFromFileTemplateGenerator
    {
        public new static readonly SkeletonFromFileTemplateGenerator Default = new SkeletonFromFileTemplateGenerator();

        public static Guid Id = new Guid("7DA3597C-7AC4-450E-B60B-21DDB6296208");

        public override bool IsSupportingTemplate(TemplateDescription templateDescription)
        {
            return templateDescription.Id == Id;
        }

        protected override IEnumerable<AssetItem> CreateAssets(AssetTemplateGeneratorParameters parameters)
        {
            var files = parameters.Tags.Get(SourceFilesPathKey);
            if (files == null)
                return base.CreateAssets(parameters);

            var importParameters = new AssetImporterParameters { Logger = parameters.Logger };
            importParameters.SelectedOutputTypes.Add(typeof(SkeletonAsset), true);

            var importedAssets = new List<AssetItem>();

            foreach (var file in files)
            {
                // TODO: should we allow to select the importer?
                var importer = AssetRegistry.FindImporterForFile(file).OfType<ModelAssetImporter>().FirstOrDefault();
                if (importer == null)
                {
                    parameters.Logger.Warning($"No importer found for file \"{file}\"");
                    continue;
                }

                var assets = importer.Import(file, importParameters).Select(x => new AssetItem(UPath.Combine(parameters.TargetLocation, x.Location), x.Asset)).ToList();
                // Create unique names amongst the list of assets
                importedAssets.AddRange(MakeUniqueNames(assets));
            }

            return importedAssets;
        }
    }
}
