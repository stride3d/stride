// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core;
using Stride.Assets.SpriteFont;

namespace Stride.Assets.UI
{
    [AssetCompiler(typeof(UIPageAsset), typeof(AssetCompilationContext))]
    public sealed class UIPageAssetCompiler : UIAssetCompilerBase<UIPageAsset>
    {
        public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
        {
            yield return new BuildDependencyInfo(typeof(SpriteFontAsset), typeof(AssetCompilationContext), BuildDependencyType.Runtime | BuildDependencyType.CompileContent);
            yield return new BuildDependencyInfo(typeof(PrecompiledSpriteFontAsset), typeof(AssetCompilationContext), BuildDependencyType.Runtime | BuildDependencyType.CompileContent);
        }

        protected override UIConvertCommand Create(string url, UIPageAsset parameters, Package package)
        {
            return new UIPageCommand(url, parameters, package);
        }
        
        private sealed class UIPageCommand : UIConvertCommand
        {
            public UIPageCommand(string url, UIPageAsset parameters, IAssetFinder assetFinder)
                : base(url, parameters, assetFinder)
            {
            }

            protected override ComponentBase Create(ICommandContext commandContext)
            {
                return new Engine.UIPage
                {
                    RootElement = Parameters.Hierarchy.RootParts.Count == 1 ? Parameters.Hierarchy.RootParts[0] : null
                };
            }
        }
    }
}
