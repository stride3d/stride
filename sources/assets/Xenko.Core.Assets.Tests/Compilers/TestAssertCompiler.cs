using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xenko.Core.Assets.Compiler;
using Xenko.Core.BuildEngine;

namespace Xenko.Core.Assets.Tests.Compilers
{
    public class TestAssertCompiler<T> : TestCompilerBase where T : Asset
    {
        private class AssertCommand : AssetCommand<T>
        {
            private readonly AssetItem assetItem;
            private readonly Action<string, T, IAssetFinder> assertFunc;

            public AssertCommand(AssetItem assetItem, T parameters, IAssetFinder assetFinder, Action<string, T, IAssetFinder> assertFunc)
                : base(assetItem.Location, parameters, assetFinder)
            {
                this.assetItem = assetItem;
                this.assertFunc = assertFunc;
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                assertFunc?.Invoke(Url, Parameters, AssetFinder);
                CompiledAssets.Add(assetItem);
                return Task.FromResult(ResultStatus.Successful);
            }
        }

        public override AssetCompilerResult Prepare(AssetCompilerContext context, AssetItem assetItem)
        {
            var result = new AssetCompilerResult(GetType().Name)
            {
                BuildSteps = new AssetBuildStep(assetItem)
            };
            result.BuildSteps.Add(new AssertCommand(assetItem, (T)assetItem.Asset, assetItem.Package, DoCommandAssert));
            return result;
        }

        protected virtual void DoCommandAssert(string url, T parameters, IAssetFinder package)
        {

        }
    }
}