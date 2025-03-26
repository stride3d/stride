// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.RegularExpressions;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using NuGet.ProjectModel;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using ILogger = Stride.Core.Diagnostics.ILogger;
using MicrosoftProject = Microsoft.Build.Evaluation.Project;

namespace Stride.Core.Assets;

public interface ICancellableAsyncBuild
{
    string AssemblyPath { get; }

    Task<BuildResult> BuildTask { get; }

    bool IsCanceled { get; }

    void Cancel();
}

public static class VSProjectHelper
{
    private const string StrideProjectType = "StrideProjectType";
    private const string StridePlatform = "StridePlatform";

    private static readonly BuildManager mainBuildManager = new();

    public static Guid GetProjectGuid(MicrosoftProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return Guid.Parse(project.GetPropertyValue("ProjectGuid"));
    }

    public static PlatformType? GetPlatformTypeFromProject(MicrosoftProject project)
    {
        return GetEnumFromProperty<PlatformType>(project, StridePlatform);
    }

    public static ProjectType? GetProjectTypeFromProject(MicrosoftProject project)
    {
        return GetEnumFromProperty<ProjectType>(project, StrideProjectType);
    }

    private static T? GetEnumFromProperty<T>(MicrosoftProject project, string propertyName) where T : struct
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(propertyName);
        var value = project.GetPropertyValue(propertyName);
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }
        return (T)Enum.Parse(typeof(T), value);
    }

    public static string GetOrCompileProjectAssembly(string fullProjectLocation, ILogger logger, string targets, bool autoCompileProject, string configuration, string platform = "AnyCPU", Dictionary<string, string>? extraProperties = null, bool onlyErrors = false, BuildRequestDataFlags flags = BuildRequestDataFlags.None)
    {
        ArgumentNullException.ThrowIfNull(fullProjectLocation);
        ArgumentNullException.ThrowIfNull(logger);

        var project = LoadProject(fullProjectLocation, configuration, platform, extraProperties);
        var assemblyPath = project.GetPropertyValue("TargetPath");
        try
        {
            if (!string.IsNullOrWhiteSpace(assemblyPath))
            {
                if (autoCompileProject)
                {
                    var asyncBuild = new CancellableAsyncBuild(project, assemblyPath);
                    asyncBuild.Build(project, targets, flags, new LoggerRedirect(logger, onlyErrors));
                    var buildResult = asyncBuild.BuildTask.Result;
                }
            }
        }
        finally
        {
            project.ProjectCollection.UnloadAllProjects();
            project.ProjectCollection.Dispose();
        }

        return assemblyPath;
    }

    public static ICancellableAsyncBuild? CompileProjectAssemblyAsync(string fullProjectLocation, ILogger logger, string targets = "Build", string configuration = "Debug", string platform = "AnyCPU", Dictionary<string, string>? extraProperties = null, BuildRequestDataFlags flags = BuildRequestDataFlags.None)
    {
        ArgumentNullException.ThrowIfNull(fullProjectLocation);
        ArgumentNullException.ThrowIfNull(logger);

        var project = LoadProject(fullProjectLocation, configuration, platform, extraProperties);
        var assemblyPath = project.GetPropertyValue("TargetPath");
        try
        {
            if (!string.IsNullOrWhiteSpace(assemblyPath))
            {
                var asyncBuild = new CancellableAsyncBuild(project, assemblyPath);
                asyncBuild.Build(project, targets, flags, new LoggerRedirect(logger));
                return asyncBuild;
            }
        }
        finally
        {
            project.ProjectCollection.UnloadAllProjects();
            project.ProjectCollection.Dispose();
        }

        return null;
    }

    public static async Task<DependencyGraphSpec> GenerateRestoreGraphFile(ILogger logger, string projectPath)
    {
        DependencyGraphSpec? spec = null;
        using (var restoreGraphResult = new TemporaryFile())
        {
            await Task.Run(() =>
            {
                var pc = new Microsoft.Build.Evaluation.ProjectCollection();

                try
                {
                    var parameters = new BuildParameters(pc)
                    {
                        Loggers = [new LoggerRedirect(logger, true)], //Instance of ILogger instantiated earlier
                        DisableInProcNode = true,
                    };

                    // Run a MSBuild /t:Restore <projectfile>
                    var request = new BuildRequestData(projectPath, new Dictionary<string, string> { { "RestoreGraphOutputPath", restoreGraphResult.Path }, { "RestoreRecursive", "false" } }, null, ["GenerateRestoreGraphFile"], null, BuildRequestDataFlags.None);

                    mainBuildManager.Build(parameters, request);
                }
                finally
                {
                    pc.UnloadAllProjects();
                    pc.Dispose();
                }
            });

            if (File.Exists(restoreGraphResult.Path) && new FileInfo(restoreGraphResult.Path).Length != 0)
            {
                spec = DependencyGraphSpec.Load(restoreGraphResult.Path);
                File.Delete(restoreGraphResult.Path);
            }
            else
            {
                spec = new DependencyGraphSpec();
            }
        }

        return spec;
    }

    public static async Task RestoreNugetPackages(ILogger logger, string projectPath)
    {
        await Task.Run(() =>
        {
            var pc = new Microsoft.Build.Evaluation.ProjectCollection();

            try
            {
                var parameters = new BuildParameters(pc)
                {
                    Loggers = [new LoggerRedirect(logger, true)], //Instance of ILogger instantiated earlier
                    DisableInProcNode = true,
                };

                // Run a MSBuild /t:Restore <projectfile>
                var request = new BuildRequestData(projectPath, new Dictionary<string, string>(), null, ["Restore"], null, BuildRequestDataFlags.None);

                mainBuildManager.Build(parameters, request);
            }
            finally
            {
                pc.UnloadAllProjects();
                pc.Dispose();
            }
        });
    }

    public static MicrosoftProject LoadProject(string fullProjectLocation, string configuration = "Debug", string platform = "AnyCPU", Dictionary<string, string>? extraProperties = null)
    {
        configuration ??= "Debug";
        platform ??= "AnyCPU";

        var globalProperties = new Dictionary<string, string>
        {
            ["Configuration"] = configuration,
            ["Platform"] = platform
        };

        if (extraProperties != null)
        {
            foreach (var extraProperty in extraProperties)
            {
                globalProperties[extraProperty.Key] = extraProperty.Value;
            }
        }

        var projectCollection = new Microsoft.Build.Evaluation.ProjectCollection(globalProperties);
        projectCollection.LoadProject(fullProjectLocation);
        var project = projectCollection.LoadedProjects.First();

        // Support for cross-targeting (TargetFrameworks and RuntimeIdentifiers)
        // Reload project with first TargetFramework and/or RuntimeIdentifier
        void TryReloadWithFirstValue(string valuePropertyName, string valuesPropertyName)
        {
            if (globalProperties.ContainsKey(valuePropertyName))
                return;

            var propertyValue = project.GetPropertyValue(valuePropertyName);
            var propertyValues = project.GetPropertyValue(valuesPropertyName);
            if (string.IsNullOrWhiteSpace(propertyValue) && !string.IsNullOrWhiteSpace(propertyValues))
            {
                project.ProjectCollection.UnloadAllProjects();
                project.ProjectCollection.Dispose();

                globalProperties.Add(valuePropertyName, propertyValues.Split(';').First());
                projectCollection = new Microsoft.Build.Evaluation.ProjectCollection(globalProperties);
                projectCollection.LoadProject(fullProjectLocation);
                project = projectCollection.LoadedProjects.First();
            }
        }

        // We need to go through them one by one (because a MSBuild Condition might depend on previous step)
        // TODO: We should deduct TFM from referencing project(s) (if any) rather than default one.
        TryReloadWithFirstValue("TargetFramework", "TargetFrameworks");
        TryReloadWithFirstValue("RuntimeIdentifier", "RuntimeIdentifiers");
        TryReloadWithFirstValue("StrideGraphicsApi", "StrideGraphicsApis");

        return project;
    }

    private class LoggerRedirect : Microsoft.Build.Utilities.Logger
    {
        private readonly ILogger logger;
        private readonly bool onlyErrors;

        public LoggerRedirect(ILogger logger, bool onlyErrors = false)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.onlyErrors = onlyErrors;
        }

        public override void Initialize(IEventSource eventSource)
        {
            ArgumentNullException.ThrowIfNull(eventSource);
            if (!onlyErrors)
            {
                eventSource.MessageRaised += MessageRaised;
                eventSource.WarningRaised += WarningRaised;
            }
            eventSource.ErrorRaised += ErrorRaised;
        }

        void MessageRaised(object sender, BuildMessageEventArgs e)
        {
            if (logger is LoggerResult loggerResult)
            {
                loggerResult.Module = $"{e.File}({e.LineNumber},{e.ColumnNumber})";
            }

            // Redirect task execution messages to verbose output
            switch (e is TaskCommandLineEventArgs ? MessageImportance.Normal : e.Importance)
            {
                case MessageImportance.High:
                    logger.Info(e.Message);
                    break;
                case MessageImportance.Normal:
                    logger.Verbose(e.Message);
                    break;
                case MessageImportance.Low:
                    logger.Debug(e.Message);
                    break;
            }
        }

        void WarningRaised(object sender, BuildWarningEventArgs e)
        {
            if (logger is LoggerResult loggerResult)
            {
                loggerResult.Module = string.Format("{0}({1},{2})", e.File, e.LineNumber, e.ColumnNumber);
            }
            logger.Warning(e.Message);
        }

        void ErrorRaised(object sender, BuildErrorEventArgs e)
        {
            if (logger is LoggerResult loggerResult)
            {
                loggerResult.Module = $"{e.File}({e.LineNumber},{e.ColumnNumber})";
            }

            if (e.Code == "NETSDK1045")
            {
                var netVersion = Regex.Match(e.Message, @"\.(NET|net) ?(\d+\.\d+)");
                if (netVersion.Success)
                    logger.Error($"{e.Code}: this project requires {netVersion} SDK, please go to https://dotnet.microsoft.com/download and download {netVersion} SDK.");
                else
                    logger.Error($"{e.Code}: {e.Message}");
            }
            else
            {
                logger.Error(e.Message);
            }
        }
    }

    public static void Reset()
    {
        mainBuildManager.ResetCaches();
    }

    private class CancellableAsyncBuild : ICancellableAsyncBuild
    {
        public CancellableAsyncBuild(MicrosoftProject project, string assemblyPath)
        {
            Project = project;
            AssemblyPath = assemblyPath;
        }

        public string AssemblyPath { get; }

        public MicrosoftProject Project { get; }

        public Task<BuildResult> BuildTask { get; private set; }

        public bool IsCanceled { get; private set; }

        internal void Build(MicrosoftProject project, string targets, BuildRequestDataFlags flags, Microsoft.Build.Utilities.Logger logger)
        {
            ArgumentNullException.ThrowIfNull(project);
            ArgumentNullException.ThrowIfNull(logger);

            // Make sure that we are using the project collection from the loaded project, otherwise we are getting
            // weird cache behavior with the msbuild system
            var projectInstance = new ProjectInstance(project.Xml, project.ProjectCollection.GlobalProperties, project.ToolsVersion, project.ProjectCollection);

            BuildTask = Task.Run(() =>
            {
                return mainBuildManager.Build(
                    new BuildParameters(project.ProjectCollection)
                    {
                        Loggers = [logger],
                        DisableInProcNode = true,
                    },
                    new BuildRequestData(projectInstance, targets.Split(';'), null, flags));
            });
        }

        public void Cancel()
        {
            var localManager = mainBuildManager;
            if (localManager != null)
            {
                localManager.CancelAllSubmissions();
                IsCanceled = true;
            }
        }
    }
}
