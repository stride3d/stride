// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions;

/// <summary>
/// A group of templates (a node of the tree on the left of the template browser), with its sub-groups.
/// </summary>
public sealed class TemplateDescriptionGroupViewModel : DispatcherViewModel, IComparable<TemplateDescriptionGroupViewModel>
{
    public const string Separator = "/";

    private readonly SortedObservableCollection<TemplateDescriptionGroupViewModel> subGroups = [];

    public TemplateDescriptionGroupViewModel(TemplateDescriptionGroupViewModel parent, string name)
        : this(parent.SafeArgument(nameof(parent)).ServiceProvider, name)
    {
        Parent = parent;
        Parent.subGroups.Add(this);
    }

    public TemplateDescriptionGroupViewModel(IViewModelServiceProvider serviceProvider, string name)
        : base(serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
        Templates = [];
        DependentProperties.Add(nameof(Parent), [nameof(Path)]);
    }

    public string Name { get; }

    public string Path => IsRoot ? "" : Parent!.Path + Name + (Name.Length > 0 ? Separator : "");

    public TemplateDescriptionGroupViewModel? Parent { get; }

    public bool IsRoot => Parent is null;

    public IReadOnlyObservableCollection<TemplateDescriptionGroupViewModel> SubGroups => subGroups;

    public ObservableList<ITemplateDescriptionViewModel> Templates { get; }

    /// <summary>
    /// The templates of this group and of all its sub-groups.
    /// </summary>
    public IEnumerable<ITemplateDescriptionViewModel> GetTemplatesRecursively()
    {
        var allGroups = SubGroups.SelectDeep(x => x.SubGroups);
        return Templates.Concat(allGroups.SelectMany(x => x.Templates));
    }

    public void Clear()
    {
        subGroups.Clear();
        Templates.Clear();
    }

    /// <inheritdoc/>
    public int CompareTo(TemplateDescriptionGroupViewModel? other)
    {
        return other is not null ? string.Compare(Name, other.Name, StringComparison.InvariantCultureIgnoreCase) : -1;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{nameof(TemplateDescriptionGroupViewModel)} [{Path}]";
    }
}
