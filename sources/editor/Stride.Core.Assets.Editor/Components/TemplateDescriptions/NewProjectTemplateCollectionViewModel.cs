// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using Stride.Core.Assets.Templates;
using Stride.Core.Extensions;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions;

/// <summary>
/// The content of the new project window of an open session: the templates that add a project to the session (a code library,
/// a plugin, an executable).
/// </summary>
public sealed class NewProjectTemplateCollectionViewModel : ProjectTemplateCollectionViewModel
{
    private readonly TemplateDescriptionGroupViewModel rootGroup;
    private bool arePropertiesValid;

    public NewProjectTemplateCollectionViewModel(IViewModelServiceProvider serviceProvider, ISessionViewModel session)
        : base(serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(session);
        Session = session;

        rootGroup = new TemplateDescriptionGroupViewModel(serviceProvider, "All templates");

        // Add a default General group
        var defaultGroup = new TemplateDescriptionGroupViewModel(rootGroup, "General");

        foreach (var template in session.FindTemplates(TemplateScope.Session))
        {
            if (!ShouldProvideTemplate(template))
                continue;

            var viewModel = new TemplateDescriptionViewModel(serviceProvider, template);
            var group = ProcessGroup(rootGroup, template.Group) ?? defaultGroup;
            group.Templates.Add(viewModel);
        }

        Location = NewOrOpenSessionTemplateCollectionViewModel.InitialLocationFor(session);

        SelectedGroup = rootGroup;
    }

    public ISessionViewModel Session { get; }

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

    private static bool ShouldProvideTemplate(TemplateDescription template)
    {
        return template.TemplateType == TemplateType.Standard;
    }
}
