// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using System.Diagnostics;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.CodeEditorSupport;
using Stride.Core.Presentation.Services;
using Stride.Core.Translation;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// Opens the solution of the session in an IDE (Open in IDE): the IDE of the <see cref="ExternalToolsSettings.DefaultIDE"/>
/// setting, or the given one.
/// </summary>
public static class CodeEditorOpenerService
{
    /// <summary>
    /// Brings the Visual Studio instance that has the solution open to the front (through <see cref="IVisualStudioService"/>,
    /// when the editor has one), or starts the IDE with the solution.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="ideInfo">The IDE, or <c>null</c> for the one of the settings.</param>
    /// <returns><c>true</c> when an IDE has the solution open.</returns>
    public static async Task<bool> StartOrToggle(ISessionViewModel session, IDEInfo? ideInfo)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!await CheckCanOpenSolution(session, ideInfo) && ideInfo?.IDEType != IDEType.VSCode)
            return false;

        Process? process = null;

        if (ideInfo?.IDEType == IDEType.VisualStudio && session.ServiceProvider.TryGet<IVisualStudioService>() is { } visualStudio)
            process = await visualStudio.GetInstanceAsync(session.SolutionPath, true);

        process ??= await StartInstance(session, ideInfo);

        return process != null;
    }

    /// <summary>
    /// Starts the IDE with the solution of the session.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="ideInfo">The IDE, or <c>null</c> for the one of the settings.</param>
    /// <returns>The process of the IDE, or <c>null</c> when it could not start.</returns>
    public static async Task<Process?> StartInstance(ISessionViewModel session, IDEInfo? ideInfo)
    {
        ArgumentNullException.ThrowIfNull(session);

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
            await session.ServiceProvider.Get<IDialogService>().MessageBoxAsync(string.Format(Tr._p("Message", "An error occurred while starting {0}."), ideInfo.IDEType), MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }
    }

    /// <summary>
    /// Checks that the session has a solution, and tells the user when it has none.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="ideInfo">The IDE, or <c>null</c> for the default one.</param>
    /// <returns><c>true</c> when the session has a solution.</returns>
    public static async Task<bool> CheckCanOpenSolution(ISessionViewModel session, IDEInfo? ideInfo)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (string.IsNullOrEmpty(session.SolutionPath))
        {
            await session.ServiceProvider.Get<IDialogService>().MessageBoxAsync(string.Format(Tr._p("Message", "The session currently open is not a {0} session."), (ideInfo ?? IDEInfo.DefaultIDE).IDEType), MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }
        return true;
    }
}
