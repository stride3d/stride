// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell;

namespace Stride.VisualStudio
{
    internal static class VsHelper
    {
        public static IVsHierarchy GetCurrentHierarchy(IServiceProvider provider)
        {
            EnvDTE.DTE vs = (EnvDTE.DTE)provider.GetService(typeof(EnvDTE.DTE)); if (vs == null) throw new InvalidOperationException("DTE not found."); return ToHierarchy(vs.SelectedItems.Item(1).ProjectItem.ContainingProject);
        }
        public static IVsHierarchy ToHierarchy(EnvDTE.Project project)
        {
            if (project == null) throw new ArgumentNullException("project"); string projectGuid = null;        // DTE does not expose the project GUID that exists at in the msbuild project file.        // Cannot use MSBuild object model because it uses a static instance of the Engine,         // and using the Project will cause it to be unloaded from the engine when the         // GC collects the variable that we declare.       
            using (XmlReader projectReader = XmlReader.Create(project.FileName))
            {
                projectReader.MoveToContent();
                object nodeName = projectReader.NameTable.Add("ProjectGuid");
                while (projectReader.Read())
                {
                    if (Object.Equals(projectReader.LocalName, nodeName))
                    {
                        projectGuid = (String)projectReader.ReadElementContentAsString(); break;
                    }
                }
            }
            Debug.Assert(!String.IsNullOrEmpty(projectGuid));
            IServiceProvider serviceProvider = new ServiceProvider(project.DTE as Microsoft.VisualStudio.OLE.Interop.IServiceProvider); return VsShellUtilities.GetHierarchy(serviceProvider, new Guid(projectGuid));
        }
        public static IVsProject ToVsProject(EnvDTE.Project project)
        {
            if (project == null) throw new ArgumentNullException("project");
            IVsProject vsProject = ToHierarchy(project) as IVsProject;
            if (vsProject == null)
            {
                throw new ArgumentException("Project is not a VS project.");
            }
            return vsProject;
        }
        public static EnvDTE.Project ToDteProject(IVsHierarchy hierarchy)
        {
            if (hierarchy == null) throw new ArgumentNullException("hierarchy");
            object prjObject = null;
            if (hierarchy.GetProperty(0xfffffffe, -2027, out prjObject) >= 0)
            {
                return (EnvDTE.Project)prjObject;
            }
            else
            {
                throw new ArgumentException("Hierarchy is not a project.");
            }
        }
        public static EnvDTE.Project ToDteProject(IVsProject project)
        {
            if (project == null) throw new ArgumentNullException("project");
            return ToDteProject(project as IVsHierarchy);
        }

        public static IEnumerable<EnvDTE.Project> GetDteProjectsInSolution(IVsSolution solution)
        {
            if (solution == null)
                yield break;

            foreach (var hier in GetProjectsInSolution(solution))
            {
                EnvDTE.Project project = null;

                try
                {
                    project = ToDteProject(hier);
                }
                catch (Exception)
                {
                }

                if (project != null)
                    yield return project;
            }
        }

        private static IEnumerable<IVsHierarchy> GetProjectsInSolution(IVsSolution solution)
        {
            if (solution == null) throw new ArgumentNullException("solution");

            IEnumHierarchies enumHierarchies;
            var guid = Guid.Empty;
            solution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION, ref guid, out enumHierarchies);

            // Invalid result?
            if (enumHierarchies == null)
                yield break;

            var hierarchyArray = new IVsHierarchy[1];
            uint hierarchyFetched;

            // Fetch one by one
            while (enumHierarchies.Next(1, hierarchyArray, out hierarchyFetched) == VSConstants.S_OK && hierarchyFetched == 1)
            {
                if (hierarchyArray[0] != null)
                    yield return hierarchyArray[0];
            }
        }
    }
}
