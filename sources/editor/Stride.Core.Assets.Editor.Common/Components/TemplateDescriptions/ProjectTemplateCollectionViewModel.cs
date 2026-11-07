// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions;

/// <summary>
/// The templates that create a project (a package) in a location on disk.
/// </summary>
public abstract class ProjectTemplateCollectionViewModel : TemplateDescriptionCollectionViewModel
{
    private UDirectory location = new(string.Empty);

    protected ProjectTemplateCollectionViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        BrowseDirectoryCommand = new AnonymousTaskCommand<string>(serviceProvider, BrowseDirectory);
    }

    /// <summary>
    /// The folder the project is created in (a folder named <see cref="TemplateDescriptionCollectionViewModel.Name"/> inside it).
    /// </summary>
    public UDirectory Location
    {
        get => location;
        set => SetValue(ref location, value);
    }

    /// <summary>
    /// Picks a folder for the directory property whose name is the parameter (<see cref="Location"/> by default).
    /// </summary>
    public ICommandBase BrowseDirectoryCommand { get; }

    /// <inheritdoc/>
    public override bool ValidateProperties(out string error)
    {
        if (SelectedTemplate is TemplateDescriptionViewModel)
        {
            if (!UPath.IsValid(Location))
            {
                error = "Invalid output directory.";
                return false;
            }
            if (!UFile.IsValid(Name) || Name.Contains(UPath.DirectorySeparatorString) || Name.Contains(UPath.DirectorySeparatorStringAlt))
            {
                error = "Invalid name.";
                return false;
            }
            var outputPath = UPath.Combine<UDirectory>(Location, Name);
            if (Directory.Exists(outputPath))
            {
                error = "Cannot use the selected name because a folder with the same name already exists in the same location.";
                return false;
            }
        }
        error = "";
        return true;
    }

    /// <summary>
    /// A name made from the default name of the selected template that collides neither with the given names nor with the
    /// folders of <see cref="Location"/>.
    /// </summary>
    protected string GenerateUniqueNameAtLocation(List<string>? conflictingNames = null)
    {
        conflictingNames ??= [];

        if (Directory.Exists(Location))
        {
            // Also add currently existing folders
            var existingFolders = Directory.GetDirectories(Location).Select(Path.GetFileName).OfType<string>();
            conflictingNames.AddRange(existingFolders);
        }
        // Generate a name that does not collide with one of the previously gathered names
        return NamingHelper.ComputeNewName(SelectedTemplate?.DefaultOutputName ?? string.Empty, conflictingNames, x => x, "{0}{1}");
    }

    private async Task BrowseDirectory(string? propertyName)
    {
        var directory = await ServiceProvider.Get<IDialogService>().OpenFolderPickerAsync(Location);
        if (directory is not null)
        {
            var property = GetType().GetProperty(string.IsNullOrEmpty(propertyName) ? nameof(Location) : propertyName);
            property?.SetValue(this, directory);
        }
    }
}
