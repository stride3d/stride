// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows.Input;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Presentation.Extensions;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// The types a list of the property grid offers to add, such as the Add component button of an entity: the types grouped by
/// category, the types of the selected group, and a search that lists the matching types of every group instead.
/// </summary>
/// <remarks>
/// The types and groups come from the node (for the components of an entity, the types the selected entities can all take), and
/// <see cref="AddCommand"/> is the node's add command, run with the picked <see cref="AbstractNodeType"/>.
/// </remarks>
public sealed class NodeTypePickerViewModel : ViewModelBase
{
    private static readonly char[] TokenSeparators = [' ', '\t', '\r', '\n'];

    public NodeTypePickerViewModel()
    {
        DependentProperties.Add(nameof(SearchToken), [nameof(IsSearching)]);
        DependentProperties.Add(nameof(Types), [nameof(HasTypes)]);
    }

    /// <summary>
    /// Gets or sets every type that can be added, in the order they are listed.
    /// </summary>
    public IReadOnlyList<AbstractNodeType> Types
    {
        get;
        set => SetValue(ref field, value ?? [], UpdateShownTypes);
    } = [];

    /// <summary>
    /// Gets whether there is any type to add.
    /// </summary>
    public bool HasTypes => Types.Count > 0;

    /// <summary>
    /// Gets or sets the groups of the types, in the order they are listed.
    /// </summary>
    public IReadOnlyList<AbstractNodeTypeGroup> Groups
    {
        get;
        set => SetValue(ref field, value ?? [], () =>
        {
            if (SelectedGroup is not null && !Groups.Contains(SelectedGroup))
                SelectedGroup = null;
        });
    } = [];

    /// <summary>
    /// Gets or sets the text the listed types match. While it is not empty, the types of every group are listed and no group is
    /// selected.
    /// </summary>
    public string SearchToken
    {
        get;
        set => SetValue(ref field, value ?? string.Empty, () =>
        {
            if (IsSearching)
                SelectedGroup = null;
            UpdateShownTypes();
        });
    } = string.Empty;

    /// <summary>
    /// Gets whether <see cref="SearchToken"/> narrows the list.
    /// </summary>
    public bool IsSearching => !string.IsNullOrWhiteSpace(SearchToken);

    /// <summary>
    /// Gets or sets the group whose types are listed while there is no search.
    /// </summary>
    public AbstractNodeTypeGroup? SelectedGroup
    {
        get;
        set => SetValue(ref field, value, UpdateShownTypes);
    }

    /// <summary>
    /// Gets the types listed: those matching the search, or else those of the selected group (none when no group is selected).
    /// </summary>
    public IReadOnlyList<AbstractNodeType> ShownTypes
    {
        get;
        private set => SetValue(ref field, value);
    } = [];

    /// <summary>
    /// Gets or sets the type that a validation adds. A search selects its first match.
    /// </summary>
    public AbstractNodeType? SelectedType
    {
        get;
        set => SetValue(ref field, value);
    }

    /// <summary>
    /// Gets or sets the command that adds a type, run with the <see cref="AbstractNodeType"/>.
    /// </summary>
    public ICommand? AddCommand
    {
        get;
        set => SetValue(ref field, value);
    }

    /// <summary>
    /// Gets whether a type matches a search: every word of the search is a part of the type's name, or its camel case initials,
    /// ignoring the case.
    /// </summary>
    /// <param name="search">The search, whose words are separated by white spaces.</param>
    /// <param name="name">The name of the type.</param>
    public static bool Matches(string search, string name)
    {
        ArgumentNullException.ThrowIfNull(search);
        ArgumentNullException.ThrowIfNull(name);

        var tokens = search.Split(TokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        return tokens.All(token => name.Contains(token, StringComparison.InvariantCultureIgnoreCase) || token.MatchCamelCase(name));
    }

    /// <summary>
    /// Adds a type with <see cref="AddCommand"/>, then clears the search and the selection.
    /// </summary>
    /// <param name="type">The type to add.</param>
    /// <returns><c>true</c> if the command ran; otherwise, <c>false</c>.</returns>
    public bool Add(AbstractNodeType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var command = AddCommand;
        var added = command?.CanExecute(type) == true;
        if (added)
            command!.Execute(type);
        Reset();
        return added;
    }

    /// <summary>
    /// Clears the search and the selected group and type: no type is listed until a group is selected or a search typed.
    /// </summary>
    public void Reset()
    {
        SearchToken = string.Empty;
        SelectedGroup = null;
        SelectedType = null;
    }

    private void UpdateShownTypes()
    {
        if (IsSearching)
        {
            var search = SearchToken.Trim();
            ShownTypes = Types.Where(x => Matches(search, x.DisplayValue)).ToList();
            SelectedType = ShownTypes.FirstOrDefault();
        }
        else
        {
            ShownTypes = SelectedGroup?.Types.ToList() ?? [];
            if (SelectedType is not null && !ShownTypes.Contains(SelectedType))
                SelectedType = null;
        }
    }
}
