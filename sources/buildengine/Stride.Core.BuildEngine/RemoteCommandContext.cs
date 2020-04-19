// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization.Assets;
using Stride.Core.Storage;

namespace Stride.Core.BuildEngine
{
    public class RemoteCommandContext : CommandContextBase
    {
        public override Logger Logger { get { return logger; } }

        internal new CommandResultEntry ResultEntry { get { return base.ResultEntry; } }

        private readonly Logger logger;
        private readonly IProcessBuilderRemote processBuilderRemote;

        public RemoteCommandContext(IProcessBuilderRemote processBuilderRemote, Command command, BuilderContext builderContext, Logger logger) : base(command, builderContext)
        {
            this.processBuilderRemote = processBuilderRemote;
            this.logger = logger;
        }

        public override IEnumerable<IDictionary<ObjectUrl, OutputObject>> GetOutputObjectsGroups()
        {
            yield return processBuilderRemote.GetOutputObjects().ToDictionary(x => x.Key, x => new OutputObject(x.Key, x.Value));
        }

        protected override Task<ResultStatus> ScheduleAndExecuteCommandInternal(Command command)
        {
            // Send serialized command
            return processBuilderRemote.SpawnCommand(command);
        }

        protected override ObjectId ComputeInputHash(UrlType type, string filePath)
        {
            return processBuilderRemote.ComputeInputHash(type, filePath);
        }
    }
}
