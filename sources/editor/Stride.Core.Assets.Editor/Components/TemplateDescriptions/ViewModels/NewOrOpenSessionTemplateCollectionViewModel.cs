// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.Settings;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Assets.Templates;
using Xenko.Core.IO;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels
{
    public class NewOrOpenSessionTemplateCollectionViewModel : ProjectTemplateCollectionViewModel
    {
        private readonly IModalDialog dialog;
        private readonly TemplateDescriptionGroupViewModel recentGroup;
        private readonly TemplateDescriptionGroupViewModel rootGroup;
        private string solutionName;
        private UDirectory solutionLocation;
        private bool arePropertiesValid;

        public NewOrOpenSessionTemplateCollectionViewModel(IViewModelServiceProvider serviceProvider, IModalDialog dialog)
            : base(serviceProvider)
        {
            this.dialog = dialog;
            rootGroup = new TemplateDescriptionGroupViewModel(serviceProvider, "New project");

            // Add a default General group
            var defaultGroup = new TemplateDescriptionGroupViewModel(rootGroup, "General");

            foreach (TemplateDescription template in TemplateManager.FindTemplates(TemplateScope.Session))
            {
                var viewModel = new PackageTemplateViewModel(serviceProvider, template);
                var group = ProcessGroup(rootGroup, template.Group) ?? defaultGroup;
                group.Templates.Add(viewModel);
            }

            recentGroup = new TemplateDescriptionGroupViewModel(serviceProvider, "Recent projects");
            foreach (var file in EditorViewModel.Instance.RecentFiles)
            {
                var viewModel = new ExistingProjectViewModel(ServiceProvider, file.FilePath, RemoveExistingProjects);
                recentGroup.Templates.Add(viewModel);
            }

            Location = InternalSettings.TemplatesWindowDialogLastNewSessionTemplateDirectory.GetValue();
            if (string.IsNullOrWhiteSpace(Location))
                Location = UPath.Combine<UDirectory>(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Xenko Projects");

            BrowseForExistingProjectCommand = new AnonymousTaskCommand(serviceProvider, BrowseForExistingProject);
            SelectedGroup = recentGroup.Templates.Count == 0 ? rootGroup : recentGroup;
        }

        public override IEnumerable<TemplateDescriptionGroupViewModel> RootGroups { get { yield return recentGroup; yield return rootGroup; } }

        public string SolutionName { get { return solutionName; } set { SetValue(ref solutionName, value); } }

        public UDirectory SolutionLocation { get { return solutionLocation; } set { SetValue(ref solutionLocation, value); } }

        public bool AutoReloadSession { get { return EditorSettings.ReloadLastSession.GetValue(); } set { SetValue(value != AutoReloadSession, () => EditorSettings.ReloadLastSession.SetValue(value)); } }

        public bool ArePropertiesValid { get { return arePropertiesValid; } set { SetValue(ref arePropertiesValid, value); } }

        public ICommandBase BrowseForExistingProjectCommand { get; private set; }

        public override bool ValidateProperties(out string error)
        {
            if (SelectedTemplate is TemplateDescriptionViewModel)
            {
                if (!string.IsNullOrWhiteSpace(SolutionLocation) && !UPath.IsValid(SolutionLocation))
                {
                    error = "Invalid solution directory.";
                    return ArePropertiesValid = false;
                }
                if (!string.IsNullOrWhiteSpace(SolutionName) && (!UFile.IsValid(SolutionName) || SolutionName.Contains(UPath.DirectorySeparatorString) || SolutionName.Contains(UPath.DirectorySeparatorStringAlt)))
                {
                    error = "Invalid solution name.";
                    return ArePropertiesValid = false;
                }
            }
            return ArePropertiesValid = base.ValidateProperties(out error);
        }

        protected override string UpdateNameFromSelectedTemplate()
        {
            // Get package names in the current session
            return GenerateUniqueNameAtLocation();
        }

        private void RemoveExistingProjects(ExistingProjectViewModel item)
        {
            if (item == null)
                return;

            EditorViewModel.Instance.RemoveRecentFile(item.Path);
            SelectedGroup.Templates.Remove(item);
            UpdateTemplateList();
        }

        private async Task BrowseForExistingProject()
        {
            var filePath = await EditorDialogHelper.BrowseForExistingProject(ServiceProvider);
            if (filePath != null)
            {
                SelectedTemplate = new ExistingProjectViewModel(ServiceProvider, filePath, RemoveExistingProjects);
                dialog?.RequestClose(DialogResult.Ok);
            }
        }
    }
}
