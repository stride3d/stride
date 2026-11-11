// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.UI.Editor.ViewModels;

namespace Stride.UI.Editor.Avalonia.Views;

/// <summary>
/// A group of the toolbox of the UI editor: a library kind ("Standard library" or "User libraries") with its categories, or a
/// category with its element factories, as the groups of the WPF UI editor's library list.
/// </summary>
/// <param name="Name">The name shown in the header of the group.</param>
/// <param name="Groups">The categories of a library kind; empty for a category.</param>
/// <param name="Factories">The factories of a category; empty for a library kind.</param>
public sealed record UIElementFactoryGroup(string Name, IReadOnlyList<UIElementFactoryGroup> Groups, IReadOnlyList<IUIElementFactory> Factories)
{
    /// <summary>
    /// The library kind of the elements of the standard library.
    /// </summary>
    public const string StandardLibrary = "Standard library";

    /// <summary>
    /// The library kind of the elements of the UI libraries of the session.
    /// </summary>
    public const string UserLibraries = "User libraries";

    /// <summary>
    /// Groups the factories by library kind, then by category, sorted by category then by name; the groups come in the order
    /// of their first factory, as the groups of a sorted WPF collection view.
    /// </summary>
    public static IReadOnlyList<UIElementFactoryGroup> Create(IEnumerable<IUIElementFactory> factories)
    {
        var sorted = factories.OrderBy(x => x.Category, StringComparer.CurrentCulture).ThenBy(x => x.Name, StringComparer.CurrentCulture).ToList();
        return sorted.GroupBy(x => x is UIElementFromSystemLibrary ? StandardLibrary : UserLibraries)
            .Select(kind => new UIElementFactoryGroup(kind.Key, kind.GroupBy(x => x.Category).Select(category => new UIElementFactoryGroup(category.Key, [], category.ToList())).ToList(), []))
            .ToList();
    }
}
