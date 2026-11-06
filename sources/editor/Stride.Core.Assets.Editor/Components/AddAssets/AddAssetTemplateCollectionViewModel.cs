// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Templates;
using Stride.Core.Extensions;

namespace Stride.Core.Assets.Editor.Components.AddAssets;

/// <summary>
/// The asset templates of a session (see <see cref="SessionViewModel.FindTemplates"/>), grouped as their descriptions say: the
/// list of the Add asset button and of the Add asset dialog.
/// </summary>
public sealed class AddAssetTemplateCollectionViewModel : AddItemTemplateCollectionViewModel
{
    private readonly SessionViewModel session;
    private DirectoryBaseViewModel? currentDirectory;

    public AddAssetTemplateCollectionViewModel(SessionViewModel session)
        : base(session.SafeArgument(nameof(session)).ServiceProvider)
    {
        this.session = session;
        Refresh();
    }

    /// <summary>
    /// The folder the asset is added to, which the proposed name is unique in (see <see cref="TargetDirectory"/>).
    /// </summary>
    public DirectoryBaseViewModel? CurrentDirectory
    {
        get => currentDirectory;
        set => SetValue(ref currentDirectory, value);
    }

    /// <summary>
    /// The folder the selected template creates its asset in: <see cref="CurrentDirectory"/>, or the first folder that accepts
    /// the asset type when that one does not.
    /// </summary>
    public DirectoryBaseViewModel? TargetDirectory { get; private set; }

    /// <summary>
    /// Rebuilds the list from the session's packages: a package added since (a plugin joining the solution)
    /// brings its own asset templates.
    /// </summary>
    public void Refresh()
    {
        RootGroup.Clear();
        foreach (var template in session.FindTemplates(TemplateScope.Asset))
        {
            // A package can be in the session while the assembly defining its asset type is not loaded
            if (template is TemplateAssetDescription assetTemplate && assetTemplate.FindAssetType() is null)
                continue;

            var group = ProcessGroup(RootGroup, template.Group);
            if (group is not null)
            {
                group.Templates.Add(new TemplateDescriptionViewModel(session.ServiceProvider, template));
            }
        }

        // The selected template belonged to the old groups
        SelectedTemplate = null;
        SelectedGroup = RootGroup;
        UpdateTemplateList();
    }

    /// <inheritdoc/>
    protected override string? UpdateNameFromSelectedTemplate()
    {
        if (SelectedTemplate?.GetTemplate() is not TemplateAssetDescription { RequireName: true } selectedTemplate || CurrentDirectory is null)
            return null;

        // If the mount point of the current folder does not support this type of asset, try to select the first mount point that support it.
        var assetType = selectedTemplate.GetAssetType();
        TargetDirectory = AssetViewModel.FindValidCreationLocation(assetType, CurrentDirectory);
        if (TargetDirectory is null)
            return null;

        var baseName = selectedTemplate.DefaultOutputName ?? selectedTemplate.AssetTypeName;
        return NamingHelper.ComputeNewName(baseName, TargetDirectory.Assets, x => x.Name, "{0}{1}");
    }
}
