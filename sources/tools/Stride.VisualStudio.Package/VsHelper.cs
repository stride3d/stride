// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Xml;
using Microsoft.VisualStudio.Shell.Interop;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell;

namespace Stride.VisualStudio
{
    internal static class VsHelper
    {
        public static IVsHierarchy ToHierarchy(EnvDTE.Project project)
        {
            if (project == null) throw new ArgumentNullException("project");
            // DTE doesn't expose the project GUID stored in the msbuild file, and the MSBuild object model
            // would unload the project when the GC collects it, so read the GUID straight from the XML.
            string projectGuid = null;
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
            IServiceProvider serviceProvider = new ServiceProvider(project.DTE as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
            return VsShellUtilities.GetHierarchy(serviceProvider, new Guid(projectGuid));
        }
    }
}
