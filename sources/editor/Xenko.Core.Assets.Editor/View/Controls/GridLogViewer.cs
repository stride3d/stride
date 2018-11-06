// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

using Xenko.Core.Assets.Diagnostics;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Diagnostics;

using Xceed.Wpf.DataGrid;

namespace Xenko.Core.Assets.Editor.View.Controls
{
    /// <summary>
    /// This control displays a collection of <see cref="ILogMessage"/> in a grid.
    /// </summary>
    [TemplatePart(Name = "PART_LogGridView", Type = typeof(DataGridControl))]
    [TemplatePart(Name = "PART_PreviousResult", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PART_NextResult", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PART_GridLogViewerCollectionSourceContainer", Type = typeof(FrameworkElement))]   
    public class GridLogViewer : Control
    {
        private int currentResult;

        /// <summary>
        /// The <see cref="DataGridControl"/> used to display log messages.
        /// </summary>
        private DataGridControl logGridView;

        /// <summary>
        /// The <see cref="ButtonBase"/> used to navigate to the previous search result.
        /// </summary>
        private ButtonBase previousResultButton;

        /// <summary>
        /// The <see cref="ButtonBase"/> used to navigate to the next search result.
        /// </summary>
        private ButtonBase nextResultButton;

        static GridLogViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GridLogViewer), new FrameworkPropertyMetadata(typeof(GridLogViewer)));
        }

        /// <summary>
        /// Identifies the <see cref="LogMessages"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LogMessagesProperty = DependencyProperty.Register("LogMessages", typeof(ICollection<ILogMessage>), typeof(GridLogViewer), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="IsToolBarVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsToolBarVisibleProperty = DependencyProperty.Register("IsToolBarVisible", typeof(bool), typeof(GridLogViewer), new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="CanFilterLog"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CanFilterLogProperty = DependencyProperty.Register("CanFilterLog", typeof(bool), typeof(GridLogViewer), new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="CanSearchLog"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CanSearchLogProperty = DependencyProperty.Register("CanSearchLog", typeof(bool), typeof(GridLogViewer), new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="SearchToken"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SearchTokenProperty = DependencyProperty.Register("SearchToken", typeof(string), typeof(GridLogViewer), new PropertyMetadata("", SearchTokenChanged));

        /// <summary>
        /// Identifies the <see cref="SearchMatchCase"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SearchMatchCaseProperty = DependencyProperty.Register("SearchMatchCase", typeof(bool), typeof(GridLogViewer), new PropertyMetadata(false, SearchTokenChanged));

        /// <summary>
        /// Identifies the <see cref="SearchMatchWord"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SearchMatchWordProperty = DependencyProperty.Register("SearchMatchWord", typeof(bool), typeof(GridLogViewer), new PropertyMetadata(false, SearchTokenChanged));

        /// <summary>
        /// Identifies the <see cref="ShowDebugMessages"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowDebugMessagesProperty = DependencyProperty.Register("ShowDebugMessages", typeof(bool), typeof(GridLogViewer), new PropertyMetadata(true, FilterPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ShowVerboseMessages"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowVerboseMessagesProperty = DependencyProperty.Register("ShowVerboseMessages", typeof(bool), typeof(GridLogViewer), new PropertyMetadata(true, FilterPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ShowInfoMessages"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowInfoMessagesProperty = DependencyProperty.Register("ShowInfoMessages", typeof(bool), typeof(GridLogViewer), new PropertyMetadata(true, FilterPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ShowWarningMessages"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowWarningMessagesProperty = DependencyProperty.Register("ShowWarningMessages", typeof(bool), typeof(GridLogViewer), new PropertyMetadata(true, FilterPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ShowErrorMessages"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowErrorMessagesProperty = DependencyProperty.Register("ShowErrorMessages", typeof(bool), typeof(GridLogViewer), new PropertyMetadata(true, FilterPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ShowFatalMessages"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowFatalMessagesProperty = DependencyProperty.Register("ShowFatalMessages", typeof(bool), typeof(GridLogViewer), new PropertyMetadata(true, FilterPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ShowStacktrace"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowStacktraceProperty = DependencyProperty.Register("ShowStacktrace", typeof(bool), typeof(GridLogViewer), new PropertyMetadata(false, FilterPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="Session"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SessionProperty = DependencyProperty.Register("Session", typeof(SessionViewModel), typeof(GridLogViewer));

        /// <summary>
        /// Gets or sets the collection of <see cref="ILogMessage"/> to display.
        /// </summary>
        public ICollection<ILogMessage> LogMessages { get { return (ICollection<ILogMessage>)GetValue(LogMessagesProperty); } set { SetValue(LogMessagesProperty, value); } }

        /// <summary>
        /// Gets or sets whether the tool bar should be visible.
        /// </summary>
        public bool IsToolBarVisible { get { return (bool)GetValue(IsToolBarVisibleProperty); } set { SetValue(IsToolBarVisibleProperty, value); } }

        /// <summary>
        /// Gets or sets whether it is possible to filter the log text.
        /// </summary>
        public bool CanFilterLog { get { return (bool)GetValue(CanFilterLogProperty); } set { SetValue(CanFilterLogProperty, value); } }

        /// <summary>
        /// Gets or sets whether it is possible to search the log text.
        /// </summary>
        public bool CanSearchLog { get { return (bool)GetValue(CanSearchLogProperty); } set { SetValue(CanSearchLogProperty, value); } }

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
        public bool SearchMatchWord { get { return (bool)GetValue(SearchMatchWordProperty); } set { SetValue(SearchMatchWordProperty, value); } }

        /// <summary>
        /// Gets or sets whether the log viewer should display debug messages.
        /// </summary>
        public bool ShowDebugMessages { get { return (bool)GetValue(ShowDebugMessagesProperty); } set { SetValue(ShowDebugMessagesProperty, value); } }

        /// <summary>
        /// Gets or sets whether the log viewer should display verbose messages.
        /// </summary>
        public bool ShowVerboseMessages { get { return (bool)GetValue(ShowVerboseMessagesProperty); } set { SetValue(ShowVerboseMessagesProperty, value); } }

        /// <summary>
        /// Gets or sets whether the log viewer should display info messages.
        /// </summary>
        public bool ShowInfoMessages { get { return (bool)GetValue(ShowInfoMessagesProperty); } set { SetValue(ShowInfoMessagesProperty, value); } }

        /// <summary>
        /// Gets or sets whether the log viewer should display warning messages.
        /// </summary>
        public bool ShowWarningMessages { get { return (bool)GetValue(ShowWarningMessagesProperty); } set { SetValue(ShowWarningMessagesProperty, value); } }

        /// <summary>
        /// Gets or sets whether the log viewer should display error messages.
        /// </summary>
        public bool ShowErrorMessages { get { return (bool)GetValue(ShowErrorMessagesProperty); } set { SetValue(ShowErrorMessagesProperty, value); } }

        /// <summary>
        /// Gets or sets whether the log viewer should display fatal messages.
        /// </summary>
        public bool ShowFatalMessages { get { return (bool)GetValue(ShowFatalMessagesProperty); } set { SetValue(ShowFatalMessagesProperty, value); } }

        /// <summary>
        /// Gets or sets whether the log viewer should display fatal messages.
        /// </summary>
        public bool ShowStacktrace { get { return (bool)GetValue(ShowStacktraceProperty); } set { SetValue(ShowStacktraceProperty, value); } }

        /// <summary>
        /// Gets or sets the session to use to select an asset related to a log message.
        /// </summary>
        public SessionViewModel Session { get { return (SessionViewModel)GetValue(SessionProperty); } set { SetValue(SessionProperty, value); } }

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            logGridView = GetTemplateChild("PART_LogGridView") as DataGridControl;
            if (logGridView == null)
                throw new InvalidOperationException("A part named 'PART_LogGridView' must be present in the ControlTemplate, and must be of type 'DataGridControl'.");

            previousResultButton = GetTemplateChild("PART_PreviousResult") as ButtonBase;
            if (previousResultButton == null)
                throw new InvalidOperationException("A part named 'PART_PreviousResult' must be present in the ControlTemplate, and must be of type 'ButtonBase'.");

            nextResultButton = GetTemplateChild("PART_NextResult") as ButtonBase;
            if (nextResultButton == null)
                throw new InvalidOperationException("A part named 'PART_NextResult' must be present in the ControlTemplate, and must be of type 'ButtonBase'.");

            var sourceContainer = GetTemplateChild("PART_GridLogViewerCollectionSourceContainer") as FrameworkElement;
            if (sourceContainer == null)
                throw new InvalidOperationException("A part named 'PART_GridLogViewerCollectionSourceContainer' must be present in the ControlTemplate, and must be of type 'FrameworkElement'.");

            var source = sourceContainer.Resources["GridLogViewerCollectionSource"];
            if (!(source is DataGridCollectionViewSourceBase))
                throw new InvalidOperationException("The 'PART_GridLogViewerCollectionSourceContainer' must be contain a 'GridLogViewerCollectionSource' resource that is the source of the collection view for the DataGridControl.");

            ((DataGridCollectionViewSourceBase)source).Filter += FilterHandler;
            logGridView.MouseDoubleClick += GridMouseDoubleClick;
            previousResultButton.Click += PreviousResultClicked;
            nextResultButton.Click += NextResultClicked;
        }

        private void FilterHandler(object value, FilterEventArgs e)
        {
            e.Accepted = FilterMethod(e.Item);
        }

        private void GridMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Session == null)
                return;

            var logMessage = logGridView.SelectedItem as AssetSerializableLogMessage;
            if (logMessage != null && !string.IsNullOrEmpty(logMessage.AssetUrl))
            {
                var asset = Session.GetAssetById(logMessage.AssetId);
                if (asset != null)
                    Session.ActiveAssetView.SelectAssetCommand.Execute(asset);
            }

            var assetLogMessage = logGridView.SelectedItem as AssetLogMessage;
            if (assetLogMessage != null && assetLogMessage.AssetReference != null)
            {
                AssetViewModel asset = Session.GetAssetById(assetLogMessage.AssetReference.Id);
                if (asset != null)
                    Session.ActiveAssetView.SelectAssetCommand.Execute(asset);
            }
        }

        private void SelectFirstOccurrence()
        {
            currentResult = 0;
            var token = SearchToken;
            if (!string.IsNullOrEmpty(token))
            {
                var message = LogMessages.FirstOrDefault(Match);
                logGridView.SelectedItem = message;
                if (message != null)
                {
                    logGridView.BringItemIntoView(message);
                }
            }
        }

        private void SelectPreviousOccurrence()
        {
            var token = SearchToken;
            if (!string.IsNullOrEmpty(token))
            {
                var message = FindPreviousMessage();
                logGridView.SelectedItem = message;
                if (message != null)
                {
                    logGridView.BringItemIntoView(message);
                }
                else
                    logGridView.SelectedItem = null;
            }
        }

        private void SelectNextOccurrence()
        {
            var token = SearchToken;
            if (!string.IsNullOrEmpty(token))
            {
                var message = FindNextMessage();
                logGridView.SelectedItem = message;
                if (message != null)
                {
                    logGridView.BringItemIntoView(message);
                }
            }
        }

        private ILogMessage FindPreviousMessage()
        {
            int count = 0;
            ILogMessage lastMessage = null;
            --currentResult;
            foreach (var message in LogMessages.Where(Match))
            {
                lastMessage = message;

                if (count == currentResult)
                {
                    return message;
                }
                ++count;
            }
            currentResult = Math.Max(0, count - 1);
            return lastMessage;
        }

        private ILogMessage FindNextMessage()
        {
            int count = 0;
            ILogMessage firstMessage = null;
            ++currentResult;
            foreach (var message in LogMessages.Where(Match))
            {
                if (firstMessage == null)
                    firstMessage = message;

                if (count == currentResult)
                {
                    return message;
                }
                ++count;
            }
            currentResult = 0;
            return firstMessage;
        }

        private bool Match(ILogMessage message)
        {
            if (Match(message.Text))
                return true;

            var assetMessage = message as AssetSerializableLogMessage;
            return assetMessage != null && Match(assetMessage.AssetUrl.FullPath);
        }

        private bool Match(string text)
        {
            var token = SearchToken;
            var stringComparison = SearchMatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            int index = text.IndexOf(token, stringComparison);
            if (index < 0)
                return false;

            if (SearchMatchWord && text.Length > 1)
            {
                if (index > 0)
                {
                    char c = text[index - 1];
                    if (char.IsLetterOrDigit(c))
                        return false;
                }
                if (index + token.Length < text.Length)
                {
                    char c = text[index + token.Length];
                    if (char.IsLetterOrDigit(c))
                        return false;
                }
            }
            return true;
        }

        private static void FilterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var logViewer = (GridLogViewer)d;
            logViewer.ApplyFilters();
        }

        /// <summary>
        /// Raised when the <see cref="SearchToken"/> property is changed.
        /// </summary>
        private static void SearchTokenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var logViewer = (GridLogViewer)d;
            logViewer.SelectFirstOccurrence();
        }

        private void PreviousResultClicked(object sender, RoutedEventArgs e)
        {
            SelectPreviousOccurrence();
        }

        private void NextResultClicked(object sender, RoutedEventArgs e)
        {
            SelectNextOccurrence();
        }

        private void ApplyFilters()
        {
            if (logGridView == null || logGridView.ItemsSource == null)
                return;

            if (!(logGridView.ItemsSource is DataGridCollectionView))
                throw new InvalidOperationException("The item source of the part 'PART_LogGridView' must be a 'DataGridCollectionView'.");

            var view = (DataGridCollectionView)logGridView.ItemsSource;
            view.Refresh();
        }

        private bool FilterMethod(object msg)
        {
            var message = (ILogMessage)msg;
            return (ShowDebugMessages && message.Type == LogMessageType.Debug)
                 || (ShowVerboseMessages && message.Type == LogMessageType.Verbose)
                 || (ShowInfoMessages && message.Type == LogMessageType.Info)
                 || (ShowWarningMessages && message.Type == LogMessageType.Warning)
                 || (ShowErrorMessages && message.Type == LogMessageType.Error)
                 || (ShowFatalMessages && message.Type == LogMessageType.Fatal);
        }
    }
}
