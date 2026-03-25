// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets.Editor.ViewModel
{
    [AssetDescription(FileExtension, AllowArchetype = false)]
    [AssetContentType(typeof(ColorPalette))]
    [AssetFormatVersion("Stride", CurrentVersion, "1.0.0.0")]
    public sealed class ColorPaletteAsset : Asset
    {
        private const string CurrentVersion = "1.0.0.0";
        public const string FileExtension = ".sdpalette";
        public string SourceFile { get; set; } = string.Empty;
        public Dictionary<string, Color3> Colors { get; set; } = new Dictionary<string, Color3>();
    }

    [AssetCompiler(typeof(ColorPaletteAsset), typeof(AssetCompilationContext))]
    public sealed class ColorPaletteCompiler : AssetCompilerBase
    {
        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem,
            string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (ColorPaletteAsset)assetItem.Asset;
            result.BuildSteps = new AssetBuildStep(assetItem);
            result.BuildSteps.Add(new ColorPaletteCommand(targetUrlInStorage, asset, assetItem.Package));
        }

        private class ColorPaletteCommand : AssetCommand<ColorPaletteAsset>
        {
            public ColorPaletteCommand(string url, ColorPaletteAsset parameters, IAssetFinder assetFinder)
                : base(url, parameters, assetFinder) { }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);

                Dictionary<string, Color3> colors;

                if (!string.IsNullOrWhiteSpace(Parameters.SourceFile))
                {
                    colors = GplPaletteParser.TryParse(Parameters.SourceFile)
                             ?? Parameters.Colors;
                }
                else
                {
                    colors = Parameters.Colors;
                }

                var runtime = new ColorPalette { Colors = colors };
                assetManager.Save(Url, runtime);

                return Task.FromResult(ResultStatus.Successful);
            }
        }
    }
}