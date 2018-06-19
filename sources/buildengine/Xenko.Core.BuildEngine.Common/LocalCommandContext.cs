// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Xenko.Core.Storage;
using Xenko.Core.Diagnostics;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Core.BuildEngine
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
