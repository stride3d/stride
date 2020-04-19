// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Threading;

using Stride.Core.Storage;

namespace Stride.Core.BuildEngine
{
    public class BuilderContext
    {
        internal readonly Dictionary<ObjectId, CommandBuildStep> CommandsInProgress = new Dictionary<ObjectId, CommandBuildStep>();

        internal FileVersionTracker InputHashes { get; private set; }

        public CommandBuildStep.TryExecuteRemoteDelegate TryExecuteRemote { get; }

        public BuilderContext(FileVersionTracker inputHashes, CommandBuildStep.TryExecuteRemoteDelegate tryExecuteRemote)
        {
            InputHashes = inputHashes;
            TryExecuteRemote = tryExecuteRemote;
        }
    }
}
