// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Collections;

namespace Stride.Core.Assets.Editor.Avalonia.Controls;

/// <summary>
/// Displays a collection of <see cref="ILogMessage"/> in a grid: the type of each message, its exception, its text and the asset it is
/// about, as the grid log viewer of the WPF editor.
/// </summary>
[TemplatePart(Name = "PART_LogGridView", Type = typeof(DataGrid))]
public sealed class GridLogViewer : TemplatedControl
{
    private IObservableList<ILogMessage>? logMessages;
    private DataGrid? logGridView;

    static GridLogViewer()
    {
        ShowDebugMessagesProperty.Changed.AddClassHandler<GridLogViewer>((x, _) => x.ApplyFilters());
        ShowVerboseMessagesProperty.Changed.AddClassHandler<GridLogViewer>((x, _) => x.ApplyFilters());
        ShowInfoMessagesProperty.Changed.AddClassHandler<GridLogViewer>((x, _) => x.ApplyFilters());
        ShowWarningMessagesProperty.Changed.AddClassHandler<GridLogViewer>((x, _) => x.ApplyFilters());
        ShowErrorMessagesProperty.Changed.AddClassHandler<GridLogViewer>((x, _) => x.ApplyFilters());
        ShowFatalMessagesProperty.Changed.AddClassHandler<GridLogViewer>((x, _) => x.ApplyFilters());
    }

    /// <summary>
    /// Identifies the <see cref="LogMessages"/> dependency property.
    /// </summary>
    public static readonly DirectProperty<GridLogViewer, IObservableList<ILogMessage>?> LogMessagesProperty =
        AvaloniaProperty.RegisterDirect<GridLogViewer, IObservableList<ILogMessage>?>(nameof(LogMessages), o => o.LogMessages, (o, v) => o.LogMessages = v);

    /// <summary>
    /// Identifies the <see cref="IsToolBarVisible"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> IsToolBarVisibleProperty =
        AvaloniaProperty.Register<GridLogViewer, bool>(nameof(IsToolBarVisible), true);

    /// <summary>
    /// Identifies the <see cref="ToolBarContent"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<object?> ToolBarContentProperty =
        AvaloniaProperty.Register<GridLogViewer, object?>(nameof(ToolBarContent));

    /// <summary>
    /// Identifies the <see cref="SelectMessageCommand"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<ICommand?> SelectMessageCommandProperty =
        AvaloniaProperty.Register<GridLogViewer, ICommand?>(nameof(SelectMessageCommand));

    /// <summary>
    /// Identifies the <see cref="ShowDebugMessages"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowDebugMessagesProperty =
        AvaloniaProperty.Register<GridLogViewer, bool>(nameof(ShowDebugMessages), true);

    /// <summary>
    /// Identifies the <see cref="ShowVerboseMessages"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowVerboseMessagesProperty =
        AvaloniaProperty.Register<GridLogViewer, bool>(nameof(ShowVerboseMessages), true);

    /// <summary>
    /// Identifies the <see cref="ShowInfoMessages"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowInfoMessagesProperty =
        AvaloniaProperty.Register<GridLogViewer, bool>(nameof(ShowInfoMessages), true);

    /// <summary>
    /// Identifies the <see cref="ShowWarningMessages"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowWarningMessagesProperty =
        AvaloniaProperty.Register<GridLogViewer, bool>(nameof(ShowWarningMessages), true);

    /// <summary>
    /// Identifies the <see cref="ShowErrorMessages"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowErrorMessagesProperty =
        AvaloniaProperty.Register<GridLogViewer, bool>(nameof(ShowErrorMessages), true);

    /// <summary>
    /// Identifies the <see cref="ShowFatalMessages"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowFatalMessagesProperty =
        AvaloniaProperty.Register<GridLogViewer, bool>(nameof(ShowFatalMessages), true);

    /// <summary>
    /// Gets or sets the collection of <see cref="ILogMessage"/> to display.
    /// </summary>
    public IObservableList<ILogMessage>? LogMessages
    {
        get => logMessages;
        set
        {
            var previous = logMessages;
            if (SetAndRaise(LogMessagesProperty, ref logMessages, value))
            {
                if (previous is not null)
                    previous.CollectionChanged -= LogMessagesCollectionChanged;
                if (value is not null)
                    value.CollectionChanged += LogMessagesCollectionChanged;
                ApplyFilters();
            }
        }
    }

    /// <summary>
    /// Gets the messages that pass the filters, which the grid shows.
    /// </summary>
    public ObservableList<ILogMessage> FilteredLogMessages { get; } = [];

