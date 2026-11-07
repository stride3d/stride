// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions;

/// <summary>
/// A project to open, in the recent projects of the project selection window.
/// </summary>
public sealed class ExistingProjectViewModel : DispatcherViewModel, ITemplateDescriptionViewModel
{
    private readonly Action<ExistingProjectViewModel> removeAction;

    public ExistingProjectViewModel(IViewModelServiceProvider serviceProvider, UFile path, Action<ExistingProjectViewModel> openAction, Action<ExistingProjectViewModel> removeAction)
        : base(serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(openAction);
        ArgumentNullException.ThrowIfNull(removeAction);
        Path = path;
        Id = Guid.NewGuid();
        this.removeAction = removeAction;
        OpenCommand = new AnonymousCommand(serviceProvider, () => openAction(this));
        ExploreCommand = new AnonymousTaskCommand(serviceProvider, ExploreAsync);
        CopySolutionPathCommand = new AnonymousTaskCommand(serviceProvider, () => CopyToClipboardAsync(Path.ToOSPath()));
        CopyFolderPathCommand = new AnonymousTaskCommand(serviceProvider, () => CopyToClipboardAsync(Path.GetFullDirectory().ToOSPath()));
        RemoveCommand = new AnonymousCommand(serviceProvider, () => this.removeAction(this));
    }

    public string Name => Path.GetFileNameWithoutExtension() ?? Path.ToString();

    public string Description => Path.ToOSPath();

    public string FullDescription => "";

    public string Group => "";

    public Guid Id { get; }

    public string DefaultOutputName => "";

    /// <summary>
    /// The solution or package file of the project.
    /// </summary>
    public UFile Path { get; }

    /// <inheritdoc/>
    public UFile? Icon => null;

    /// <inheritdoc/>
    public IReadOnlyList<UFile> Screenshots => [];

    /// <summary>
    /// Opens the project.
    /// </summary>
    public ICommandBase OpenCommand { get; }

    /// <summary>
    /// Shows the project's file in the file manager of the platform.
    /// </summary>
    public ICommandBase ExploreCommand { get; }

    public ICommandBase CopySolutionPathCommand { get; }

    public ICommandBase CopyFolderPathCommand { get; }

    /// <summary>
    /// Removes the project from the recent projects.
    /// </summary>
    public ICommandBase RemoveCommand { get; }

    public TemplateDescription? GetTemplate()
    {
        return null;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{nameof(ExistingProjectViewModel)} [{Path}]";
    }

    private async Task ExploreAsync()
    {
        try
        {
            var explorer = ServiceProvider.TryGet<IFileExplorerService>() ?? new FileExplorerService();
            await explorer.ShowFileAsync(Path.ToOSPath());
        }
        catch (Exception)
        {
            // We don't provide feedback when the file manager cannot be started.
        }
    }

    private async Task CopyToClipboardAsync(string text)
    {
        try
        {
            if (ServiceProvider.TryGet<IClipboardService>() is { } clipboard)
                await clipboard.SetTextAsync(text);
        }
        catch (Exception)
        {
            // We don't provide feedback when copying fails.
        }
    }
}
