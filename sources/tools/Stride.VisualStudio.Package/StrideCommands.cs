// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.Build.Execution;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Stride.VisualStudio.BuildEngine;
using Stride.VisualStudio.Commands;
using Process = System.Diagnostics.Process;
using Project = EnvDTE.Project;
using ProjectItem = EnvDTE.ProjectItem;
using Task = System.Threading.Tasks.Task;

namespace Stride.VisualStudio
{
    public static class StrideCommands
    {
        static class ProjectItemKind
        {
            public static string SolutionFolder = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";
            public static string PhysicalFile = "{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}";
            public static string PhysicalFolder = "{6BB5F8EF-4483-11D3-8BCF-00C04F8EC28C}";
            public static string VirtualFolder = "{6BB5F8F0-4483-11D3-8BCF-00C04F8EC28C}";
            public static string Subproject = "{EA6618E8-6E24-4528-94BE-6889FE16485C}";
        }

        public static IServiceProvider ServiceProvider { get; set; }

        private static async void OpenWithGameStudioMenuCommand_Callback(object sender, EventArgs e)
        {
            var dte = (DTE2)ServiceProvider.GetService(typeof(SDTE));

            var solutionFile = dte.Solution?.FileName;

            // Is there any active solution?
            if (solutionFile == null)
                return;

            // Locate GameStudio
            var packageInfo = await StrideCommandsProxy.FindStrideSdkDir(solutionFile, "Stride.GameStudio");
            if (packageInfo.LoadedVersion == null || packageInfo.SdkPaths.Count == 0)
                return;

            var mainExecutable = packageInfo.SdkPaths.First(x => Path.GetFileName(x) == "Stride.GameStudio.exe");
            if (mainExecutable == null)
            {
                throw new InvalidOperationException("Could not locate GameStudio process");
            }
            if (Process.Start(mainExecutable, $"\"{solutionFile}\"") == null)
            {
                throw new InvalidOperationException("Could not start GameStudio process");
            }
        }

        private static void CleanIntermediateAssetsProjectMenuCommand_BeforeQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;

            // Default: Disabled
            menuCommand.Enabled = false;

            // Find selected project
            var project = GetSelectedProject();
            if (project == null)
            {
                menuCommand.Text = "Clean intermediate assets for ...";
                return;
            }

            // Update menu text to contains selected project name
            menuCommand.Text = string.Format("Clean intermediate assets for {0}", project.Name);
            menuCommand.Enabled = true;
        }


        private static async void CleanIntermediateAssetsSolutionMenuCommand_Callback(object sender, EventArgs e)
        {
            var dte = (DTE2)ServiceProvider.GetService(typeof(SDTE));

            // Is there any active solution?
            if (dte.Solution == null)
                return;

            foreach (var project in Projects())
            {
                await CleanIntermediateAsset(dte, project);
            }
        }

        /// <summary>
        /// Enumerates all projects in current solution.
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<Project> Projects()
        {
            var dte = (DTE2)ServiceProvider.GetService(typeof(SDTE));

            var projects = new List<Project>();

            var item = dte.Solution.Projects.GetEnumerator();
            while (item.MoveNext())
            {
                var project = item.Current as Project;
                if (project == null)
                    continue;

                if (project.Kind == ProjectItemKind.SolutionFolder)
                {
                    // Solution folder: recursive call
                    projects.AddRange(GetSolutionFolderProjects(project));
                }
                else
                {
                    // Project: add it
                    projects.Add(project);
                }
            }

            return projects;
        }

        private static IEnumerable<Project> GetSolutionFolderProjects(Project solutionFolder)
        {
            var projects = new List<Project>();

            for (var i = 1; i <= solutionFolder.ProjectItems.Count; i++)
            {
                var subProject = solutionFolder.ProjectItems.Item(i).SubProject;
                if (subProject == null)
                    continue;

                if (subProject.Kind == ProjectItemKind.SolutionFolder)
                {
                    // Solution folder: recursive call
                    projects.AddRange(GetSolutionFolderProjects(subProject));
                }
                else
                {
                    // Project: add it
                    projects.Add(subProject);
                }
            }

            return projects;
        }

        private static async void CleanIntermediateAssetsProjectMenuCommand_Callback(object sender, EventArgs e)
        {
            // Find selected project
            var project = GetSelectedProject();
            if (project == null)
                return;

            var dte = (DTE2)ServiceProvider.GetService(typeof(SDTE));

            // Is there any active solution?
            if (dte.Solution == null)
                return;

            await CleanIntermediateAsset(dte, project);
        }

