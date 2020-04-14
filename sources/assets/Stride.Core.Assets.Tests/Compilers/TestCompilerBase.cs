using System;
using System.Collections.Generic;
using Xenko.Core.Assets.Analysis;
using Xenko.Core.Assets.Compiler;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Core.Assets.Tests.Compilers
{
    public abstract class TestCompilerBase : IAssetCompiler
    {
        public static HashSet<AssetItem> CompiledAssets;

        public abstract AssetCompilerResult Prepare(AssetCompilerContext context, AssetItem assetItem);

        public virtual IEnumerable<Type> GetRuntimeTypes(AssetItem assetItem) { yield break; }

        public virtual IEnumerable<ObjectUrl> GetInputFiles(AssetItem assetItem) { yield break; }

        public virtual IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem) { yield break; }

        public virtual IEnumerable<Type> GetInputTypesToExclude(AssetItem assetItem) { yield break; }

        public virtual bool AlwaysCheckRuntimeTypes { get; } = true;
    }
}