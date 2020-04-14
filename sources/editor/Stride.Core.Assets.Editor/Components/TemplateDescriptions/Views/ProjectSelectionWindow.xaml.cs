// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using System.Windows;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.IO;
using Stride.Core.Presentation.View;
using Stride.Core.Presentation.Windows;
using Stride.Core.Translation;
using MessageBoxButton = Stride.Core.Presentation.Services.MessageBoxButton;
using MessageBoxImage = Stride.Core.Presentation.Services.MessageBoxImage;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions.Views
{
    /// <summary>
    /// Interaction logic for ProjectSelectionWindow.xaml
    /// </summary>
    public partial class ProjectSelectionWindow
    {
        public ProjectSelectionWindow()
        {
            InitializeComponent();
            Width = Math.Min(Width, SystemParameters.WorkArea.Width);
            Height = Math.Min(Height, SystemParameters.WorkArea.Height);
            Title = string.Format(Tr._p("Title", "Project selection - {0}"), EditorPath.EditorTitle);
        }

        public NewSessionParameters NewSessionParameters { get; private set; }

        public UFile ExistingSessionPath { get; private set; }

        public NewOrOpenSessionTemplateCollectionViewModel Templates { get { return (NewOrOpenSessionTemplateCollectionViewModel)DataContext; } set { DataContext = value; } }

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

            if (Result == Presentation.Services.DialogResult.Ok)
            {
                var recentProject = Templates.SelectedTemplate as ExistingProjectViewModel;
                var newPackageTemplate = Templates.SelectedTemplate as TemplateDescriptionViewModel;
                if (recentProject != null)
                {
                    ExistingSessionPath = recentProject.Path;
                }
                else if (newPackageTemplate != null)
                {
                    NewSessionParameters = new NewSessionParameters
                    {
                        TemplateDescription = Templates.SelectedTemplate != null ? newPackageTemplate.GetTemplate() : null,
                        OutputName = Templates.Name,
                        OutputDirectory = Templates.Location,
                        SolutionName = Templates.SolutionName,
                        SolutionLocation = Templates.SolutionLocation,
                    };
                }
            }
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (Result == Presentation.Services.DialogResult.Ok)
            {
                EditorSettings.Save();
                InternalSettings.TemplatesWindowDialogLastNewSessionTemplateDirectory.SetValue(Templates.Location.FullPath);
                InternalSettings.Save();
            }
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
