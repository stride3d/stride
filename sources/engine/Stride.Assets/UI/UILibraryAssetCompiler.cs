// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core;
using Stride.Assets.Sprite;
using Stride.Assets.SpriteFont;

namespace Stride.Assets.UI
{
    [AssetCompiler(typeof(UILibraryAsset), typeof(AssetCompilationContext))]
    public sealed class UILibraryAssetCompiler : UIAssetCompilerBase<UILibraryAsset>
    {
        public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
        {
            yield return new BuildDependencyInfo(typeof(SpriteFontAsset), typeof(AssetCompilationContext), BuildDependencyType.Runtime | BuildDependencyType.CompileContent);
            yield return new BuildDependencyInfo(typeof(SpriteSheetAsset), typeof(AssetCompilationContext), BuildDependencyType.Runtime | BuildDependencyType.CompileContent);
        }

        protected override UIConvertCommand Create(string url, UILibraryAsset parameters, Package package)
        {
            return new UILibraryCommand(url, parameters, package);
        }

        private sealed class UILibraryCommand : UIConvertCommand
        {
            public UILibraryCommand(string url, UILibraryAsset parameters, IAssetFinder assetFinder)
                : base(url, parameters, assetFinder)
            {
            }

            protected override ComponentBase Create(ICommandContext commandContext)
            {
                var uiLibrary = new Engine.UILibrary();
                foreach (var kv in Parameters.PublicUIElements)
                {
                    if (Parameters.Hierarchy.RootParts.All(x => x.Id != kv.Key))
                    {
                        // We might want to allow that in the future.
                        commandContext.Logger.Warning($"Only root elements can be exposed publicly. Skipping [{kv.Key}].");
                        continue;
                    }

                    // Copy Key/Value pair
                    UIElementDesign element;
                    if (Parameters.Hierarchy.Parts.TryGetValue(kv.Key, out element))
                    {
                        uiLibrary.UIElements.Add(kv.Value, element.UIElement);
                    }
                    else
                    {
                        commandContext.Logger.Error($"Cannot find the element with the id [{kv.Value}] to expose [{kv.Key}].");
                    }
                }
                return uiLibrary;
            }
        }
    }
}
