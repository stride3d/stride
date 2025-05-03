using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia;

public class NewOrOpenSessionTemplateCollectionViewModel
{
    private readonly IModalDialog dialog;
    private readonly IViewModelServiceProvider serviceProvider;
    public NewOrOpenSessionTemplateCollectionViewModel(IViewModelServiceProvider serviceProvider)
    {   
        this.serviceProvider = serviceProvider;
        BrowseForExistingProjectCommand = new AnonymousTaskCommand(serviceProvider, BrowseForExistingProject);
    }
    public ICommandBase BrowseForExistingProjectCommand { get; }
    public bool AutoReloadSession { get; }

    private async Task BrowseForExistingProject()
    {
        var filePath = await EditorDialogHelper.BrowseForExistingProject(serviceProvider);
        if (filePath != null)
        {
            SelectedTemplate = new ExistingProjectViewModel(ServiceProvider, filePath, RemoveExistingProjects);
            dialog?.RequestClose(DialogResult.Ok);
        }
    }

}
