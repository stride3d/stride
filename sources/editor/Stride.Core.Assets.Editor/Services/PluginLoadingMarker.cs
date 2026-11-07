// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;
using Stride.Core.Translation;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// A file that says an editor is loading the plugins of a session: written when the session starts loading, removed once it
/// has opened (or failed to open without crashing). A marker whose process is gone means that editor died while its plugins
/// were joining: the next editor to start offers safe mode once (<see cref="OfferSafeModeAsync"/>).
/// </summary>
/// <remarks>
/// One file per process, in a folder beside the user's settings (<c>PluginLoading/&lt;process id&gt;.marker</c> under
/// <see cref="EditorPath.UserDataPath"/>), so that several editors starting at the same time, of either toolkit, don't take
/// each other's marker for a crash. The file holds the session's path and the start time of the process, which tells a
/// process that is gone from a later one that got the same id.
/// </remarks>
[Experimental("STREXP002")]
public sealed class PluginLoadingMarker
{
    private const string Extension = ".marker";

    /// <param name="directory">The folder of the markers.</param>
    public PluginLoadingMarker(string directory)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        Directory = directory;
    }

    /// <summary>
    /// The folder of the markers.
    /// </summary>
    public string Directory { get; }

    /// <summary>
    /// The markers of the editors of the user: <c>PluginLoading</c> beside the settings files.
    /// </summary>
    public static PluginLoadingMarker CreateDefault() => new(Path.Combine(EditorPath.UserDataPath, "PluginLoading"));

    /// <summary>
    /// The marker of this process.
    /// </summary>
    public string FilePath => GetFilePath(Environment.ProcessId);

    /// <summary>
    /// Writes the marker of this process: a session starts loading, and its plugins join it.
    /// </summary>
    /// <param name="sessionPath">The session's file, or null for a new project.</param>
    public void Write(UFile? sessionPath)
    {
        try
        {
            System.IO.Directory.CreateDirectory(Directory);
            var startTime = Process.GetCurrentProcess().StartTime.ToUniversalTime().Ticks;
            File.WriteAllLines(FilePath, [sessionPath?.ToOSPath() ?? "", startTime.ToString(CultureInfo.InvariantCulture)]);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Without a marker, a crash only goes without the offer of safe mode
        }
    }

    /// <summary>
    /// Removes the marker of this process: the session has opened, or failed to open without a crash.
    /// </summary>
    public void Remove() => Delete(FilePath);

    /// <summary>
    /// The markers that editors left behind: their process is gone, so it died while plugins were joining a session.
    /// </summary>
    public IReadOnlyList<LeftMarker> FindLeftBehind()
    {
        if (!System.IO.Directory.Exists(Directory))
            return [];
        var result = new List<LeftMarker>();
        foreach (var file in System.IO.Directory.EnumerateFiles(Directory, "*" + Extension))
        {
            if (!int.TryParse(Path.GetFileNameWithoutExtension(file), NumberStyles.None, CultureInfo.InvariantCulture, out var processId))
                continue;
            string[] lines;
            try
            {
                lines = File.ReadAllLines(file);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                continue;
            }
            var startTime = lines.Length > 1 && long.TryParse(lines[1], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks) ? ticks : 0;
            if (IsRunning(processId, startTime))
                continue;
            result.Add(new LeftMarker(file, lines.Length > 0 && lines[0].Length > 0 ? new UFile(lines[0]) : null));
        }
        return result;
    }

    /// <summary>
    /// Deletes markers left behind, once their crash was dealt with.
    /// </summary>
    public static void Clear(IEnumerable<LeftMarker> markers)
    {
        ArgumentNullException.ThrowIfNull(markers);
        foreach (var marker in markers)
            Delete(marker.FilePath);
    }

    /// <summary>
    /// When markers were left behind, asks once whether to start in safe mode, then clears them whatever the answer.
    /// </summary>
    /// <param name="dialogService">The dialog service of the editor.</param>
    /// <returns>True when the user chose safe mode; false when nothing was left behind or the user chose to start normally.</returns>
    public async Task<bool> OfferSafeModeAsync(IDialogService dialogService)
    {
        ArgumentNullException.ThrowIfNull(dialogService);
        var left = FindLeftBehind();
        if (left.Count == 0)
            return false;
        try
        {
            var session = left.Select(x => x.SessionPath).FirstOrDefault(x => x is not null);
            var stopped = session is not null
                ? string.Format(Tr._p("Message", "Game Studio stopped while it was opening the project **{0}**."), session.GetFileName())
                : Tr._p("Message", "Game Studio stopped while it was opening a project.");
            var message = stopped + "\r\n\r\n" + Tr._p("Message", "Do you want to start in safe mode? Only the plugins of Stride are loaded, and those of the project are off until you restart normally.");
            var buttons = new[]
            {
                new DialogButtonInfo { Content = Tr._p("Button", "Start in safe mode"), Result = 1, IsDefault = true },
                new DialogButtonInfo { Content = Tr._p("Button", "Start normally"), Result = 2, IsCancel = true },
            };
            return await dialogService.MessageBoxAsync(message, buttons, MessageBoxImage.Warning) == 1;
        }
        finally
        {
            // Offered once: the next start does not ask again
            Clear(left);
        }
    }

    private string GetFilePath(int processId) => Path.Combine(Directory, processId.ToString(CultureInfo.InvariantCulture) + Extension);

    private static bool IsRunning(int processId, long startTime)
    {
        if (processId == Environment.ProcessId)
            return true;
        try
        {
            using var process = Process.GetProcessById(processId);
            // Another process that got the same id later
            return startTime == 0 || process.StartTime.ToUniversalTime().Ticks == startTime;
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
        {
            return false;
        }
    }

    private static void Delete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Left for the next start, which asks again
        }
    }
}

/// <summary>
/// A marker that an editor left behind (<see cref="PluginLoadingMarker.FindLeftBehind"/>).
/// </summary>
/// <param name="FilePath">The marker's file.</param>
/// <param name="SessionPath">The session that editor was loading, or null for a new project.</param>
[Experimental("STREXP002")]
public sealed record LeftMarker(string FilePath, UFile? SessionPath);
