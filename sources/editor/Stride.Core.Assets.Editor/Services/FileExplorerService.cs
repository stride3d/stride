// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// The <see cref="IFileExplorerService"/> of the desktop: Windows' explorer, macOS' Finder, and on Linux the file manager that
/// implements the freedesktop.org <c>FileManager1</c> interface, else the program <c>xdg-open</c> picks.
/// </summary>
public sealed class FileExplorerService : IFileExplorerService
{
    /// <summary>
    /// How long the file manager has to answer the request to show a file, on Linux.
    /// </summary>
    private static readonly TimeSpan ShowItemsTimeout = TimeSpan.FromSeconds(5);

    /// <inheritdoc/>
    public async Task ShowFileAsync(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{filePath}\"") { UseShellExecute = true });
            return;
        }
        if (OperatingSystem.IsMacOS())
        {
            var startInfo = new ProcessStartInfo("open") { UseShellExecute = false };
            startInfo.ArgumentList.Add("-R");
            startInfo.ArgumentList.Add(filePath);
            Process.Start(startInfo);
            return;
        }
        // The file managers of the Linux desktops (Nautilus, Dolphin, Nemo, Thunar...) select the file through FileManager1;
        // without one, the folder opens
        if (!await TryShowItemsAsync(filePath))
        {
            await OpenFolderAsync(Path.GetDirectoryName(filePath) ?? filePath);
        }
    }

    /// <inheritdoc/>
    public Task OpenFolderAsync(string folderPath)
    {
        ArgumentNullException.ThrowIfNull(folderPath);

        Process.Start(new ProcessStartInfo(folderPath) { UseShellExecute = true });
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OpenFileAsync(string filePath, bool tryEdit)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        var startInfo = new ProcessStartInfo(filePath) { UseShellExecute = true };
        // Only Windows tells the programs that open a file from the ones that edit it
        if (tryEdit && OperatingSystem.IsWindows())
        {
            startInfo.Verb = startInfo.Verbs.FirstOrDefault(x => string.Equals(x, "edit", StringComparison.OrdinalIgnoreCase));
        }
        Process.Start(startInfo);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OpenFileWithAsync(string programPath, string filePath)
    {
        ArgumentNullException.ThrowIfNull(programPath);
        ArgumentNullException.ThrowIfNull(filePath);

        var startInfo = new ProcessStartInfo(programPath) { UseShellExecute = false };
        startInfo.ArgumentList.Add(filePath);
        Process.Start(startInfo);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asks the file manager of the session bus to show the file, with <c>dbus-send</c>.
    /// </summary>
    /// <returns><c>true</c> if a file manager answered; <c>false</c> if there is none, or no <c>dbus-send</c>.</returns>
    private static async Task<bool> TryShowItemsAsync(string filePath)
    {
        var startInfo = new ProcessStartInfo("dbus-send")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("--session");
        // Waits for the answer: the exit code tells whether a file manager implements the interface
        startInfo.ArgumentList.Add("--print-reply");
        startInfo.ArgumentList.Add("--dest=org.freedesktop.FileManager1");
        startInfo.ArgumentList.Add("--type=method_call");
        startInfo.ArgumentList.Add("/org/freedesktop/FileManager1");
        startInfo.ArgumentList.Add("org.freedesktop.FileManager1.ShowItems");
        startInfo.ArgumentList.Add($"array:string:{new Uri(filePath).AbsoluteUri}");
        startInfo.ArgumentList.Add("string:");
        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
                return false;

            using var timeout = new CancellationTokenSource(ShowItemsTimeout);
            await process.WaitForExitAsync(timeout.Token);
            return process.ExitCode == 0;
        }
        catch (Win32Exception)
        {
            // No dbus-send
            return false;
        }
        catch (OperationCanceledException)
        {
            // A file manager that doesn't answer: open the folder rather than wait
            return false;
        }
    }
}
