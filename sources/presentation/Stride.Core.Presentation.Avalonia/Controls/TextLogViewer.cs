// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Collections;

namespace Stride.Core.Presentation.Avalonia.Controls;

[TemplatePart(Name = "PART_LogText", Type = typeof(SelectableTextBlock))]
[TemplatePart(Name = "PART_ClearLog", Type = typeof(Button))]
[TemplatePart(Name = "PART_PreviousResult", Type = typeof(Button))]
[TemplatePart(Name = "PART_NextResult", Type = typeof(Button))]
public sealed class TextLogViewer : TemplatedControl
{
    private IObservableCollection<ILogMessage>? messages;
    private SelectableTextBlock? textBlock;
    private readonly List<SearchRange> searchMatches = [];
    private SearchRange previousRange;
    private int currentResult;

    static TextLogViewer()
    {
        SearchTokenProperty.Changed.AddClassHandler<TextLogViewer>(OnSearchTokenChanged);
        SearchMatchCaseProperty.Changed.AddClassHandler<TextLogViewer>(OnSearchTokenChanged);
        SearchMatchWordProperty.Changed.AddClassHandler<TextLogViewer>(OnSearchTokenChanged);

        DebugBrushProperty.Changed.AddClassHandler<TextLogViewer>(OnTextPropertyChanged);
        VerboseBrushProperty.Changed.AddClassHandler<TextLogViewer>(OnTextPropertyChanged);
        InfoBrushProperty.Changed.AddClassHandler<TextLogViewer>(OnTextPropertyChanged);
        WarningBrushProperty.Changed.AddClassHandler<TextLogViewer>(OnTextPropertyChanged);
        ErrorBrushProperty.Changed.AddClassHandler<TextLogViewer>(OnTextPropertyChanged);
        FatalBrushProperty.Changed.AddClassHandler<TextLogViewer>(OnTextPropertyChanged);

        ShowDebugMessagesProperty.Changed.AddClassHandler<TextLogViewer>(OnTextPropertyChanged);
        ShowVerboseMessagesProperty.Changed.AddClassHandler<TextLogViewer>(OnTextPropertyChanged);
        ShowInfoMessagesProperty.Changed.AddClassHandler<TextLogViewer>(OnTextPropertyChanged);
        ShowWarningMessagesProperty.Changed.AddClassHandler<TextLogViewer>(OnTextPropertyChanged);
        ShowErrorMessagesProperty.Changed.AddClassHandler<TextLogViewer>(OnTextPropertyChanged);
        ShowFatalMessagesProperty.Changed.AddClassHandler<TextLogViewer>(OnTextPropertyChanged);
        ShowStacktraceProperty.Changed.AddClassHandler<TextLogViewer>(OnTextPropertyChanged);
    }

    /// <summary>
    /// Identifies the <see cref="LogMessages"/> dependency property.
    /// </summary>
    public static readonly DirectProperty<TextLogViewer, IObservableCollection<ILogMessage>?> LogMessagesProperty =
        AvaloniaProperty.RegisterDirect<TextLogViewer, IObservableCollection<ILogMessage>?>(nameof(LogMessages), o => o.LogMessages, (o, v) => o.LogMessages = v);

    /// <summary>
    /// Identifies the <see cref="IsToolBarVisible"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> IsToolBarVisibleProperty =
        AvaloniaProperty.Register<TextLogViewer, bool>(nameof(IsToolBarVisible), true);

    /// <summary>
    /// Identifies the <see cref="CanClearLog"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> CanClearLogProperty =
        AvaloniaProperty.Register<TextLogViewer, bool>(nameof(CanClearLog), true);

    /// <summary>
    /// Identifies the <see cref="CanFilterLog"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> CanFilterLogProperty =
        AvaloniaProperty.Register<TextLogViewer, bool>(nameof(CanFilterLog), true);

