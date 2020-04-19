// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Storage;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.BuildEngine
{
    public class LocalCommandContext : CommandContextBase
    {
        private readonly IExecuteContext executeContext;

        private readonly LoggerResult logger;

        public CommandBuildStep Step { get; protected set; }

        public override LoggerResult Logger { get { return logger; } }

        public LocalCommandContext(IExecuteContext executeContext, CommandBuildStep step, BuilderContext builderContext) : base(step.Command, builderContext)
        {
            this.executeContext = executeContext;
            logger = new ForwardingLoggerResult(executeContext.Logger);
            Step = step;
        }

        public override IEnumerable<IReadOnlyDictionary<ObjectUrl, OutputObject>> GetOutputObjectsGroups()
        {
            return Step.GetOutputObjectsGroups();
        }

        public override ObjectId ComputeInputHash(UrlType type, string filePath)
        {
            return executeContext.ComputeInputHash(type, filePath);
        }
    }
}
