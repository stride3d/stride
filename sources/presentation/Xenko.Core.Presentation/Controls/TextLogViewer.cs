// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Xenko.Core.Annotations;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Internal;

namespace Xenko.Core.Presentation.Controls
{
    /// <summary>
    /// This control displays a collection of <see cref="ILogMessage"/>.
    /// </summary>
    [TemplatePart(Name = "PART_LogTextBox", Type = typeof(RichTextBox))]
    [TemplatePart(Name = "PART_ClearLog", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PART_PreviousResult", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PART_NextResult", Type = typeof(ButtonBase))]
    public class TextLogViewer : Control
    {
        private readonly List<TextRange> searchMatches = new List<TextRange>();
        private int currentResult;

        /// <summary>
        /// The <see cref="RichTextBox"/> in which the log messages are actually displayed.
        /// </summary>
        private RichTextBox logTextBox;

        /// <summary>
        /// Identifies the <see cref="LogMessages"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LogMessagesProperty = DependencyProperty.Register("LogMessages", typeof(ICollection<ILogMessage>), typeof(TextLogViewer), new PropertyMetadata(null, LogMessagesPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="AutoScroll"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoScrollProperty = DependencyProperty.Register("AutoScroll", typeof(bool), typeof(TextLogViewer), new PropertyMetadata(BooleanBoxes.TrueBox));

        /// <summary>
        /// Identifies the <see cref="IsToolBarVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsToolBarVisibleProperty = DependencyProperty.Register("IsToolBarVisible", typeof(bool), typeof(TextLogViewer), new PropertyMetadata(BooleanBoxes.TrueBox));

        /// <summary>
        /// Identifies the <see cref="CanClearLog"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CanClearLogProperty = DependencyProperty.Register("CanClearLog", typeof(bool), typeof(TextLogViewer), new PropertyMetadata(BooleanBoxes.TrueBox));

        /// <summary>
        /// Identifies the <see cref="CanFilterLog"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CanFilterLogProperty = DependencyProperty.Register("CanFilterLog", typeof(bool), typeof(TextLogViewer), new PropertyMetadata(BooleanBoxes.TrueBox));

        /// <summary>
        /// Identifies the <see cref="CanSearchLog"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CanSearchLogProperty = DependencyProperty.Register("CanSearchLog", typeof(bool), typeof(TextLogViewer), new PropertyMetadata(BooleanBoxes.TrueBox));

        /// <summary>
        /// Identifies the <see cref="SearchToken"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SearchTokenProperty = DependencyProperty.Register("SearchToken", typeof(string), typeof(TextLogViewer), new PropertyMetadata("", SearchTokenChanged));

        /// <summary>
        /// Identifies the <see cref="SearchMatchCase"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SearchMatchCaseProperty = DependencyProperty.Register("SearchMatchCase", typeof(bool), typeof(TextLogViewer), new PropertyMetadata(BooleanBoxes.FalseBox, SearchTokenChanged));

        /// <summary>
        /// Identifies the <see cref="SearchMatchWord"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SearchMatchWordProperty = DependencyProperty.Register("SearchMatchWord", typeof(bool), typeof(TextLogViewer), new PropertyMetadata(BooleanBoxes.FalseBox, SearchTokenChanged));

