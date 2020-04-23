// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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

namespace Stride.Assets.Models
{
    [AssetCompiler(typeof(ModelAsset), typeof(AssetCompilationContext))]
    public class ModelAssetCompiler : AssetCompilerBase
    {
        public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
        {
            yield return new BuildDependencyInfo(typeof(SkeletonAsset), typeof(AssetCompilationContext), BuildDependencyType.Runtime | BuildDependencyType.CompileContent);
            yield return new BuildDependencyInfo(typeof(MaterialAsset), typeof(AssetCompilationContext), BuildDependencyType.Runtime);
        }

        public override IEnumerable<ObjectUrl> GetInputFiles(AssetItem assetItem)
        {
            var modelAsset = (ModelAsset)assetItem.Asset;
            var assetDirectory = assetItem.FullPath.GetParent();
            var assetSource = UPath.Combine(assetDirectory, modelAsset.Source);
            yield return new ObjectUrl(UrlType.File, assetSource);
        }

        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (ModelAsset)assetItem.Asset;
            // Get absolute path of asset source on disk
            var assetDirectory = assetItem.FullPath.GetParent();
            var assetSource = UPath.Combine(assetDirectory, asset.Source);

            var gameSettingsAsset = context.GetGameSettingsAsset();
            var renderingSettings = gameSettingsAsset.GetOrCreate<RenderingSettings>();
            var allow32BitIndex = renderingSettings.DefaultGraphicsProfile >= GraphicsProfile.Level_9_2;
            var maxInputSlots = renderingSettings.DefaultGraphicsProfile >= GraphicsProfile.Level_10_1 ? 32 : 16;
            var allowUnsignedBlendIndices = context.GetGraphicsPlatform(assetItem.Package) != GraphicsPlatform.OpenGLES;
            var extension = asset.Source.GetFileExtension();

            // Find skeleton asset, if any
            AssetItem skeleton = null;
            if (asset.Skeleton != null)
                skeleton = assetItem.Package.FindAssetFromProxyObject(asset.Skeleton);

            var importModelCommand = ImportModelCommand.Create(extension);
            if (importModelCommand == null)
            {
                result.Error($"No importer found for model extension '{extension}. The model '{assetSource}' can't be imported.");
                return;
            }

            importModelCommand.InputFilesGetter = () => GetInputFiles(assetItem);
            importModelCommand.Mode = ImportModelCommand.ExportMode.Model;
            importModelCommand.SourcePath = assetSource;
            importModelCommand.Location = targetUrlInStorage;
            importModelCommand.Allow32BitIndex = allow32BitIndex;
            importModelCommand.MaxInputSlots = maxInputSlots;
            importModelCommand.AllowUnsignedBlendIndices = allowUnsignedBlendIndices;
            importModelCommand.Materials = asset.Materials;
            importModelCommand.ScaleImport = asset.ScaleImport;
            importModelCommand.PivotPosition = asset.PivotPosition;
            importModelCommand.MergeMeshes = asset.MergeMeshes;
            importModelCommand.DeduplicateMaterials = asset.DeduplicateMaterials;
            importModelCommand.ModelModifiers = asset.Modifiers;

            if (skeleton != null)
            {
                importModelCommand.SkeletonUrl = skeleton.Location;
                // Note: skeleton override values
                importModelCommand.ScaleImport = ((SkeletonAsset)skeleton.Asset).ScaleImport;
                importModelCommand.PivotPosition = ((SkeletonAsset)skeleton.Asset).PivotPosition;
            }

            importModelCommand.Package = assetItem.Package;

            result.BuildSteps = new AssetBuildStep(assetItem);
            result.BuildSteps.Add(importModelCommand);
        }
    }
}
