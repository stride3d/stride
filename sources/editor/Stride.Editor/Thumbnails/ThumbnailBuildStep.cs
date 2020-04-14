// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.BuildEngine;
using Stride.Core.Diagnostics;

namespace Stride.Editor.Thumbnails
{
    /// <summary>
    /// Special <see cref="BuildStep"/> that will forward dependencies to a <see cref="Command"/> implementing <see cref="IThumbnailCommand"/>.
    /// This should be used to properly set <see cref="IThumbnailCommand.DependencyBuildStatus"/>.
    /// </summary>
    public class ThumbnailBuildStep : CommandBuildStep
    {
        //private readonly BuildStep dependencies;

        public ThumbnailBuildStep(Command command)
            : base(command)
        {
            //this.dependencies = dependencies;
        }

        /// <inheritdoc/>
        public override Task<ResultStatus> Execute(IExecuteContext executeContext, BuilderContext builderContext)
        {
//            var thumbnailCompiler = Command as IThumbnailCommand;
//            if (thumbnailCompiler != null)
//            {
//                var highestLogMessageType = LogMessageType.Debug;
//
//                // Check worst type of log message in BuildStep
//                foreach (var message in dependencies.EnumerateRecursively().SelectMany(x => x.Logger.Messages))
//                {
//                    if (highestLogMessageType < message.Type)
//                        highestLogMessageType = message.Type;
//                }
//
//                // Also, if build step failed, mark it as an error
//                if (highestLogMessageType < LogMessageType.Error && dependencies.Failed)
//                {
//                    highestLogMessageType = LogMessageType.Error;
//                }
//
//                // TODO: This is not serializable (OK for now since thumbnails are never built in a separate process); later, a specific class to store results will be needed
//                thumbnailCompiler.DependencyBuildStatus = highestLogMessageType;
//            }

            return base.Execute(executeContext, builderContext);
        }
    }
}
