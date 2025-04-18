using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia;

public class NewOrOpenSessionTemplateCollectionViewModel
{
    public NewOrOpenSessionTemplateCollectionViewModel(IViewModelServiceProvider serviceProvider)
    {
        
        BrowseForExistingProjectCommand = new AnonymousTaskCommand(serviceProvider, BrowseForExistingProject);
    }
    public ICommandBase BrowseForExistingProjectCommand { get; }
    public bool AutoReloadSession { get; }

    private Task BrowseForExistingProject()
    {
        return Task.CompletedTask;
    }

}
