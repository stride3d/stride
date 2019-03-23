// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Presentation.Dialogs;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.View;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Engine;
using MessageBoxImage = Xenko.Core.Presentation.Services.MessageBoxImage;
using MessageBoxButton = Xenko.Core.Presentation.Services.MessageBoxButton;
using Xenko.Core.Extensions;
using Xenko.Core.Translation;
using Xenko.Core.Assets.Templates;

namespace Xenko.Assets.Presentation.Templates
{
    /// <summary>
    /// Interaction logic for ScriptNameWindow.xaml
    /// </summary>
    public partial class ScriptNameWindow
    {
        private readonly ViewModelServiceProvider services;
        private static readonly List<string> ReservedNames = new List<string>();
        private readonly bool enableTemplateSelect;
        private readonly TemplateAssetDescription defaultScriptTemplate;

        static ScriptNameWindow()
        {
            // Reserve the name of the base classes and interfaces
            ReservedNames.Add(nameof(AsyncScript));
            ReservedNames.Add(nameof(SyncScript));
            ReservedNames.Add(nameof(StartupScript));
            typeof(ScriptComponent).GetInterfaces().ForEach(x => ReservedNames.Add(x.Name));
            var type = typeof(ScriptComponent);
            while (type != typeof(object) && type != null)
            {
                ReservedNames.Add(type.Name);
                type = type.BaseType;
            }
        }

        public ScriptNameWindow(string defaultClassName, string defaultNamespace, TemplateAssetDescription defaultScriptTemplate, bool enableTemplateSelect, IEnumerable<TemplateAssetDescription> scriptTemplates)
        {
            var dispatcher = new DispatcherService(Dispatcher);
            services = new ViewModelServiceProvider(new object[] { dispatcher, new DialogService(dispatcher, EditorViewModel.Instance.EditorName) });
            InitializeComponent();
            ClassNameTextBox.Text = defaultClassName;
            NamespaceTextBox.Text = defaultNamespace;

            TemplateComboBox.Visibility = enableTemplateSelect ? Visibility.Visible : Visibility.Collapsed;
            if (enableTemplateSelect)
            {
                TemplateComboBox.ItemsSource = scriptTemplates;
                TemplateComboBox.SelectedValue = defaultScriptTemplate; 
            }

            this.enableTemplateSelect = enableTemplateSelect;
            this.defaultScriptTemplate = defaultScriptTemplate;
        }

        public string ClassName { get; private set; }

        public string Namespace { get; private set; }

        public TemplateAssetDescription ScriptTemplate { get; private set; }

        private async void Validate()
        {
            ClassName = Utilities.BuildValidClassName(ClassNameTextBox.Text, ReservedNames);
            Namespace = Utilities.BuildValidNamespaceName(NamespaceTextBox.Text, ReservedNames);
            ScriptTemplate = enableTemplateSelect ? TemplateComboBox.SelectedValue as TemplateAssetDescription : defaultScriptTemplate;

            if (string.IsNullOrWhiteSpace(ClassName) || string.IsNullOrWhiteSpace(Namespace))
            {
                await services.Get<IDialogService>().MessageBox(Tr._p("Message", "The names you entered are invalid or empty."), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Result = Xenko.Core.Presentation.Services.DialogResult.Ok;
            Close();
        }

        private void Cancel()
        {
            ClassName = null;
            Namespace = null;
            ScriptTemplate = null;
            Result = Xenko.Core.Presentation.Services.DialogResult.Cancel;
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
