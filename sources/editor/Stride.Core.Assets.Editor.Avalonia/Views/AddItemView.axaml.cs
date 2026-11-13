// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

/// <summary>
/// The list of an <see cref="AddItemTemplateCollectionViewModel"/> (the Add asset button and dialog): a search box, the groups
/// and the templates of the selected group. A click on a template validates it: <see cref="ItemValidated"/> is raised and
/// <see cref="AddItemCommand"/> runs with the template.
/// </summary>
public sealed partial class AddItemView : UserControl
{
    public static readonly StyledProperty<ICommand?> AddItemCommandProperty =
        AvaloniaProperty.Register<AddItemView, ICommand?>(nameof(AddItemCommand));

    public static readonly StyledProperty<ICommand?> SelectFilesToCreateItemCommandProperty =
        AvaloniaProperty.Register<AddItemView, ICommand?>(nameof(SelectFilesToCreateItemCommand));

    public AddItemView()
    {
        InitializeComponent();
        Templates.AddHandler(PointerReleasedEvent, OnTemplatePointerReleased, RoutingStrategies.Bubble);
        // The list box itself handles Enter (it selects the focused item), so a tunnel handler sees the key first
        Templates.AddHandler(KeyDownEvent, OnTemplatesKeyDown, RoutingStrategies.Tunnel);
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// Raised when the user validated a template (a click or Enter on it), before <see cref="AddItemCommand"/> runs.
    /// </summary>
    public event EventHandler<TemplateValidatedEventArgs>? ItemValidated;

    /// <summary>
    /// Raised once the user clicked the import button and <see cref="SelectFilesToCreateItemCommand"/> started: a flyout hosting
    /// the view closes then.
    /// </summary>
    public event EventHandler? ImportRequested;

    /// <summary>
    /// The command run with the validated template (an <see cref="ITemplateDescriptionViewModel"/>).
    /// </summary>
    public ICommand? AddItemCommand
    {
        get => GetValue(AddItemCommandProperty);
        set => SetValue(AddItemCommandProperty, value);
    }

    /// <summary>
    /// The command of the import button, which is hidden when there is none.
    /// </summary>
    public ICommand? SelectFilesToCreateItemCommand
    {
        get => GetValue(SelectFilesToCreateItemCommandProperty);
        set => SetValue(SelectFilesToCreateItemCommandProperty, value);
    }

    /// <summary>
    /// Gets the templates shown.
    /// </summary>
    public AddItemTemplateCollectionViewModel? Collection => DataContext as AddItemTemplateCollectionViewModel;

    /// <summary>
    /// Clears the search and the selection, as the WPF control does when it loads: every template is listed again.
    /// </summary>
    public void Reset()
    {
        if (Collection is { } collection)
        {
            collection.SearchToken = string.Empty;
            collection.SelectedTemplate = null;
            collection.SelectedGroup = collection.RootGroup;
        }
        Groups.SelectedItem = null;
        SearchBox.Focus();
    }

    /// <summary>
    /// Validates a template: raises <see cref="ItemValidated"/> and runs <see cref="AddItemCommand"/>.
    /// </summary>
    public void Validate(ITemplateDescriptionViewModel template)
    {
        ArgumentNullException.ThrowIfNull(template);
        ItemValidated?.Invoke(this, new TemplateValidatedEventArgs(template));
        if (AddItemCommand is { } command && command.CanExecute(template))
            command.Execute(template);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // The groups cannot stay selected while a search lists the templates of every group
        if (Collection is { } collection)
            collection.PropertyChanged += OnCollectionPropertyChanged;
    }

    private void OnCollectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AddItemTemplateCollectionViewModel.IsSearching) && Collection is { IsSearching: true })
            Groups.SelectedItem = null;
    }

    private void OnGroupSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Only a group picked in the list changes the selected group: the root group (every template) is not listed, so the
        // list shows no selection then and must not clear the view model's
        if (Groups.SelectedItem is TemplateDescriptionGroupViewModel group && Collection is { } collection && !collection.IsSearching)
            collection.SelectedGroup = group;
    }

    private void OnTemplatePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left)
            return;
        if ((e.Source as Visual)?.FindAncestorOfType<ListBoxItem>(true)?.DataContext is ITemplateDescriptionViewModel template)
            Validate(template);
    }

    private void OnTemplatesKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Collection?.SelectedTemplate is { } template)
        {
            e.Handled = true;
            Validate(template);
        }
    }

    private void OnImportClick(object? sender, RoutedEventArgs e)
    {
        // The button runs its command after this handler: the event is posted so that a host hiding the view (a flyout) does not
        // detach it before the command started
        Dispatcher.UIThread.Post(() => ImportRequested?.Invoke(this, EventArgs.Empty));
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        // Enter in the search box validates the selected template, or the first match of the search
        if (e.Key == Key.Enter && Collection is { } collection && (collection.SelectedTemplate ?? collection.Templates.FirstOrDefault()) is { } template)
        {
            e.Handled = true;
            Validate(template);
        }
    }
}

/// <summary>
/// The template the user validated in an <see cref="AddItemView"/>.
/// </summary>
public sealed class TemplateValidatedEventArgs : EventArgs
{
    public TemplateValidatedEventArgs(ITemplateDescriptionViewModel template)
    {
        Template = template;
    }

    public ITemplateDescriptionViewModel Template { get; }
}
