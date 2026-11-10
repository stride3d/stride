// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;

namespace Stride.Core.Assets.Editor.Tests.Helpers;

/// <summary>
/// A file explorer that records what it is asked to show or open, without starting a program.
/// </summary>
public sealed class FakeFileExplorerService : IFileExplorerService
{
    /// <summary>
    /// The requests so far, as "show", "folder", "open", "edit" or "with &lt;program&gt;" followed by the path.
    /// </summary>
    public List<string> Requests { get; } = [];

    /// <summary>
    /// Whether the next requests fail, as when the program can't be started.
    /// </summary>
    public bool Fail { get; set; }

    public Task ShowFileAsync(string filePath) => Record($"show {filePath}");

    public Task OpenFolderAsync(string folderPath) => Record($"folder {folderPath}");

    public Task OpenFileAsync(string filePath, bool tryEdit) => Record($"{(tryEdit ? "edit" : "open")} {filePath}");

    public Task OpenFileWithAsync(string programPath, string filePath) => Record($"with {programPath} {filePath}");

    private Task Record(string request)
    {
        if (Fail)
            throw new InvalidOperationException("The program can't be started");
        Requests.Add(request);
        return Task.CompletedTask;
    }
}
