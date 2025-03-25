// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.CodeEditorSupport;
using Stride.Core.Presentation.Services;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Services
{
    public static class CodeEditorOpenerService
    {
        public static async Task<bool> StartOrToggle(SessionViewModel session, IDEInfo ideInfo)
        {
            if (!await CheckCanOpenSolution(session, ideInfo) && ideInfo?.IDEType != IDEType.VSCode)
                return false;

            Process process = null; 

            if (ideInfo?.IDEType == IDEType.VisualStudio)
                process = await VisualStudioService.GetInstance(session, true);
            
            process ??= await StartInstance(session, ideInfo);
            
            return process != null;
        }

        public static async Task<Process> StartInstance(SessionViewModel session, IDEInfo ideInfo)
        {
            if (!await CheckCanOpenSolution(session, ideInfo))
                return null;

            var startInfo = new ProcessStartInfo();
            if (ideInfo == null)
            {
                var defaultIDEName = EditorSettings.DefaultIDE.GetValue();

                if (!EditorSettings.DefaultIDE.GetAcceptableValues().Contains(defaultIDEName))
                    defaultIDEName = EditorSettings.DefaultIDE.DefaultValue;
                
                IEnumerable<IDEInfo> ides = IDEInfoVersions.AvailableIDEs();
                
                ideInfo = ides.FirstOrDefault(x => x.DisplayName == defaultIDEName) ?? IDEInfo.DefaultIDE;
            }
            
            string ideArguments = ideInfo.IDEType == IDEType.VSCode ? session.SolutionPath.GetFullDirectory() : session.SolutionPath;

            // It will be null if either "Default", or if not available anymore (uninstalled?)
            if (ideInfo.ProgramPath != null && File.Exists(ideInfo.ProgramPath))
            {
                startInfo.FileName = ideInfo.ProgramPath;
                startInfo.Arguments = $"\"{ideArguments}\"";
                startInfo.CreateNoWindow = ideInfo.IDEType == IDEType.VSCode;
            }
            else
            {
                startInfo.FileName = session.SolutionPath.ToOSPath();
                startInfo.UseShellExecute = true;
            }
            try
            {
                return Process.Start(startInfo);
            }
            catch
            {
                await session.Dialogs.MessageBoxAsync(string.Format(Tr._p("Message", "An error occurred while starting {0}."), ideInfo.IDEType), MessageBoxButton.OK, MessageBoxImage.Information);
                return null;
            }
        }

        internal static async Task<bool> CheckCanOpenSolution(SessionViewModel session, IDEInfo ideInfo)
        {
            if (string.IsNullOrEmpty(session.SolutionPath))
            {
                await session.Dialogs.MessageBoxAsync(string.Format(Tr._p("Message", "The session currently open is not a {0} session."), ideInfo.IDEType), MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            return true;
        }
    }
}
