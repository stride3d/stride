// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using System.Windows;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.View;
using Stride.Core.Presentation.Windows;
using Stride.Core.Translation;
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

        public NewOrOpenSessionTemplateCollectionViewModel Templates
        {
            get { return (NewOrOpenSessionTemplateCollectionViewModel)DataContext; }
            set
            {
                if (Templates != null)
                    Templates.OpenRequested -= OnOpenRequested;
                DataContext = value;
                if (value != null)
                    value.OpenRequested += OnOpenRequested;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (Result == Stride.Core.Presentation.Services.DialogResult.Ok)
            {
                if (!ValidateProperties())
                {
                    e.Cancel = true;
                    return;
                }
            }

            if (Result == Stride.Core.Presentation.Services.DialogResult.Ok)
            {
                ExistingSessionPath = Templates.GetExistingSessionPath();
                NewSessionParameters = Templates.GetNewSessionParameters();
            }
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (Result == Stride.Core.Presentation.Services.DialogResult.Ok)
            {
                Templates.SaveSettings();
            }
        }

        private void OnOpenRequested(object sender, EventArgs e)
        {
            // A recent project, or one picked with the browse button: open it at once
            RequestClose(Stride.Core.Presentation.Services.DialogResult.Ok);
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
                DialogHelper.BlockingMessageBox(DispatcherService.Create(), error, EditorPath.EditorTitle, [IDialogService.ButtonOK], MessageBoxImage.Error);
                return false;
            }
            return true;
        }
    }
}
