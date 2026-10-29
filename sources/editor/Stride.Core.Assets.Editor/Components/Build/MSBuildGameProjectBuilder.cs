// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Execution;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Components.Build;

/// <summary>
/// Builds a project with MSBuild in the editor's process (<see cref="VSProjectHelper.CompileProjectAssemblyAsync"/>), as the
/// WPF editor does: the build queues behind the other MSBuild builds of the editor, and runs in MSBuild's worker nodes.
/// </summary>
public sealed class MSBuildGameProjectBuilder : IGameProjectBuilder
{
    /// <inheritdoc/>
    public IGameProjectBuild? StartBuild(GameProjectBuildRequest request, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(logger);

        var properties = new Dictionary<string, string>(request.Properties);
        var build = VSProjectHelper.CompileProjectAssemblyAsync(request.ProjectPath, logger, request.Target, request.Configuration, request.Platform, properties);
        return build is null ? null : new MSBuildGameProjectBuild(build, logger);
    }

    private sealed class MSBuildGameProjectBuild : IGameProjectBuild
    {
        private readonly ICancellableAsyncBuild build;

        public MSBuildGameProjectBuild(ICancellableAsyncBuild build, ILogger logger)
        {
            this.build = build;
            Completion = WaitAsync(build, logger);
        }

        public string AssemblyPath => build.AssemblyPath;

        public Task<bool> Completion { get; }

        public bool IsCanceled => build.IsCanceled;

        public void Cancel() => build.Cancel();

        private static async Task<bool> WaitAsync(ICancellableAsyncBuild build, ILogger logger)
        {
            var result = await build.BuildTask;
            // MSBuild logs the errors of the projects but not an exception of its own: log it, unless it only says that the build
            // was canceled, as the user asked
            if (result.Exception is { } exception && !build.IsCanceled)
                logger.Error("MSBuild failed to build the project.", exception);
            return result.OverallResult == BuildResultCode.Success;
        }
    }
}
