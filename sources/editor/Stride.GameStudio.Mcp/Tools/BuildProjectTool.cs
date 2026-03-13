// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class BuildProjectTool
{
    private static Task? _buildWrapperTask;
    private static LoggerResult? _currentBuildLogger;
    private static string? _lastBuildProject;
    private static string? _lastAssemblyPath;
    private static volatile bool _isCanceled;
    private static readonly object _buildLock = new();

    internal static (Task? wrapperTask, LoggerResult? logger, string? project, string? assemblyPath, bool isCanceled) GetBuildState()
    {
        lock (_buildLock)
        {
            return (_buildWrapperTask, _currentBuildLogger, _lastBuildProject, _lastAssemblyPath, _isCanceled);
        }
    }

    [McpServerTool(Name = "build_project"), Description("Triggers a build of the current game project. The build runs asynchronously; use get_build_status to check progress. Only one build can run at a time.")]
    public static async Task<string> BuildProject(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("Build configuration: 'Debug' or 'Release'")] string? configuration = null,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            lock (_buildLock)
            {
                // Check if a build is already in progress
                if (_buildWrapperTask != null && !_buildWrapperTask.IsCompleted)
                {
                    return new { error = "A build is already in progress. Use get_build_status to check or wait for it to complete.", build = (object?)null };
                }

                var currentProject = session.CurrentProject;
                if (currentProject == null)
                {
                    return new { error = "No current project is set in the session.", build = (object?)null };
                }

                var projectPath = currentProject.ProjectPath?.ToOSPath();
                if (string.IsNullOrEmpty(projectPath) || !File.Exists(projectPath))
                {
                    return new { error = $"Project path not found: {projectPath}", build = (object?)null };
                }

                var config = configuration ?? "Debug";
                if (config != "Debug" && config != "Release")
                {
                    return new { error = $"Invalid configuration: '{config}'. Expected 'Debug' or 'Release'.", build = (object?)null };
                }

                var extraProperties = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(session.SolutionPath?.ToOSPath()))
                {
                    var solutionPath = UPath.Combine(Environment.CurrentDirectory, session.SolutionPath);
                    extraProperties["SolutionPath"] = solutionPath.ToOSPath();
                    extraProperties["SolutionDir"] = solutionPath.GetParent().ToOSPath() + Path.DirectorySeparatorChar;
                }

                var logger = new LoggerResult();
                _currentBuildLogger = logger;
                _lastBuildProject = projectPath;
                _lastAssemblyPath = null;
                _isCanceled = false;

                // Wrap the build in a Task.Run so we never expose Task<BuildResult> at the field level.
                // VSProjectHelper.CompileProjectAssemblyAsync returns ICancellableAsyncBuild whose
                // BuildTask property is Task<BuildResult>, requiring Microsoft.Build references.
                // By awaiting it inside Task.Run, we only store a plain Task.
                var localProjectPath = projectPath;
                var localConfig = config;
                var localExtraProperties = extraProperties;
                var localLogger = logger;
                _buildWrapperTask = Task.Run(async () =>
                {
                    var asyncBuild = VSProjectHelper.CompileProjectAssemblyAsync(
                        localProjectPath, localLogger, "Build", localConfig, "AnyCPU", localExtraProperties);

                    if (asyncBuild == null)
                    {
                        localLogger.Error("Failed to start build. The project may not have a valid TargetPath.");
                        return;
                    }

                    lock (_buildLock)
                    {
                        _lastAssemblyPath = asyncBuild.AssemblyPath;
                    }

                    await asyncBuild.BuildTask;

                    if (asyncBuild.IsCanceled)
                    {
                        _isCanceled = true;
                    }
                });

                return new
                {
                    error = (string?)null,
                    build = (object)new
                    {
                        status = "started",
                        project = Path.GetFileName(projectPath),
                        configuration = config,
                    },
                };
            }
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
