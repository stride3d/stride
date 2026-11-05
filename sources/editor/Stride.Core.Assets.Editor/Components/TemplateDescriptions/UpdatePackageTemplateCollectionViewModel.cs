// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Templates;
using Stride.Core.Extensions;
using Stride.Core.IO;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions;

/// <summary>
/// The package templates that can update the selected package of the session (the "Update package" menu).
/// </summary>
public sealed class UpdatePackageTemplateCollectionViewModel : ProjectTemplateCollectionViewModel
{
    private readonly SessionViewModel session;

    public UpdatePackageTemplateCollectionViewModel(SessionViewModel session)
        : base(session.SafeArgument().ServiceProvider)
    {
        this.session = session;

        var rootGroup = new TemplateDescriptionGroupViewModel(ServiceProvider, "All templates");

        foreach (var template in session.FindTemplates(TemplateScope.Package))
        {
            var viewModel = new PackageTemplateViewModel(ServiceProvider, template, session);
            rootGroup.Templates.Add(viewModel);
        }

        Location = session.SolutionPath?.GetFullDirectory() ?? InternalSettings.TemplatesWindowDialogLastNewSessionTemplateDirectory.GetValue();
        if (string.IsNullOrWhiteSpace(Location))
            Location = UPath.Combine<UDirectory>(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Stride Projects");

        SelectedGroup = rootGroup;
    }

    /// <inheritdoc/>
    public override IEnumerable<TemplateDescriptionGroupViewModel> RootGroups => [];

    /// <inheritdoc/>
    protected override string UpdateNameFromSelectedTemplate()
    {
        // Get package names in the current session
        return GenerateUniqueNameAtLocation(session.AllPackages.Select(x => x.Name).ToList());
    }
}
