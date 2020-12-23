// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;

using Stride.LauncherApp.Services;
using Stride.LauncherApp.ViewModels;
using Stride.Core.Packages;
using Stride.Core.Presentation.Dialogs;
using Stride.Core.Presentation.Extensions;
using Stride.Core.Presentation.View;
using Stride.Core.Presentation.ViewModel;
using NuGet.Frameworks;

namespace Stride.LauncherApp.Views
{
    /// <summary>
    /// Interaction logic for LauncherWindow.xaml
    /// </summary>
    public partial class LauncherWindow
    {
        
        public LauncherWindow()
        {
            InitializeComponent();
            ExitOnUserClose = true;
            Loaded += OnLoaded;
            TabControl.SelectedIndex = LauncherSettings.CurrentTab >= 0 ? LauncherSettings.CurrentTab : 0;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var handle = new WindowInteropHelper(this);
            LauncherViewModel.WindowHandle = handle.Handle;

            InitializeWindowSize();
        }

        private void InitializeWindowSize()
        {
            var workArea = this.GetWorkArea();
            Width = Math.Min(Width, workArea.Width);
            Height = Math.Min(Height, workArea.Height);
            this.CenterToArea(workArea);
        }

        public bool ExitOnUserClose { get; set; }
        
        private LauncherViewModel ViewModel => (LauncherViewModel)DataContext;

        internal void Initialize(NugetStore store, string defaultLogText = null)
        {
            var dispatcherService = new DispatcherService(Dispatcher);
            var dialogService = new DialogService(dispatcherService, Launcher.ApplicationName);
            var serviceProvider = new ViewModelServiceProvider(new object[] {dispatcherService, dialogService});
            DataContext = new LauncherViewModel(serviceProvider, store);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (ViewModel.StrideVersions.Any(x => x.IsProcessing))
            {
                var forceClose = Launcher.DisplayMessage("Some background operations are still in progress. Force close?");

                if (!forceClose)
                {
                    e.Cancel = true;
                    return;
                }
            }

            var viewModel = (LauncherViewModel)DataContext;
            LauncherSettings.ActiveVersion = viewModel.ActiveVersion != null ? viewModel.ActiveVersion.Name : ""; 
            LauncherSettings.Save();
            if (ExitOnUserClose)
                Environment.Exit(1);
        }

        private void SelectedTabChanged(object sender, SelectionChangedEventArgs e)
        {
            LauncherSettings.CurrentTab = TabControl.SelectedIndex;
        }

        private void FrameworkChanged(object sender, SelectionChangedEventArgs e)
        {
            var framework = (string)FrameworkSelector.SelectedItem;
            if (framework != null && LauncherSettings.PreferredFramework != framework)
            {
                LauncherSettings.PreferredFramework = framework;
                LauncherSettings.Save();
            }
        }

        private void OpenWithClicked(object sender, RoutedEventArgs e)
        {
            var dependencyObject = sender as DependencyObject;
            if (dependencyObject == null)
                return;

            var scrollViewer = dependencyObject.FindVisualParentOfType<ScrollViewer>();
            scrollViewer?.FindLogicalParentOfType<Popup>()?.SetCurrentValue(Popup.IsOpenProperty, false);
        }
    }
}
