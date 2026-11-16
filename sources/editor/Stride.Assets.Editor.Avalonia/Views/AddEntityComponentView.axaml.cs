// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// The Add component button of the components of an entity in the property grid. It drops down the components that the selected
/// entities can all take, grouped by category, with a search box; a click or Enter on a component adds it with
/// <see cref="AddNewItemCommand"/>.
/// </summary>
/// <remarks>
/// The list itself is a <see cref="NodeTypePickerViewModel"/>. A category is shown when the pointer rests on it, or when it is
/// clicked; it stays shown while the pointer is over its components.
/// </remarks>
public sealed partial class AddEntityComponentView : UserControl
{
    public static readonly StyledProperty<IEnumerable<AbstractNodeType>?> AvailableComponentTypesProperty =
        AvaloniaProperty.Register<AddEntityComponentView, IEnumerable<AbstractNodeType>?>(nameof(AvailableComponentTypes));

    public static readonly StyledProperty<IEnumerable<AbstractNodeTypeGroup>?> AvailableComponentTypeGroupsProperty =
        AvaloniaProperty.Register<AddEntityComponentView, IEnumerable<AbstractNodeTypeGroup>?>(nameof(AvailableComponentTypeGroups));

    public static readonly StyledProperty<ICommand?> AddNewItemCommandProperty =
        AvaloniaProperty.Register<AddEntityComponentView, ICommand?>(nameof(AddNewItemCommand));

    /// <summary>
    /// How long the pointer rests on a category before its components are shown.
    /// </summary>
    internal static readonly TimeSpan HoverDelay = TimeSpan.FromSeconds(0.2);

    private readonly DispatcherTimer hoverTimer;
    private AbstractNodeTypeGroup? hoveredGroup;

    public AddEntityComponentView()
    {
        InitializeComponent();
        PickerRoot.DataContext = Picker;
        hoverTimer = new DispatcherTimer { Interval = HoverDelay };
        hoverTimer.Tick += OnHoverTimerTick;

        Groups.SelectionChanged += OnGroupSelectionChanged;
        Groups.AddHandler(PointerMovedEvent, OnGroupsPointerMoved, RoutingStrategies.Bubble, true);
        Groups.PointerExited += OnGroupsPointerExited;
        TypeList.PointerEntered += OnTypeListPointerEntered;
        TypeList.PointerExited += OnTypeListPointerExited;
        TypeList.AddHandler(PointerReleasedEvent, OnTypeListPointerReleased, RoutingStrategies.Bubble, true);
        // The search box and the list box handle the arrows and Enter themselves, so tunnel handlers see the keys first
        SearchBox.AddHandler(KeyDownEvent, OnSearchBoxKeyDown, RoutingStrategies.Tunnel);
        TypeList.AddHandler(KeyDownEvent, OnTypeListKeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Gets or sets every component type that can be added.
    /// </summary>
    public IEnumerable<AbstractNodeType>? AvailableComponentTypes
    {
        get => GetValue(AvailableComponentTypesProperty);
        set => SetValue(AvailableComponentTypesProperty, value);
    }

    /// <summary>
    /// Gets or sets the component types grouped by category.
    /// </summary>
    public IEnumerable<AbstractNodeTypeGroup>? AvailableComponentTypeGroups
    {
        get => GetValue(AvailableComponentTypeGroupsProperty);
        set => SetValue(AvailableComponentTypeGroupsProperty, value);
    }

    /// <summary>
    /// Gets or sets the command that adds a component, run with its <see cref="AbstractNodeType"/>.
    /// </summary>
    public ICommand? AddNewItemCommand
    {
        get => GetValue(AddNewItemCommandProperty);
        set => SetValue(AddNewItemCommandProperty, value);
    }

    /// <summary>
    /// Gets the list the button drops down.
    /// </summary>
    public NodeTypePickerViewModel Picker { get; } = new();

    /// <summary>
    /// Gets whether the list is dropped down.
    /// </summary>
    public bool IsOpen => AddButton.Flyout?.IsOpen == true;

    /// <summary>
    /// Drops the list down, as a click on the button does.
    /// </summary>
    public void Open()
    {
        if (!IsOpen)
            AddButton.Flyout?.ShowAt(AddButton);
    }

    /// <summary>
    /// Closes the list.
    /// </summary>
    public void Close()
    {
        AddButton.Flyout?.Hide();
    }

    /// <summary>
    /// Adds a component type to the selected entities and closes the list.
    /// </summary>
    /// <param name="type">The component type to add.</param>
    public void Validate(AbstractNodeType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        Close();
        Picker.Add(type);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == AvailableComponentTypesProperty)
            Picker.Types = AvailableComponentTypes?.ToList() ?? [];
        else if (change.Property == AvailableComponentTypeGroupsProperty)
            Picker.Groups = AvailableComponentTypeGroups?.ToList() ?? [];
        else if (change.Property == AddNewItemCommandProperty)
            Picker.AddCommand = AddNewItemCommand;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        hoverTimer.Stop();
        base.OnDetachedFromVisualTree(e);
    }

    private void OnFlyoutOpened(object? sender, EventArgs e)
    {
        AddButton.IsChecked = true;
        SearchBox.Focus();
    }

    private void OnFlyoutClosed(object? sender, EventArgs e)
    {
        AddButton.IsChecked = false;
        hoverTimer.Stop();
        hoveredGroup = null;
        Groups.SelectedItem = null;
        Picker.Reset();
    }

    private void OnGroupSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // A clicked category is shown at once; the selection is cleared when the pointer leaves the components
        if (Groups.SelectedItem is AbstractNodeTypeGroup group && !Picker.IsSearching)
            Picker.SelectedGroup = group;
    }

