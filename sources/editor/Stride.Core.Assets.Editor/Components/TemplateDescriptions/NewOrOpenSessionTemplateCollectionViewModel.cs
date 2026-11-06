// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions;

/// <summary>
/// The content of the project selection window shown when the editor starts without a session: the recent projects to open,
/// and the templates (new game, samples) to create a new session from.
/// </summary>
/// <remarks>
/// The WPF view model reads the recent files from the editor view model and closes its dialog itself; this one gets the recent
/// files from its host and raises <see cref="OpenRequested"/> when a project is to be opened at once (a recent project, or one
/// picked with <see cref="BrowseForExistingProjectCommand"/>).
/// </remarks>
public sealed class NewOrOpenSessionTemplateCollectionViewModel : ProjectTemplateCollectionViewModel
{
    /// <summary>
    /// The default location of the new projects, when the settings have none: "Stride Projects" in the user's documents.
    /// </summary>
    public static UDirectory DefaultLocation => UPath.Combine<UDirectory>(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Stride Projects");

    // Library template Id from samples/Library/Library/Library.sdtpl. Code Library adds a
    // project to an existing session: it can't seed a new solution on its own (no entry point
    // to run), so we hide it from the startup dialog. The Plugin template stays: a plugin can be
    // a solution of its own (asset editing, no game). Hardcoded until a TemplateDescription-
    // level "adds a project to existing session" marker lets both this filter and the inverse
    // one in NewProjectTemplateCollectionViewModel collapse to a single check.
    private static readonly Guid StrideLibraryTemplateId = new("7B79F1B7-3A55-4C84-AED9-3F4F3EE4B6E5");

    private readonly Action<UFile> removeRecentFile;
    private readonly TemplateDescriptionGroupViewModel recentGroup;
    private readonly TemplateDescriptionGroupViewModel rootGroup;
    private readonly TemplateDescriptionGroupViewModel defaultGroup;
    private readonly HashSet<TemplateDescription> listedTemplates = new(ReferenceEqualityComparer.Instance);
    private string solutionName = string.Empty;
    private UDirectory? solutionLocation;
    private bool arePropertiesValid;
    private string? downloadStatus;
    private bool isDownloading;
    private double downloadProgress;
    private bool isDownloadSizeUnknown = true;

    /// <param name="serviceProvider">The services; <see cref="IDialogService"/> is needed to browse for a project or a folder.</param>
    /// <param name="recentFiles">The recent sessions, most recent first.</param>
    /// <param name="removeRecentFile">Forgets a recent session (the "Remove from list" command).</param>
    public NewOrOpenSessionTemplateCollectionViewModel(IViewModelServiceProvider serviceProvider, IEnumerable<UFile> recentFiles, Action<UFile> removeRecentFile)
        : base(serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(recentFiles);
        ArgumentNullException.ThrowIfNull(removeRecentFile);
        this.removeRecentFile = removeRecentFile;
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
        foreach (var file in recentFiles)
        {
            recentGroup.Templates.Add(new ExistingProjectViewModel(ServiceProvider, file, OpenExistingProject, RemoveExistingProject));
        }

        Location = InternalSettings.TemplatesWindowDialogLastNewSessionTemplateDirectory.GetValue();
        if (string.IsNullOrWhiteSpace(Location))
            Location = DefaultLocation;

        BrowseForExistingProjectCommand = new AnonymousTaskCommand(serviceProvider, BrowseForExistingProjectAsync);
        SelectedGroup = recentGroup.Templates.Count == 0 ? rootGroup : recentGroup;
    }

    /// <summary>
    /// Raised when the user chose a project to open: a recent project, or a file picked with <see cref="BrowseForExistingProjectCommand"/>.
    /// <see cref="SelectedTemplate"/> is then the <see cref="ExistingProjectViewModel"/> to open.
    /// </summary>
    public event EventHandler? OpenRequested;

    /// <inheritdoc/>
    public override IEnumerable<TemplateDescriptionGroupViewModel> RootGroups
    {
        get
        {
            yield return recentGroup;
            yield return rootGroup;
        }
    }

    /// <summary>
    /// The group of the recent projects.
    /// </summary>
    public TemplateDescriptionGroupViewModel RecentGroup => recentGroup;

    /// <summary>
    /// The group of the new project templates, with the "General" group and the groups of the templates under it.
    /// </summary>
    public TemplateDescriptionGroupViewModel NewProjectGroup => rootGroup;

    /// <summary>
    /// The name of the solution; empty to name it after the project.
    /// </summary>
    public string SolutionName
    {
        get => solutionName;
        set => SetValue(ref solutionName, value ?? string.Empty);
    }

    /// <summary>
    /// The folder of the solution; <c>null</c> or empty to put it in the project's folder.
    /// </summary>
    public UDirectory? SolutionLocation
    {
        get => solutionLocation;
        set => SetValue(ref solutionLocation, value);
    }

    /// <summary>
    /// The "Reload last session automatically at startup" setting.
    /// </summary>
    public bool AutoReloadSession
    {
        get => EditorSettings.ReloadLastSession.GetValue();
        set => SetValue(value != AutoReloadSession, () => EditorSettings.ReloadLastSession.SetValue(value));
    }

    /// <summary>
    /// The result of the last <see cref="ValidateProperties"/>.
    /// </summary>
    public bool ArePropertiesValid
    {
        get => arePropertiesValid;
        private set => SetValue(ref arePropertiesValid, value);
    }

    /// <summary>
    /// Picks a solution or package file and requests to open it.
    /// </summary>
    public ICommandBase BrowseForExistingProjectCommand { get; }

    /// <summary>
    /// The template download in progress (or failed), <c>null</c> when there is none.
    /// </summary>
    public string? DownloadStatus
    {
        get => downloadStatus;
        private set => SetValue(ref downloadStatus, value);
    }

    /// <summary>
    /// Whether <see cref="DownloadStatus"/> is shown: only while new project templates are listed, where the downloads add templates.
    /// </summary>
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

    /// <summary>
    /// Whether a template download is in progress (<see cref="DownloadStatus"/> may also report a failure).
    /// </summary>
    public bool IsDownloading
    {
        get => isDownloading;
        private set => SetValue(ref isDownloading, value);
    }

    /// <summary>
    /// Progress of the download from 0 to 1 (0 while its size is not known).
    /// </summary>
    public double DownloadProgress
    {
        get => downloadProgress;
        private set => SetValue(ref downloadProgress, value);
    }

    /// <summary>
    /// Whether the size of the download is not known yet (no progress to show).
    /// </summary>
    public bool IsDownloadSizeUnknown
    {
        get => isDownloadSizeUnknown;
        private set => SetValue(ref isDownloadSizeUnknown, value);
    }

    /// <summary>
    /// The parameters of the new session to create from the selected template, or <c>null</c> when a project is selected instead.
    /// </summary>
    public NewSessionParameters? GetNewSessionParameters()
    {
        if (SelectedTemplate is not TemplateDescriptionViewModel template)
            return null;
        return new NewSessionParameters
        {
            TemplateDescription = template.GetTemplate(),
            OutputName = Name,
            OutputDirectory = Location,
            SolutionName = SolutionName,
            SolutionLocation = SolutionLocation!,
        };
    }

    /// <summary>
    /// The project to open, or <c>null</c> when a template is selected instead.
    /// </summary>
    public UFile? GetExistingSessionPath()
    {
        return (SelectedTemplate as ExistingProjectViewModel)?.Path;
    }

    /// <summary>
    /// Remembers the location of the new projects, as the WPF window does when it closes with a choice.
    /// </summary>
    public void SaveSettings()
    {
        EditorSettings.Save();
        InternalSettings.TemplatesWindowDialogLastNewSessionTemplateDirectory.SetValue(Location.FullPath);
        InternalSettings.SaveProfile();
    }

    /// <inheritdoc />
    public override void Destroy()
    {
        TemplateManager.PackagesChanged -= OnTemplatePackagesChanged;
        TemplateDownloads.Changed -= OnTemplateDownloadsChanged;
        base.Destroy();
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    protected override string? UpdateNameFromSelectedTemplate()
    {
        return GenerateUniqueNameAtLocation();
    }

    private void AddNewTemplates()
    {
        if (IsDestroyed)
            return;
        var added = false;
        foreach (var template in TemplateManager.FindTemplates(TemplateScope.Session))
        {
            if (template.Id == StrideLibraryTemplateId || !listedTemplates.Add(template))
                continue;
            var viewModel = new TemplateDescriptionViewModel(ServiceProvider, template);
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

    private void OpenExistingProject(ExistingProjectViewModel item)
    {
        SelectedTemplate = item;
        OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RemoveExistingProject(ExistingProjectViewModel item)
    {
        removeRecentFile(item.Path);
        recentGroup.Templates.Remove(item);
        UpdateTemplateList();
    }

    private async Task BrowseForExistingProjectAsync()
    {
        var filePath = await BrowseForExistingProjectAsync(ServiceProvider);
        if (filePath is not null)
        {
            OpenExistingProject(new ExistingProjectViewModel(ServiceProvider, filePath, OpenExistingProject, RemoveExistingProject));
        }
    }

    /// <summary>
    /// Asks the user for a solution or package file, starting in the folder of the last opened session.
    /// </summary>
    /// <returns>The file, or <c>null</c> when the user cancelled.</returns>
    public static async Task<UFile?> BrowseForExistingProjectAsync(IViewModelServiceProvider serviceProvider)
    {
        var initialDirectory = InternalSettings.FileDialogLastOpenSessionDirectory.GetValue();
        var solutionPatterns = Core.Solutions.Solution.SolutionExtensions.Select(x => "*" + x).ToList();
        var filters = new List<FilePickerFilter>
        {
            new("Solution or package files") { Patterns = [.. solutionPatterns, "*" + Package.PackageFileExtension] },
            new("Solution file") { Patterns = solutionPatterns },
            new("Package file") { Patterns = ["*" + Package.PackageFileExtension] },
        };
        return await serviceProvider.Get<IDialogService>().OpenFilePickerAsync(string.IsNullOrEmpty(initialDirectory) ? null : new UDirectory(initialDirectory), filters);
    }
}
