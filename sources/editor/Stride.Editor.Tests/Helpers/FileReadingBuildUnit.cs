// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.BuildEngine;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Editor.Tests.Helpers;

/// <summary>
/// A build unit with one command that registers a file as an input, as the commands that compile effects register the shader
/// files they read. Two units of the same file have the same command: the second one takes its result from the cache.
/// </summary>
public sealed class FileReadingBuildUnit : AssetBuildUnit
{
    private static readonly Guid ContextId = Guid.NewGuid();
    private readonly string filePath;
    private CommandBuildStep? commandStep;

    public FileReadingBuildUnit(string filePath)
        : base(new AssetBuildUnitIdentifier(ContextId, AssetId.New()))
    {
        this.filePath = filePath;
    }

    /// <summary>
    /// Gets the status of the command once the unit was built: <see cref="ResultStatus.NotTriggeredWasSuccessful"/> when it came from the cache.
    /// </summary>
    public ResultStatus CommandStatus => commandStep?.Status ?? ResultStatus.NotProcessed;

    protected override ListBuildStep Prepare()
    {
        commandStep = new CommandBuildStep(new ReadFileCommand(filePath));
        var steps = new ListBuildStep();
        steps.Add(commandStep);
        return steps;
    }

    private sealed class ReadFileCommand(string filePath) : Command
    {
        public override string Title => $"Read {filePath}";

        public override string ToString() => Title;

        protected override void ComputeParameterHash(BinarySerializationWriter writer)
        {
            base.ComputeParameterHash(writer);
            writer.Write(filePath);
        }

        protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
        {
            commandContext.RegisterInputDependency(new ObjectUrl(UrlType.File, filePath));
            return Task.FromResult(ResultStatus.Successful);
        }
    }
}
