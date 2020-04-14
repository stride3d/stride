// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Stride.VisualStudio
{
    public class BuildLogPipeGenerator
    {
        private string logPipeUrl = "net.pipe://localhost/Stride.BuildEngine.Monitor." + Guid.NewGuid();
        private SolutionEventsListener solutionEventsListener;

        public string LogPipeUrl
        {
            get { return logPipeUrl; }
        }

        public BuildLogPipeGenerator(IServiceProvider serviceProvider)
        {
            // Initialize the solution listener that will set StrideVSBuilderMonitorGuid for this instance of VisualStudio.
            solutionEventsListener = new SolutionEventsListener(serviceProvider);
            solutionEventsListener.AfterProjectOpened += OnProjectOpened;

            // Process already opened projects
            var solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            if (solution != null)
            {
                IEnumHierarchies enumerator;
                var guid = Guid.Empty;
                var hierarchy = new IVsHierarchy[1] { null };
                uint fetched = 0;

                solution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION, ref guid, out enumerator);
                for (enumerator.Reset(); enumerator.Next(1, hierarchy, out fetched) == VSConstants.S_OK && fetched == 1; )
                {
                    OnProjectOpened(hierarchy[0]);
                }
            }
        }

        private void OnProjectOpened(IVsHierarchy vsHierarchy)
        {
            // Register pipe url so that MSBuild can transfer it
            var vsProject = vsHierarchy as IVsProject;
            if (vsProject != null)
            {
                var dteProject = VsHelper.ToDteProject(vsProject);

                // We will only deal with .csproj files for now
                // Should we support C++/CLI .vcxproj as well?
                if (!dteProject.FileName.EndsWith(".csproj"))
                    return;

                // Find current project active configuration
                var configManager = dteProject.ConfigurationManager;
                if (configManager == null)
                    return;

                EnvDTE.Configuration activeConfig;
                try
                {
                    activeConfig = configManager.ActiveConfiguration;
                }
                catch (Exception)
                {
                    if (configManager.Count == 0)
                        return;

                    activeConfig = configManager.Item(1);
                }

                // Get global parameters for Configuration and Platform
                var globalProperties = new Dictionary<string, string>();
                globalProperties["Configuration"] = activeConfig.ConfigurationName;
                globalProperties["Platform"] = activeConfig.PlatformName == "Any CPU" ? "AnyCPU" : activeConfig.PlatformName;

                // Check if project matches: Condition="'$(StrideCurrentPackagePath)' != '' and '$(StrideIsExecutable)' == 'true'"
                var projectInstance = new ProjectInstance(dteProject.FileName, globalProperties, null);
                var packagePathProperty = projectInstance.Properties.FirstOrDefault(x => x.Name == "StrideCurrentPackagePath");
                var isExecutableProperty = projectInstance.Properties.FirstOrDefault(x => x.Name == "StrideIsExecutable");
                if (packagePathProperty == null || isExecutableProperty == null || isExecutableProperty.EvaluatedValue.ToLowerInvariant() != "true")
                    return;

                var buildProjects = ProjectCollection.GlobalProjectCollection.GetLoadedProjects(dteProject.FileName);
                foreach (var buildProject in buildProjects)
                {
                    buildProject.SetGlobalProperty("StrideBuildEngineLogPipeUrl", logPipeUrl);
                }
            }
        }
    }
}
