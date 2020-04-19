// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.View;
using Stride.Core.Presentation.Windows;
using MessageBoxButton = Stride.Core.Presentation.Services.MessageBoxButton;
using MessageBoxImage = Stride.Core.Presentation.Services.MessageBoxImage;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions.Views
{
    /// <summary>
    /// Interaction logic for NewPackageWindow.xaml
    /// </summary>
    public partial class NewProjectWindow : INewProjectDialog
    {
        public NewProjectWindow()
        {
            InitializeComponent();
            Width = Math.Min(Width, SystemParameters.WorkArea.Width);
            Height = Math.Min(Height, SystemParameters.WorkArea.Height);
        }

        public UDirectory DefaultOutputDirectory { get; set; }
        
        public NewPackageParameters Parameters { get; private set; }

        private NewProjectTemplateCollectionViewModel Templates => (NewProjectTemplateCollectionViewModel)DataContext;

        public override Task<DialogResult> ShowModal()
        {
            if (!string.IsNullOrWhiteSpace(DefaultOutputDirectory))
                Templates.Location = DefaultOutputDirectory;

            return base.ShowModal();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (Result == Presentation.Services.DialogResult.Ok)
            {
                if (!ValidateProperties())
                {
                    e.Cancel = true;
                    return;
                }
            }

            Parameters = new NewPackageParameters();
            if (Result == Presentation.Services.DialogResult.Ok)
            {
                Parameters.TemplateDescription = Templates.SelectedTemplate?.GetTemplate();
                Parameters.OutputName = Templates.Name;
                Parameters.OutputDirectory = Templates.Location;
            }
            base.OnClosing(e);
        }

        private void OnTextBoxValidated(object sender, EventArgs e)
        {
            ValidateProperties();
        }

        private bool ValidateProperties()
        {
            string error;
            if (!Templates.ValidateProperties(out error))
            {
                DialogHelper.BlockingMessageBox(DispatcherService.Create(), error, EditorPath.EditorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }
    }
}
