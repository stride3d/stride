// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Assets.Analysis;
using Stride.Core.BuildEngine;
using Stride.Core.Annotations;
using System.Threading.Tasks;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using System.Linq;

namespace Stride.Core.Assets.Compiler
{
    /// <summary>
    /// An asset compiler that will compile an asset with all its dependencies.
    /// </summary>
    public class AssetDependenciesCompiler
    {
        public readonly BuildDependencyManager BuildDependencyManager;

        /// <summary>
        /// Raised when a single asset has been compiled.
        /// </summary>
        public EventHandler<AssetCompiledArgs> AssetCompiled;

        public AssetDependenciesCompiler(Type compilationContext)
        {
            if (!typeof(ICompilationContext).IsAssignableFrom(compilationContext))
                throw new InvalidOperationException($"{nameof(compilationContext)} should inherit from ICompilationContext");

            BuildDependencyManager = new BuildDependencyManager();
        }

        /// <summary>
        /// Prepare the list of assets to be built, building all the steps and linking them properly
        /// </summary>
        /// <param name="context">The AssetCompilerContext</param>
        /// <param name="assetItems">The assets to prepare for build</param>
        /// <returns></returns>
        public AssetCompilerResult PrepareMany(AssetCompilerContext context, List<AssetItem> assetItems)
        {
            var finalResult = new AssetCompilerResult();
            var compiledItems = new Dictionary<AssetId, BuildStep>();
            foreach (var assetItem in assetItems)
            {
                var visitedItems = new HashSet<BuildAssetNode>();
                Prepare(finalResult, context, assetItem, context.CompilationContext, visitedItems, compiledItems);
            }
            return finalResult;
        }

        /// <summary>
        /// Prepare a single asset to be built
        /// </summary>
        /// <param name="context">The AssetCompilerContext</param>
        /// <param name="assetItem">The asset to build</param>
        /// <returns></returns>
        public AssetCompilerResult Prepare(AssetCompilerContext context, AssetItem assetItem)
        {
            var finalResult = new AssetCompilerResult();
            var visitedItems = new HashSet<BuildAssetNode>();
            var compiledItems = new Dictionary<AssetId, BuildStep>();
            Prepare(finalResult, context, assetItem, context.CompilationContext, visitedItems, compiledItems);
            return finalResult;
        }

        private void Prepare(AssetCompilerResult finalResult, AssetCompilerContext context, AssetItem assetItem, [NotNull] Type compilationContext, HashSet<BuildAssetNode> visitedItems, Dictionary<AssetId, BuildStep> compiledItems, BuildStep parentBuildStep = null, 
            BuildDependencyType dependencyType = BuildDependencyType.Runtime)
        {
            if (compilationContext == null) throw new ArgumentNullException(nameof(compilationContext));
            var assetNode = BuildDependencyManager.FindOrCreateNode(assetItem, compilationContext);
            compiledItems.TryGetValue(assetNode.AssetItem.Id, out var assetBuildSteps);

            // Prevent re-entrancy in the same node
            if (visitedItems.Add(assetNode))
            {
                assetNode.Analyze(context);

                // Invoke the compiler to prepare the build step for this asset if the dependency needs to compile it (Runtime or CompileContent)
                if ((dependencyType & ~BuildDependencyType.CompileAsset) != 0 && assetBuildSteps == null)
                {
                    var mainCompiler = BuildDependencyManager.AssetCompilerRegistry.GetCompiler(assetItem.Asset.GetType(), assetNode.CompilationContext);
                    if (mainCompiler == null)
                        return;

                    var compilerResult = mainCompiler.Prepare(context, assetItem);

                    if ((dependencyType & BuildDependencyType.Runtime) == BuildDependencyType.Runtime && compilerResult.HasErrors) //allow Runtime dependencies to fail
                    {
                        assetBuildSteps = new ErrorBuildStep(assetItem, compilerResult.Messages);
                    }
                    else
                    {

                        assetBuildSteps = compilerResult.BuildSteps;
                        compiledItems.Add(assetNode.AssetItem.Id, assetBuildSteps);

                        // Copy the log to the final result (note: this does not copy or forward the build steps)
                        compilerResult.CopyTo(finalResult);
                        if (compilerResult.HasErrors)
                        {
                            finalResult.Error($"Failed to prepare asset {assetItem.Location}");
                            return;
                        }
                    }

                    // Add the resulting build steps to the final
                    finalResult.BuildSteps.Add(assetBuildSteps);

                    AssetCompiled?.Invoke(this, new AssetCompiledArgs(assetItem, compilerResult));
                }

                // Go through the dependencies of the node and prepare them as well
                foreach (var reference in assetNode.References)
                {
                    var target = reference.Target;
                    Prepare(finalResult, context, target.AssetItem, target.CompilationContext, visitedItems, compiledItems, assetBuildSteps, reference.DependencyType);
                    if (finalResult.HasErrors)
                    {
                        return;
                    }
                }

                // If we didn't prepare any build step for this asset let's exit here.
                if (assetBuildSteps == null)
                    return;
            }

            // Link the created build steps to their parent step.
            if (parentBuildStep != null && assetBuildSteps != null && (dependencyType & BuildDependencyType.CompileContent) == BuildDependencyType.CompileContent) //only if content is required Content.Load
                BuildStep.LinkBuildSteps(assetBuildSteps, parentBuildStep);
        }

        private class ErrorBuildStep : AssetBuildStep
        {
            private readonly List<ILogMessage> messages;

            public ErrorBuildStep(AssetItem assetItem, IEnumerable<ILogMessage> messages)
                : base(assetItem)
            {
                this.messages = messages.ToList();
            }

            public override Task<ResultStatus> Execute(IExecuteContext executeContext, BuilderContext builderContext)
            {
                foreach (var message in messages)
                    executeContext.Logger.Log(message);
                return Task.FromResult(ResultStatus.Failed);
            }
        }
    }
}
