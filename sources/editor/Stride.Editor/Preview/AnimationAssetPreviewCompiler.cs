// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Assets.Materials;
using Stride.Assets.Models;

namespace Stride.Editor.Preview
{
    [AssetCompiler(typeof(AnimationAsset), typeof(PreviewCompilationContext))]
    public class AnimationAssetPreviewCompiler : AnimationAssetCompiler
    {
        public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
        {
            foreach (var inputFile in base.GetInputTypes(assetItem))
            {
                yield return inputFile;
            }

            yield return new BuildDependencyInfo(typeof(ModelAsset), typeof(AssetCompilationContext), BuildDependencyType.Runtime | BuildDependencyType.CompileContent);
            yield return new BuildDependencyInfo(typeof(MaterialAsset), typeof(AssetCompilationContext), BuildDependencyType.Runtime | BuildDependencyType.CompileContent);
        }
    }
}
