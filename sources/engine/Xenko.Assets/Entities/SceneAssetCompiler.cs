// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Compiler;
using Xenko.Core.BuildEngine;
using Xenko.Core.Serialization.Contents;
using Xenko.Engine;

namespace Xenko.Assets.Entities
{
    [AssetCompiler(typeof(SceneAsset), typeof(AssetCompilationContext))]
    public class SceneAssetCompiler : EntityHierarchyCompilerBase<SceneAsset>
    {
        protected override AssetCommand<SceneAsset> Create(string url, SceneAsset assetParameters, Package package)
        {
            return new SceneCommand(url, assetParameters, package);
        }

        private class SceneCommand : AssetCommand<SceneAsset>
        {
            public SceneCommand(string url, SceneAsset parameters, IAssetFinder assetFinder) : base(url, parameters, assetFinder)
            {
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                var assetManager = new ContentManager();

                var scene = new Scene
                {
                    Parent = Parameters.Parent,
                    Offset = Parameters.Offset
                };

                foreach (var rootEntity in Parameters.Hierarchy.RootParts)
                {
                    scene.Entities.Add(rootEntity);
                }
                assetManager.Save(Url, scene);

                return Task.FromResult(ResultStatus.Successful);
            }

            public override string ToString()
            {
                return $"Scene command for asset '{Url}'.";
            }
        }
    }
}