    /// <summary>
    /// Identifies the <see cref="CanSearchLog"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> CanSearchLogProperty =
        AvaloniaProperty.Register<TextLogViewer, bool>(nameof(CanSearchLog), true);

    /// <summary>
    /// Identifies the <see cref="SearchToken"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<string> SearchTokenProperty =
        AvaloniaProperty.Register<TextLogViewer, string>(nameof(SearchToken));

    /// <summary>
    /// Identifies the <see cref="SearchMatchCase"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> SearchMatchCaseProperty =
        AvaloniaProperty.Register<TextLogViewer, bool>(nameof(SearchMatchCase));

    /// <summary>
    /// Identifies the <see cref="SearchMatchWord"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> SearchMatchWordProperty =
        AvaloniaProperty.Register<TextLogViewer, bool>(nameof(SearchMatchWord));

    /// <summary>
    /// Identifies the <see cref="DebugBrush"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<Brush> DebugBrushProperty =
        AvaloniaProperty.Register<TextLogViewer, Brush>(nameof(DebugBrush), new SolidColorBrush(Colors.White));

    /// <summary>
    /// Identifies the <see cref="VerboseBrush"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<Brush> VerboseBrushProperty =
        AvaloniaProperty.Register<TextLogViewer, Brush>(nameof(VerboseBrush), new SolidColorBrush(Colors.White));

    /// <summary>
    /// Identifies the <see cref="InfoBrush"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<Brush> InfoBrushProperty =
        AvaloniaProperty.Register<TextLogViewer, Brush>(nameof(InfoBrush), new SolidColorBrush(Colors.White));

    /// <summary>
    /// Identifies the <see cref="WarningBrush"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<Brush> WarningBrushProperty =
        AvaloniaProperty.Register<TextLogViewer, Brush>(nameof(WarningBrush), new SolidColorBrush(Colors.White));

    /// <summary>
    /// Identifies the <see cref="ErrorBrush"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<Brush> ErrorBrushProperty =
        AvaloniaProperty.Register<TextLogViewer, Brush>(nameof(ErrorBrush), new SolidColorBrush(Colors.White));

    /// <summary>
    /// Identifies the <see cref="FatalBrush"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<Brush> FatalBrushProperty =
        AvaloniaProperty.Register<TextLogViewer, Brush>(nameof(FatalBrush), new SolidColorBrush(Colors.White));

    /// <summary>
    /// Identifies the <see cref="ShowDebugMessages"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowDebugMessagesProperty =
        AvaloniaProperty.Register<TextLogViewer, bool>(nameof(ShowDebugMessages), true);

    /// <summary>
    /// Identifies the <see cref="ShowVerboseMessages"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowVerboseMessagesProperty =
        AvaloniaProperty.Register<TextLogViewer, bool>(nameof(ShowVerboseMessages), true);

    /// <summary>
    /// Identifies the <see cref="ShowInfoMessages"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowInfoMessagesProperty =
        AvaloniaProperty.Register<TextLogViewer, bool>(nameof(ShowInfoMessages), true);

    /// <summary>
    /// Identifies the <see cref="ShowWarningMessages"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowWarningMessagesProperty =
        AvaloniaProperty.Register<TextLogViewer, bool>(nameof(ShowWarningMessages), true);

    /// <summary>
    /// Identifies the <see cref="ShowErrorMessages"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowErrorMessagesProperty =
        AvaloniaProperty.Register<TextLogViewer, bool>(nameof(ShowErrorMessages), true);

    /// <summary>
    /// Identifies the <see cref="ShowFatalMessages"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowFatalMessagesProperty =
        AvaloniaProperty.Register<TextLogViewer, bool>(nameof(ShowFatalMessages), true);

    /// <summary>
    /// Identifies the <see cref="ShowStacktrace"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowStacktraceProperty =
        AvaloniaProperty.Register<TextLogViewer, bool>(nameof(ShowStacktrace), true);

