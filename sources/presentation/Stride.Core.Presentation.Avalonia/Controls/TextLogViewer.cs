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

namespace Stride.Core.Assets.Editor.Avalonia.Controls;

[TemplatePart(Name = "PART_LogText", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_ClearLog", Type = typeof(Button))]
[TemplatePart(Name = "PART_PreviousResult", Type = typeof(Button))]
[TemplatePart(Name = "PART_NextResult", Type = typeof(Button))]
public sealed class TextLogViewer : TemplatedControl
{
    private IObservableCollection<ILogMessage> messages;
    private TextBlock? textBlock;

    /// <summary>
    /// Identifies the <see cref="LogMessages"/> dependency property.
    /// </summary>
    public static readonly DirectProperty<TextLogViewer, IObservableCollection<ILogMessage>> LogMessagesProperty =
        AvaloniaProperty.RegisterDirect<TextLogViewer, IObservableCollection<ILogMessage>>(nameof(LogMessages), o => o.LogMessages, (o, v) => o.LogMessages = v);

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
    public IObservableCollection<ILogMessage> LogMessages
    {
        get => messages;
        set => SetAndRaise(LogMessagesProperty, ref messages, value);
    }

    /// <summary>
    /// Gets or sets the brush used to emphasize debug messages.
    /// </summary>
    public Brush DebugBrush
    {
        get { return GetValue(DebugBrushProperty); }
        set { SetValue(DebugBrushProperty, value); }
    }

    /// <summary>
    /// Gets or sets the brush used to emphasize verbose messages.
    /// </summary>
    public Brush VerboseBrush
    {
        get { return GetValue(VerboseBrushProperty); }
        set { SetValue(VerboseBrushProperty, value); }
    }

    /// <summary>
    /// Gets or sets the brush used to emphasize info messages.
    /// </summary>
    public Brush InfoBrush
    {
        get { return GetValue(InfoBrushProperty); }
        set { SetValue(InfoBrushProperty, value); }
    }

    /// <summary>
    /// Gets or sets the brush used to emphasize warning messages.
    /// </summary>
    public Brush WarningBrush
    {
        get { return GetValue(WarningBrushProperty); }
        set { SetValue(WarningBrushProperty, value); }
    }

    /// <summary>
    /// Gets or sets the brush used to emphasize error messages.
    /// </summary>
    public Brush ErrorBrush
    {
        get { return GetValue(ErrorBrushProperty); }
        set { SetValue(ErrorBrushProperty, value); }
    }

    /// <summary>
    /// Gets or sets the brush used to emphasize fatal messages.
    /// </summary>
    public Brush FatalBrush
    {
        get { return GetValue(FatalBrushProperty); }
        set { SetValue(FatalBrushProperty, value); }
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
        get { return GetValue(ShowStacktraceProperty); }
        set { SetValue(ShowStacktraceProperty, value); }
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        textBlock = e.NameScope.Find<TextBlock>("PART_LogText");

        if (e.NameScope.Find<Button>("PART_ClearLog") is Button clearLogButton)
        {
            clearLogButton.Click += ClearLog;
        }

        if (e.NameScope.Find<Button>("PART_PreviousResult") is Button previousResultButton)
        {
            previousResultButton.Click += PreviousResultClicked;
        }

        if (e.NameScope.Find<Button>("PART_NextResult") is Button nextResultButton)
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
        foreach (var message in logMessages.Where(x => ShouldDisplayMessage(x.Type)))
        {
            sb.Clear();
            if (message.Module != null)
            {
                sb.AppendFormat("[{0}]: ", message.Module);
            }

            sb.AppendFormat("{0}: {1}", message.Type, message.Text);
            var ex = message.ExceptionInfo;
            if (ex != null)
            {
                if (ShowStacktrace)
                {
                    sb.AppendFormat("{0}{1}{0}", Environment.NewLine, ex);
                }
                else
                {
                    sb.Append(" (...)");
                }
            }
            sb.AppendLine();

            var lineText = sb.ToString();
            var logColor = GetLogColor(message.Type);
            textBlock.Inlines.Add(new Run(lineText) { Foreground = logColor });

            // FIXME xplat-editor search
        }
    }

    private void ClearLog(object? sender, RoutedEventArgs e)
    {
        LogMessages.Clear();
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

        // FIXME xplat-editor search
        //ClearSearchResults();
        textBlock.Inlines?.Clear();
        if (LogMessages != null)
        {
            // Make a copy
            var logMessages = LogMessages.ToList();
            AppendText(logMessages);
        }
    }
    
    private void SelectPreviousOccurrence()
    {
        // FIXME xplat-editor search
    }

    private void SelectNextOccurrence()
    {
        // FIXME xplat-editor search
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
}