    private void OnGroupsPointerMoved(object? sender, PointerEventArgs e)
    {
        if (Picker.IsSearching)
            return;

        // While a category is selected, it stays shown
        var group = Groups.SelectedItem as AbstractNodeTypeGroup
                    ?? (e.Source as Visual)?.FindAncestorOfType<ListBoxItem>(true)?.DataContext as AbstractNodeTypeGroup;
        if (ReferenceEquals(group, hoveredGroup))
            return;

        hoveredGroup = group;
        hoverTimer.Stop();
        if (group is not null)
            hoverTimer.Start();
    }

    private void OnGroupsPointerExited(object? sender, PointerEventArgs e)
    {
        // The category under the pointer when it left stays shown, so that the pointer can move to its components
        hoverTimer.Stop();
        hoveredGroup = null;
    }

    private void OnHoverTimerTick(object? sender, EventArgs e)
    {
        hoverTimer.Stop();
        if (hoveredGroup is not null && !Picker.IsSearching)
            Picker.SelectedGroup = hoveredGroup;
    }

    private void OnTypeListPointerEntered(object? sender, PointerEventArgs e)
    {
        // The shown category is selected while the pointer is over its components, so that crossing other categories on the way
        // back does not change it
        hoverTimer.Stop();
        if (!Picker.IsSearching && Picker.SelectedGroup is { } group)
            Groups.SelectedItem = group;
    }

    private void OnTypeListPointerExited(object? sender, PointerEventArgs e)
    {
        if (!Picker.IsSearching)
            Groups.SelectedItem = null;
    }

    private void OnTypeListPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left)
            return;
        if ((e.Source as Visual)?.FindAncestorOfType<ListBoxItem>(true)?.DataContext is AbstractNodeType type)
        {
            e.Handled = true;
            Validate(type);
        }
    }

    private void OnSearchBoxKeyDown(object? sender, KeyEventArgs e)
    {
        var shown = Picker.ShownTypes;
        switch (e.Key)
        {
            case Key.Enter:
                // The selected component, or the first one listed
                if ((Picker.SelectedType ?? shown.FirstOrDefault()) is { } type)
                    Validate(type);
                e.Handled = true;
                break;

            case Key.Escape:
                Close();
                e.Handled = true;
                break;

            case Key.Up:
            case Key.Down:
            case Key.PageUp:
            case Key.PageDown:
                if (shown.Count == 0)
                    return;
                var index = Picker.SelectedType is { } selected ? IndexOf(shown, selected) : -1;
                var step = e.Key switch
                {
                    Key.Up => -1,
                    Key.Down => 1,
                    Key.PageUp => -10,
                    _ => 10,
                };
                index = Math.Clamp(index + step, 0, shown.Count - 1);
                Picker.SelectedType = shown[index];
                TypeList.ScrollIntoView(index);
                e.Handled = true;
                break;
        }
    }

    private void OnTypeListKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Picker.SelectedType is { } type)
        {
            e.Handled = true;
            Validate(type);
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
        }
    }

    private static int IndexOf(IReadOnlyList<AbstractNodeType> types, AbstractNodeType type)
    {
        for (var i = 0; i < types.Count; ++i)
        {
            if (Equals(types[i], type))
                return i;
        }
        return -1;
    }
}
