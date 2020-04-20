// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

using Stride.Core.Assets.Diagnostics;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Collections;

namespace Stride.Core.Assets.Editor.View.Controls
{
    /// <summary>
    /// This control displays a collection of <see cref="ILogMessage"/> in a grid.
    /// </summary>
    [TemplatePart(Name = "PART_LogGridView", Type = typeof(DataGridEx))]
    public class GridLogViewer : Control
    {
        /// <summary>
        /// The <see cref="DataGridControl"/> used to display log messages.
        /// </summary>
        private DataGridEx logGridView;

        static GridLogViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GridLogViewer), new FrameworkPropertyMetadata(typeof(GridLogViewer)));
        }

        /// <summary>
        /// Identifies the <see cref="LogMessages"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LogMessagesProperty = DependencyProperty.Register("LogMessages", typeof(ObservableList<ILogMessage>), typeof(GridLogViewer), new PropertyMetadata(null, LogMessagesPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="IsToolBarVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsToolBarVisibleProperty = DependencyProperty.Register("IsToolBarVisible", typeof(bool), typeof(GridLogViewer), new PropertyMetadata(true));

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
        public ObservableList<ILogMessage> LogMessages
        {
            get { return (ObservableList<ILogMessage>)GetValue(LogMessagesProperty); }
            set
            {
                var prevList = LogMessages;
                if (prevList != null)
                {
                    prevList.CollectionChanged -= OnLogMessagesCollectionChanged;
                }
                SetValue(LogMessagesProperty, value);
                if (value != null)
                {
                    value.CollectionChanged += OnLogMessagesCollectionChanged;
                }
            }
        }

        public ObservableList<ILogMessage> FilteredLogMessages { get; set; } = new ObservableList<ILogMessage>();

        /// <summary>
        /// Gets or sets whether the tool bar should be visible.
        /// </summary>
        public bool IsToolBarVisible { get { return (bool)GetValue(IsToolBarVisibleProperty); } set { SetValue(IsToolBarVisibleProperty, value); } }

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

            logGridView = GetTemplateChild("PART_LogGridView") as DataGridEx;
            if (logGridView == null)
                throw new InvalidOperationException("A part named 'PART_LogGridView' must be present in the ControlTemplate, and must be of type 'DataGridControl'.");

            logGridView.MouseDoubleClick += GridMouseDoubleClick;
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

        private static void LogMessagesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var logViewer = (GridLogViewer)d;

            if (e.OldValue is ObservableList<ILogMessage> prevList)
            {
                prevList.CollectionChanged -= logViewer.OnLogMessagesCollectionChanged;
            }
            if (e.NewValue is ObservableList<ILogMessage> newList)
            {
                newList.CollectionChanged += logViewer.OnLogMessagesCollectionChanged;
            }
        }

        private void OnLogMessagesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (logGridView == null || logGridView.ItemsSource == null || LogMessages == null)
                return;

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                FilteredLogMessages.Clear();
            }
            else
            {
                // Only apply filter if there's any new displayable messages
                bool refreshFilter = false;
                if (e.OldItems != null)
                    refreshFilter = e.OldItems.OfType<ILogMessage>().Any(IsMessageVisible);

                if (!refreshFilter && e.NewItems != null)
                    refreshFilter = e.NewItems.OfType<ILogMessage>().Any(IsMessageVisible);

                if (refreshFilter)
                    ApplyFilters();
            }
        }

        private static void FilterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var logViewer = (GridLogViewer)d;
            logViewer.ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (logGridView == null || logGridView.ItemsSource == null || LogMessages == null)
                return;

            FilteredLogMessages.Clear();
            FilteredLogMessages.AddRange(LogMessages.Where(IsMessageVisible));
        }

        private bool IsMessageVisible(ILogMessage x)
        {
            return x.IsDebug() && ShowDebugMessages
                || x.IsError() && ShowErrorMessages
                || x.IsFatal() && ShowFatalMessages
                || x.IsInfo() && ShowInfoMessages
                || x.IsVerbose() && ShowVerboseMessages
                || x.IsWarning() && ShowWarningMessages;
        }
    }
}
