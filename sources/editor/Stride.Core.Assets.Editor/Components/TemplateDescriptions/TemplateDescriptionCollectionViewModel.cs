// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions;

/// <summary>
/// The templates a template browser shows: groups on one side, the templates of the selected group (and its sub-groups) on the
/// other, and the name of what the selected template creates.
/// </summary>
public abstract class TemplateDescriptionCollectionViewModel : DispatcherViewModel
{
    private readonly ObservableList<ITemplateDescriptionViewModel> templates = [];

    private TemplateDescriptionGroupViewModel? selectedGroup;
    private ITemplateDescriptionViewModel? selectedTemplate;
    private string name = string.Empty;
    private bool userDefinedName;

    protected TemplateDescriptionCollectionViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    /// <summary>
    /// The templates of the selected group, sub-groups included.
    /// </summary>
    public IReadOnlyObservableCollection<ITemplateDescriptionViewModel> Templates => templates;

    public abstract IEnumerable<TemplateDescriptionGroupViewModel> RootGroups { get; }

    public TemplateDescriptionGroupViewModel? SelectedGroup
    {
        get => selectedGroup;
        set => SetValue(ref selectedGroup, value, UpdateTemplateList);
    }

    public ITemplateDescriptionViewModel? SelectedTemplate
    {
        get => selectedTemplate;
        set
        {
            if (SetValue(ref selectedTemplate, value))
            {
                OnPropertyChanged(nameof(HasSelectedTemplate), nameof(IsNewProjectSelected));
            }
            UpdateName();
        }
    }

    /// <summary>
    /// Whether a template (or a recent project) is selected: the selection can be accepted.
    /// </summary>
    public bool HasSelectedTemplate => selectedTemplate is not null;

    /// <summary>
    /// Whether the selected template creates a new project (not a recent project to open): its name and location can be edited.
    /// </summary>
    public bool IsNewProjectSelected => selectedTemplate is TemplateDescriptionViewModel;

    /// <summary>
    /// The name of what the selected template creates: a name from the template until the user types one.
    /// </summary>
    public string Name
    {
        get => name;
        set
        {
            userDefinedName = !string.IsNullOrEmpty(value) && name != value;
            SetValue(ref name, value ?? string.Empty);
        }
    }

    /// <summary>
    /// Checks the properties the user can set before the template is run.
    /// </summary>
    /// <param name="error">The error to show when they are not valid.</param>
    /// <returns><c>true</c> when the template can be run.</returns>
    public abstract bool ValidateProperties(out string error);

    /// <summary>
    /// Finds or creates the group of the given path ("A/B" makes B a sub-group of A) under the root group.
    /// </summary>
    /// <returns>The group, or <c>null</c> for an empty path.</returns>
    protected static TemplateDescriptionGroupViewModel? ProcessGroup(TemplateDescriptionGroupViewModel rootGroup, string? groupPath)
    {
        if (string.IsNullOrWhiteSpace(groupPath))
            return null;

        var groupDirectories = groupPath.Split('/', '\\');
        return groupDirectories.Aggregate(rootGroup, (current, groupDirectory) => current.SubGroups.FirstOrDefault(x => x.Name == groupDirectory) ?? new TemplateDescriptionGroupViewModel(current, groupDirectory));
    }

    /// <summary>
    /// The name to propose for the selected template.
    /// </summary>
    protected abstract string? UpdateNameFromSelectedTemplate();

    /// <summary>
    /// Narrows or orders the templates of the selected group before they are listed; all of them, in their order, by default.
    /// </summary>
    protected virtual IEnumerable<ITemplateDescriptionViewModel> FilterTemplates(IEnumerable<ITemplateDescriptionViewModel> groupTemplates)
    {
        return groupTemplates;
    }

    protected void UpdateTemplateList()
    {
        templates.Clear();
        if (SelectedGroup is not null)
        {
            templates.AddRange(FilterTemplates(SelectedGroup.GetTemplatesRecursively()));
        }
    }

    private void UpdateName()
    {
        if (userDefinedName)
            return;

        Name = SelectedTemplate is not null ? UpdateNameFromSelectedTemplate() ?? string.Empty : string.Empty;

        userDefinedName = false;
    }
}