        /// <summary>
        /// Identifies the <see cref="SearchMatchBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SearchMatchBrushProperty = DependencyProperty.Register("SearchMatchBrush", typeof(Brush), typeof(TextLogViewer), new PropertyMetadata(Brushes.LightSteelBlue, TextPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="DebugBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DebugBrushProperty = DependencyProperty.Register("DebugBrush", typeof(Brush), typeof(TextLogViewer), new PropertyMetadata(Brushes.White, TextPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="VerboseBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VerboseBrushProperty = DependencyProperty.Register("VerboseBrush", typeof(Brush), typeof(TextLogViewer), new PropertyMetadata(Brushes.White, TextPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="InfoBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InfoBrushProperty = DependencyProperty.Register("InfoBrush", typeof(Brush), typeof(TextLogViewer), new PropertyMetadata(Brushes.White, TextPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="WarningBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WarningBrushProperty = DependencyProperty.Register("WarningBrush", typeof(Brush), typeof(TextLogViewer), new PropertyMetadata(Brushes.White, TextPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ErrorBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ErrorBrushProperty = DependencyProperty.Register("ErrorBrush", typeof(Brush), typeof(TextLogViewer), new PropertyMetadata(Brushes.White, TextPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="FatalBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FatalBrushProperty = DependencyProperty.Register("FatalBrush", typeof(Brush), typeof(TextLogViewer), new PropertyMetadata(Brushes.White, TextPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ShowDebugMessages"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowDebugMessagesProperty = DependencyProperty.Register("ShowDebugMessages", typeof(bool), typeof(TextLogViewer), new PropertyMetadata(BooleanBoxes.TrueBox, TextPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ShowVerboseMessages"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowVerboseMessagesProperty = DependencyProperty.Register("ShowVerboseMessages", typeof(bool), typeof(TextLogViewer), new PropertyMetadata(BooleanBoxes.TrueBox, TextPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ShowInfoMessages"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowInfoMessagesProperty = DependencyProperty.Register("ShowInfoMessages", typeof(bool), typeof(TextLogViewer), new PropertyMetadata(BooleanBoxes.TrueBox, TextPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ShowWarningMessages"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowWarningMessagesProperty = DependencyProperty.Register("ShowWarningMessages", typeof(bool), typeof(TextLogViewer), new PropertyMetadata(BooleanBoxes.TrueBox, TextPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ShowErrorMessages"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowErrorMessagesProperty = DependencyProperty.Register("ShowErrorMessages", typeof(bool), typeof(TextLogViewer), new PropertyMetadata(BooleanBoxes.TrueBox, TextPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ShowFatalMessages"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowFatalMessagesProperty = DependencyProperty.Register("ShowFatalMessages", typeof(bool), typeof(TextLogViewer), new PropertyMetadata(BooleanBoxes.TrueBox, TextPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ShowStacktrace"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowStacktraceProperty = DependencyProperty.Register("ShowStacktrace", typeof(bool), typeof(TextLogViewer), new PropertyMetadata(BooleanBoxes.FalseBox, TextPropertyChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="TextLogViewer"/> class.
        /// </summary>
        public TextLogViewer()
        {
            Loaded += (s, e) =>
            {
                try
                {
                    if (AutoScroll)
                        logTextBox?.ScrollToEnd();
                }
                catch (Exception ex)
                {
                    // It happened a few times that ScrollToEnd throws an exception that crashes the whole application.
                    // Let's ignore it if this happens again.
                    ex.Ignore();
                }
            };
        }

        /// <summary>
        /// Gets or sets the collection of <see cref="ILogMessage"/> to display.
        /// </summary>
        public ICollection<ILogMessage> LogMessages { get { return (ICollection<ILogMessage>)GetValue(LogMessagesProperty); } set { SetValue(LogMessagesProperty, value); } }

        /// <summary>
        /// Gets or sets whether the control should automatically scroll when new lines are added when the scrollbar is already at the bottom.
        /// </summary>
        public bool AutoScroll { get { return (bool)GetValue(AutoScrollProperty); } set { SetValue(AutoScrollProperty, value.Box()); } }

        /// <summary>
        /// Gets or sets whether the tool bar should be visible.
        /// </summary>
        public bool IsToolBarVisible { get { return (bool)GetValue(IsToolBarVisibleProperty); } set { SetValue(IsToolBarVisibleProperty, value.Box()); } }

        /// <summary>
        /// Gets or sets whether it is possible to clear the log text.
        /// </summary>
        public bool CanClearLog { get { return (bool)GetValue(CanClearLogProperty); } set { SetValue(CanClearLogProperty, value.Box()); } }

        /// <summary>
        /// Gets or sets whether it is possible to filter the log text.
        /// </summary>
        public bool CanFilterLog { get { return (bool)GetValue(CanFilterLogProperty); } set { SetValue(CanFilterLogProperty, value.Box()); } }

        /// <summary>
        /// Gets or sets whether it is possible to search the log text.
        /// </summary>
        public bool CanSearchLog { get { return (bool)GetValue(CanSearchLogProperty); } set { SetValue(CanSearchLogProperty, value.Box()); } }

        /// <summary>
        /// Gets or sets the current search token.
        /// </summary>
        public string SearchToken { get { return (string)GetValue(SearchTokenProperty); } set { SetValue(SearchTokenProperty, value); } }

