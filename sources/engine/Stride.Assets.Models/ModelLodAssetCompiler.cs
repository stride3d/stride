// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Assets.Materials;
using Stride.Graphics;
using Stride.Core.BuildEngine;
using System.Threading.Tasks;
using Stride.Core.Serialization;
using Stride.Rendering;

namespace Stride.Assets.Models
{
    [AssetCompiler(typeof(ModelLodAsset), typeof(AssetCompilationContext))]
    public class ModelLodAssetCompiler : AssetCompilerBase
    {
        public override IEnumerable<ObjectUrl> GetInputFiles(AssetItem assetItem)
        {
            var modelAsset = (ModelLodAsset)assetItem.Asset;
            var assetDirectory = assetItem.FullPath.GetParent();
            var assetSource = UPath.Combine(assetDirectory, modelAsset.Source);
            yield return new ObjectUrl(UrlType.File, assetSource);
        }

        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (ModelLodAsset)assetItem.Asset;
            var assetSource = GetAbsolutePath(assetItem, asset.Source);
            var extension = assetSource.GetFileExtension();
            var buildStep = new AssetBuildStep(assetItem);

            var gameSettingsAsset = context.GetGameSettingsAsset();
            var renderingSettings = gameSettingsAsset.GetOrCreate<RenderingSettings>();
            var allow32BitIndex = renderingSettings.DefaultGraphicsProfile >= GraphicsProfile.Level_9_2;
            var maxInputSlots = renderingSettings.DefaultGraphicsProfile >= GraphicsProfile.Level_10_1 ? 32 : 16;
            var allowUnsignedBlendIndices = context.GetGraphicsPlatform(assetItem.Package) != GraphicsPlatform.OpenGLES;

            var importModelCommand = ImportModelCommand.Create(extension);
            if (importModelCommand == null)
            {
                result.Error($"No importer found for model extension '{extension}. The lod model '{assetSource}' can't be imported.");
                return;
            }

            importModelCommand.InputFilesGetter = () => GetInputFiles(assetItem);

            // Find skeleton asset, if any
            AssetItem skeleton = null;
            if (asset.Skeleton != null)
                skeleton = assetItem.Package.FindAssetFromProxyObject(asset.Skeleton);

            importModelCommand.SourcePath = assetSource;
            importModelCommand.Location = targetUrlInStorage;
            importModelCommand.Mode = ImportModelCommand.ExportMode.ModelLod;
            importModelCommand.ScaleImport = asset.ScaleImport;
            importModelCommand.PivotPosition = asset.PivotPosition;
            importModelCommand.SourcePath = assetSource;
            importModelCommand.Location = targetUrlInStorage;
            importModelCommand.Allow32BitIndex = allow32BitIndex;
            importModelCommand.MaxInputSlots = maxInputSlots;
            importModelCommand.AllowUnsignedBlendIndices = allowUnsignedBlendIndices;
            importModelCommand.MergeMeshes = asset.MergeMeshes;
            importModelCommand.DeduplicateMaterials = asset.DeduplicateMaterials;
            importModelCommand.ModelModifiers = new List<IModelModifier>();
            importModelCommand.LodQuality = asset.Quality;
            importModelCommand.LodLevel = asset.Level;
            importModelCommand.Package = assetItem.Package;
            importModelCommand.Materials = asset.Materials;

            if (skeleton != null)
            {
                importModelCommand.SkeletonUrl = skeleton.Location;
                importModelCommand.ScaleImport = ((SkeletonAsset)skeleton.Asset).ScaleImport;
                importModelCommand.PivotPosition = ((SkeletonAsset)skeleton.Asset).PivotPosition;
            }

            buildStep.Add(importModelCommand);
            result.BuildSteps = buildStep;
        }

    
    }
}
