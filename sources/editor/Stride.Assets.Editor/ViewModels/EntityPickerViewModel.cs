// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Services;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Reflection;
using Stride.Core.Translation;
using Stride.Engine;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// The entity picker of the Pick buttons of the entity and component properties (<see cref="IEntityPickerService"/>), as the WPF
/// <c>EntityPickerWindow</c>: the hierarchy of an editor, narrowed to the items that pass a filter and, when a component is picked,
/// to the entities that have a component of that type. A search text narrows it further to the entities whose name contains it.
/// </summary>
public sealed class EntityPickerViewModel : DispatcherViewModel
{
    private readonly EntityHierarchyEditorViewModel editor;
    private readonly Func<EntityHierarchyItemViewModel, bool> filter;
    private IReadOnlyList<PickableEntityViewModel> roots;
    private string searchText = string.Empty;
    private PickableEntityViewModel? selectedEntity;

    /// <param name="editor">The editor whose hierarchy is shown.</param>
    /// <param name="componentType">The type of the component to pick, or <c>null</c> to pick an entity.</param>
    /// <param name="filter">The items of the hierarchy that are shown, with their children, or <c>null</c> for all of them.</param>
    public EntityPickerViewModel(EntityHierarchyEditorViewModel editor, Type? componentType, Func<EntityHierarchyItemViewModel, bool>? filter = null)
        : base(editor.ServiceProvider)
    {
        this.editor = editor;
        this.filter = filter ?? (_ => true);
        // As in WPF: a component type, or an interface or abstract type that components implement
        ComponentType = componentType is not null && (typeof(EntityComponent).IsAssignableFrom(componentType) || componentType.IsAbstract) ? componentType : null;
        Message = ComponentType is null
            ? Tr._p("Message", "Select an entity:")
            : string.Format(Tr._p("Message", "Select an entity that has a {0} component."), TypeDescriptorFactory.Default.AttributeRegistry.GetDisplayName(ComponentType));
        roots = BuildRoots();
    }

    /// <summary>
    /// Gets the type of the component to pick, or <c>null</c> when an entity is picked.
    /// </summary>
    public Type? ComponentType { get; }

    /// <summary>
    /// Gets the text over the hierarchy, which says what to pick.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the shown items at the root of the hierarchy.
    /// </summary>
    public IReadOnlyList<PickableEntityViewModel> Roots { get => roots; private set => SetValue(ref roots, value); }

    /// <summary>
    /// Gets or sets the text that the names of the shown entities contain (case-insensitive); empty shows them all.
    /// </summary>
    public string SearchText
    {
        get => searchText;
        set
        {
            if (SetValue(ref searchText, value ?? string.Empty))
            {
                SelectedEntity = null;
                Roots = BuildRoots();
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected item of the hierarchy, or <c>null</c>.
    /// </summary>
    public PickableEntityViewModel? SelectedEntity
    {
        get => selectedEntity;
        set => SetValue(ref selectedEntity, value, nameof(SelectedEntity), nameof(SelectionValid));
    }

    /// <summary>
    /// Gets whether the selection can be picked: an entity and, when a component is picked, one of its components of the type.
    /// </summary>
    public bool SelectionValid
    {
        get
        {
            if (SelectedEntity?.Entity is null)
                return false;
            // Not IsInstanceOfType: the component type can be an interface
            return ComponentType is null || (SelectedEntity.SelectedComponent is { } component && ComponentType.IsAssignableFrom(component.Component.GetType()));
        }
    }

    /// <summary>
    /// Gets the picked entity when the selection can be picked, otherwise <c>null</c>.
    /// </summary>
    public IPickedEntity? PickedEntity => SelectionValid ? SelectedEntity : null;

    internal void OnSelectedComponentChanged(PickableEntityViewModel item)
    {
        if (item == SelectedEntity)
            OnPropertyChanged(nameof(SelectionValid));
    }

    private IReadOnlyList<PickableEntityViewModel> BuildRoots()
    {
        var search = searchText.Trim();
        return editor.HierarchyRoot.Children.Where(filter).Select(x => new PickableEntityViewModel(this, x, filter, ComponentType, search)).Where(x => x.IsShown).ToList();
    }
}

/// <summary>
/// An item of the hierarchy of an <see cref="EntityPickerViewModel"/>: an entity, a folder or a child scene.
/// </summary>
public sealed class PickableEntityViewModel : ViewModelBase, IPickedEntity
{
    private readonly EntityPickerViewModel picker;
    private PickableComponentViewModel? selectedComponent;

    internal PickableEntityViewModel(EntityPickerViewModel picker, EntityHierarchyItemViewModel item, Func<EntityHierarchyItemViewModel, bool> filter, Type? componentType, string search)
    {
        this.picker = picker;
        Item = item;
        Entity = item as EntityViewModel;
        if (Entity is not null && componentType is not null)
        {
            Components = Entity.AssetSideEntity.Components.Select((x, i) => (Component: x, Index: i)).Where(x => componentType.IsInstanceOfType(x.Component)).Select(x => new PickableComponentViewModel(x.Index, x.Component)).ToList();
            selectedComponent = Components.FirstOrDefault();
        }
        var isPickable = Entity is not null && (componentType is null || Components.Count > 0);
        var nameMatches = search.Length == 0 || (item.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false);
        Children = item.Children.Where(filter).Select(x => new PickableEntityViewModel(picker, x, filter, componentType, search)).Where(x => x.IsShown).ToList();
        IsShown = (isPickable && nameMatches) || Children.Count > 0;
    }

    /// <summary>
    /// Gets the item of the hierarchy.
    /// </summary>
    public EntityHierarchyItemViewModel Item { get; }

    /// <summary>
    /// Gets the name of the item.
    /// </summary>
    public string? Name => Item.Name;

    /// <inheritdoc/>
    public EntityViewModel? Entity { get; }

    /// <summary>
    /// Gets the shown children of the item.
    /// </summary>
    public IReadOnlyList<PickableEntityViewModel> Children { get; }

    /// <summary>
    /// Gets the components of the entity that have the type to pick; empty when an entity is picked.
    /// </summary>
    public IReadOnlyList<PickableComponentViewModel> Components { get; } = [];

    /// <summary>
    /// Gets or sets the picked component, or <c>null</c>.
    /// </summary>
    public PickableComponentViewModel? SelectedComponent
    {
        get => selectedComponent;
        set
        {
            if (SetValue(ref selectedComponent, value))
                picker.OnSelectedComponentChanged(this);
        }
    }

    /// <inheritdoc/>
    public int ComponentIndex => SelectedComponent?.Index ?? -1;

    /// <summary>
    /// Gets whether the item is shown: it can be picked and its name contains the search text, or one of its children is shown.
    /// </summary>
    internal bool IsShown { get; }
}

/// <summary>
/// A component that can be picked in an <see cref="EntityPickerViewModel"/>.
/// </summary>
/// <param name="Index">The index of the component in the components of its entity.</param>
/// <param name="Component">The component.</param>
public sealed record PickableComponentViewModel(int Index, EntityComponent Component)
{
    /// <summary>
    /// Gets the text of the component in the list: its type and index, as in WPF.
    /// </summary>
    public string DisplayName => $"{Component.GetType().Name} (Index: {Index})";
}