        /// <summary>
        /// Gets or sets whether the search result should match the case.
        /// </summary>
        public bool SearchMatchCase { get { return (bool)GetValue(SearchMatchCaseProperty); } set { SetValue(SearchMatchCaseProperty, value); } }

        /// <summary>
        /// Gets or sets whether the search result should match whole words only.
        /// </summary>
        public bool SearchMatchWord { get { return (bool)GetValue(SearchMatchWordProperty); } set { SetValue(SearchMatchWordProperty, value.Box()); } }

        /// <summary>
        /// Gets or sets the brush used to emphasize search results.
        /// </summary>
        public Brush SearchMatchBrush { get { return (Brush)GetValue(SearchMatchBrushProperty); } set { SetValue(SearchMatchBrushProperty, value); } }

        /// <summary>
        /// Gets or sets the brush used to emphasize debug messages.
        /// </summary>
        public Brush DebugBrush { get { return (Brush)GetValue(DebugBrushProperty); } set { SetValue(DebugBrushProperty, value); } }

        /// <summary>
        /// Gets or sets the brush used to emphasize verbose messages.
        /// </summary>
        public Brush VerboseBrush { get { return (Brush)GetValue(VerboseBrushProperty); } set { SetValue(VerboseBrushProperty, value); } }

        /// <summary>
        /// Gets or sets the brush used to emphasize info messages.
        /// </summary>
        public Brush InfoBrush { get { return (Brush)GetValue(InfoBrushProperty); } set { SetValue(InfoBrushProperty, value); } }

        /// <summary>
        /// Gets or sets the brush used to emphasize warning messages.
        /// </summary>
        public Brush WarningBrush { get { return (Brush)GetValue(WarningBrushProperty); } set { SetValue(WarningBrushProperty, value); } }

        /// <summary>
        /// Gets or sets the brush used to emphasize error messages.
        /// </summary>
        public Brush ErrorBrush { get { return (Brush)GetValue(ErrorBrushProperty); } set { SetValue(ErrorBrushProperty, value); } }

        /// <summary>
        /// Gets or sets the brush used to emphasize fatal messages.
        /// </summary>
        public Brush FatalBrush { get { return (Brush)GetValue(FatalBrushProperty); } set { SetValue(FatalBrushProperty, value); } }

        /// <summary>
        /// Gets or sets whether the log viewer should display debug messages.
        /// </summary>
        public bool ShowDebugMessages { get { return (bool)GetValue(ShowDebugMessagesProperty); } set { SetValue(ShowDebugMessagesProperty, value.Box()); } }

        /// <summary>
        /// Gets or sets whether the log viewer should display verbose messages.
        /// </summary>
        public bool ShowVerboseMessages { get { return (bool)GetValue(ShowVerboseMessagesProperty); } set { SetValue(ShowVerboseMessagesProperty, value.Box()); } }

        /// <summary>
        /// Gets or sets whether the log viewer should display info messages.
        /// </summary>
        public bool ShowInfoMessages { get { return (bool)GetValue(ShowInfoMessagesProperty); } set { SetValue(ShowInfoMessagesProperty, value.Box()); } }

        /// <summary>
        /// Gets or sets whether the log viewer should display warning messages.
        /// </summary>
        public bool ShowWarningMessages { get { return (bool)GetValue(ShowWarningMessagesProperty); } set { SetValue(ShowWarningMessagesProperty, value.Box()); } }

        /// <summary>
        /// Gets or sets whether the log viewer should display error messages.
        /// </summary>
        public bool ShowErrorMessages { get { return (bool)GetValue(ShowErrorMessagesProperty); } set { SetValue(ShowErrorMessagesProperty, value.Box()); } }

        /// <summary>
        /// Gets or sets whether the log viewer should display fatal messages.
        /// </summary>
        public bool ShowFatalMessages { get { return (bool)GetValue(ShowFatalMessagesProperty); } set { SetValue(ShowFatalMessagesProperty, value.Box()); } }

        /// <summary>
        /// Gets or sets whether the log viewer should display fatal messages.
        /// </summary>
        public bool ShowStacktrace { get { return (bool)GetValue(ShowStacktraceProperty); } set { SetValue(ShowStacktraceProperty, value.Box()); } }

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            logTextBox = GetTemplateChild("PART_LogTextBox") as RichTextBox;
            if (logTextBox == null)
                throw new InvalidOperationException("A part named 'PART_LogTextBox' must be present in the ControlTemplate, and must be of type 'RichTextBox'.");

