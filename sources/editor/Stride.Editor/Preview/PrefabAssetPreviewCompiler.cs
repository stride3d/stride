// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Analysis;
using Xenko.Core.Assets.Compiler;
using Xenko.Animations;
using Xenko.Assets.Entities;
using Xenko.Assets.Materials;
using Xenko.Assets.Navigation;
using Xenko.Assets.Sprite;
using Xenko.Assets.Textures;
using Xenko.Rendering;

namespace Xenko.Editor.Preview
{
    //do not compile sounds in the editor game

    //do not compile animations in the editor game

    [AssetCompiler(typeof(PrefabAsset), typeof(PreviewCompilationContext))]
    public class PrefabAssetPreviewCompiler : PrefabAssetCompiler
    {
        public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
        {
            foreach (var type in AssetRegistry.GetAssetTypes(typeof(Model)))
            {
                yield return new BuildDependencyInfo(type, typeof(AssetCompilationContext), BuildDependencyType.Runtime);
            }
            foreach (var type in AssetRegistry.GetAssetTypes(typeof(AnimationClip)))
            {
                yield return new BuildDependencyInfo(type, typeof(AssetCompilationContext), BuildDependencyType.Runtime);
            }
            yield return new BuildDependencyInfo(typeof(TextureAsset), typeof(AssetCompilationContext), BuildDependencyType.Runtime);
            yield return new BuildDependencyInfo(typeof(PrefabAsset), typeof(AssetCompilationContext), BuildDependencyType.Runtime);
            yield return new BuildDependencyInfo(typeof(MaterialAsset), typeof(AssetCompilationContext), BuildDependencyType.Runtime);
            yield return new BuildDependencyInfo(typeof(SpriteSheetAsset), typeof(AssetCompilationContext), BuildDependencyType.Runtime);
        }

        public override IEnumerable<Type> GetInputTypesToExclude(AssetItem assetItem)
        {
            yield return typeof(SceneAsset);
            yield return typeof(NavigationMeshAsset);
        }
    }
}