    /// <summary>
    /// Gets or sets the collection of <see cref="ILogMessage"/> to display.
    /// </summary>
    public IObservableCollection<ILogMessage>? LogMessages
    {
        get => messages;
        set => SetAndRaise(LogMessagesProperty, ref messages, value);
    }

    /// <summary>
    /// Gets or sets whether the tool bar should be visible.
    /// </summary>
    public bool IsToolBarVisible
    {
        get => GetValue(IsToolBarVisibleProperty);
        set => SetValue(IsToolBarVisibleProperty, value);
    }

    /// <summary>
    /// Gets or sets whether it is possible to clear the log text.
    /// </summary>
    public bool CanClearLog
    {
        get => GetValue(CanClearLogProperty);
        set => SetValue(CanClearLogProperty, value);
    }

    /// <summary>
    /// Gets or sets whether it is possible to filter the log text.
    /// </summary>
    public bool CanFilterLog
    {
        get => GetValue(CanFilterLogProperty);
        set => SetValue(CanFilterLogProperty, value);
    }

    /// <summary>
    /// Gets or sets whether it is possible to search the log text.
    /// </summary>
    public bool CanSearchLog
    {
        get => GetValue(CanSearchLogProperty);
        set => SetValue(CanSearchLogProperty, value);
    }