        private static async Task CleanIntermediateAsset(DTE2 dte, Project project)
        {
            if (project.FileName == null || Path.GetExtension(project.FileName) != ".csproj")
                return;

            // Find current project active configuration
            var configManager = project.ConfigurationManager;
            var activeConfig = configManager.ActiveConfiguration;

            // Get global parameters for Configuration and Platform
            var globalProperties = new Dictionary<string, string>();
            globalProperties["Configuration"] = activeConfig.ConfigurationName;
            globalProperties["Platform"] = activeConfig.PlatformName == "Any CPU" ? "AnyCPU" : activeConfig.PlatformName;

            // Check if project has a StrideCurrentPackagePath
            var projectInstance = new ProjectInstance(project.FileName, globalProperties, null);
            var packagePathProperty = projectInstance.Properties.FirstOrDefault(x => x.Name == "StrideCurrentPackagePath");
            if (packagePathProperty == null)
                return;

            // Prepare build request
            var request = new BuildRequestData(project.FileName, globalProperties, null, new[] { "StrideCleanAsset" }, null);
            var pc = new Microsoft.Build.Evaluation.ProjectCollection();
            var buildParameters = new BuildParameters(pc);
            var buildLogger = new IDEBuildLogger(GetOutputPane(), new TaskProvider(ServiceProvider), VsHelper.ToHierarchy(project));
            buildParameters.Loggers = new[] { buildLogger };

            // Trigger async build
            buildLogger.OutputWindowPane.OutputStringThreadSafe(string.Format("Cleaning assets for project {0}...\r\n", project.Name));
            BuildManager.DefaultBuildManager.BeginBuild(buildParameters);
            var submission = BuildManager.DefaultBuildManager.PendBuildRequest(request);
            BuildResult buildResult = await submission.ExecuteAsync();
            BuildManager.DefaultBuildManager.EndBuild();
            buildLogger.OutputWindowPane.OutputStringThreadSafe("Done\r\n");
        }

        private static IVsOutputWindowPane GetOutputPane()
        {
            var outputWindow = (IVsOutputWindow)Package.GetGlobalService(typeof(SVsOutputWindow));

            // Get Output pane
            IVsOutputWindowPane pane;
            Guid generalPaneGuid = VSConstants.GUID_OutWindowGeneralPane;
            outputWindow.CreatePane(ref generalPaneGuid, "General", 1, 0);
            outputWindow.GetPane(ref generalPaneGuid, out pane);
            return pane;
        }

        private static Project GetSelectedProject()
        {
            var item = GetSelectedItem();

            // Project, return as is
            if (item is Project)
                return (Project)item;

            // ProjectItem, return containing project
            if (item is ProjectItem)
                return ((ProjectItem)item).ContainingProject;

            return null;
        }

        private static object GetSelectedItem()
        {
            var dte = (DTE2)ServiceProvider.GetService(typeof(SDTE));
            var selectedItems = (UIHierarchyItem[])dte.ToolWindows.SolutionExplorer.SelectedItems;

            // Expect a single result (no multi selection)
            if (selectedItems.Length != 1)
                return null;

            return selectedItems[0].Object;
        }
        
        internal static void RegisterCommands(OleMenuCommandService mcs)
        {
            // Create command for Stride -> Clean intermediate assets for Solution
            var openWithGameStudioCommandID = new CommandID(GuidList.guidStride_VisualStudio_PackageCmdSet, (int)StridePackageCmdIdList.cmdStrideOpenWithGameStudio);
            var openWithGameStudioMenuCommand = new OleMenuCommand(OpenWithGameStudioMenuCommand_Callback, openWithGameStudioCommandID);
            mcs.AddCommand(openWithGameStudioMenuCommand);

            // Create command for Stride -> Clean intermediate assets for Solution
            var cleanIntermediateAssetsSolutionCommandID = new CommandID(GuidList.guidStride_VisualStudio_PackageCmdSet, (int)StridePackageCmdIdList.cmdStrideCleanIntermediateAssetsSolutionCommand);
            var cleanIntermediateAssetsSolutionMenuCommand = new OleMenuCommand(CleanIntermediateAssetsSolutionMenuCommand_Callback, cleanIntermediateAssetsSolutionCommandID);
            mcs.AddCommand(cleanIntermediateAssetsSolutionMenuCommand);

            // Create command for Stride -> Clean intermediate assets for {selected project}
            var cleanIntermediateAssetsProjectCommandID = new CommandID(GuidList.guidStride_VisualStudio_PackageCmdSet, (int)StridePackageCmdIdList.cmdStrideCleanIntermediateAssetsProjectCommand);
            var cleanIntermediateAssetsProjectMenuCommand = new OleMenuCommand(CleanIntermediateAssetsProjectMenuCommand_Callback, cleanIntermediateAssetsProjectCommandID);
            cleanIntermediateAssetsProjectMenuCommand.BeforeQueryStatus += CleanIntermediateAssetsProjectMenuCommand_BeforeQueryStatus;
            cleanIntermediateAssetsProjectMenuCommand.Enabled = false;
            mcs.AddCommand(cleanIntermediateAssetsProjectMenuCommand);
        }
    }
}
