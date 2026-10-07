// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Storage;

namespace Stride.Core.BuildEngine;

public class BuilderContext
{
    internal readonly Dictionary<ObjectId, CommandBuildStep> CommandsInProgress = [];

    internal FileVersionTracker InputHashes { get; private set; }

    public CommandBuildStep.TryExecuteRemoteDelegate TryExecuteRemote { get; }

    /// <summary>Optional hook invoked when a command throws an exception that escapes the top-level catch (a bug, not a handled build error).</summary>
    public Action<CommandBuildStep, Exception>? CommandFailed { get; }

    public BuilderContext(FileVersionTracker inputHashes, CommandBuildStep.TryExecuteRemoteDelegate tryExecuteRemote, Action<CommandBuildStep, Exception>? commandFailed = null)
    {
        InputHashes = inputHashes;
        TryExecuteRemote = tryExecuteRemote;
        CommandFailed = commandFailed;
    }
}
