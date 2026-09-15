// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core;
using Stride.Core.Presentation.Dialogs;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.View;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Translation;
using MessageBoxButton = Stride.Core.Presentation.Services.MessageBoxButton;
using MessageBoxImage = Stride.Core.Presentation.Services.MessageBoxImage;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Assets.Presentation.Templates
{
    /// <summary>
    /// Interaction logic for GameTemplateWindow.xaml
    /// </summary>
    public partial class ProjectLibraryWindow : INotifyPropertyChanged
    {
        private readonly IViewModelServiceProvider services;
        private bool hasError;

        /// <param name="title">The window title; the code library one when null.</param>
        /// <param name="referencingProjects">Projects offered as the one to reference the new project from; none hides the choice.</param>
        /// <param name="showNamespace">Whether the namespace is asked for; otherwise it is the library name.</param>
        public ProjectLibraryWindow(string defaultLibraryName, string title = null, IReadOnlyList<string> referencingProjects = null, bool showNamespace = true)
        {
            if (defaultLibraryName == null) throw new ArgumentNullException(nameof(defaultLibraryName));

            var dispatcher = new DispatcherService(Dispatcher);
            var dialog = new DialogService(dispatcher, EditorViewModel.Instance.EditorName);
            services = new ViewModelServiceProvider(new object[] { dispatcher, dialog });

            LibraryName = defaultLibraryName;
            Namespace = defaultLibraryName;
            ShowNamespace = showNamespace;
            ReferencingProjects = referencingProjects ?? Array.Empty<string>();
            SelectedReferencingProject = ReferencingProjects.Count > 0 ? ReferencingProjects[0] : null;
            InitializeComponent();
            DataContext = this;
            if (title != null)
                Title = title;

            LibBox.TextChanged += LibBoxTextChanged;
        }

        private void LibBoxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            HasError = LibNameInputValidator(LibBox.Text);   // true=name collision
        }

        public bool HasError
        {
            get { return hasError; }
            set
            {
                hasError = value;
                OnPropertyChanged(nameof(HasError));
            }
        }

        public Func<string, bool> LibNameInputValidator { get; set; }

        public string LibraryName { get; set; }

        public string Namespace { get; set; }

        public bool ShowNamespace { get; }

        public IReadOnlyList<string> ReferencingProjects { get; }

        public bool HasReferencingProjects => ReferencingProjects.Count > 0;

        public string SelectedReferencingProject { get; set; }

        private async void ButtonOk(object sender, RoutedEventArgs e)
        {
            string error;
            if (!NamingHelper.IsValidNamespace(LibraryName, out error))
            {
                await services.Get<IDialogService>().MessageBoxAsync(string.Format(Tr._p("Message", "Type a valid library name. Error with {0}"), error), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (ShowNamespace && !NamingHelper.IsValidNamespace(Namespace, out error))
            {
                await services.Get<IDialogService>().MessageBoxAsync(string.Format(Tr._p("Message", "Type a valid namespace name. Error with {0}"), error), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Result = Stride.Core.Presentation.Services.DialogResult.Ok;
            Close();
        }

        private void ButtonCancel(object sender, RoutedEventArgs e)
        {
            Result = Stride.Core.Presentation.Services.DialogResult.Cancel;
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