            var clearLogButton = GetTemplateChild("PART_ClearLog") as ButtonBase;
            if (clearLogButton != null)
            {
                clearLogButton.Click += ClearLog;
            }

            var previousResultButton = GetTemplateChild("PART_PreviousResult") as ButtonBase;
            if (previousResultButton != null)
            {
                previousResultButton.Click += PreviousResultClicked;
            }
            var nextResultButton = GetTemplateChild("PART_NextResult") as ButtonBase;
            if (nextResultButton != null)
            {
                nextResultButton.Click += NextResultClicked;
            }

            ResetText();
        }

        private void ClearLog(object sender, RoutedEventArgs e)
        {
            LogMessages.Clear();
        }

        private void ResetText()
        {
            if (logTextBox != null)
            {
                ClearSearchResults();
                var document = new FlowDocument(new Paragraph());
                if (LogMessages != null)
                {
                    var logMessages = LogMessages.ToList();
                    AppendText(document, logMessages);
                }
                logTextBox.Document = document;
            }
        }

        private void AppendText([NotNull] FlowDocument document, [NotNull] IEnumerable<ILogMessage> logMessages)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (logMessages == null) throw new ArgumentNullException(nameof(logMessages));
            if (logTextBox != null)
            {
                var paragraph = (Paragraph)document.Blocks.AsEnumerable().First();
                var stringComparison = SearchMatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                var searchToken = SearchToken;
                foreach (var message in logMessages.Where(x => ShouldDisplayMessage(x.Type)))
                {
                    string content = message.Text;
                    var ex = message.ExceptionInfo;
                    if (ShowStacktrace && ex != null)
                        content = $"{content}{Environment.NewLine}{ex}";
                    var lineText = $"{(message.Module != null ? $"[{message.Module}]: " : string.Empty)}{message.Type}:{content}{Environment.NewLine}";

                    var logColor = GetLogColor(message.Type);
                    if (string.IsNullOrEmpty(searchToken))
                    {
                        paragraph.Inlines.Add(new Run(lineText) { Foreground = logColor });
                    }
                    else
                    {
                        do
                        {
                            var tokenIndex = lineText.IndexOf(searchToken, stringComparison);
                            if (tokenIndex == -1)
                            {
                                paragraph.Inlines.Add(new Run(lineText) { Foreground = logColor });
                                break;
                            }
                            var acceptResult = true;
                            if (SearchMatchWord && lineText.Length > 1)
                            {
                                if (tokenIndex > 0)
                                {
                                    var c = lineText[tokenIndex - 1];
                                    if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                                        acceptResult = false;
                                }
                                if (tokenIndex + searchToken.Length < lineText.Length)
                                {
                                    var c = lineText[tokenIndex + searchToken.Length];
                                    if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                                        acceptResult = false;
                                }
                            }

                            if (acceptResult)
                            {
                                if (tokenIndex > 0)
                                    paragraph.Inlines.Add(new Run(lineText.Substring(0, tokenIndex)) { Foreground = logColor });

                                var tokenRun = new Run(lineText.Substring(tokenIndex, searchToken.Length)) { Background = SearchMatchBrush, Foreground = logColor };
                                paragraph.Inlines.Add(tokenRun);
                                var tokenRange = new TextRange(tokenRun.ContentStart, tokenRun.ContentEnd);
                                searchMatches.Add(tokenRange);
                                lineText = lineText.Substring(tokenIndex + searchToken.Length);
                            }
                        } while (lineText.Length > 0);
                    }
                }
            }
        }


        private void ClearSearchResults()
        {
            searchMatches.Clear();
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
            if (searchMatches.Count > 0)
            {
                var previousResult = (searchMatches.Count + currentResult - 1) % searchMatches.Count;
                SelectSearchResult(previousResult);
            }
        }

        private void SelectNextOccurrence()
        {
            if (searchMatches.Count > 0)
            {
                var nextResult = (currentResult + 1) % searchMatches.Count;
                SelectSearchResult(nextResult);
            }
        }

        private void SelectSearchResult(int resultIndex)
        {
            var result = searchMatches[resultIndex];
            logTextBox.Selection.Select(result.Start, result.End);
            var selectionRect = logTextBox.Selection.Start.GetCharacterRect(LogicalDirection.Forward);
            var offset = selectionRect.Top + logTextBox.VerticalOffset;
            logTextBox.ScrollToVerticalOffset(offset - logTextBox.ActualHeight / 2);
            logTextBox.BringIntoView();
            currentResult = resultIndex;
        }

        private bool ShouldDisplayMessage(LogMessageType type)
        {
            switch (type)
            {
                case LogMessageType.Debug:
                    return ShowDebugMessages;
                case LogMessageType.Verbose:
                    return ShowVerboseMessages;
                case LogMessageType.Info:
                    return ShowInfoMessages;
                case LogMessageType.Warning:
                    return ShowWarningMessages;
                case LogMessageType.Error:
                    return ShowErrorMessages;
                case LogMessageType.Fatal:
                    return ShowFatalMessages;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private Brush GetLogColor(LogMessageType type)
        {
            switch (type)
            {
                case LogMessageType.Debug:
                    return DebugBrush;
                case LogMessageType.Verbose:
                    return VerboseBrush;
                case LogMessageType.Info:
                    return InfoBrush;
                case LogMessageType.Warning:
                    return WarningBrush;
                case LogMessageType.Error:
                    return ErrorBrush;
                case LogMessageType.Fatal:
                    return FatalBrush;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private static void TextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var logViewer = (TextLogViewer)d;
            logViewer.ResetText();
            logViewer.logTextBox?.ScrollToEnd();
        }

        /// <summary>
        /// Raised when the <see cref="LogMessages"/> dependency property is changed.
        /// </summary>
        private static void LogMessagesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var logViewer = (TextLogViewer)d;
            var oldValue = e.OldValue as ICollection<ILogMessage>;
            var newValue = e.NewValue as ICollection<ILogMessage>;
            if (oldValue != null)
            {
                // ReSharper disable SuspiciousTypeConversion.Global - go home resharper, you're drunk
                var notifyCollectionChanged = oldValue as INotifyCollectionChanged;
                // ReSharper restore SuspiciousTypeConversion.Global
                if (notifyCollectionChanged != null)
                {
                    notifyCollectionChanged.CollectionChanged -= logViewer.LogMessagesCollectionChanged;
                }
            }
            if (e.NewValue != null)
            {
                // ReSharper disable SuspiciousTypeConversion.Global - go home resharper, you're drunk
                var notifyCollectionChanged = newValue as INotifyCollectionChanged;
                // ReSharper restore SuspiciousTypeConversion.Global
                if (notifyCollectionChanged != null)
                {
                    notifyCollectionChanged.CollectionChanged += logViewer.LogMessagesCollectionChanged;
                }
            }
            logViewer.ResetText();
        }

        /// <summary>
        /// Raised when the <see cref="SearchToken"/> property is changed.
        /// </summary>
        private static void SearchTokenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var logViewer = (TextLogViewer)d;
            logViewer.ResetText();
            logViewer.SelectFirstOccurrence();
        }

        /// <summary>
        /// Raised when the collection of log messages is observable and changes.
        /// </summary>
        private void LogMessagesCollectionChanged(object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            var shouldScroll = AutoScroll && logTextBox != null && logTextBox.ExtentHeight - logTextBox.ViewportHeight - logTextBox.VerticalOffset < 1.0;

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems != null)
                {
                    if (logTextBox != null)
                    {
                        if (logTextBox.Document == null)
                        {
                            logTextBox.Document = new FlowDocument(new Paragraph());
                        }
                        AppendText(logTextBox.Document, e.NewItems.Cast<ILogMessage>());
                    }
                }
            }
            else
            {
                ResetText();
            }

            if (shouldScroll)
            {
                // Sometimes crashing with ExecutionEngineException in Window.GetWindowMinMax() if not ran with a dispatcher low priority.
                // Note: priority should still be higher than DispatcherPriority.Input so that user input have a chance to scroll.
                Dispatcher.InvokeAsync(() => logTextBox.ScrollToEnd(), DispatcherPriority.DataBind);
            }
        }

        private void PreviousResultClicked(object sender, RoutedEventArgs e)
        {
            SelectPreviousOccurrence();
        }

        private void NextResultClicked(object sender, RoutedEventArgs e)
        {
            SelectNextOccurrence();
        }
    }
}
