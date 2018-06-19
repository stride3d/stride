// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Compiler;
using Xenko.Core.BuildEngine;
using Xenko.Core;
using Xenko.Core.IO;
using Xenko.Rendering;
using Xenko.Shaders.Compiler;

namespace Xenko.Assets.Effect
{
    /// <summary>
    /// Entry point to compile an <see cref="EffectShaderAsset"/>
    /// </summary>
    [AssetCompiler(typeof(EffectShaderAsset), typeof(AssetCompilationContext))]
    public class EffectShaderAssetCompiler : AssetCompilerBase
    {
        public static readonly PropertyKey<ConcurrentDictionary<string, string>> ShaderLocationsKey = new PropertyKey<ConcurrentDictionary<string, string>>("ShaderPathsKey", typeof(EffectShaderAssetCompiler));

        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var url = EffectCompilerBase.DefaultSourceShaderFolder + "/" + Path.GetFileName(assetItem.FullPath);

            var originalSourcePath = assetItem.FullPath;
            result.BuildSteps = new AssetBuildStep(assetItem);
            result.BuildSteps.Add(new ImportStreamCommand { SourcePath = originalSourcePath, Location = url, SaveSourcePath = true });
            var shaderLocations = (ConcurrentDictionary<string, string>)context.Properties.GetOrAdd(ShaderLocationsKey, key => new ConcurrentDictionary<string, string>());

            // Store directly this into the context TODO this this temporary
            shaderLocations[url] = originalSourcePath;
        }
    }
}
