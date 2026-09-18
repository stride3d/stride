// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels
{
    public class NewOrOpenSessionTemplateCollectionViewModel : ProjectTemplateCollectionViewModel
    {
        // Library template Id from samples/Library/Library/Library.sdtpl. Code Library adds a
        // project to an existing session — it can't seed a new solution on its own (no entry point
        // to run), so we hide it from the startup dialog. Hardcoded until a TemplateDescription-
        // level "adds a project to existing session" marker lets both this filter and the inverse
        // one in NewProjectTemplateCollectionViewModel collapse to a single check.
        private static readonly Guid StrideLibraryTemplateId = new("7B79F1B7-3A55-4C84-AED9-3F4F3EE4B6E5");

        private readonly IModalDialog dialog;
        private readonly TemplateDescriptionGroupViewModel recentGroup;
        private readonly TemplateDescriptionGroupViewModel rootGroup;
        private readonly TemplateDescriptionGroupViewModel defaultGroup;
        private readonly HashSet<TemplateDescription> listedTemplates = new(ReferenceEqualityComparer.Instance);
        private string solutionName;
        private UDirectory solutionLocation;
        private bool arePropertiesValid;

        public NewOrOpenSessionTemplateCollectionViewModel(IViewModelServiceProvider serviceProvider, IModalDialog dialog)
            : base(serviceProvider)
        {
            this.dialog = dialog;
            rootGroup = new TemplateDescriptionGroupViewModel(serviceProvider, "New project");

            // Add a default General group
            defaultGroup = new TemplateDescriptionGroupViewModel(rootGroup, "General");
            AddNewTemplates();

            // Template packages still downloading at startup: show their progress, list their templates once ready.
            TemplateManager.PackagesChanged += OnTemplatePackagesChanged;
            TemplateDownloads.Changed += OnTemplateDownloadsChanged;
            UpdateDownloadStatus();
            DependentProperties.Add(nameof(SelectedGroup), [nameof(ShowDownloadStatus)]);
            DependentProperties.Add(nameof(DownloadStatus), [nameof(ShowDownloadStatus)]);

            recentGroup = new TemplateDescriptionGroupViewModel(serviceProvider, "Recent projects");
            foreach (var file in EditorViewModel.Instance.RecentFiles)
            {
                var viewModel = new ExistingProjectViewModel(ServiceProvider, file.FilePath, RemoveExistingProjects);
                recentGroup.Templates.Add(viewModel);
            }

            Location = InternalSettings.TemplatesWindowDialogLastNewSessionTemplateDirectory.GetValue();
            if (string.IsNullOrWhiteSpace(Location))
                Location = UPath.Combine<UDirectory>(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Stride Projects");

            BrowseForExistingProjectCommand = new AnonymousTaskCommand(serviceProvider, BrowseForExistingProject);
            SelectedGroup = recentGroup.Templates.Count == 0 ? rootGroup : recentGroup;
        }

        public override IEnumerable<TemplateDescriptionGroupViewModel> RootGroups { get { yield return recentGroup; yield return rootGroup; } }

        public string SolutionName { get { return solutionName; } set { SetValue(ref solutionName, value); } }

        public UDirectory SolutionLocation { get { return solutionLocation; } set { SetValue(ref solutionLocation, value); } }

        public bool AutoReloadSession { get { return EditorSettings.ReloadLastSession.GetValue(); } set { SetValue(value != AutoReloadSession, () => EditorSettings.ReloadLastSession.SetValue(value)); } }

        public bool ArePropertiesValid { get { return arePropertiesValid; } set { SetValue(ref arePropertiesValid, value); } }

        public ICommandBase BrowseForExistingProjectCommand { get; private set; }

        /// <summary>The template download in progress (or failed), null when there is none.</summary>
        public string DownloadStatus { get; private set { SetValue(ref field, value); } }

        /// <summary>Whether <see cref="DownloadStatus"/> is shown: only while new project templates are listed, where the downloads add templates.</summary>
        public bool ShowDownloadStatus
        {
            get
            {
                if (DownloadStatus is null)
                    return false;
                for (var group = SelectedGroup; group is not null; group = group.Parent)
                {
                    if (group == rootGroup)
                        return true;
                }
                return false;
            }
        }

        /// <summary>Whether a template download is in progress (<see cref="DownloadStatus"/> may also report a failure).</summary>
        public bool IsDownloading { get; private set { SetValue(ref field, value); } }

        /// <summary>Progress of the download from 0 to 1 (0 while its size is not known).</summary>
        public double DownloadProgress { get; private set { SetValue(ref field, value); } }

        /// <summary>Whether the size of the download is not known yet (no progress to show).</summary>
        public bool IsDownloadSizeUnknown { get; private set { SetValue(ref field, value); } } = true;

        /// <inheritdoc />
        public override void Destroy()
        {
            TemplateManager.PackagesChanged -= OnTemplatePackagesChanged;
            TemplateDownloads.Changed -= OnTemplateDownloadsChanged;
            base.Destroy();
        }

        private void AddNewTemplates()
        {
            if (IsDestroyed)
                return;
            var added = false;
            foreach (TemplateDescription template in TemplateManager.FindTemplates(TemplateScope.Session))
            {
                if (template.Id == StrideLibraryTemplateId || !listedTemplates.Add(template))
                    continue;
                var viewModel = new PackageTemplateViewModel(ServiceProvider, template);
                var group = ProcessGroup(rootGroup, template.Group) ?? defaultGroup;
                group.Templates.Add(viewModel);
                added = true;
            }
            if (added && SelectedGroup is not null)
            {
                // Refreshing the list clears the selection in the view: keep the user's choice.
                var selected = SelectedTemplate;
                UpdateTemplateList();
                SelectedTemplate = selected;
            }
        }

        private void OnTemplatePackagesChanged() => Dispatcher.InvokeAsync(AddNewTemplates);

        private void OnTemplateDownloadsChanged() => Dispatcher.InvokeAsync(UpdateDownloadStatus);

        private void UpdateDownloadStatus()
        {
            if (IsDestroyed)
                return;
            var download = TemplateDownloads.Current;
            DownloadStatus = download is null ? null : TemplateDownloads.Describe(download);
            IsDownloading = download is { Failed: false };
            DownloadProgress = download?.Fraction ?? 0;
            IsDownloadSizeUnknown = download?.Fraction is null;
        }

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