    /// <summary>
    /// Gets or sets the current search token.
    /// </summary>
    public string SearchToken
    {
        get => GetValue(SearchTokenProperty);
        set => SetValue(SearchTokenProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the search result should match the case.
    /// </summary>
    public bool SearchMatchCase
    {
        get => GetValue(SearchMatchCaseProperty);
        set => SetValue(SearchMatchCaseProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the search result should match whole words only.
    /// </summary>
    public bool SearchMatchWord
    {
        get => GetValue(SearchMatchWordProperty);
        set => SetValue(SearchMatchWordProperty, value);
    }

    /// <summary>
    /// Gets or sets the brush used to emphasize debug messages.
    /// </summary>
    public Brush DebugBrush
    {
        get => GetValue(DebugBrushProperty);
        set => SetValue(DebugBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the brush used to emphasize verbose messages.
    /// </summary>
    public Brush VerboseBrush
    {
        get => GetValue(VerboseBrushProperty);
        set => SetValue(VerboseBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the brush used to emphasize info messages.
    /// </summary>
    public Brush InfoBrush
    {
        get => GetValue(InfoBrushProperty);
        set => SetValue(InfoBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the brush used to emphasize warning messages.
    /// </summary>
    public Brush WarningBrush
    {
        get => GetValue(WarningBrushProperty);
        set => SetValue(WarningBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the brush used to emphasize error messages.
    /// </summary>
    public Brush ErrorBrush
    {
        get => GetValue(ErrorBrushProperty);
        set => SetValue(ErrorBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the brush used to emphasize fatal messages.
    /// </summary>
    public Brush FatalBrush
    {
        get => GetValue(FatalBrushProperty);
        set => SetValue(FatalBrushProperty, value);
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

    /// <summary>
    /// Gets or sets whether the log viewer should display fatal messages.
    /// </summary>
    public bool ShowStacktrace
    {
        get => GetValue(ShowStacktraceProperty);
        set => SetValue(ShowStacktraceProperty, value);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        textBlock = e.NameScope.Find<SelectableTextBlock>("PART_LogText");

        if (e.NameScope.Find<Button>("PART_ClearLog") is { } clearLogButton)
        {
            clearLogButton.Click += ClearLog;
        }

        if (e.NameScope.Find<Button>("PART_PreviousResult") is { } previousResultButton)
        {
            previousResultButton.Click += PreviousResultClicked;
        }

        if (e.NameScope.Find<Button>("PART_NextResult") is { } nextResultButton)
        {
            nextResultButton.Click += NextResultClicked;
        }

        ResetText();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LogMessagesProperty)
        {
            LogMessagesPropertyChanged((IObservableCollection<ILogMessage>?)change.OldValue, (IObservableCollection<ILogMessage>?)change.NewValue);
        }
    }

    private void AppendText(IEnumerable<ILogMessage> logMessages)
    {
        if (textBlock?.Inlines == null) return;

        var sb = new StringBuilder();
        var stringComparison = SearchMatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var searchToken = SearchToken;
        foreach (var message in logMessages.Where(x => ShouldDisplayMessage(x.Type)))
        {
            sb.Clear();
            if (message.Module != null)
            {
                sb.Append($"[{message.Module}]: ");
            }

            sb.Append($"{message.Type}: {message.Text}");
            var ex = message.ExceptionInfo;
            if (ex != null)
            {
                if (ShowStacktrace)
                {
                    sb.Append($"{Environment.NewLine}{ex}{Environment.NewLine}");
                }
                else
                {
                    sb.Append(" (...)");
                }
            }
            sb.AppendLine();

            var lineText = sb.ToString().AsSpan();
            var logColor = GetLogColor(message.Type);
            if (string.IsNullOrEmpty(searchToken))
            {
                textBlock.Inlines.Add(new Run(lineText.ToString()) { Foreground = logColor });
            }
            else
            {
                do
                {
                    var tokenIndex = lineText.IndexOf(searchToken, stringComparison);
                    if (tokenIndex == -1)
                    {
                        textBlock.Inlines.Add(new Run(lineText.ToString()) { Foreground = logColor });
                        previousRange = new SearchRange(previousRange.End, lineText.Length);
                        break;
                    }

                    var acceptResult = true;
                    if (SearchMatchWord && lineText.Length > 1)
                    {
                        if (tokenIndex > 0)
                        {
                            var c = lineText[tokenIndex - 1];
                            if (c is >= 'A' and <= 'Z' or >= 'a' and <= 'z')
                                acceptResult = false;
                        }
                        if (tokenIndex + searchToken.Length < lineText.Length)
                        {
                            var c = lineText[tokenIndex + searchToken.Length];
                            if (c is >= 'A' and <= 'Z' or >= 'a' and <= 'z')
                                acceptResult = false;
                        }
                    }

                    if (acceptResult)
                    {
                        if (tokenIndex > 0)
                        {
                            textBlock.Inlines.Add(new Run(lineText[..tokenIndex].ToString()) { Foreground = logColor });
                            previousRange = new SearchRange(previousRange.End, tokenIndex);
                        }

                        var tokenRun = new Run(lineText[tokenIndex..(tokenIndex + searchToken.Length)].ToString()) { Foreground = Brushes.LightSteelBlue };
                        textBlock.Inlines.Add(tokenRun);
                        var tokenRange = new SearchRange(previousRange.End, searchToken.Length);
                        searchMatches.Add(tokenRange);
                        previousRange = tokenRange;
                    }
                    else
                    {
                        textBlock.Inlines.Add(new Run(lineText[..(tokenIndex + searchToken.Length)].ToString()) { Foreground = logColor });
                        previousRange = new SearchRange(previousRange.End, tokenIndex + searchToken.Length);
                    }
                    lineText = lineText[(tokenIndex + searchToken.Length)..];
                } while (lineText.Length > 0);
            }
        }
    }

    private void ClearLog(object? sender, RoutedEventArgs e)
    {
        LogMessages?.Clear();
    }

    private Brush GetLogColor(LogMessageType type)
    {
        return type switch
        {
            LogMessageType.Debug => DebugBrush,
            LogMessageType.Verbose => VerboseBrush,
            LogMessageType.Info => InfoBrush,
            LogMessageType.Warning => WarningBrush,
            LogMessageType.Error => ErrorBrush,
            LogMessageType.Fatal => FatalBrush,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
    }

    /// <summary>
    /// Raised when the collection of log messages is observable and changes.
    /// </summary>
    private void LogMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // FIXME xplat-editor
        //var shouldScroll = AutoScroll && logTextBox != null && logTextBox.ExtentHeight - logTextBox.ViewportHeight - logTextBox.VerticalOffset < 1.0;

        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            if (e.NewItems != null)
            {
                if (textBlock != null)
                {
                    AppendText(e.NewItems.Cast<ILogMessage>());
                }
            }
        }
        else
        {
            ResetText();
        }

        // FIXME xplat-editor
        //if (shouldScroll)
        //{
        //    // Sometimes crashing with ExecutionEngineException in Window.GetWindowMinMax() if not ran with a dispatcher low priority.
        //    // Note: priority should still be higher than DispatcherPriority.Input so that user input have a chance to scroll.
        //    Dispatcher.InvokeAsync(() => logTextBox.ScrollToEnd(), DispatcherPriority.DataBind);
        //}
    }

    /// <summary>
    /// Called when the <see cref="LogMessages"/> property has changed.
    /// </summary>
    private void LogMessagesPropertyChanged(IObservableCollection<ILogMessage>? oldValue, IObservableCollection<ILogMessage>? newValue)
    {
        if (oldValue != null)
        {
            oldValue.CollectionChanged -= LogMessagesCollectionChanged;
        }
        if (newValue != null)
        {
            newValue.CollectionChanged += LogMessagesCollectionChanged;
        }

        ResetText();
    }

    private void NextResultClicked(object? sender, RoutedEventArgs e)
    {
        SelectNextOccurrence();
    }

    private void PreviousResultClicked(object? sender, RoutedEventArgs e)
    {
        SelectPreviousOccurrence();
    }

    private void ResetText()
    {
        if (textBlock == null) return;

        ClearSearchResults();
        textBlock.Inlines?.Clear();
        if (LogMessages != null)
        {
            // Make a copy
            var logMessages = LogMessages.ToList();
            AppendText(logMessages);
        }
    }

    private void ClearSearchResults()
    {
        searchMatches.Clear();
        previousRange = default;
    }

    private void SelectFirstOccurrence()
    {
        if (searchMatches.Count > 0)
        {
            SelectSearchResult(0);
        }
    }

    private void SelectPreviousOccurrence()
    {
        var count = searchMatches.Count;
        if (count > 0)
        {
            var previousResult = (count + currentResult - 1) % count;
            SelectSearchResult(previousResult);
        }
    }

    private void SelectNextOccurrence()
    {
        var count = searchMatches.Count;
        if (count > 0)
        {
            var nextResult = (currentResult + 1) % count;
            SelectSearchResult(nextResult);
        }
    }

    private void SelectSearchResult(int resultIndex)
    {
        var result = searchMatches[resultIndex];
        textBlock!.SelectionStart = result.Start;
        textBlock!.SelectionEnd = result.End;
        currentResult = resultIndex;
        // FIXME xplat-editor scroll into view
    }

    private bool ShouldDisplayMessage(LogMessageType type)
    {
        return type switch
        {
            LogMessageType.Debug => ShowDebugMessages,
            LogMessageType.Verbose => ShowVerboseMessages,
            LogMessageType.Info => ShowInfoMessages,
            LogMessageType.Warning => ShowWarningMessages,
            LogMessageType.Error => ShowErrorMessages,
            LogMessageType.Fatal => ShowFatalMessages,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
    }

    private static void OnSearchTokenChanged(TextLogViewer sender, AvaloniaPropertyChangedEventArgs _)
    {
        sender.ResetText();
        sender.SelectFirstOccurrence();
    }

    private static void OnTextPropertyChanged(TextLogViewer sender, AvaloniaPropertyChangedEventArgs _)
    {
        sender.ResetText();
    }

    private readonly record struct SearchRange(int Start, int Length)
    {
        public int End => Start + Length;
    }
}
