// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;
using Stride.Core.MostRecentlyUsedFiles;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;

namespace Stride.GameStudio.Avalonia.ViewModels;

/// <summary>
/// An entry of the "recent sessions" menu.
/// </summary>
internal sealed class RecentSessionViewModel : DispatcherViewModel
{
    public RecentSessionViewModel(IViewModelServiceProvider serviceProvider, MostRecentlyUsedFile file, Func<UFile, Task> open)
        : base(serviceProvider)
    {
        FilePath = file.FilePath;
        Version = file.Version;
        Timestamp = file.Timestamp;
        OpenCommand = new AnonymousTaskCommand(serviceProvider, () => open(FilePath));
    }

    public UFile FilePath { get; }

    public string Name => FilePath.GetFileNameWithoutExtension() ?? FilePath.ToString();

    /// <summary>
    /// The path of the file as the system writes it, shown in the menu as in WPF.
    /// </summary>
    public string DisplayPath => FilePath.ToOSPath();

    /// <summary>
    /// The version of the editor that opened the session last (its major and minor numbers), shown before the path as in WPF.
    /// </summary>
    public string Version { get; }

    public long Timestamp { get; }

    public ICommandBase OpenCommand { get; }
}
