// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.ViewModel
{
    public class ImportedAssetViewModel<TAsset> : AssetViewModel<TAsset> where TAsset : Asset
    {
        public ImportedAssetViewModel(AssetViewModelConstructionParameters parameters) : base(parameters)
        {
        }

        protected virtual IAssetImporter GetImporter()
        {
            return null;
        }

        protected virtual void PrepareImporterInputParametersForUpdateFromSource(PropertyCollection importerInputParameters, TAsset asset)
        {
            // Do nothing by default
        }

        protected virtual void UpdateAssetFromSource(TAsset assetToMerge)
        {
            // Do nothing by default
        }

        protected internal sealed override async Task UpdateAssetFromSource(Logger logger)
        {
            var importer = GetImporter();
            if (importer != null)
            {
                var importParameters = new AssetImporterParameters { Logger = logger };
                PrepareImporterInputParametersForUpdateFromSource(importParameters.InputParameters, Asset);
                importParameters.SelectedOutputTypes.Add(AssetType, true);
                try
                {
                    var newAsset = await Task.Run(() => importer.Import(Asset.MainSource, importParameters).SingleOrDefault(x => x.Asset is TAsset));
                    if (newAsset != null)
                    {
                        UpdateAssetFromSource((TAsset)newAsset.Asset);
                    }
                }
                catch (Exception e)
                {
                    logger.Error($"An exception occurred while updating asset [{Url}] from its source(s).", e);
                }
            }
        }
    }
}
