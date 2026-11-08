// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using System.Windows.Input;
using Stride.Assets.Editor.Templates;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Templates;
using Stride.Core.Presentation.Dialogs;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.View;
using Stride.Core.Presentation.ViewModels;
using MessageBoxButton = Stride.Core.Presentation.Services.MessageBoxButton;
using MessageBoxImage = Stride.Core.Presentation.Services.MessageBoxImage;

namespace Stride.Assets.Presentation.Templates
{
    /// <summary>
    /// The names and the template of a new script (<see cref="ScriptNameViewModel"/>).
    /// </summary>
    public partial class ScriptNameWindow
    {
        private readonly ViewModelServiceProvider services;
        private readonly ScriptNameViewModel viewModel;

        public ScriptNameWindow(ScriptNameViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);
            this.viewModel = viewModel;
            var dispatcher = new DispatcherService(Dispatcher);
            services = new ViewModelServiceProvider(new object[] { dispatcher, new DialogService(dispatcher, EditorViewModel.Instance.EditorName) });
            InitializeComponent();
            ClassNameTextBox.Text = viewModel.ClassName;
            NamespaceTextBox.Text = viewModel.Namespace;

            TemplateComboBox.Visibility = viewModel.EnableTemplateSelect ? Visibility.Visible : Visibility.Collapsed;
            if (viewModel.EnableTemplateSelect)
            {
                TemplateComboBox.ItemsSource = viewModel.Templates;
                TemplateComboBox.SelectedValue = viewModel.SelectedTemplate;
            }
        }

        private async void Validate()
        {
            viewModel.ClassName = ClassNameTextBox.Text;
            viewModel.Namespace = NamespaceTextBox.Text;
            if (viewModel.EnableTemplateSelect)
                viewModel.SelectedTemplate = TemplateComboBox.SelectedValue as TemplateAssetDescription;

            if (!viewModel.Validate(out var error))
            {
                await services.Get<IDialogService>().MessageBoxAsync(error, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Result = Stride.Core.Presentation.Services.DialogResult.Ok;
            Close();
        }

        private void Cancel()
        {
            Result = Stride.Core.Presentation.Services.DialogResult.Cancel;
            Close();
        }

        private void ButtonOk(object sender, RoutedEventArgs e)
        {
            Validate();
        }

        private void ButtonCancel(object sender, RoutedEventArgs e)
        {
            Cancel();
        }

        private void TextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Validate();
        }
    }
}
