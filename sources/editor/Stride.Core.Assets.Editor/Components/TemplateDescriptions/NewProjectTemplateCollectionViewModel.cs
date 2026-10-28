// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Templates;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions;

/// <summary>
/// The content of the new project window of an open session: the templates that add a project to the session (a code library,
/// a plugin, an executable).
/// </summary>
public sealed class NewProjectTemplateCollectionViewModel : ProjectTemplateCollectionViewModel
{
    // Ids of samples/Library/Library/Library.sdtpl and samples/Plugin/Plugin/Plugin.sdtpl; ProjectExecutable.sdtpl is matched by path
    private static readonly Guid StrideLibraryTemplateId = new("7B79F1B7-3A55-4C84-AED9-3F4F3EE4B6E5");
    private static readonly Guid StridePluginTemplateId = new("2D5E7F1A-8C3B-4E6A-9F0D-1B2C3D4E5F60");

    private readonly TemplateDescriptionGroupViewModel rootGroup;
    private bool arePropertiesValid;

    public NewProjectTemplateCollectionViewModel(IViewModelServiceProvider serviceProvider, SessionViewModel session)
        : base(serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(session);
        Session = session;

        rootGroup = new TemplateDescriptionGroupViewModel(serviceProvider, "All templates");

        // Add a default General group
        var defaultGroup = new TemplateDescriptionGroupViewModel(rootGroup, "General");

        foreach (var template in session.FindTemplates(TemplateScope.Session))
        {
            if (!IsAssetsOnlyTemplate(template))
                continue;

            var viewModel = new TemplateDescriptionViewModel(serviceProvider, template);
            var group = ProcessGroup(rootGroup, template.Group) ?? defaultGroup;
            group.Templates.Add(viewModel);
        }

        Location = session.SolutionPath?.GetFullDirectory() ?? new UDirectory(InternalSettings.TemplatesWindowDialogLastNewSessionTemplateDirectory.GetValue());
        if (string.IsNullOrWhiteSpace(Location))
            Location = NewOrOpenSessionTemplateCollectionViewModel.DefaultLocation;

        SelectedGroup = rootGroup;
    }

    public SessionViewModel Session { get; }

    /// <summary>
    /// The result of the last <see cref="ValidateProperties"/>.
    /// </summary>
    public bool ArePropertiesValid
    {
        get => arePropertiesValid;
        private set => SetValue(ref arePropertiesValid, value);
    }

    /// <inheritdoc/>
    public override IEnumerable<TemplateDescriptionGroupViewModel> RootGroups => rootGroup.Yield();

    /// <summary>
    /// The parameters of the project to create from the selected template, or <c>null</c> when none is selected.
    /// </summary>
    public NewPackageParameters? GetNewPackageParameters()
    {
        if (SelectedTemplate?.GetTemplate() is not { } template)
            return null;
        return new NewPackageParameters
        {
            TemplateDescription = template,
            OutputName = Name,
            OutputDirectory = Location,
        };
    }

    /// <inheritdoc/>
    public override bool ValidateProperties(out string error)
    {
        return ArePropertiesValid = base.ValidateProperties(out error);
    }

    /// <inheritdoc/>
    protected override string? UpdateNameFromSelectedTemplate()
    {
        // Get package names in the current session
        return GenerateUniqueNameAtLocation(Session.AllPackages.Select(x => x.Name).ToList());
    }

    private static bool IsAssetsOnlyTemplate(TemplateDescription template)
    {
        if (template.Id == StrideLibraryTemplateId || template.Id == StridePluginTemplateId)
            return true;
        return template.FullPath?.FullPath.EndsWith("ProjectExecutable.sdtpl", StringComparison.Ordinal) == true;
    }
}
