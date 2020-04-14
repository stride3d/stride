// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Serialization.Contents;
using Stride.Engine;

namespace Stride.Assets.Entities
{
    [AssetCompiler(typeof(PrefabAsset), typeof(AssetCompilationContext))]
    public class PrefabAssetCompiler : EntityHierarchyCompilerBase<PrefabAsset>
    {
        protected override AssetCommand<PrefabAsset> Create(string url, PrefabAsset assetParameters, Package package)
        {
            return new PrefabCommand(url, assetParameters, package);
        }

        private class PrefabCommand : AssetCommand<PrefabAsset>
        {
            public PrefabCommand(string url, PrefabAsset parameters, IAssetFinder assetFinder) : base(url, parameters, assetFinder)
            {
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);

                var prefab = new Prefab();
                foreach (var rootEntity in Parameters.Hierarchy.RootParts)
                {
                    prefab.Entities.Add(rootEntity);
                }
                assetManager.Save(Url, prefab);

                return Task.FromResult(ResultStatus.Successful);
            }

            public override string ToString()
            {
                return $"Prefab command for asset '{Url}'.";
            }
        }
    }
}
