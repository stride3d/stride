// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using System;
using System.Collections.Generic;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Assets.Materials;
using Stride.Assets.Models;
using Stride.Editor.Preview;

namespace Stride.Assets.Editor.Preview
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