    /// <summary>
    /// Gets or sets whether the tool bar should be visible.
    /// </summary>
    public bool IsToolBarVisible
    {
        get => GetValue(IsToolBarVisibleProperty);
        set => SetValue(IsToolBarVisibleProperty, value);
    }

    /// <summary>
    /// Gets or sets more filters, shown in the tool bar before the type filters.
    /// </summary>
    public object? ToolBarContent
    {
        get => GetValue(ToolBarContentProperty);
        set => SetValue(ToolBarContentProperty, value);
    }

    /// <summary>
    /// Gets or sets the command executed with the message that is double-clicked, for example to select its asset.
    /// </summary>
    public ICommand? SelectMessageCommand
    {
        get => GetValue(SelectMessageCommandProperty);
        set => SetValue(SelectMessageCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the log viewer should display debug messages.
    /// </summary>
    public bool ShowDebugMessages
    {
        get => GetValue(ShowDebugMessagesProperty);
        set => SetValue(ShowDebugMessagesProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the log viewer should display verbose messages.
    /// </summary>
    public bool ShowVerboseMessages
    {
        get => GetValue(ShowVerboseMessagesProperty);
        set => SetValue(ShowVerboseMessagesProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the log viewer should display info messages.
    /// </summary>
    public bool ShowInfoMessages
    {
        get => GetValue(ShowInfoMessagesProperty);
        set => SetValue(ShowInfoMessagesProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the log viewer should display warning messages.
    /// </summary>
    public bool ShowWarningMessages
    {
        get => GetValue(ShowWarningMessagesProperty);
        set => SetValue(ShowWarningMessagesProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the log viewer should display error messages.
    /// </summary>
    public bool ShowErrorMessages
    {
        get => GetValue(ShowErrorMessagesProperty);
        set => SetValue(ShowErrorMessagesProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the log viewer should display fatal messages.
    /// </summary>
    public bool ShowFatalMessages
    {
        get => GetValue(ShowFatalMessagesProperty);
        set => SetValue(ShowFatalMessagesProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (logGridView is not null)
            logGridView.DoubleTapped -= GridDoubleTapped;

        logGridView = e.NameScope.Find<DataGrid>("PART_LogGridView")
            ?? throw new InvalidOperationException("A part named 'PART_LogGridView' must be present in the ControlTemplate, and must be of type 'DataGrid'.");
        logGridView.DoubleTapped += GridDoubleTapped;
    }

    private void GridDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (logGridView?.SelectedItem is ILogMessage message && SelectMessageCommand is { } command && command.CanExecute(message))
            command.Execute(message);
    }

    private void LogMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (LogMessages is null)
            return;

        // Apply only the change: the messages are added one by one, and filtering them all again each time is quadratic
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems?.Count == 1 && e.NewStartingIndex >= 0:
                var newMessage = (ILogMessage)e.NewItems[0]!;
                if (IsMessageVisible(newMessage))
                {
                    if (e.NewStartingIndex == LogMessages.Count - 1)
                        FilteredLogMessages.Add(newMessage);
                    else
                        FilteredLogMessages.Insert(CountVisibleMessages(e.NewStartingIndex), newMessage);
                }
                break;
            case NotifyCollectionChangedAction.Remove when e.OldItems?.Count == 1 && e.OldStartingIndex >= 0:
                if (IsMessageVisible((ILogMessage)e.OldItems[0]!))
                {
                    if (e.OldStartingIndex == LogMessages.Count)
                        FilteredLogMessages.RemoveAt(FilteredLogMessages.Count - 1);
                    else
                        FilteredLogMessages.RemoveAt(CountVisibleMessages(e.OldStartingIndex));
                }
                break;
            default:
                ApplyFilters();
                break;
        }
    }

    /// <summary>
    /// Counts the visible messages before the given index of <see cref="LogMessages"/>, which is the index in <see cref="FilteredLogMessages"/> of a message at this index.
    /// </summary>
    private int CountVisibleMessages(int index)
    {
        var count = 0;
        for (var i = 0; i < index; ++i)
        {
            if (IsMessageVisible(LogMessages![i]))
                ++count;
        }
        return count;
    }

    private void ApplyFilters()
    {
        FilteredLogMessages.Clear();
        if (LogMessages is not null)
            FilteredLogMessages.AddRange(LogMessages.Where(IsMessageVisible));
    }

    private bool IsMessageVisible(ILogMessage message)
    {
        return message.IsDebug() && ShowDebugMessages
            || message.IsError() && ShowErrorMessages
            || message.IsFatal() && ShowFatalMessages
            || message.IsInfo() && ShowInfoMessages
            || message.IsVerbose() && ShowVerboseMessages
            || message.IsWarning() && ShowWarningMessages;
    }
}
