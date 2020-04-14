// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.Assets.Diagnostics;
using Stride.Core.BuildEngine;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Assets.Effect;
using Stride.Editor.Resources;

namespace Stride.Editor.Thumbnails
{
    /// <summary>
    /// Base implementation for <see cref="IAssetCompiler"/> suitable to build a thumbnail of a single type of <see cref="Asset"/>.
    /// </summary>
    /// <typeparam name="T">Type of the asset</typeparam>
    public abstract class ThumbnailCompilerBase<T> : IThumbnailCompiler where T : Asset
    {
        protected const string ThumbnailStorageNamePrefix = "__THUMBNAIL__";

        /// <summary>
        /// The typed asset associated to <see cref="AssetItem"/>
        /// </summary>
        protected T Asset;

        private class ThumbnailFailureBuildStep : BuildStep
        {
            public ThumbnailFailureBuildStep(IEnumerable<ILogMessage> messages)
            {
                messages.ForEach(x => Logger.Log(x));
            }

            public override string Title { get { return "FailureThumbnail"; } }

            public override Task<ResultStatus> Execute(IExecuteContext executeContext, BuilderContext builderContext)
            {
                return Task.FromResult(ResultStatus.Failed);
            }

            public override string ToString()
            {
                return Title;
            }
        }

        /// <inheritdoc/>
        public int Priority { get; set; }

        /// <inheritdoc/>
        public bool IsStatic { get; set; }

        /// <summary>
        /// Compiles the asset from the specified package.
        /// </summary>
        /// <param name="context">The thumbnail compile context</param>
        /// <param name="thumbnailStorageUrl">The absolute URL to the asset's thumbnail, relative to the storage.</param>
        /// <param name="assetItem">The asset to compile</param>
        /// <param name="originalPackage"></param>
        /// <param name="result">The result where the commands and logs should be output.</param>
        protected abstract void CompileThumbnail(ThumbnailCompilerContext context, string thumbnailStorageUrl, AssetItem assetItem, Package originalPackage, AssetCompilerResult result);
        
        protected virtual string BuildThumbnailStoreName(UFile assetUrl)
        {
            return assetUrl.GetDirectoryAndFileNameWithoutExtension().Insert(0, ThumbnailStorageNamePrefix);
        }

        private static void OnThumbnailStepProcessed(ThumbnailCompilerContext context, AssetItem assetItem, string thumbnailStorageUrl, BuildStepEventArgs buildStepEventArgs)
        {
            // returns immediately if the user has not subscribe to the event
            if (!context.ShouldNotifyThumbnailBuilt)
                return;

            // TODO: the way to get last build step (which should be thumbnail, not its dependencies) should be done differently, at the compiler level
            // (we need to generate two build step that can be accessed directly, one for dependency and one for thumbnail)
            var lastBuildStep = buildStepEventArgs.Step is ListBuildStep ? ((ListBuildStep)buildStepEventArgs.Step).Steps.LastOrDefault() ?? buildStepEventArgs.Step : buildStepEventArgs.Step;

            // Retrieving build result
            var result = ThumbnailBuildResult.Failed;
            if (lastBuildStep.Succeeded)
                result = ThumbnailBuildResult.Succeeded;
            else if (lastBuildStep.Status == ResultStatus.Cancelled)
                result = ThumbnailBuildResult.Cancelled;

            // TODO: Display error logo if anything else went wrong?

            var changed = lastBuildStep.Status != ResultStatus.NotTriggeredWasSuccessful;

            // Open the image data stream if the build succeeded
            Stream thumbnailStream = null;
            ObjectId thumbnailHash = ObjectId.Empty;

            if (lastBuildStep.Succeeded)
            {
                thumbnailStream = MicrothreadLocalDatabases.DatabaseFileProvider.OpenStream(thumbnailStorageUrl, VirtualFileMode.Open, VirtualFileAccess.Read);
                thumbnailHash = MicrothreadLocalDatabases.DatabaseFileProvider.ContentIndexMap[thumbnailStorageUrl];
            }

            try
            {
                context.NotifyThumbnailBuilt(assetItem, result, changed, thumbnailStream, thumbnailHash);
            }
            finally
            {
                // Close the image data stream if opened
                if (thumbnailStream != null)
                {
                    thumbnailStream.Dispose();
                }
            }
        }

        public AssetCompilerResult Prepare(AssetCompilerContext context, AssetItem assetItem)
        {
            var compilerResult = new AssetCompilerResult();

            Asset = (T)assetItem.Asset;
            var thumbnailCompilerContext = (ThumbnailCompilerContext)context;
            
            // Build the path of the thumbnail in the storage
            var thumbnailStorageUrl = BuildThumbnailStoreName(assetItem.Location);
            
            // Check if this asset produced any error
            // (dependent assets errors are generally ignored as long as thumbnail could be generated,
            // but we will add a thumbnail overlay to indicate the state is not good)
            var currentAssetHasErrors = false;

            try
            {
                CompileThumbnail(thumbnailCompilerContext, thumbnailStorageUrl, assetItem, assetItem.Package, compilerResult);
            }
            catch (Exception)
            {
                // If an exception occurs, ensure that the build of thumbnail will fail.
                compilerResult.Error($"An exception occurred while compiling the asset [{assetItem.Location}]");
            }

            foreach (var logMessage in compilerResult.Messages)
            {
                // Ignore anything less than error
                if (!logMessage.IsAtLeast(LogMessageType.Error))
                    continue;
            
                // Check if there is any non-asset log message
                // (they are probably just emitted by current compiler, so they concern current asset)
                // TODO: Maybe we should wrap every message in AssetLogMessage before copying them in compilerResult?
                var assetLogMessage = logMessage as AssetLogMessage;
                if (assetLogMessage == null)
                {
                    currentAssetHasErrors = true;
                    break;
                }
            
                // If it was an asset log message, check it concerns current asset
                if (assetLogMessage.AssetReference != null && assetLogMessage.AssetReference.Location == assetItem.Location)
                {
                    currentAssetHasErrors = true;
                    break;
                }
            }
            if (currentAssetHasErrors)
            {
                // if a problem occurs while compiling, we add a special build step that will always fail.
                compilerResult.BuildSteps.Add(new ThumbnailFailureBuildStep(compilerResult.Messages));
            }
            
            var currentAsset = assetItem; // copy the current asset item and embrace it in the callback
            compilerResult.BuildSteps.StepProcessed += (_, buildStepArgs) => OnThumbnailStepProcessed(thumbnailCompilerContext, currentAsset, thumbnailStorageUrl, buildStepArgs);

            return compilerResult;
        }

        public virtual IEnumerable<ObjectUrl> GetInputFiles(AssetItem assetItem)
        {
            yield break;
        }

        public virtual IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
        {
            yield break;
        }

        public virtual IEnumerable<Type> GetInputTypesToExclude(AssetItem assetItem)
        {
            yield break;
        }

        public virtual bool AlwaysCheckRuntimeTypes { get; } = true;

        public IEnumerable<Type> GetRuntimeTypes(AssetItem assetItem)
        {
            yield break;
        }
    }
}
