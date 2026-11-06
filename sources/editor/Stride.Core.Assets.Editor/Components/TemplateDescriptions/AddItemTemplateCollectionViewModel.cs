// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions;

/// <summary>
/// The templates of an "add item" list (the Add asset button and dialog): one root group whose sub-groups are listed on the side,
/// and a search text that lists the matching templates of every group instead.
/// </summary>
public class AddItemTemplateCollectionViewModel : TemplateDescriptionCollectionViewModel
{
    private string searchToken = string.Empty;

    public AddItemTemplateCollectionViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        RootGroup = new TemplateDescriptionGroupViewModel(serviceProvider, "All templates");
        DependentProperties.Add(nameof(SearchToken), [nameof(IsSearching)]);
    }

    /// <inheritdoc/>
    public override IEnumerable<TemplateDescriptionGroupViewModel> RootGroups => RootGroup.Yield();

    public TemplateDescriptionGroupViewModel RootGroup { get; }

    /// <summary>
    /// The text the name of a listed template contains (case-insensitive). While it is not empty, the templates of every group
    /// are searched, sorted by name, and the groups cannot be selected.
    /// </summary>
    public string SearchToken
    {
        get => searchToken;
        set => SetValue(ref searchToken, value ?? string.Empty, () => { SelectedGroup = RootGroup; UpdateTemplateList(); });
    }

    /// <summary>
    /// Whether <see cref="SearchToken"/> narrows the list.
    /// </summary>
    public bool IsSearching => !string.IsNullOrWhiteSpace(searchToken);

    /// <inheritdoc/>
    public override bool ValidateProperties(out string error)
    {
        error = string.Empty;
        return true;
    }

    /// <inheritdoc/>
    protected override IEnumerable<ITemplateDescriptionViewModel> FilterTemplates(IEnumerable<ITemplateDescriptionViewModel> groupTemplates)
    {
        if (!IsSearching)
            return groupTemplates;

        var token = searchToken.Trim();
        return groupTemplates
            .Where(x => x.Name.Contains(token, StringComparison.CurrentCultureIgnoreCase))
            .OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase);
    }

    /// <inheritdoc/>
    protected override string? UpdateNameFromSelectedTemplate()
    {
        return null;
    }
}
