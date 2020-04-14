// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Animations;
using Stride.Assets.Entities;
using Stride.Assets.Materials;
using Stride.Assets.Navigation;
using Stride.Assets.Sprite;
using Stride.Assets.Textures;
using Stride.Rendering;

namespace Stride.Editor.Preview
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
