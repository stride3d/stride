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
    private readonly IBuildCrashRouting? crashRouting;

    /// <summary>
    /// Initializes a new instance of the <see cref="MSBuildGameProjectBuilder"/> class.
    /// </summary>
    /// <param name="crashRouting">Brings the asset compiler crashes of the builds back to the editor, or <c>null</c> to leave them in
    /// the default crash store.</param>
    public MSBuildGameProjectBuilder(IBuildCrashRouting? crashRouting = null)
    {
        this.crashRouting = crashRouting;
    }

    /// <inheritdoc/>
    public IGameProjectBuild? StartBuild(GameProjectBuildRequest request, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(logger);

        var properties = new Dictionary<string, string>(request.Properties);
        // An asset compiler crash of this build comes back to this editor, which hands it to the crash reporter once the build ends
        if (crashRouting?.CrashDirectory is { } crashDirectory)
            properties["StrideCrashDir"] = crashDirectory;
        var build = VSProjectHelper.CompileProjectAssemblyAsync(request.ProjectPath, logger, request.Target, request.Configuration, request.Platform, properties);
        return build is null ? null : new MSBuildGameProjectBuild(build, crashRouting, logger);
    }

    private sealed class MSBuildGameProjectBuild : IGameProjectBuild
    {
        private readonly ICancellableAsyncBuild build;

        public MSBuildGameProjectBuild(ICancellableAsyncBuild build, IBuildCrashRouting? crashRouting, ILogger logger)
        {
            this.build = build;
            Completion = WaitAsync(build, crashRouting, logger);
        }

        public string AssemblyPath => build.AssemblyPath;

        public Task<bool> Completion { get; }

        public bool IsCanceled => build.IsCanceled;

        public void Cancel() => build.Cancel();

        private static async Task<bool> WaitAsync(ICancellableAsyncBuild build, IBuildCrashRouting? crashRouting, ILogger logger)
        {
            var result = await build.BuildTask;
            // The asset compiler ran, and wrote any crash, by the time the build ends
            crashRouting?.SurfacePendingCrashes(logger);
            // MSBuild logs the errors of the projects but not an exception of its own: log it, unless it only says that the build
            // was canceled, as the user asked
            if (result.Exception is { } exception && !build.IsCanceled)
                logger.Error("MSBuild failed to build the project.", exception);
            return result.OverallResult == BuildResultCode.Success;
        }
    }
}
